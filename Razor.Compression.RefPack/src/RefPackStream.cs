// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using JetBrains.Annotations;
using Razor.Extensions;

namespace Razor.Compression.RefPack;

/// <summary>Represents a stream that provides compression or decompression functionality using the RefPack algorithm.</summary>
/// <remarks>The <see cref="RefPackStream"/> class operates on a base stream while either compressing or decompressing data, depending on the specified mode. The <see cref="CompressionMode"/> determines the operation mode for the stream.</remarks>
[PublicAPI]
public sealed class RefPackStream : Stream
{
    private bool _disposed;
    private readonly Stream _stream;
    private readonly CompressionMode _mode;
    private readonly bool _leaveOpen;

    /// <summary>Represents a stream that provides compression or decompression functionality using the RefPack algorithm.</summary>
    /// <param name="stream">The base stream to operate on.</param>
    /// <param name="mode">The mode of the stream.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="RefPackStream" /> object is disposed; otherwise, <see langword="false" />.</param>
    /// <remarks>The <see cref="RefPackStream" /> class operates on a base stream while either compressing or decompressing data, depending on the specified mode. The <see cref="CompressionMode"/> determines the operation mode for the stream.</remarks>
    /// <exception cref="ArgumentException">Thrown when the given <paramref name="stream" /> is not seekable.</exception>
    public RefPackStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must support seeking.", nameof(stream));
        }

        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
    }

    /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
    /// <value><see langword="true" /> if the stream is in decompression mode (<see cref="CompressionMode.Decompress" />); otherwise, <see langword="false" />.</value>
    /// <remarks>This property returns <see langword="true" /> when the stream is configured for reading (decompression). Attempting to read from the stream when this property is <see langword="false" /> will result in a <see cref="NotSupportedException" />.</remarks>
    public override bool CanRead => _mode is CompressionMode.Decompress && _stream.CanRead;

    /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
    /// <value><see langword="false" /> in all cases as seeking is not supported by the <see cref="RefPackStream" />.</value>
    /// <remarks>The <see cref="CanSeek" /> property always returns <see langword="false" /> because seeking is not supported by streams operating in compression or decompression modes using the RefPack algorithm. Any attempt to call methods that rely on seeking, such as <see cref="Stream.Seek" />, will result in a <see cref="NotSupportedException" />.</remarks>
    public override bool CanSeek => false;

    /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
    /// <value><see langword="true" /> if the stream is in compression mode (<see cref="CompressionMode.Compress" />); otherwise, <see langword="false" />.</value>
    /// <remarks>This property returns <see langword="true" /> when the stream is configured for writing (compression). Attempting to write to the stream when this property is <see langword="false" /> will result in a <see cref="NotSupportedException" />.</remarks>
    public override bool CanWrite => _mode is CompressionMode.Compress && _stream.CanWrite;

    /// <summary>Gets the length of the stream in bytes.</summary>
    /// <value>For compression mode, returns the length of the underlying base stream. For decompression mode, returns the uncompressed size of the stream.</value>
    /// <remarks>In decompression mode, this property determines the uncompressed size of the stream using utilities specific to the RefPack algorithm. If the stream is disposed, accessing this property will throw an <see cref="ObjectDisposedException" />. In compression mode, this property directly returns the length of the underlying base stream.</remarks>
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

    /// <summary>Gets or sets the current position within the stream.</summary>
    /// <value>This property is not supported and always throws a <see cref="NotSupportedException" />.</value>
    /// <exception cref="NotSupportedException">Always thrown when attempting to get or set this property.</exception>
    /// <remarks>The <see cref="Position" /> property is not implemented in <see cref="RefPackStream" />, as seeking is not supported for this stream.</remarks>
    public override long Position
    {
        get => throw new NotSupportedException("Getting stream position is not supported.");
        set => throw new NotSupportedException("Setting stream position is not supported.");
    }

    /// <summary>Releases the resources used by the <see cref="RefPackStream"/> instance. Optionally releases the managed resources.</summary>
    /// <param name="disposing">A boolean value indicating whether to release managed resources: <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
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

    ~RefPackStream()
    {
        Dispose(disposing: false);
    }

    /// <summary>Clears all buffers for the underlying stream and causes any buffered data to be written to the underlying device.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the stream is not writable.</exception>
    public override void Flush()
    {
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

    /// <summary>Reads a sequence of bytes from the current RefPack stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
    /// <param name="offset">The zero-based byte offset in the <paramref name="buffer"/> at which to begin storing the data read from the stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the stream is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given <paramref name="offset" /> or <paramref name="count" /> is negative or if the given <paramref name="offset" /> + <paramref name="count" /> is greater than the length of the given <paramref name="buffer" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not readable or if the stream is not a valid RefPack stream.</exception>
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

        if (!RefPackDecoderUtilities.IsRefPackCompressed(_stream))
        {
            throw new InvalidOperationException("The stream is not a RefPack stream.");
        }

        _stream.Position = 0;
        using BinaryReader reader = new(_stream, EncodingExtensions.Ansi, leaveOpen: true);
        return (int)RefPackDecoder.Decode(reader, buffer, offset, count);
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

    /// <summary>Sets the position within the current stream. This operation is not supported for <see cref="RefPackStream" />.</summary>
    /// <param name="offset">The byte offset relative to the <paramref name="origin" />.</param>
    /// <param name="origin">A <see cref="SeekOrigin" /> value indicating the reference point used to get the new position.</param>
    /// <returns>This method does not return a value as seeking is not supported and will always throw a <see cref="NotSupportedException" />.</returns>
    /// <exception cref="NotSupportedException">Always thrown as seeking is not supported for this stream.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Seeking is not supported.");
    }

    /// <summary>Throws a <see cref="NotSupportedException"/> because setting the length of a <see cref="RefPackStream" /> is not supported.</summary>
    /// <param name="value">The desired length of the <see cref="RefPackStream"/>. This parameter has no effect as the operation is not supported.</param>
    /// <exception cref="NotSupportedException">Always thrown because setting the length is not supported.</exception>
    public override void SetLength(long value)
    {
        throw new NotSupportedException("Setting the length is not supported.");
    }

    /// <summary>Writes a sequence of bytes to the current stream and advances the position within the stream by the number of bytes written.</summary>
    /// <param name="buffer">The byte array that contains the data to write to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the <paramref name="buffer"/> at which to begin copying bytes to the stream.</param>
    /// <param name="count">The maximum number of bytes to write to the stream.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="count"/> is negative, or if the sum of <paramref name="offset"/> and <paramref name="count"/> exceeds the length of the <paramref name="buffer"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not writable.</exception>
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

        _stream.Position = 0;
        using BinaryWriter writer = new(_stream, EncodingExtensions.Ansi, leaveOpen: true);
        RefPackEncoder.Encode(writer, buffer, offset, buffer.Length);
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
