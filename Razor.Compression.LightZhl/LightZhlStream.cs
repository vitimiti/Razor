// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Razor.Compression.LightZhl.Internals;

namespace Razor.Compression.LightZhl;

/// <summary>Represents a lightweight stream for handling compression and decompression using the ZHL algorithm.</summary>
/// <remarks>The LightZhlStream is designed to wrap an underlying stream and provide compression or decompression functionality based on the specified mode. It overrides several members of the <see cref="Stream"/> class, tailored for its specific operational use case. Streaming functionality will vary between read and write modes depending on the <see cref="CompressionMode"/>.</remarks>
/// <exception cref="NotSupportedException">Thrown when unsupported operations such as seeking or setting stream length are attempted.</exception>
/// <threadsafety>Instance members of this class are not guaranteed to be thread-safe. Synchronization mechanisms must be implemented externally if used concurrently.</threadsafety>
/// <inheritdoc />
public sealed class LightZhlStream : Stream
{
    private readonly Stream _stream;
    private readonly CompressionMode _mode;
    private readonly bool _leaveOpen;

    private bool _disposed;
    private byte[]? _decompressedBuffer;
    private int _decompressedPosition;
    private MemoryStream? _uncompressedBuffer;
    private bool _compressDirty;

    /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
    /// <value>true if the stream supports reading and is in decompression mode, and the underlying stream is readable; otherwise, false.</value>
    /// <remarks>The property returns false if the stream has been disposed or is not in decompression mode, or the underlying stream is not readable.</remarks>
    public override bool CanRead => !_disposed && _mode is CompressionMode.Decompress && _stream.CanRead;

    /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
    /// <value>Always false, as the stream does not support seeking.</value>
    /// <remarks>The property always returns false, as seeking is not supported by the LightZhlStream.</remarks>
    public override bool CanSeek => false;

    /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
    /// <value>true if the stream supports writing and is in compression mode, and the underlying stream is writable; otherwise, false.</value>
    /// <remarks>The property returns false if the stream has been disposed or is not in compression mode, or the underlying stream is not writable.</remarks>
    public override bool CanWrite => !_disposed && _mode is CompressionMode.Compress && _stream.CanWrite;

    /// <summary>Gets the length of the stream.</summary>
    /// <exception cref="NotSupportedException">Always throws because this stream does not support retrieving the length.</exception>
    /// <remarks>The <see cref="Length"/> property is not supported for <see cref="LightZhlStream"/> and will always throw a <see cref="NotSupportedException"/>.</remarks>
    public override long Length =>
        throw new NotSupportedException("Getting length is not supported for LightZhlStream.");

    /// <summary>Gets or sets the position within the current stream.</summary>
    /// <value>The position within the stream.</value>
    /// <remarks>The property is not supported by LightZhlStream and will always throw a NotSupportedException when attempting to get or set its value.</remarks>
    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    /// <summary>A sealed class that represents a stream for compression and decompression using the LightZhl compression mechanism.</summary>
    /// <param name="stream">The stream to wrap.</param>
    /// <param name="mode">The compression mode to use.</param>
    /// <param name="leaveOpen">Indicates whether the underlying stream should be left open after the stream is disposed.</param>
    /// <exception cref="ArgumentException"><paramref name="stream"/> does not support seeking.</exception>
    /// <remarks>The <paramref name="stream"/> must support seeking for the <see cref="LightZhlStream"/> to be able to read or write data.</remarks>
    public LightZhlStream([NotNull] Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        if (mode is CompressionMode.Compress && !stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking when used for compression.", nameof(stream));
        }

        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;

        if (_mode != CompressionMode.Compress)
        {
            return;
        }

        _uncompressedBuffer = new MemoryStream();
        _compressDirty = false;
    }

    /// <summary>Releases the resources used by the <see cref="LightZhlStream"/>.</summary>
    /// <param name="disposing">Indicates whether the method is being called from a direct call to <see cref="Dispose"/> (true) or from a finalizer (false).</param>
    /// <remarks>If <paramref name="disposing"/> is true, this method releases all managed and unmanaged resources. If false, only unmanaged resources are released.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // If we are in compression mode, finalize the compressed payload on dispose.
            if (_mode == CompressionMode.Compress)
            {
                FlushCompressedPayload();
                _uncompressedBuffer?.Dispose();
                _uncompressedBuffer = null;
            }

            if (!_leaveOpen)
            {
                _stream.Dispose();
            }
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>Flushes the underlying stream and writes any buffered compressed data to the wrapped stream.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not writable.</exception>
    /// <remarks>Invoking this method ensures all buffered data is processed and written to the underlying stream. The underlying stream's flush method is also called.</remarks>
    public override void Flush()
    {
        if (!CanWrite)
        {
            throw new InvalidOperationException("The stream is not writable.");
        }

        FlushCompressedPayload();
        _stream.Flush();
    }

    /// <summary>Asynchronously flushes the stream and clears all buffers.</summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <remarks>This method ensures that any buffered data is written to the underlying storage while respecting the cancellation token.</remarks>
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Flush();
                },
                cancellationToken
            );

    /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer into which the data is read.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data read from the stream.</param>
    /// <param name="count">The maximum number of bytes to read from the stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Either <paramref name="offset"/> or <paramref name="count"/> is negative, or the sum of <paramref name="offset"/> and <paramref name="count"/> is greater than the <paramref name="buffer.Length"/>.</exception>
    /// <exception cref="InvalidOperationException">The stream is not in a readable state.</exception>
    public override int Read([NotNull] byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        if (!CanRead)
        {
            throw new InvalidOperationException("The stream is not readable.");
        }

        EnsureDecompressedLoaded();

        if (_decompressedBuffer is null)
        {
            // No data to read (empty compressed payload).
            return 0;
        }

        var remaining = _decompressedBuffer.Length - _decompressedPosition;
        if (remaining <= 0)
        {
            return 0;
        }

        var toCopy = Math.Min(count, remaining);
        Array.Copy(_decompressedBuffer, _decompressedPosition, buffer, offset, toCopy);
        _decompressedPosition += toCopy;
        return toCopy;
    }

    /// <summary>Reads data from the stream into the provided span buffer.</summary>
    /// <param name="buffer">The span buffer to fill with data read from the stream.</param>
    /// <returns>The total number of bytes read into the buffer. This might be less than the requested number of bytes if not enough data is available.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not open for reading or the compression mode does not support reading.</exception>
    /// <exception cref="IOException">An I/O error occurred, such as the stream being closed or unavailable.</exception>
    public override int Read(Span<byte> buffer)
    {
        var arr = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            var read = Read(arr, 0, buffer.Length);
            new ReadOnlySpan<byte>(arr, 0, read).CopyTo(buffer);
            return read;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(arr);
        }
    }

    /// <summary>Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The byte offset in the buffer at which to begin writing data.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the task result contains the total number of bytes read into the buffer. The result can be less than the number of bytes requested if the data is not currently available, or zero if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or the sum of <paramref name="offset"/> and <paramref name="count"/> is greater than the buffer's length.</exception>
    /// <exception cref="InvalidOperationException">The stream is not readable.</exception>
    public override Task<int> ReadAsync(
        [NotNull] byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<int>(cancellationToken)
            : Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Read(buffer, offset, count);
                },
                cancellationToken
            );
    }

    /// <summary>Reads a sequence of bytes from the current stream asynchronously and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The region of memory to write the data read from the stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous read operation. The value of the task contains the total number of bytes read into the buffer.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }

        var task = Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Read(buffer.Span);
            },
            cancellationToken
        );

        return new ValueTask<int>(task);
    }

    /// <summary>Writes a sequence of bytes to the current stream and advances the stream position.</summary>
    /// <param name="buffer">The buffer holding the data to be written.</param>
    /// <param name="offset">The zero-based byte offset in the buffer from which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to write from the buffer to the stream.</param>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is less than zero, or their sum is greater than the length of <paramref name="buffer"/>.</exception>
    /// <exception cref="InvalidOperationException">The stream does not support writing.</exception>
    public override void Write([NotNull] byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        if (!CanWrite)
        {
            throw new InvalidOperationException("The stream is not writable.");
        }

        if (count == 0)
        {
            return;
        }

        _uncompressedBuffer ??= new MemoryStream();
        _uncompressedBuffer.Write(buffer, offset, count);
        _compressDirty = true;
    }

    /// <summary>Writes a span of bytes to the uncompressed buffer of the <see cref="LightZhlStream"/>.</summary>
    /// <param name="buffer">A read-only span of bytes to write into the stream.</param>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not writable.</exception>
    /// <remarks>If the <paramref name="buffer"/> length is zero, no action is performed.</remarks>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!CanWrite)
        {
            throw new InvalidOperationException("The stream is not writable.");
        }

        if (buffer.Length == 0)
        {
            return;
        }

        _uncompressedBuffer ??= new MemoryStream();
        _uncompressedBuffer.Write(buffer);
        _compressDirty = true;
    }

    /// <summary>Writes a sequence of bytes to the LightZhlStream asynchronously using the specified buffer, offset, and count.</summary>
    /// <param name="buffer">The buffer containing data to be written to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to write from the buffer to the stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="offset"/> or <paramref name="count"/> is negative, or their sum exceeds the length of the buffer.</exception>
    /// <exception cref="OperationCanceledException">The operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    public override Task WriteAsync([NotNull] byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Write(buffer, offset, count);
                },
                cancellationToken
            );
    }

    /// <summary>Asynchronously writes a sequence of bytes to the current stream and advances the position within the stream by the number of bytes written.</summary>
    /// <param name="buffer">The region of memory to write data from.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A value task that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    /// <remarks>The method writes the contents of the provided memory buffer to the stream. If the stream has been disposed or the operation is canceled, exceptions are thrown.</remarks>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        var task = Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Write(buffer.Span);
            },
            cancellationToken
        );

        return new ValueTask(task);
    }

    /// <summary>Reads and returns the next byte from the stream, or -1 if no more bytes are available.</summary>
    /// <returns>The next byte cast to an <see cref="int"/>, or -1 if the end of the stream is reached.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
    public override int ReadByte()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!CanRead)
        {
            throw new NotSupportedException("Reading a single byte is not supported.");
        }

        EnsureDecompressedLoaded();
        return _decompressedBuffer is null || _decompressedPosition >= _decompressedBuffer.Length
            ? -1
            : _decompressedBuffer[_decompressedPosition++];
    }

    /// <summary>Writes a single byte to the current stream at the current position.</summary>
    /// <param name="value">The byte value to write to the stream.</param>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support writing.</exception>
    /// <remarks>This method writes the byte into an internal buffer, marking it as dirty for compression.</remarks>
    public override void WriteByte(byte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!CanWrite)
        {
            throw new NotSupportedException("Writing a single byte is not supported.");
        }

        _uncompressedBuffer ??= new MemoryStream();
        _uncompressedBuffer.WriteByte(value);
        _compressDirty = true;
    }

    /// <summary>Attempts to set the position within the stream, but seeking is not supported by LightZhlStream.</summary>
    /// <param name="offset">The point relative to <paramref name="origin"/> to which to seek.</param>
    /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point for the offset.</param>
    /// <returns>This method does not return a value as it always throws an exception.</returns>
    /// <exception cref="NotSupportedException">Seeking is not supported for LightZhlStream.</exception>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("Seeking is not supported.");

    /// <summary>Overrides the method to throw a <see cref="NotSupportedException"/>, as setting the length is not supported for <see cref="LightZhlStream"/>.</summary>
    /// <param name="value">The length to set for the stream.</param>
    /// <exception cref="NotSupportedException">Always thrown to indicate that setting the length is not supported.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException("Setting the length is not supported.");

    /// <summary>Closes the current LightZhlStream and releases the resources associated with it.</summary>
    /// <remarks>This method disposes of the stream and suppresses finalization to optimize resource cleanup. It overrides <see cref="Stream.Close"/>.</remarks>
    [SuppressMessage(
        "csharpsquid",
        "S3971:\"GC.SuppressFinalize\" should not be called",
        Justification = "Follow Stream standards."
    )]
    [SuppressMessage(
        "Usage",
        "CA1816:Dispose methods should call SuppressFinalize",
        Justification = "Follow Stream standards."
    )]
    [SuppressMessage(
        "ReSharper",
        "GCSuppressFinalizeForTypeWithoutDestructor",
        Justification = "Follow Stream standards."
    )]
    public override void Close()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void EnsureDecompressedLoaded()
    {
        if (_decompressedBuffer != null)
        {
            return;
        }

        // Read all compressed data from current position to end
        using var ms = new MemoryStream();
        // If stream can seek, we read from current position to end
        // without resetting to 0, to respect caller's positioning.
        // For non-seekable, CopyTo will read to EOF.
        _stream.CopyTo(ms);
        var compressed = ms.ToArray();

        // Decode into a dynamically sized buffer.
        if (compressed.Length == 0)
        {
            _decompressedBuffer = [];
            _decompressedPosition = 0;
            return;
        }

        var decoder = new LightZhlDecoder();

        // Start with a heuristic capacity and grow until decode succeeds.
        // We can't know the decompressed size up-front. Double on failure.
        var capacity = Math.Max(compressed.Length * 4, 1024);
        while (true)
        {
            var dst = new byte[capacity];
            var ok = decoder.Decode(compressed, dst, out _, out var written);

            if (ok)
            {
                // Trim to actual size.
                if (written != dst.Length)
                {
                    Array.Resize(ref dst, written);
                }

                _decompressedBuffer = dst;
                _decompressedPosition = 0;
                return;
            }
            // Grow and retry
            // Avoid unbounded runaway: cap capacity growth to something reasonable.
            // If it still fails at huge sizes, we surface an error.
            checked
            {
                var next = capacity <= 16 * 1024 * 1024 ? capacity * 2 : capacity + (capacity / 2);
                if (next <= capacity || next > 256 * 1024 * 1024)
                {
                    throw new InvalidOperationException(
                        "Failed to decode LightZhl stream: output size appears to be unbounded or data is invalid."
                    );
                }

                capacity = next;
            }
        }
    }

    private void FlushCompressedPayload()
    {
        if (_uncompressedBuffer is null || !_compressDirty)
        {
            return;
        }

        var raw = _uncompressedBuffer.GetBuffer();
        var rawLen = (int)_uncompressedBuffer.Length;

        var maxOut = LightZhlEncoder.CalculateMaxCompressedSize(rawLen);
        var outBuf = ArrayPool<byte>.Shared.Rent(maxOut);
        try
        {
            if (
                !LightZhlEncoder.Encode(
                    new ReadOnlySpan<byte>(raw, 0, rawLen),
                    outBuf.AsSpan(0, maxOut),
                    out var produced
                )
            )
            {
                throw new InvalidOperationException("Failed to encode LightZhl stream.");
            }

            // Replace underlying content with compressed payload
            _stream.Position = 0;
            _stream.Write(outBuf, 0, produced);
            _stream.SetLength(produced);

            _compressDirty = false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(outBuf);
        }
    }
}
