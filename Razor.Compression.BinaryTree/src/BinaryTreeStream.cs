// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using JetBrains.Annotations;
using Razor.Extensions;

namespace Razor.Compression.BinaryTree;

[PublicAPI]
public sealed class BinaryTreeStream : Stream
{
    private readonly Stream _stream;
    private readonly CompressionMode _mode;
    private readonly bool _leaveOpen;

    private bool _disposed;
    private byte[]? _decodedBuffer;
    private int _decodedLength;
    private int _readPosition;

    public override bool CanRead => _mode is CompressionMode.Decompress && _stream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => _mode is CompressionMode.Compress && _stream.CanWrite;

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

    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    public BinaryTreeStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking.", nameof(stream));
        }

        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
    }

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

    ~BinaryTreeStream()
    {
        Dispose(disposing: false);
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // For read mode, Flush should be a no-op.
        if (CanWrite)
        {
            _stream.Flush();
        }
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

        if (_readPosition >= _decodedLength)
        {
            return 0; // EOF
        }

        var toCopy = Math.Min(count, _decodedLength - _readPosition);
        Buffer.BlockCopy(_decodedBuffer!, _readPosition, buffer, offset, toCopy);
        _readPosition += toCopy;
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

        if (_readPosition >= _decodedLength)
        {
            return 0; // EOF
        }

        var toCopy = Math.Min(buffer.Length, _decodedLength - _readPosition);
        new ReadOnlySpan<byte>(_decodedBuffer!, _readPosition, toCopy).CopyTo(buffer);
        _readPosition += toCopy;
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

        // Invalidate any previous decoded cache as data changed.
        if (_decodedBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(_decodedBuffer);
            _decodedBuffer = null;
            _decodedLength = 0;
            _readPosition = 0;
        }
    }

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
