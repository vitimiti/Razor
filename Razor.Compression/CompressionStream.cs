// -----------------------------------------------------------------------
// <copyright file="CompressionStream.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Razor.Compression.BinaryTree;
using Razor.Compression.HuffmanWithRunlength;
using Razor.Compression.LightZhl;
using Razor.Compression.RefPack;

namespace Razor.Compression;

/// <summary>Provides a stream for compressing or decompressing data.</summary>
public sealed class CompressionStream : Stream
{
    private const string NotReadableStreamError = "The stream is not readable.";

    private readonly CompressionMode _mode;
    private readonly CompressionType _type;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    private bool _disposed;
    private Stream? _decoder;

    /// <summary>Initializes a new instance of the <see cref="CompressionStream"/> class.Provides functionality for handling compression and decompression streams using various compression types.</summary>
    /// <remarks>This class extends <see cref="Stream"/> and supports reading, writing, and flushing operations based on the specified compression mode and type.</remarks>
    /// <param name="stream">The underlying stream to use for reading and writing.</param>
    /// <param name="type">The compression type to use for compression and decompression.</param>
    /// <param name="leaveOpen"><c>true</c> to leave the stream open after the <see cref="CompressionStream"/> object is disposed; otherwise, <c>false</c>.</param>
    /// <exception cref="ArgumentException">The stream must support seeking.</exception>
    public CompressionStream([NotNull] Stream stream, CompressionType type, bool leaveOpen = false)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking.", nameof(stream));
        }

        _stream = stream;
        _leaveOpen = leaveOpen;
        _mode = CompressionMode.Compress;
        _type = type;
    }

    /// <summary>Initializes a new instance of the <see cref="CompressionStream"/> class.Provides functionality for handling compression and decompression streams using various compression types.</summary>
    /// <remarks>This class extends <see cref="Stream"/> and supports both compression and decompression based on the specified <see cref="CompressionType"/> and <see cref="CompressionMode"/>.</remarks>
    /// <param name="stream">The underlying stream to perform read or write operations on.</param>
    /// <param name="leaveOpen"><c>true</c> to leave the underlying stream open after the <see cref="CompressionStream"/> is disposed; otherwise, <c>false</c>.</param>
    /// <exception cref="ArgumentException">Thrown when the provided stream does not support seeking.</exception>
    public CompressionStream([NotNull] Stream stream, bool leaveOpen = false)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking.", nameof(stream));
        }

        _stream = stream;
        _leaveOpen = leaveOpen;
        _mode = CompressionMode.Decompress;
        _type = CompressionType.None;
    }

    /// <summary>Gets the default compression type used by the system.</summary>
    /// <value>A <see cref="CompressionType"/> value indicating the preferred compression algorithm. By default, this is set to <see cref="CompressionType.RefPack"/>.</value>
    public static CompressionType PreferredCompressionType => CompressionType.RefPack;

    /// <summary>Gets a value indicating whether the stream supports reading.</summary>
    /// <value><c>true</c> if the stream supports reading and is not disposed, and the compression mode is set to <see cref="CompressionMode.Decompress"/>; otherwise, <c>false</c>.</value>
    public override bool CanRead => !_disposed && _mode is CompressionMode.Decompress && _stream.CanRead;

    /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
    /// <value>Always returns <c>false</c> as seeking is not supported by this stream.</value>
    public override bool CanSeek => false;

    /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
    /// <value><c>true</c> if the stream is in compression mode, not disposed, and the underlying stream supports writing; otherwise, <c>false</c>.</value>
    public override bool CanWrite => !_disposed && _mode is CompressionMode.Compress && _stream.CanWrite;

    /// <summary>Gets the length of the stream, considering its compression mode.</summary>
    /// <value>The length of the stream in bytes. When in <see cref="CompressionMode.Compress"/>, this represents the length of the underlying stream. When in <see cref="CompressionMode.Decompress"/>, this may represent the uncompressed size if determinable.</value>
    public override long Length
    {
        get
        {
            if (_mode is CompressionMode.Compress)
            {
                return _stream.Length;
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_stream.Length < 8)
            {
                return _stream.Length;
            }

            var pos = _stream.Position;
            try
            {
                Span<byte> header = stackalloc byte[8];
                var read = _stream.Read(header);
                if (read < 8)
                {
                    return _stream.Length;
                }

                CompressionType comp = MapTagToType(header[..4]);
                if (comp is CompressionType.None)
                {
                    return _stream.Length;
                }

                // C++ stores uncompressed size as native little-endian Int32
                var uncompressed = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(4, 4));
                return uncompressed;
            }
            finally
            {
                _stream.Position = pos;
            }
        }
    }

    /// <summary>Gets or sets the position within the stream.</summary>
    /// <value>Always throws <see cref="NotSupportedException"/> as seeking is not supported.</value>
    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    /// <summary>Gets the compression type based on the content of the stream.</summary>
    /// <value>A <see cref="CompressionType"/> value representing the detected compression algorithm, or <see cref="CompressionType.None"/> if no valid compression type is detected or the stream content is too small to determine the type.</value>
    public CompressionType CompressionType
    {
        get
        {
            if (_stream.Length < 8)
            {
                return CompressionType.None;
            }

            var pos = _stream.Position;
            try
            {
                Span<byte> tag = stackalloc byte[4];
                var read = _stream.Read(tag);
                return read < 4 ? CompressionType.None : MapTagToType(tag);
            }
            finally
            {
                _stream.Position = pos;
            }
        }
    }

    /// <summary>Gets a value indicating whether the stream is compressed.</summary>
    /// <value><c>true</c> if the stream uses a compression type other than <see cref="CompressionType.None"/>; otherwise, <c>false</c>.</value>
    public bool IsCompressed => CompressionType is not CompressionType.None;

    /// <summary>Gets the size of the data in its uncompressed state.</summary>
    /// <value>A <see cref="long"/> value representing the uncompressed size of the data. If the stream's length is less than 8 bytes or if the compression type is <see cref="CompressionType.None"/>, this value will match the stream's length.</value>
    public long UncompressedSize
    {
        get
        {
            if (_stream.Length < 8)
            {
                return _stream.Length;
            }

            var pos = _stream.Position;
            try
            {
                Span<byte> header = stackalloc byte[8];
                var read = _stream.Read(header);
                if (read < 8)
                {
                    return _stream.Length;
                }

                CompressionType comp = MapTagToType(header[..4]);
                return comp is CompressionType.None
                    ? _stream.Length
                    : BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(4, 4));
            }
            finally
            {
                _stream.Position = pos;
            }
        }
    }

    /// <summary>Flushes all buffers for the compression stream and ensures that any buffered data is written to the underlying device.</summary>
    /// <remarks>This method clears any internal buffers and invokes the flush operation on both the decoder and the underlying stream. If the stream is disposed, an exception is thrown.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has already been disposed.</exception>
    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _decoder?.Flush();
        _stream.Flush();
    }

    /// <summary>Asynchronously clears all buffers for the stream and ensures that any buffered data is written to the underlying device.</summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous flush operation.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = _decoder?.FlushAsync(cancellationToken);
        return _stream.FlushAsync(cancellationToken);
    }

    /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or <paramref name="offset"/> + <paramref name="count"/> is greater than the buffer length.</exception>
    /// <exception cref="InvalidOperationException">The stream does not support reading.</exception>
    public override int Read([NotNull] byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        if (!CanRead)
        {
            throw new InvalidOperationException(NotReadableStreamError);
        }

        if (_decoder is not null)
        {
            return _decoder.Read(buffer, offset, count);
        }

        if (_stream.Length < 8)
        {
            _stream.Position = 0;
            return _stream.Read(buffer, offset, count);
        }

        // Read 8-byte header (tag + uncompressed length)
        Span<byte> header = stackalloc byte[8];
        _stream.Position = 0;
        var read = _stream.Read(header);
        if (read < 8)
        {
            _stream.Position = 0;
            return _stream.Read(buffer, offset, count);
        }

        // Determine compression type by tag
        Span<byte> tag = header[..4];
        CompressionType compType = MapTagToType(tag);
        if (compType == CompressionType.None)
        {
            // No valid compression tag: treat entire stream as raw data
            _stream.Position = 0;
            return _stream.Read(buffer, offset, count);
        }

        // Position stream at start of codec payload
        _stream.Position = 8;

        // Create the appropriate decoder over the remaining payload
        _decoder = compType switch
        {
            CompressionType.BinaryTree => new BinaryTreeStream(_stream, CompressionMode.Decompress, leaveOpen: true),
            CompressionType.HuffmanWithRunlength => new HuffmanWithRunlengthStream(
                _stream,
                CompressionMode.Decompress,
                leaveOpen: true
            ),
            CompressionType.RefPack => new RefPackStream(_stream, CompressionMode.Decompress, leaveOpen: true),
            CompressionType.NoxLzh => new LightZhlStream(_stream, CompressionMode.Decompress, leaveOpen: true),
            CompressionType.ZLib1
            or CompressionType.ZLib2
            or CompressionType.ZLib3
            or CompressionType.ZLib4
            or CompressionType.ZLib5
            or CompressionType.ZLib6
            or CompressionType.ZLib7
            or CompressionType.ZLib8
            or CompressionType.ZLib9 => new ZLibStream(_stream, CompressionMode.Decompress, leaveOpen: true),
            CompressionType.None => throw new NotSupportedException(
                "Cannot read compressed data from a stream that does not contain a compression tag."
            ),
            _ => throw new InvalidOperationException("Unsupported compression type."),
        };

        // Forward read to the initialized decoder stream
        return _decoder.Read(buffer, offset, count);
    }

    /// <summary>Reads data from the stream into the specified buffer.</summary>
    /// <param name="buffer">A span of bytes where the read data will be stored.</param>
    /// <returns>The number of bytes read into the buffer. Returns 0 if the end of the stream is reached.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the stream is not readable.</exception>
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!CanRead)
        {
            throw new InvalidOperationException(NotReadableStreamError);
        }

        // Rent a temporary array to avoid extra allocations and copy into the provided buffer
        var tmp = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            var read = Read(tmp, 0, buffer.Length);
            new ReadOnlySpan<byte>(tmp, 0, read).CopyTo(buffer);
            return read;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tmp);
        }
    }

    /// <summary>Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to write the data into.</param>
    /// <param name="offset">The byte offset in the buffer at which to begin writing data read from the stream.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the result of the asynchronous read operation. The result contains the total number of bytes read into the buffer.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or their sum exceeds the buffer length.</exception>
    /// <exception cref="InvalidOperationException">The stream is not readable.</exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
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

        return !CanRead
            ? throw new InvalidOperationException(NotReadableStreamError)
            : Task.Run(() => Read(buffer, offset, count), cancellationToken);
    }

    /// <summary>Reads a sequence of bytes from the current stream asynchronously and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">The region of memory to write the data read from the stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A value task that represents the asynchronous read operation. The result contains the total number of bytes read into the buffer.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not readable.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the current stream has been disposed.</exception>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return !CanRead
            ? throw new InvalidOperationException(NotReadableStreamError)
            : new ValueTask<int>(Read(buffer.Span));
    }

    /// <summary>Attempts to read a single byte from the stream.</summary>
    /// <returns>The byte read, or -1 if the end of the stream has been reached.</returns>
    /// <exception cref="NotSupportedException">Reading a single byte is not supported for this stream.</exception>
    public override int ReadByte() => throw new NotSupportedException("Reading a single byte is not supported.");

    /// <summary>Attempts to set the position within the current stream. Not supported for <see cref="CompressionStream"/>.</summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
    /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <returns>This method does not return a value as seeking is not supported and will always throw an exception.</returns>
    /// <exception cref="NotSupportedException">Thrown in all cases as seeking is not supported by <see cref="CompressionStream"/>.</exception>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("Seeking is not supported.");

    /// <summary>Throws a <see cref="NotSupportedException"/> because setting the length is not supported.</summary>
    /// <param name="value">The desired length of the stream, which is not applicable for this implementation.</param>
    /// <exception cref="NotSupportedException">Always thrown as this operation is not supported.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException("Setting the length is not supported.");

    /// <summary>Writes a sequence of bytes to the current compression stream and advances the position within the stream by the number of bytes written.</summary>
    /// <param name="buffer">An array of bytes to write to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin reading bytes.</param>
    /// <param name="count">The number of bytes to write to the stream.</param>
    /// <exception cref="InvalidOperationException">The stream does not support writing, or the write operation failed for the compression type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The combination of <paramref name="offset"/> and <paramref name="count"/> exceeds the length of <paramref name="buffer"/> or a negative value is provided.</exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
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

        // Build header tag
        var headerTag = _type switch
        {
            CompressionType.BinaryTree => "EAB\0"u8.ToArray(),
            CompressionType.HuffmanWithRunlength => "EAH\0"u8.ToArray(),
            CompressionType.RefPack => "EAR\0"u8.ToArray(),
            CompressionType.NoxLzh => "NOX\0"u8.ToArray(),
            CompressionType.ZLib1
            or CompressionType.ZLib2
            or CompressionType.ZLib3
            or CompressionType.ZLib4
            or CompressionType.ZLib5
            or CompressionType.ZLib6
            or CompressionType.ZLib7
            or CompressionType.ZLib8
            or CompressionType.ZLib9 => CreateZLibHeader((int)_type - (int)CompressionType.ZLib1 + 1),
            CompressionType.None => throw new NotSupportedException(
                "Cannot write compressed data to a stream that does not contain a compression tag."
            ),
            _ => throw new ArgumentOutOfRangeException($"Unsupported compression type: {_type}", nameof(_type)),
        };

        // Write header with zero length first (to mirror C++ semantics), then patch length after successful compression
        var headerPos = _stream.Position;
        Span<byte> header = stackalloc byte[8];
        headerTag.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.Slice(4, 4), 0);
        _stream.Write(header);

        // Now write the compressed payload
        switch (_type)
        {
            case CompressionType.BinaryTree:
            {
                using BinaryTreeStream specificStream = new(_stream, CompressionMode.Compress, leaveOpen: true);
                specificStream.Write(buffer, offset, count);
                break;
            }

            case CompressionType.HuffmanWithRunlength:
            {
                using HuffmanWithRunlengthStream specificStream = new(
                    _stream,
                    CompressionMode.Compress,
                    leaveOpen: true
                );
                specificStream.Write(buffer, offset, count);
                break;
            }

            case CompressionType.RefPack:
            {
                using RefPackStream specificStream = new(_stream, CompressionMode.Compress, leaveOpen: true);
                specificStream.Write(buffer, offset, count);
                break;
            }

            case CompressionType.NoxLzh:
            {
                using LightZhlStream specificStream = new(_stream, CompressionMode.Compress, leaveOpen: true);
                specificStream.Write(buffer, offset, count);
                break;
            }

            case CompressionType.ZLib1
            or CompressionType.ZLib2
            or CompressionType.ZLib3
            or CompressionType.ZLib4
            or CompressionType.ZLib5
            or CompressionType.ZLib6
            or CompressionType.ZLib7
            or CompressionType.ZLib8
            or CompressionType.ZLib9:
            {
                var level = (int)_type - (int)CompressionType.ZLib1 + 1; // 1..9 (tag only)
                using ZLibStream specificStream = new(_stream, (CompressionLevel)level, _leaveOpen);
                specificStream.Write(buffer, offset, count);
                break;
            }

            case CompressionType.None:
                throw new NotSupportedException(
                    "Cannot write compressed data to a stream that does not contain a compression tag."
                );
            default:
                throw new ArgumentOutOfRangeException($"Unsupported compression type: {_type}", nameof(_type));
        }

        // Patch the uncompressed length in the header (little-endian), preserving current position
        var afterPayload = _stream.Position;
        _stream.Position = headerPos + 4;
        Span<byte> lenPatch = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(lenPatch, count);
        _stream.Write(lenPatch);
        _stream.Position = afterPayload;
    }

    /// <summary>Writes a sequence of bytes to the stream using a read-only span.</summary>
    /// <param name="buffer">The read-only span of bytes to write to the stream.</param>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="NotSupportedException">The stream does not support writing.</exception>
    /// <exception cref="IOException">An I/O error occurred during the write operation.</exception>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        // Use an array pool to avoid per-call allocation
        var tmp = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.CopyTo(tmp);
            Write(tmp, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tmp);
        }
    }

    /// <summary>Asynchronously writes a sequence of bytes to the current stream and advances the position within the stream by the number of bytes written.</summary>
    /// <param name="buffer">The buffer containing the data to write to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the stream.</param>
    /// <param name="count">The maximum number of bytes to write from the buffer.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative, or their sum exceeds the buffer length.</exception>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The stream is not writable.</exception>
    public override Task WriteAsync([NotNull] byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        return !CanWrite
            ? throw new InvalidOperationException("The stream is not writable.")
            : Task.Run(() => Write(buffer, offset, count), cancellationToken);
    }

    /// <summary>Asynchronously writes a sequence of bytes to the current stream.</summary>
    /// <param name="buffer">The region of memory to write data from.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new()) =>
        new(WriteAsync(buffer.Span.ToArray(), 0, buffer.Length, cancellationToken));

    /// <summary>Attempts to write a single byte to the stream.</summary>
    /// <param name="value">The byte to write to the stream.</param>
    /// <exception cref="NotSupportedException">Writing a single byte is not supported by this stream.</exception>
    public override void WriteByte(byte value) =>
        throw new NotSupportedException("Writing a single byte is not supported.");

    /// <summary>Closes the current <see cref="CompressionStream"/> and releases any associated system resources.</summary>
    /// <remarks>This method overrides <see cref="Stream.Close"/> to ensure proper resource disposal and comply with stream standards.</remarks>
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

    /// <summary>Releases the resources used by the <see cref="CompressionStream"/>.</summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _decoder?.Dispose();

            if (!_leaveOpen)
            {
                _stream.Dispose();
            }
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private static byte[] CreateZLibHeader(int level)
    {
        // "ZL1\0"..."ZL9\0" (ASCII digit for level)
        var header = "ZL0\0"u8.ToArray();
        header[2] = (byte)('0' + level);
        return header;
    }

    private static CompressionType MapTagToType(ReadOnlySpan<byte> tag)
    {
        if (tag.SequenceEqual("NOX\0"u8))
        {
            return CompressionType.NoxLzh;
        }

        if (tag.SequenceEqual("EAB\0"u8))
        {
            return CompressionType.BinaryTree;
        }

        if (tag.SequenceEqual("EAH\0"u8))
        {
            return CompressionType.HuffmanWithRunlength;
        }

        if (tag.SequenceEqual("EAR\0"u8))
        {
            return CompressionType.RefPack;
        }

        if (tag.Length < 4 || tag[0] != (byte)'Z' || tag[1] != (byte)'L' || tag[3] != 0)
        {
            return CompressionType.None;
        }

        var digit = tag[2] - (byte)'0';
        return digit switch
        {
            1 => CompressionType.ZLib1,
            2 => CompressionType.ZLib2,
            3 => CompressionType.ZLib3,
            4 => CompressionType.ZLib4,
            5 => CompressionType.ZLib5,
            6 => CompressionType.ZLib6,
            7 => CompressionType.ZLib7,
            8 => CompressionType.ZLib8,
            9 => CompressionType.ZLib9,
            _ => CompressionType.None,
        };
    }
}
