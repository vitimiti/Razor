// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Razor.Compression.HuffmanWithRunlength.Internals;
using Razor.Extensions;

namespace Razor.Compression.HuffmanWithRunlength;

/// <summary>Provides a stream implementation that combines Huffman encoding and run-length encoding for compression or decompression of data. This class cannot be inherited.</summary>
/// <remarks>The HuffmanWithRunlengthStream supports operations for encoding or decoding data using Huffman coding combined with run-length encoding. The behavior of the stream depends on the specified <see cref="CompressionMode"/> when instantiated. The stream does not support seeking operations.</remarks>
/// <threadsafety>This class does not guarantee thread safety. Synchronization is the caller's responsibility.</threadsafety>
public sealed class HuffmanWithRunlengthStream : Stream
{
    private readonly Stream _stream;
    private readonly CompressionMode _mode;
    private readonly bool _leaveOpen;

    private bool _disposed;

    /// <summary>Gets a value indicating whether the stream supports reading operations.</summary>
    /// <value>Returns <c>true</c> if the stream supports reading, is not disposed, and the compression mode is set to decompression; otherwise, <c>false</c>.</value>
    /// <remarks>The stream can only be read when it is opened in decompression mode and has not been disposed. This property checks the underlying stream's <c>CanRead</c> property as part of its evaluation.</remarks>
    public override bool CanRead => !_disposed && _mode is CompressionMode.Decompress && _stream.CanRead;

    /// <summary>Gets a value indicating whether the stream supports seeking operations.</summary>
    /// <value>Always returns <c>false</c> as seeking is not supported by the stream.</value>
    /// <remarks>Seeking is explicitly not supported due to the nature of the compression algorithm used. Attempting to seek will result in a <c>NotSupportedException</c>.</remarks>
    public override bool CanSeek => false;

    /// <summary>Gets a value indicating whether the stream supports writing operations.</summary>
    /// <value>Returns <c>true</c> if the stream supports writing, is not disposed, and the compression mode is set to compression; otherwise, <c>false</c>.</value>
    /// <remarks>The stream can only be written to when it is opened in compression mode and has not been disposed. This property checks the underlying stream's <c>CanWrite</c> property as part of its evaluation.</remarks>
    public override bool CanWrite => !_disposed && _mode is CompressionMode.Compress && _stream.CanWrite;

    /// <summary>Gets the length of the stream.</summary>
    /// <value>Returns the length of the stream, which varies based on the compression mode: the original length of the underlying stream in compression mode, or the calculated uncompressed size in decompression mode.</value>
    /// <remarks>The value represents the length of the data being processed. Accessing this property in decompression mode requires inspecting the uncompressed size, which may involve computation. If the stream has been disposed, an <c>ObjectDisposedException</c> is thrown.</remarks>
    public override long Length
    {
        get
        {
            if (_mode is CompressionMode.Compress)
            {
                return _stream.Length;
            }

            ObjectDisposedException.ThrowIf(_disposed, this);
            return HuffmanWithRunlengthDecoderUtilities.GetUncompressedSize(_stream);
        }
    }

    /// <summary>Gets or sets the current position within the stream.</summary>
    /// <value>Accessing this property always throws a <see cref="NotSupportedException"/>.</value>
    /// <remarks>The <c>Position</c> property is not supported for this stream and will throw a <see cref="NotSupportedException"/> regardless of any operation.</remarks>
    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    /// <summary>Represents a stream for handling data compression and decompression using a combination of Huffman coding and run-length encoding. Provides functionality for reading and writing compressed data.</summary>
    /// <param name="stream">The stream to wrap.</param>
    /// <param name="mode">The compression mode to use.</param>
    /// <param name="leaveOpen">Indicates whether the underlying stream should be left open after the stream is disposed.</param>
    /// <exception cref="ArgumentException"><paramref name="stream"/> does not support seeking.</exception>
    /// <remarks>The <paramref name="stream"/> must support seeking for the <see cref="HuffmanWithRunlengthStream"/> to be able to read or write data.</remarks>
    public HuffmanWithRunlengthStream([NotNull] Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking.", nameof(stream));
        }

        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
    }

    /// <summary>Releases the resources used by the <see cref="HuffmanWithRunlengthStream"/>.</summary>
    /// <param name="disposing">Indicates whether the method was called from the Dispose method (<c>true</c>) or from a finalizer (<c>false</c>).</param>
    /// <remarks>If <paramref name="disposing"/> is <c>true</c>, this method releases all managed and unmanaged resources. If <paramref name="disposing"/> is <c>false</c>, only unmanaged resources are released.</remarks>
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

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>Flushes data from the stream to the underlying storage and clears any internal buffers, ensuring all written data is persisted.</summary>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not in a writable state.</exception>
    /// <remarks>This method propagates the flush operation to the underlying stream if the stream is writable.</remarks>
    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!CanWrite)
        {
            throw new InvalidOperationException("The stream is not writable.");
        }

        _stream.Flush();
    }

    /// <summary>Asynchronously clears all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not in a valid state for this operation.</exception>
    /// <remarks>This method ensures that any buffered data is written to the underlying storage to maintain consistency. If the <paramref name="cancellationToken"/> is triggered, the operation will attempt to cancel, though partial data may still be flushed.</remarks>
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

    /// <summary>Reads decompressed data from the stream into the specified buffer.</summary>
    /// <param name="buffer">The buffer to store the decompressed data.</param>
    /// <param name="offset">The byte offset in the buffer at which to begin reading data.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not readable or is not a valid HuffmanWithRunlength stream.</exception>
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

        if (!HuffmanWithRunlengthDecoderUtilities.IsHuffmanWithRunlengthCompressed(_stream))
        {
            throw new InvalidOperationException("The stream is not a HuffmanWithRunlength stream.");
        }

        _stream.Position = 0;
        using BinaryReader reader = new(_stream, EncodingExtensions.Ansi, leaveOpen: true);
        return (int)HuffmanWithRunlengthDecoder.Decode(reader, buffer, offset, count);
    }

    /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">A span of bytes to store the data read from the stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not in decompression mode.</exception>
    public override int Read(Span<byte> buffer) => Read(buffer.ToArray(), 0, buffer.Length);

    /// <summary>Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data read from the stream.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the <c>TResult</c> parameter contains the total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/>, <paramref name="count"/>, or their sum is negative or exceeds the bounds of the <paramref name="buffer"/>.</exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled through the provided <paramref name="cancellationToken"/>.</exception>
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
    /// <param name="buffer">A region of memory to write the data read from the stream.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A value task representing the asynchronous read operation. The result contains the total number of bytes read into the buffer.</returns>
    /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
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

        var task = Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
                try
                {
                    var read = Read(rented, 0, buffer.Length);
                    new ReadOnlySpan<byte>(rented, 0, read).CopyTo(buffer.Span);
                    return read;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            },
            cancellationToken
        );

        return new ValueTask<int>(task);
    }

    /// <summary>Attempts to read a single byte from the stream. Not supported in this implementation.</summary>
    /// <returns>Always throws an exception.</returns>
    /// <exception cref="NotSupportedException">Reading a single byte is not supported.</exception>
    public override int ReadByte() => throw new NotSupportedException("Reading a single byte is not supported.");

    /// <summary>Sets the position within the current stream. This operation is not supported by <see cref="HuffmanWithRunlengthStream"/>.</summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
    /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <returns>This method does not return a value as it always throws a <see cref="NotSupportedException"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown because seeking is not supported by this stream.</exception>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("Seeking is not supported.");

    /// <summary>Throws a <see cref="NotSupportedException"/> because setting the length of the stream is not supported.</summary>
    /// <param name="value">The length value to set, which is not supported for this stream.</param>
    /// <exception cref="NotSupportedException">Always thrown to indicate that setting the length is not supported.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException("Setting the length is not supported.");

    /// <summary>Writes a sequence of bytes to the current stream and advances the stream's position by the number of bytes written, using Huffman and run-length encoding for compression.</summary>
    /// <param name="buffer">The buffer containing data to write.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin writing data.</param>
    /// <param name="count">The number of bytes to write from the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or <paramref name="offset"/> + <paramref name="count"/> is greater than the size of <paramref name="buffer"/>.</exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not writable.</exception>
    /// <remarks>This method clears any existing content in the stream and truncates it before writing new data. Compression is applied using Huffman and run-length encoding.</remarks>
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

        // Start writing from the beginning and truncate previous content to avoid trailing bytes.
        _stream.Position = 0;
        _stream.SetLength(0);

        using BinaryWriter writer = new(_stream, EncodingExtensions.Ansi, leaveOpen: true);
        HuffmanWithRunlengthEncoder.Encode(writer, buffer, offset, count);
    }

    /// <summary>Writes a sequence of bytes from the specified <see cref="ReadOnlySpan{T}"/> to the current stream and advances the stream position by the number of bytes written.</summary>
    /// <param name="buffer">A read-only span containing the data to write to the stream.</param>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <remarks>This method allows writing data without additional memory allocation by directly working with spans.</remarks>
    public override void Write(ReadOnlySpan<byte> buffer) => Write(buffer.ToArray(), 0, buffer.Length);

    /// <summary>Writes a sequence of bytes to the stream asynchronously.</summary>
    /// <param name="buffer">An array of bytes to be written to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the <paramref name="buffer"/> at which to begin writing bytes.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or their sum exceeds the length of <paramref name="buffer"/>.</exception>
    /// <exception cref="TaskCanceledException">The operation was canceled.</exception>
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

    /// <summary>Writes a sequence of bytes from the provided read-only memory buffer to the current stream asynchronously.</summary>
    /// <param name="buffer">The buffer containing the data to write.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="NotSupportedException">The stream does not support writing.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
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

    /// <summary>Attempts to write a single byte to the stream. Not supported by this implementation.</summary>
    /// <param name="value">The byte to write to the stream.</param>
    /// <exception cref="NotSupportedException">Thrown whenever this method is called, as single-byte writing is not supported.</exception>
    public override void WriteByte(byte value) =>
        throw new NotSupportedException("Writing a single byte is not supported.");

    /// <summary>Closes the current stream and releases any resources associated with it. Calls <see cref="Dispose(bool)"/> with the disposing parameter set to true and suppresses finalization of the object.</summary>
    /// <remarks>Overrides the <see cref="Stream.Close"/> method to ensure proper resource cleanup and adherence to the <see cref="Stream"/> standards.</remarks>
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
}
