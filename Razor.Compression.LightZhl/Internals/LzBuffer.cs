// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl.Internals;

internal class LzBuffer
{
    protected int _bufferPosition;

    protected readonly byte[] Buffer = new byte[EncodingGlobals.BufferSize];

    protected void ToBuffer(byte value)
    {
        Buffer[EncodingUtilities.Wrap(_bufferPosition++)] = value;
    }

    protected void ToBuffer(ReadOnlySpan<byte> source)
    {
        var begin = EncodingUtilities.Wrap(_bufferPosition);
        var end = begin + source.Length;
        if (end > EncodingGlobals.BufferSize)
        {
            var left = EncodingGlobals.BufferSize - begin;
            source[..left].CopyTo(Buffer.AsSpan(begin));
            source[left..].CopyTo(Buffer);
        }
        else
        {
            source.CopyTo(Buffer.AsSpan(begin));
        }

        _bufferPosition += source.Length;
    }

    protected int NumberMatch(int positionWrapped, ReadOnlySpan<byte> pointer, int limitNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limitNumber, EncodingGlobals.BufferSize);

        var canCompareWithoutWrap = EncodingGlobals.BufferSize - positionWrapped >= limitNumber;
        return canCompareWithoutWrap
            ? NumberMatchNoWrap(positionWrapped, pointer, limitNumber)
            : NumberMatchWrap(positionWrapped, pointer, limitNumber);
    }

    private int NumberMatchNoWrap(int positionWrapped, ReadOnlySpan<byte> pointer, int limitNumber)
    {
        for (var i = 0; i < limitNumber; ++i)
        {
            if (Buffer[positionWrapped + i] != pointer[i])
            {
                return i;
            }
        }

        return limitNumber;
    }

    private int NumberMatchWrap(int positionWrapped, ReadOnlySpan<byte> pointer, int limitNumber)
    {
        var shift = EncodingGlobals.BufferSize - positionWrapped;

        // Compare until end of buffer
        for (var i = positionWrapped; i < EncodingGlobals.BufferSize; ++i)
        {
            if (Buffer[i] != pointer[i - positionWrapped])
            {
                return i - positionWrapped;
            }
        }

        // Compare from start of buffer for the remaining bytes
        var remaining = limitNumber - shift;
        for (var i = 0; i < remaining; ++i)
        {
            if (Buffer[i] != pointer[shift + i])
            {
                return shift + i;
            }
        }

        return limitNumber;
    }
}
