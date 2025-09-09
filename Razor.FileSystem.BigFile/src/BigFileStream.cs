// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.FileSystem.BigFile;

public sealed class BigFileStream : Stream
{
    private readonly byte[] _buffer;
    private readonly int _start;
    private readonly int _length;
    private int _pos;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;

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

    internal static BigFileStream FromSharedBuffer(byte[] sharedBuffer, int start, int length)
    {
        return new BigFileStream(sharedBuffer, start, length);
    }

    internal static BigFileStream FromOwnedBuffer(byte[] ownedBuffer)
    {
        return new BigFileStream(ownedBuffer, 0, ownedBuffer.Length);
    }

    public override void Flush()
    {
        // no-op
    }

    public override int Read(byte[] buffer, int offset, int count)
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

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Read-only stream.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Read-only stream.");
    }
}
