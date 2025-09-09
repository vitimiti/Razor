// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using JetBrains.Annotations;
using Razor.Extensions;

namespace Razor.Compression.HuffmanWithRunlength;

[PublicAPI]
public sealed class HuffmanWithRunlengthStream : Stream
{
    private readonly Stream _stream;
    private readonly CompressionMode _mode;
    private readonly bool _leaveOpen;

    private bool _disposed;

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
                : HuffmanWithRunlengthDecoderUtilities.GetUncompressedSize(_stream);
        }
    }

    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    public HuffmanWithRunlengthStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
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

        _disposed = true;
        base.Dispose(disposing);
    }

    ~HuffmanWithRunlengthStream()
    {
        Dispose(disposing: false);
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!CanWrite)
        {
            throw new InvalidOperationException("The stream is not writable.");
        }

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

        if (!HuffmanWithRunlengthDecoderUtilities.IsHuffmanWithRunlengthCompressed(_stream))
        {
            throw new InvalidOperationException("The stream is not a HuffmanWithRunlength stream.");
        }

        _stream.Position = 0;
        using BinaryReader reader = new(_stream, EncodingExtensions.Ansi, leaveOpen: true);
        return (int)HuffmanWithRunlengthDecoder.Decode(reader, buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        return Read(buffer.ToArray(), 0, buffer.Length);
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

        // Start writing from the beginning and truncate previous content to avoid trailing bytes.
        _stream.Position = 0;
        _stream.SetLength(0);

        using BinaryWriter writer = new(_stream, EncodingExtensions.Ansi, leaveOpen: true);
        HuffmanWithRunlengthEncoder.Encode(writer, buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Write(buffer.ToArray(), 0, buffer.Length);
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
    public override void Close()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
