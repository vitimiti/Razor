// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using JetBrains.Annotations;
using Razor.Compression.RefPack.Internals;
using Razor.Extensions;

namespace Razor.Compression.RefPack;

[PublicAPI]
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

    public override bool CanRead =>
        !_disposed && _mode is CompressionMode.Decompress && _stream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite =>
        !_disposed && _mode is CompressionMode.Compress && _stream.CanWrite;

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

    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    public RefPackStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
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

    ~RefPackStream()
    {
        Dispose(disposing: false);
    }

    public override void Flush()
    {
        if (!CanWrite)
        {
            throw new InvalidOperationException("The stream is not writable.");
        }

        EnsureCompressedWritten();
        _stream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Flush();
            },
            cancellationToken
        );
    }

    public override int Read(byte[] buffer, int offset, int count)
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

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Read(buffer, offset, count);
            },
            cancellationToken
        );
    }

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

        if (count == 0)
        {
            return;
        }

        // Accumulate uncompressed bytes to encode later on Flush/Dispose.
        _writeBuffer!.Write(buffer, offset, count);
    }

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

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Write(buffer, offset, count);
            },
            cancellationToken
        );
    }

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

        return new ValueTask(
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
