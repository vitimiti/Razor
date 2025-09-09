// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl;

internal sealed class LightZhlDecoder
{
    private const int BufferBits = 16;
    private const int BufferSize = 1 << BufferBits;
    private const int BufferMask = BufferSize - 1;
    private const int Min = 4;
    private const int SymbolsCount = 256 + 16 + 2;

    private readonly DecodingGroup[] _groupTable = new DecodingGroup[16];
    private readonly short[] _symbolTable = new short[SymbolsCount];
    private readonly short[] _stat = new short[SymbolsCount];
    private readonly byte[] _buf = new byte[BufferSize];

    private int _bufPos;
    private uint _bits;
    private int _nBits;

    // csharpier-ignore
    private static short[] InitialSymbolTable =>
    [
        256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 0, 32, 48, 255, 1, 2, 3, 4, 5,
        6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 33, 34, 35,
        36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64,
        65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92,
        93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
        117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138,
        139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160,
        161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182,
        183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204,
        205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226,
        227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248,
        249, 250, 251, 252, 253, 254, 272, 273
    ];

    // csharpier-ignore
    private static DecodingGroup[] InitialGroups =>
    [
        new(2, 0), new(3, 4), new(3, 12), new(4, 20), new(4, 36), new(4, 52), new(4, 68), new(4, 84), new(4, 100),
        new(4, 116), new(4, 132), new(4, 148), new(4, 164), new(5, 180), new(5, 212), new(5, 244),
    ];

    // csharpier-ignore
    private static DecodingMatchOverItem[] MatchOverTable =>
    [
        new(1, 8), new(2, 10), new(3, 14), new(4, 22), new(5, 38), new(6, 70), new(7, 134), new(8, 262),
    ];

    // csharpier-ignore
    private static DecodingDisplayPrefixItem[] DisplayPrefixTable =>
    [
        new(0, 0), new(0, 1), new(1, 2), new(2, 4), new(3, 8), new(4, 16), new(5, 32), new(6, 64),
    ];

    public LightZhlDecoder()
    {
        InitializeDefaultTables();
        _bufPos = 0;
        _bits = 0;
        _nBits = 0;
    }

    public bool Decode(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        out int consumed,
        out int written
    )
    {
        consumed = 0;
        written = 0;

        var sourceIndex = 0;
        var destinationIndex = 0;

        _bits = 0;
        _nBits = 0;

        while (true)
        {
            var outcome = ProcessStep(source, destination, ref sourceIndex, ref destinationIndex);
            if (outcome == DecodingStepOutcome.Continue)
            {
                continue;
            }

            if (outcome == DecodingStepOutcome.Finished)
            {
                break;
            }

            // Failed
            return false;
        }

        consumed = sourceIndex;
        written = destinationIndex;
        return true;
    }

    private void InitializeDefaultTables()
    {
        // Initial symbol table
        for (var i = 0; i < SymbolsCount; i++)
        {
            _symbolTable[i] = InitialSymbolTable[i];
        }

        // Initial groups
        for (var i = 0; i < 16; i++)
        {
            _groupTable[i] = InitialGroups[i];
        }
    }

    private static int Wrap(int pos)
    {
        return pos & BufferMask;
    }

    private void ToBuffer(byte value)
    {
        _buf[Wrap(_bufPos++)] = value;
    }

    private void ToBuffer(ReadOnlySpan<byte> source)
    {
        var begin = Wrap(_bufPos);
        var end = begin + source.Length;
        if (end > BufferSize)
        {
            var left = BufferSize - begin;
            source[..left].CopyTo(_buf.AsSpan(begin));
            source[left..].CopyTo(_buf);
        }
        else
        {
            source.CopyTo(_buf.AsSpan(begin));
        }

        _bufPos += source.Length;
    }

    private void BufferCopy(Span<byte> destination, int position, int size)
    {
        var begin = Wrap(position);
        var end = begin + size;
        if (end > BufferSize)
        {
            var left = BufferSize - begin;
            _buf.AsSpan(begin, left).CopyTo(destination);
            _buf.AsSpan(0, size - left).CopyTo(destination[left..]);
        }
        else
        {
            _buf.AsSpan(begin, size).CopyTo(destination);
        }
    }

    private int GetBits(ReadOnlySpan<byte> source, ref int sourceIndex, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 8);
        if (_nBits < count)
        {
            if (sourceIndex >= source.Length)
            {
                _nBits = 0;
                return -1;
            }

            _bits |= (uint)(source[sourceIndex++] << (24 - _nBits));
            _nBits += 8;
        }

        var ret = (int)(_bits >> (32 - count));
        _bits <<= count;
        _nBits -= count;
        return ret;
    }

    private bool TryReadGroupSymbol(ReadOnlySpan<byte> source, ref int sourceIndex, out int symbol)
    {
        symbol = -1;

        var groupValue = GetBits(source, ref sourceIndex, 4);
        if (groupValue < 0)
        {
            return false;
        }

        ref var group = ref _groupTable[groupValue];
        if (group.NumberOfBits == 0)
        {
            var pos = group.Position;
            if ((uint)pos >= SymbolsCount)
            {
                return false;
            }

            symbol = _symbolTable[pos];
            return true;
        }

        var got = GetBits(source, ref sourceIndex, group.NumberOfBits);
        if (got < 0)
        {
            return false;
        }

        var pos2 = group.Position + got;
        if ((uint)pos2 >= SymbolsCount)
        {
            return false;
        }

        symbol = _symbolTable[pos2];
        return true;
    }

    private bool TryDecodeMatchOver(
        int symbol,
        ReadOnlySpan<byte> source,
        ref int sourceIndex,
        out int matchOver
    )
    {
        // 256..263 map to 0..7
        if (symbol < 256 + 8)
        {
            matchOver = symbol - 256;
            return true;
        }

        var index = symbol - 256 - 8;
        if ((uint)index >= MatchOverTable.Length)
        {
            matchOver = 0;
            return false;
        }

        ref readonly var item = ref MatchOverTable[index];
        var extra = GetBits(source, ref sourceIndex, item.NumberOfExtraBits);
        if (extra < 0)
        {
            matchOver = 0;
            return false;
        }

        matchOver = item.Base + extra;
        return true;
    }

    private bool TryReadDisplacement(
        ReadOnlySpan<byte> source,
        ref int sourceIndex,
        out int display
    )
    {
        display = 0;

        var prefix = GetBits(source, ref sourceIndex, 3);
        if (prefix < 0)
        {
            return false;
        }

        ref readonly var item = ref DisplayPrefixTable[prefix];
        var bitsCount = item.NumberOfBits + (BufferBits - 7);

        if (bitsCount > 8)
        {
            bitsCount -= 8;
            var hi = GetBits(source, ref sourceIndex, 8);
            if (hi < 0)
            {
                return false;
            }

            display |= (hi << bitsCount);
        }

        var lo = GetBits(source, ref sourceIndex, bitsCount);
        if (lo < 0)
        {
            return false;
        }

        display |= lo;
        display += item.Display << (BufferBits - 7);

        return display is >= 0 and < BufferSize;
    }

    private bool TryCopyMatch(
        Span<byte> destination,
        ref int destinationIndex,
        int display,
        int matchOver
    )
    {
        var matchLength = matchOver + Min;
        if (destinationIndex + matchLength > destination.Length)
        {
            return false;
        }

        var pos = _bufPos - display;
        if (matchLength < display)
        {
            // Non-overlapping copy from ring buffer
            BufferCopy(destination.Slice(destinationIndex, matchLength), pos, matchLength);
        }
        else
        {
            // Overlapping copy: first copy 'display' bytes from ring buffer, then self-extend
            BufferCopy(destination.Slice(destinationIndex, display), pos, display);
            var span = destination.Slice(destinationIndex, matchLength);
            for (var i = 0; i < matchLength - display; ++i)
            {
                span[display + i] = span[i];
            }
        }

        ToBuffer(destination.Slice(destinationIndex, matchLength));
        destinationIndex += matchLength;
        return true;
    }

    private bool ValidateSymbolAndUpdateStat(int symbol)
    {
        if ((uint)symbol >= SymbolsCount)
        {
            return false;
        }

        if (_stat[symbol] < short.MaxValue)
        {
            _stat[symbol]++;
        }

        return true;
    }

    private static void ShellSort(Span<DecodingTempHuffmanStat> source)
    {
        // Matches the hardcoded gap sequence from the original code
        // h = 40, 13, 4, 1 over a 1-based array in C++; adapt for 0-based here
        var count = source.Length;
        int[] gaps = [40, 13, 4, 1];
        foreach (var h in gaps)
        {
            for (var i = h; i < count; i++)
            {
                var value = source[i];
                var j = i;
                while (j >= h && value < source[j - h])
                {
                    source[j] = source[j - h];
                    j -= h;
                }

                source[j] = value;
            }
        }
    }

    private void RecalculateSymbolsSorted()
    {
        // Build temp array with (index, statValue)
        Span<DecodingTempHuffmanStat> tmp = stackalloc DecodingTempHuffmanStat[SymbolsCount];
        for (var j = 0; j < SymbolsCount; j++)
        {
            tmp[j] = new DecodingTempHuffmanStat((short)j, _stat[j]);
            _stat[j] = (short)(_stat[j] >> 1);
        }

        // Shell sort as in original (descending by n, then by i)
        ShellSort(tmp);

        // Symbol table becomes "sorted indices" by frequency
        for (var i = 0; i < SymbolsCount; i++)
        {
            _symbolTable[i] = tmp[i].I;
        }
    }

    private bool ReadNewGroupsLayout(ReadOnlySpan<byte> source, ref int sourceIndex)
    {
        var lastNBits = 0;
        var pos = 0;
        for (var i = 0; i < 16; i++)
        {
            var count = 0;
            while (true)
            {
                var bit = GetBits(source, ref sourceIndex, 1);
                if (bit < 0)
                {
                    return false;
                }

                if (bit != 0)
                {
                    break;
                }

                count++;
            }

            lastNBits += count;
            _groupTable[i] = new DecodingGroup(lastNBits, pos);
            pos += 1 << lastNBits;
        }

        return pos < SymbolsCount + 255;
    }

    private bool RecalculateAndReadGroups(ReadOnlySpan<byte> source, ref int sourceIndex)
    {
        RecalculateSymbolsSorted();
        return ReadNewGroupsLayout(source, ref sourceIndex);
    }

    private DecodingStepOutcome ProcessStep(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        ref int sourceIndex,
        ref int destinationIndex
    )
    {
        if (
            !TryReadGroupSymbol(source, ref sourceIndex, out var symbol)
            || !ValidateSymbolAndUpdateStat(symbol)
        )
        {
            return DecodingStepOutcome.Failed;
        }

        switch (symbol)
        {
            // Literal
            case < 256 when destinationIndex >= destination.Length:
                return DecodingStepOutcome.Failed;
            case < 256:
            {
                var b = (byte)symbol;
                destination[destinationIndex++] = b;
                ToBuffer(b);
                return DecodingStepOutcome.Continue;
            }
            // Recalc stats and groups
            case SymbolsCount - 2:
                return RecalculateAndReadGroups(source, ref sourceIndex)
                    ? DecodingStepOutcome.Continue
                    : DecodingStepOutcome.Failed;
            // End marker
            case SymbolsCount - 1:
                return DecodingStepOutcome.Finished;
        }

        // Match path
        if (!TryDecodeMatchOver(symbol, source, ref sourceIndex, out var matchOver))
        {
            return DecodingStepOutcome.Failed;
        }

        if (!TryReadDisplacement(source, ref sourceIndex, out var display))
        {
            return DecodingStepOutcome.Failed;
        }

        return TryCopyMatch(destination, ref destinationIndex, display, matchOver)
            ? DecodingStepOutcome.Continue
            : DecodingStepOutcome.Failed;
    }
}
