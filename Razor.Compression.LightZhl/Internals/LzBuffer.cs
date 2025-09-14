// -----------------------------------------------------------------------
// <copyright file="LzBuffer.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal class LzBuffer
{
    protected int BufferPosition { get; set; }

    protected byte[] Buffer { get; } = new byte[EncodingGlobals.BufferSize];

    protected void ToBuffer(byte value) => Buffer[EncodingUtilities.Wrap(BufferPosition++)] = value;

    protected void ToBuffer(ReadOnlySpan<byte> source)
    {
        var begin = EncodingUtilities.Wrap(BufferPosition);
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

        BufferPosition += source.Length;
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
