// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl.Internals;

internal sealed class EncodingSystem(EncodingStat stat, BitWriter bitWriter)
{
    internal const int MaxMatchOver = 517;
    internal const int MaxRaw = 64;

    private void CallStat()
    {
        // avoid recursion: nextStat >= 2
        stat.NextStat = 2;

        PutSymbol(EncodingGlobals.SymbolCount - 2);

        var groups = new int[16];
        stat.CalculateStat(groups);

        var lastNBits = 0;
        for (var i = 0; i < 16; ++i)
        {
            var bitsCount = groups[i];
            var delta = bitsCount - lastNBits;
            lastNBits = bitsCount;

            // emit delta steps of zeroes followed by a one
            bitWriter.PutBits(delta + 1, 1);
        }
    }

    private void PutSymbol(ushort symbol)
    {
        if (--stat.NextStat <= 0)
        {
            CallStat();
        }

        stat.Stat[symbol]++;

        ref EncodingSymbol item = ref stat.SymbolTable[symbol];
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(item.NumberOfBits, 0);

        bitWriter.PutBits(item.NumberOfBits, item.Code);
    }

    public void PutRaw(ReadOnlySpan<byte> source)
    {
        foreach (var b in source)
        {
            PutSymbol(b);
        }
    }

    public void PutMatch(ReadOnlySpan<byte> sourceStart, int rawCount, int matchOver, int display)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rawCount, MaxRaw);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(matchOver, MaxMatchOver);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(display, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(display, EncodingGlobals.BufferSize);

        PutRaw(sourceStart[..rawCount]);

        var newMatchOver = matchOver - 38;
        ref readonly EncodingMatchOverItem item = ref EncodingGlobals.MatchOverEncodeTable[newMatchOver >> 5];
        PutSymbol((ushort)(item.Symbol + 4));
        bitWriter.PutBits(item.NumberOfBits + 4, (uint)((item.Bits << 4) | (newMatchOver & 0x1F)));

        ref readonly EncodingDisplayItem displayItem = ref EncodingGlobals.DisplayEncodeTable[
            display >> (EncodingGlobals.BufferBits - 7)
        ];

        var nBits = displayItem.NumberOfBits + (EncodingGlobals.BufferBits - 7);
        var bits = (uint)(
            (displayItem.Bits << (EncodingGlobals.BufferBits - 7))
            | (display & ((1 << (EncodingGlobals.BufferBits - 7)) - 1))
        );

        if (nBits > 16)
        {
            bitWriter.PutBits(nBits - 16, bits >> 16);
            bitWriter.PutBits(16, bits & 0xFFFF);
        }
        else
        {
            bitWriter.PutBits(nBits, bits);
        }
    }

    public int Finish()
    {
        PutSymbol(EncodingGlobals.SymbolCount - 1);
        bitWriter.FlushEos();
        return bitWriter.BytesWritten;
    }
}
