// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl.Internals;

internal sealed class BitWriter(Span<byte> destination)
{
    private readonly byte[] _dst = destination.ToArray();

    private uint _bits;
    private int _bitsCount;

    public int BytesWritten { get; private set; }

    public void PutBits(int codeBits, uint code)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(codeBits, 16);

        _bits |= code << (32 - _bitsCount - codeBits);
        _bitsCount += codeBits;
        if (_bitsCount < 16)
        {
            return;
        }

        WriteByte((byte)(_bits >> 24));
        WriteByte((byte)(_bits >> 16));
        _bitsCount -= 16;
        _bits <<= 16;
    }

    public void FlushEos()
    {
        while (_bitsCount > 0)
        {
            WriteByte((byte)(_bits >> 24));
            _bitsCount -= 8;
            _bits <<= 8;
        }
    }

    private void WriteByte(byte b)
    {
        if (BytesWritten >= _dst.Length)
        {
            throw new InvalidOperationException("Output buffer too small.");
        }

        _dst[BytesWritten++] = b;
    }
}
