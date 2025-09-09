// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using JetBrains.Annotations;

namespace Razor.Compression.LightZhl;

[PublicAPI]
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

    public override bool CanRead => _mode is CompressionMode.Decompress && _stream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => _mode is CompressionMode.Compress && _stream.CanWrite;
    public override long Length =>
        throw new NotSupportedException("Getting length is not supported for LightZhlStream.");

    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    public LightZhlStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        if (mode is CompressionMode.Compress && !stream.CanSeek)
        {
            throw new ArgumentException(
                "The stream must support seeking when used for compression.",
                nameof(stream)
            );
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
                try
                {
                    FlushCompressedPayload();
                }
                catch
                {
                    // Swallow exceptions on dispose to match Stream behavior.
                }
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

    ~LightZhlStream()
    {
        Dispose(disposing: false);
    }

    public override void Flush()
    {
        if (!CanWrite)
        {
            throw new InvalidOperationException("The stream is not writable.");
        }

        FlushCompressedPayload();
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
        CancellationToken cancellationToken = default
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
                return Read(buffer.Span);
            },
            cancellationToken
        );

        return new ValueTask<int>(task);
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

        if (_uncompressedBuffer is null)
        {
            // Should not happen, but guard anyway.
            _uncompressedBuffer = new MemoryStream();
        }

        _uncompressedBuffer.Write(buffer, offset, count);
        _compressDirty = true;
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

        _uncompressedBuffer ??= new MemoryStream();
        _uncompressedBuffer.Write(buffer);
        _compressDirty = true;
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
        CancellationToken cancellationToken = default
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

    public override int ReadByte()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!CanRead)
        {
            throw new NotSupportedException("Reading a single byte is not supported.");
        }

        EnsureDecompressedLoaded();

        if (_decompressedBuffer is null || _decompressedPosition >= _decompressedBuffer.Length)
        {
            return -1;
        }

        return _decompressedBuffer[_decompressedPosition++];
    }

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

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Seeking is not supported.");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Setting the length is not supported.");
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
            _decompressedBuffer = Array.Empty<byte>();
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
