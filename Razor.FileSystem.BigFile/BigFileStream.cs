// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Razor.FileSystem.BigFile;

/// <summary>A sealed stream implementation for handling large file data with read-only access.</summary>
public sealed class BigFileStream : Stream
{
    private readonly byte[] _buffer;
    private readonly int _start;
    private readonly int _length;
    private int _pos;

    /// <summary>Gets a value indicating whether the stream supports reading.</summary>
    /// <value>Always returns <c>true</c> as the <see cref="BigFileStream"/> is read-only.</value>
    public override bool CanRead => true;

    /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
    /// <value>Always returns <c>true</c> as the <see cref="BigFileStream"/> supports seek operations.</value>
    public override bool CanSeek => true;

    /// <summary>Gets a value indicating whether the stream supports writing.</summary>
    /// <value>Always returns <c>false</c> as the <see cref="BigFileStream"/> is read-only.</value>
    public override bool CanWrite => false;

    /// <summary>Gets the length of the stream in bytes.</summary>
    /// <value>The total number of bytes in the stream.</value>
    public override long Length => _length;

    /// <summary>Gets or sets the position within the current stream.</summary>
    /// <value>The current position as a zero-based byte offset in the stream.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the position is set to a value less than zero or beyond the length of the stream.</exception>
    public override long Position
    {
        get => _pos;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _length);
            _pos = (int)value;
        }
    }

    private BigFileStream(byte[] buffer, int start, int length)
    {
        _buffer = buffer;
        _start = start;
        _length = length;
        _pos = 0;
    }

    internal static BigFileStream FromSharedBuffer(byte[] sharedBuffer, int start, int length) =>
        new(sharedBuffer, start, length);

    internal static BigFileStream FromOwnedBuffer(byte[] ownedBuffer) => new(ownedBuffer, 0, ownedBuffer.Length);

    /// <summary>Flushes the stream. This method is a no-op for the BigFileStream as it is a read-only stream.</summary>
    public override void Flush()
    {
        // no-op
    }

    /// <summary>Reads a sequence of bytes from the stream and advances the position by the number of bytes read.</summary>
    /// <param name="buffer">The buffer to store the read bytes.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the read bytes.</param>
    /// <param name="count">The maximum number of bytes to read from the stream.</param>
    /// <returns>The total number of bytes read into the buffer. Returns 0 if the end of the stream is reached.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the offset or count is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when the offset and count do not specify a valid range in the buffer.</exception>
    public override int Read([NotNull] byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        var remaining = _length - _pos;
        if (remaining <= 0)
        {
            return 0;
        }

        if (count > remaining)
        {
            count = remaining;
        }

        Buffer.BlockCopy(_buffer, _start + _pos, buffer, offset, count);
        _pos += count;
        return count;
    }

    /// <summary>Sets the position within the stream.</summary>
    /// <param name="offset">The point relative to <paramref name="origin"/> to seek to.</param>
    /// <param name="origin">Specifies the reference point to begin seeking from. Must be one of the <see cref="SeekOrigin"/> values.</param>
    /// <returns>The new position within the stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="origin"/> is invalid, or the resulting position is less than zero or exceeds the stream length.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPos = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _pos + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        ArgumentOutOfRangeException.ThrowIfNegative(newPos);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPos, _length);

        _pos = (int)newPos;
        return _pos;
    }

    /// <summary>Sets the length of the stream. This operation is not supported as the BigFileStream is a read-only stream.</summary>
    /// <param name="value">The desired length of the stream.</param>
    /// <exception cref="NotSupportedException">Thrown unconditionally as this stream does not support modification.</exception>
    public override void SetLength(long value) => throw new NotSupportedException("Read-only stream.");

    /// <summary>Writes data to the stream. This operation is not supported for the BigFileStream as it is a read-only stream.</summary>
    /// <param name="buffer">The buffer containing data to write.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to write to the stream.</param>
    /// <exception cref="NotSupportedException">Always thrown to indicate that writing is not supported.</exception>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Read-only stream.");
}
