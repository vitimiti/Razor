// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using JetBrains.Annotations;
using Razor.Compression.BinaryTree;
using Razor.Compression.HuffmanWithRunlength;
using Razor.Compression.LightZhl;
using Razor.Compression.RefPack;

namespace Razor.Compression;

[PublicAPI]
public sealed class CompressionStream : Stream
{
    private const string NotReadableStreamError = "The stream is not readable.";

    private readonly CompressionMode _mode;
    private readonly CompressionType _type;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    private bool _disposed;
    private Stream? _decoder;

    public static CompressionType PreferredCompressionType => CompressionType.RefPack;

    public override bool CanRead =>
        !_disposed && _mode is CompressionMode.Decompress && _stream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite =>
        !_disposed && _mode is CompressionMode.Compress && _stream.CanWrite;

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

                var comp = MapTagToType(header[..4]);
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

    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

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

    public bool IsCompressed => CompressionType is not CompressionType.None;

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

                var comp = MapTagToType(header[..4]);
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

    public CompressionStream(Stream stream, CompressionType type, bool leaveOpen = false)
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

    public CompressionStream(Stream stream, bool leaveOpen = false)
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
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _decoder?.Flush();
        _stream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _decoder?.FlushAsync(cancellationToken);
        return _stream.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
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
        var tag = header[..4];
        var compType = MapTagToType(tag);
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
            CompressionType.BinaryTree => new BinaryTreeStream(
                _stream,
                CompressionMode.Decompress,
                leaveOpen: true
            ),
            CompressionType.HuffmanWithRunlength => new HuffmanWithRunlengthStream(
                _stream,
                CompressionMode.Decompress,
                leaveOpen: true
            ),
            CompressionType.RefPack => new RefPackStream(
                _stream,
                CompressionMode.Decompress,
                leaveOpen: true
            ),
            CompressionType.NoxLzh => new LightZhlStream(
                _stream,
                CompressionMode.Decompress,
                leaveOpen: true
            ),
            CompressionType.ZLib1
            or CompressionType.ZLib2
            or CompressionType.ZLib3
            or CompressionType.ZLib4
            or CompressionType.ZLib5
            or CompressionType.ZLib6
            or CompressionType.ZLib7
            or CompressionType.ZLib8
            or CompressionType.ZLib9 => new ZLibStream(
                _stream,
                CompressionMode.Decompress,
                leaveOpen: true
            ),
            _ => throw new InvalidOperationException("Unsupported compression type."),
        };

        // Forward read to the initialized decoder stream
        return _decoder.Read(buffer, offset, count);
    }

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

    public override Task<int> ReadAsync(
        byte[] buffer,
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

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = new CancellationToken()
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return !CanRead
            ? throw new InvalidOperationException(NotReadableStreamError)
            : new ValueTask<int>(Read(buffer.Span));
    }

    public override int ReadByte()
    {
        throw new NotSupportedException("Reading a single byte is not supported.");
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Seeking is not supported.");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Setting the length is not supported.");
    }

    public override void Write(byte[] buffer, int offset, int count)
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
            or CompressionType.ZLib9 => CreateZLibHeader(
                (int)_type - (int)CompressionType.ZLib1 + 1
            ),
            _ => throw new ArgumentOutOfRangeException(
                $"Unsupported compression type: {_type}",
                nameof(_type)
            ),
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
                using BinaryTreeStream specificStream = new(
                    _stream,
                    CompressionMode.Compress,
                    leaveOpen: true
                );
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
                using RefPackStream specificStream = new(
                    _stream,
                    CompressionMode.Compress,
                    leaveOpen: true
                );
                specificStream.Write(buffer, offset, count);
                break;
            }
            case CompressionType.NoxLzh:
            {
                using LightZhlStream specificStream = new(
                    _stream,
                    CompressionMode.Compress,
                    leaveOpen: true
                );
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
            default:
                throw new ArgumentOutOfRangeException(
                    $"Unsupported compression type: {_type}",
                    nameof(_type)
                );
        }

        // Patch the uncompressed length in the header (little-endian), preserving current position
        var afterPayload = _stream.Position;
        _stream.Position = headerPos + 4;
        Span<byte> lenPatch = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(lenPatch, count);
        _stream.Write(lenPatch);
        _stream.Position = afterPayload;
    }

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

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        return !CanWrite
            ? throw new InvalidOperationException("The stream is not writable.")
            : Task.Run(() => Write(buffer, offset, count), cancellationToken);
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = new CancellationToken()
    )
    {
        return new ValueTask(
            WriteAsync(buffer.Span.ToArray(), 0, buffer.Length, cancellationToken)
        );
    }

    public override void WriteByte(byte value)
    {
        throw new NotSupportedException("Writing a single byte is not supported.");
    }

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
