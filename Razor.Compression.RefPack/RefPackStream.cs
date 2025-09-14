// -----------------------------------------------------------------------
// <copyright file="RefPackStream.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Razor.Compression.RefPack.Internals;
using Razor.Extensions;

namespace Razor.Compression.RefPack;

/// <summary>Provides a stream implementation for handling compressed and decompressed data using the RefPack compression algorithm. This class supports both read and write operations depending on the specified compression mode.</summary>
public sealed class RefPackStream : Stream
{
    private readonly Stream _stream;
    private readonly CompressionMode _mode;
    private readonly bool _leaveOpen;
    private readonly MemoryStream? _writeBuffer;

    private bool _disposed;
    private byte[]? _decodedBuffer;
    private int _decodedLength;
    private int _decodedReadPosition;
    private bool _decodedInitialized;
    private bool _encodedWritten;

    /// <summary>Initializes a new instance of the <see cref="RefPackStream"/> class.Represents a stream implementation for handling RefPack compression and decompression.</summary>
    /// <param name="stream">The stream to wrap.</param>
    /// <param name="mode">The compression mode to use.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposing; false to close the stream.</param>
    /// <remarks>This class is designed to work with RefPack compression algorithms, supporting both compression and decompression modes. It requires a seekable stream for operation and optionally allows keeping the underlying stream open after the RefPackStream is disposed.</remarks>
    /// <exception cref="ArgumentException">Thrown when the provided stream does not support seeking.</exception>
    public RefPackStream([NotNull] Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking.", nameof(stream));
        }

        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
        if (_mode is CompressionMode.Compress)
        {
            _writeBuffer = new MemoryStream();
        }
    }

    /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
    /// <value><c>true</c> if the stream supports reading in decompress mode and has not been disposed; otherwise, <c>false</c>.</value>
    /// <remarks>This property returns <c>false</c> if the stream is in compress mode, has been disposed, or the underlying stream does not support reading.</remarks>
    public override bool CanRead => !_disposed && _mode is CompressionMode.Decompress && _stream.CanRead;

    /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
    /// <value><c>false</c> as seeking is not supported in <see cref="RefPackStream"/>.</value>
    /// <remarks>This property always returns <c>false</c> because the <see cref="RefPackStream"/> implementation does not allow seeking operations.</remarks>
    public override bool CanSeek => false;

    /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
    /// <value><c>true</c> if the stream supports writing in compress mode and has not been disposed; otherwise, <c>false</c>.</value>
    /// <remarks>This property returns <c>false</c> if the stream is in decompress mode, has been disposed, or the underlying stream does not support writing.</remarks>
    public override bool CanWrite => !_disposed && _mode is CompressionMode.Compress && _stream.CanWrite;

    /// <summary>Gets the length of the stream in bytes.</summary>
    /// <value>The length of the stream in bytes. For compress mode, this is the length of the underlying stream. For decompress mode, this is the uncompressed size of the data.</value>
    /// <remarks>This property throws <see cref="ObjectDisposedException"/> if the stream has been disposed. In decompress mode, the uncompressed size is calculated using <c>RefPackDecoderUtilities.GetUncompressedSize</c>.</remarks>
    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _mode is CompressionMode.Compress
                ? _stream.Length
                : RefPackDecoderUtilities.GetUncompressedSize(_stream);
        }
    }

    /// <summary>Gets or sets the current position of the stream. This property is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
    /// <value>Attempting to get or set this property will result in a <see cref="NotSupportedException"/> being thrown.</value>
    /// <remarks>This stream does not support seeking; therefore, the position cannot be retrieved or set.</remarks>
    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    /// <summary>Flushes the underlying stream and ensures all compressed data is written.</summary>
    /// <remarks>This method ensures that any buffered compressed data is written to the underlying stream. If the stream is not writable, an exception is thrown.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not in a writable state.</exception>
    public override void Flush()
    {
        if (!CanWrite)
        {
            throw new InvalidOperationException("The stream is not writable.");
        }

        EnsureCompressedWritten();
        _stream.Flush();
    }

    /// <summary>Asynchronously clears all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
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

    /// <summary>Reads a sequence of bytes from the current stream and advances the read position by the number of bytes read.</summary>
    /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset"/> or <paramref name="count"/> is negative, or when <paramref name="offset"/> + <paramref name="count"/> exceeds the buffer length.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the stream is not readable.</exception>
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

        var remaining = _decodedLength - _decodedReadPosition;
        if (remaining <= 0)
        {
            return 0; // EOF
        }

        var toCopy = Math.Min(count, remaining);
        Buffer.BlockCopy(_decodedBuffer!, _decodedReadPosition, buffer, offset, toCopy);
        _decodedReadPosition += toCopy;
        return toCopy;
    }

    /// <summary>Reads a sequence of bytes from the stream into the provided span and advances the position by the number of bytes read.</summary>
    /// <param name="buffer">The span to fill with the data read from the stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if fewer are currently available, or zero if the end of the stream has been reached.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the stream is not readable.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!CanRead)
        {
            throw new InvalidOperationException("The stream is not readable.");
        }

        EnsureDecoded();
        var remaining = _decodedLength - _decodedReadPosition;
        if (remaining <= 0)
        {
            return 0;
        }

        var toCopy = Math.Min(buffer.Length, remaining);
        new ReadOnlySpan<byte>(_decodedBuffer!, _decodedReadPosition, toCopy).CopyTo(buffer);
        _decodedReadPosition += toCopy;
        return toCopy;
    }

    /// <summary>Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <param name="cancellationToken">A cancellation token to observe while awaiting the task.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the task result is the total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when offset or count is negative or if their sum exceeds the buffer length.</exception>
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

    /// <summary>Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the task contains the total number of bytes read into the buffer, or 0 if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    /// <exception cref="NotSupportedException">Thrown when the stream does not support reading.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : new ValueTask<int>(
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

    /// <summary>Reads a single byte from the stream.</summary>
    /// <returns>The byte cast to an <see cref="int"/>, or -1 if the end of the stream has been reached.</returns>
    /// <exception cref="NotSupportedException">Thrown when the operation is not supported in the current context.</exception>
    public override int ReadByte() => throw new NotSupportedException("Reading a single byte is not supported.");

    /// <summary>Attempts to set the position within the stream. This operation is not supported.</summary>
    /// <param name="offset">The offset relative to the origin from which to begin seeking.</param>
    /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point for the offset.</param>
    /// <returns>Always throws a <see cref="NotSupportedException"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown whenever this method is called, as seeking is not supported.</exception>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("Seeking is not supported.");

    /// <summary>Throws a <see cref="NotSupportedException"/> as setting the length of the stream is not supported.</summary>
    /// <param name="value">The desired length of the stream, which is ignored.</param>
    /// <exception cref="NotSupportedException">Always thrown as this operation is not supported.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException("Setting the length is not supported.");

    /// <summary>Writes a sequence of bytes to the current stream and advances the position within the stream by the number of bytes written.</summary>
    /// <param name="buffer">The byte array that contains the data to be written to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to be written to the stream.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when offset or count is negative, or when offset + count is greater than the buffer length.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the stream is not writable.</exception>
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

        // Accumulate uncompressed bytes to encode later on Flush/Dispose.
        _writeBuffer!.Write(buffer, offset, count);
    }

    /// <summary>Writes a sequence of bytes to the current stream using a read-only span.</summary>
    /// <param name="buffer">The region of memory containing the data to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the stream is not writable.</exception>
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

        _writeBuffer!.Write(buffer);
    }

    /// <summary>Asynchronously writes a sequence of bytes to the current RefPackStream and advances the position within the stream by the number of bytes written.</summary>
    /// <param name="buffer">The buffer containing data to write to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to write to the stream.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the offset, count, or their sum exceeds the buffer length or is negative.</exception>
    /// <exception cref="TaskCanceledException">Thrown if the operation is canceled via the cancellation token.</exception>
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

    /// <summary>Writes the sequence of bytes from the provided memory buffer to the stream asynchronously.</summary>
    /// <param name="buffer">The region of memory to write data from.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the stream is disposed.</exception>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled(cancellationToken)
            : new ValueTask(
                Task.Run(
                    () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Write(buffer.Span);
                    },
                    cancellationToken
                )
            );
    }

    /// <summary>Attempts to write a single byte to the underlying stream.</summary>
    /// <param name="value">The byte to write to the stream.</param>
    /// <exception cref="NotSupportedException">Thrown when attempting to write a single byte, as this operation is not supported.</exception>
    public override void WriteByte(byte value) =>
        throw new NotSupportedException("Writing a single byte is not supported.");

    /// <summary>Closes the current stream and releases any resources associated with it.</summary>
    /// <remarks>This method disposes of the underlying resources used by the RefPackStream and ensures that the stream is properly finalized. It follows standard Stream behavior, suppressing finalization to optimize resource cleanup processes.</remarks>
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

    /// <summary>Releases the unmanaged resources used by the RefPackStream and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && _mode is CompressionMode.Compress)
        {
            // Ensure we encode and write once before closing.
            EnsureCompressedWritten();
        }

        if (disposing && !_leaveOpen)
        {
            _stream.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private void EnsureDecoded()
    {
        if (_decodedInitialized)
        {
            return;
        }

        if (!RefPackDecoderUtilities.IsRefPackCompressed(_stream))
        {
            throw new InvalidOperationException("The stream is not a RefPack stream.");
        }

        _stream.Position = 0;
        using var reader = new BinaryReader(_stream, EncodingExtensions.Ansi, leaveOpen: true);

        var expected = RefPackDecoderUtilities.GetUncompressedSize(_stream);
        _decodedBuffer = new byte[expected];
        _decodedLength = (int)RefPackDecoder.Decode(reader, _decodedBuffer, 0, (int)expected);
        _decodedReadPosition = 0;
        _decodedInitialized = true;
    }

    private void EnsureCompressedWritten()
    {
        if (_encodedWritten || _writeBuffer is null)
        {
            return;
        }

        // Prepare base stream for write
        _stream.Position = 0;
        using var writer = new BinaryWriter(_stream, EncodingExtensions.Ansi, leaveOpen: true);

        if (_writeBuffer.TryGetBuffer(out ArraySegment<byte> seg))
        {
            RefPackEncoder.Encode(writer, seg.Array!, seg.Offset, seg.Count);
        }
        else
        {
            var data = _writeBuffer.ToArray();
            RefPackEncoder.Encode(writer, data, 0, data.Length);
        }

        // Truncate any old content beyond current position
        if (_stream.CanSeek && _stream.CanWrite)
        {
            _stream.SetLength(_stream.Position);
        }

        _encodedWritten = true;
    }
}
