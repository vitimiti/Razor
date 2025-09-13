// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Razor.Compression.BinaryTree.Internals;
using Razor.Extensions;

namespace Razor.Compression.BinaryTree;

/// <summary>Represents a stream implementation that utilizes a binary tree structure for compression or decompression of data.</summary>
public sealed class BinaryTreeStream : Stream
{
    private readonly Stream _stream;
    private readonly CompressionMode _mode;
    private readonly bool _leaveOpen;

    private bool _disposed;
    private byte[]? _decodedBuffer;
    private int _decodedLength;
    private int _readPosition;

    /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
    /// <value><c>true</c> if the stream supports reading; otherwise, <c>false</c>.</value>
    /// <remarks>This property returns <c>false</c> if the stream is disposed, or if the <see cref="CompressionMode"/> is set to <c>CompressionMode.Compress</c>, or if the underlying stream does not support reading.</remarks>
    public override bool CanRead => !_disposed && _mode is CompressionMode.Decompress && _stream.CanRead;

    /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
    /// <value><c>false</c>, as this stream does not support seeking.</value>
    /// <remarks>This property always returns <c>false</c> because the <see cref="BinaryTreeStream"/> implementation does not support seeking operations, regardless of the underlying stream's capabilities.</remarks>
    public override bool CanSeek => false;

    /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
    /// <value><c>true</c> if the stream supports writing; otherwise, <c>false</c>.</value>
    /// <remarks>This property returns <c>false</c> if the stream is disposed, or if the <see cref="CompressionMode"/> is set to <c>CompressionMode.Decompress</c>, or if the underlying stream does not support writing.</remarks>
    public override bool CanWrite => !_disposed && _mode is CompressionMode.Compress && _stream.CanWrite;

    /// <summary>Gets the length of the stream in bytes.</summary>
    /// <value>The total length of the stream in bytes. For compression mode, it represents the length of the underlying stream. For decompression mode, it represents the uncompressed size of the data.</value>
    /// <remarks>This property throws <see cref="ObjectDisposedException"/> if the stream is disposed. Accessing the length in decompression mode relies on the implementation of <see cref="BinaryTreeDecoderUtilities.GetUncompressedSize(Stream)"/>.</remarks>
    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _mode is CompressionMode.Compress
                ? _stream.Length
                : BinaryTreeDecoderUtilities.GetUncompressedSize(_stream);
        }
    }

    /// <summary>Gets or sets the position within the stream.</summary>
    /// <value>Getting or setting this property is not supported and always throws a <see cref="NotSupportedException"/>.</value>
    /// <remarks>BinaryTreeStream does not support seeking or maintaining a position within the stream. Attempts to get or set the position will result in a <see cref="NotSupportedException"/> being thrown.</remarks>
    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    /// <summary>Initializes a new instance of the <see cref="BinaryTreeStream"/> class.</summary>
    /// <param name="stream">The stream to wrap.</param>
    /// <param name="mode">The compression mode to use.</param>
    /// <param name="leaveOpen">Indicates whether to leave the stream open after disposing.</param>
    /// <exception cref="ArgumentException"><paramref name="stream"/> does not support seeking.</exception>
    /// <remarks>The <paramref name="stream"/> must support seeking for the <see cref="BinaryTreeStream"/> to be able to read or write data.</remarks>
    public BinaryTreeStream([NotNull] Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking.", nameof(stream));
        }

        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
    }

    /// <summary>Releases the resources used by the <see cref="BinaryTreeStream"/>.</summary>
    /// <param name="disposing">Indicates whether to release both managed and unmanaged resources (true) or only unmanaged resources (false).</param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && !_leaveOpen)
        {
            _stream.Dispose();
        }

        if (_decodedBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(_decodedBuffer);
            _decodedBuffer = null;
            _decodedLength = 0;
            _readPosition = 0;
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>Flushes the underlying stream, ensuring that all buffered data is written to the target stream, if applicable.</summary>
    /// <remarks>If the stream is in decompression mode, this method performs no operation. For compression mode, all buffered data is written out to the underlying stream.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the <see cref="BinaryTreeStream"/> has been disposed.</exception>
    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // For read mode, Flush should be a no-op.
        if (CanWrite)
        {
            _stream.Flush();
        }
    }

    /// <summary>Asynchronously clears all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
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

    /// <summary>Reads a sequence of bytes from the stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to store the read data.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data.</param>
    /// <param name="count">The maximum number of bytes to read from the current stream.</param>
    /// <returns>The total number of bytes read into the <paramref name="buffer"/>. This might be less than the number of bytes requested if the data is not currently available, or zero if the end of the stream is reached.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is less than zero, or their sum exceeds the length of <paramref name="buffer"/>.</exception>
    /// <exception cref="InvalidOperationException">The stream is not readable.</exception>
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

        EnsureDecoded();

        if (_readPosition >= _decodedLength)
        {
            return 0; // EOF
        }

        var toCopy = Math.Min(count, _decodedLength - _readPosition);
        Buffer.BlockCopy(_decodedBuffer!, _readPosition, buffer, offset, toCopy);
        _readPosition += toCopy;
        return toCopy;
    }

    /// <summary>Reads a sequence of bytes from the stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or 0 if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not readable.</exception>
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!CanRead)
        {
            throw new InvalidOperationException("The stream is not readable.");
        }

        EnsureDecoded();

        if (_readPosition >= _decodedLength)
        {
            return 0; // EOF
        }

        var toCopy = Math.Min(buffer.Length, _decodedLength - _readPosition);
        new ReadOnlySpan<byte>(_decodedBuffer, _readPosition, toCopy).CopyTo(buffer);
        _readPosition += toCopy;
        return toCopy;
    }

    /// <summary>Reads a sequence of bytes from the stream asynchronously and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to write the data to.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data read from the stream.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the <see cref="Task{TResult}.Result"/> parameter contains the total number of bytes read into the buffer.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or their sum exceeds the buffer length.</exception>
    /// <remarks>If no data is available, the returned task will complete with a result of 0.</remarks>
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

    /// <summary>Asynchronously reads a sequence of bytes from the current stream.</summary>
    /// <param name="buffer">The region of memory to write the data read from the stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A value task that represents the asynchronous read operation. The result contains the total number of bytes read into the buffer.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = new CancellationToken()
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }

        // Lightweight async wrapper since decoding is done on first use and cached.
        return new ValueTask<int>(
            Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Read(buffer.Span);
                },
                cancellationToken
            )
        );
    }

    /// <summary>Throws a <see cref="NotSupportedException"/> indicating that reading a single byte is not supported.</summary>
    /// <returns>This method does not return a value as it always throws an exception.</returns>
    /// <exception cref="NotSupportedException">Always thrown to indicate that reading a single byte is not supported.</exception>
    public override int ReadByte() => throw new NotSupportedException("Reading a single byte is not supported.");

    /// <summary>Overrides the seeking functionality of the stream, which is not supported by <see cref="BinaryTreeStream"/>.</summary>
    /// <param name="offset">The point relative to <paramref name="origin"/> from which to begin seeking.</param>
    /// <param name="origin">Specifies the reference point used to obtain the new position.</param>
    /// <returns>This method does not return a value as it always throws an exception.</returns>
    /// <exception cref="NotSupportedException">Thrown because seeking is not supported in <see cref="BinaryTreeStream"/>.</exception>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("Seeking is not supported.");

    /// <summary>Sets the length of the stream. Not supported in <see cref="BinaryTreeStream"/>.</summary>
    /// <param name="value">The desired length of the stream.</param>
    /// <exception cref="NotSupportedException">Always thrown as this operation is not supported.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException("Setting the length is not supported.");

    /// <summary>Writes a sequence of bytes to the BinaryTreeStream.</summary>
    /// <param name="buffer">The byte array that contains the data to write.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to write to the stream.</param>
    /// <exception cref="ObjectDisposedException">The stream is already disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or their sum is greater than the <paramref name="buffer"/> length.</exception>
    /// <exception cref="InvalidOperationException">The stream is not writable.</exception>
    /// <remarks>If the <paramref name="count"/> is zero, the stream will be truncated to a zero-length compressed content.</remarks>
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
            // Truncate to zero-length compressed content
            _stream.SetLength(0);
            return;
        }

        // Overwrite from start and truncate to the compressed output length.
        _stream.Position = 0;
        using BinaryWriter writer = new(_stream, EncodingExtensions.Ansi, leaveOpen: true);

        // Encode only the requested slice.
        BinaryTreeEncoder.Encode(writer, buffer, offset, count);

        // Ensure underlying stream length matches the actual compressed data produced.
        _stream.Flush();
        _stream.SetLength(_stream.Position);
        _stream.Position = 0;

        if (_decodedBuffer is null)
        {
            return;
        }

        // Invalidate any previous decoded cache as data changed.
        ArrayPool<byte>.Shared.Return(_decodedBuffer);
        _decodedBuffer = null;
        _decodedLength = 0;
        _readPosition = 0;
    }

    /// <summary>Writes the given data from the specified read-only span to the stream.</summary>
    /// <param name="buffer">The read-only span containing the data to be written to the stream.</param>
    /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support writing.</exception>
    /// <remarks>Uses a rented buffer to avoid unnecessary allocations and delegates writing to an overloaded method.</remarks>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        // Delegate to byte[] overload without extra allocation by renting.
        var rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.CopyTo(rented);
            Write(rented, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>Asynchronously writes a sequence of bytes to the current stream and advances the current position within the stream.</summary>
    /// <param name="buffer">The buffer containing data to write to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the buffer from which to begin copying bytes.</param>
    /// <param name="count">The number of bytes to write to the stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">The current stream instance has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or <paramref name="offset"/> + <paramref name="count"/> exceeds the buffer length.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled by the <paramref name="cancellationToken"/>.</exception>
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

    /// <summary>Writes a sequence of bytes to the current stream asynchronously.</summary>
    /// <param name="buffer">The region of memory to write data from.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The operation is canceled.</exception>
    /// <remarks>This method writes data from the provided buffer into the stream. Cancellation tokens are supported to cancel the operation if required.</remarks>
    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = new CancellationToken()
    )
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

    /// <summary>Writes a single byte to the current stream.</summary>
    /// <param name="value">The byte to write to the stream.</param>
    /// <exception cref="NotSupportedException">Writing a single byte is not supported.</exception>
    public override void WriteByte(byte value) =>
        throw new NotSupportedException("Writing a single byte is not supported.");

    /// <summary>Closes the current stream and releases any resources associated with it.</summary>
    /// <remarks>This method calls <see cref="Dispose(bool)"/> to release managed and unmanaged resources and suppresses the finalization of the object.</remarks>
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

    private void EnsureDecoded()
    {
        if (_decodedBuffer is not null)
        {
            return;
        }

        // Validate header and decode entire payload once.
        if (!BinaryTreeDecoderUtilities.IsBinaryTreeCompressed(_stream))
        {
            throw new InvalidOperationException("The stream is not a BinaryTree stream.");
        }

        // Determine uncompressed size and allocate buffer.
        var uncompressedSize = BinaryTreeDecoderUtilities.GetUncompressedSize(_stream);

        var buffer = ArrayPool<byte>.Shared.Rent(checked((int)uncompressedSize));

        // Reset and decode fully into buffer.
        _stream.Position = 0;
        using BinaryReader reader = new(_stream, EncodingExtensions.Ansi, leaveOpen: true);

        var decoded = (int)BinaryTreeDecoder.Decode(reader, buffer, 0, (int)uncompressedSize);

        _decodedBuffer = buffer;
        _decodedLength = decoded;
        _readPosition = 0;

        // After decoding, keep underlying position at 0 for consistency.
        _stream.Position = 0;
    }
}
