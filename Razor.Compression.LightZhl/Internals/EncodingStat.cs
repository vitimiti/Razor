// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl.Internals;

internal sealed class EncodingStat
{
    private const int RecalculateLength = 0x1000;

    internal short[] Stat { get; } = new short[EncodingGlobals.SymbolCount];
    internal int NextStat { get; set; } = RecalculateLength;
    internal EncodingSymbol[] SymbolTable { get; } = new EncodingSymbol[EncodingGlobals.SymbolCount];

    internal EncodingStat() => EncodingGlobals.InitialSymbolTable.CopyTo(SymbolTable, 0);

    internal void CalculateStat(int[] groups)
    {
        Span<EncodingTempHuffStat> source = stackalloc EncodingTempHuffStat[EncodingGlobals.SymbolCount];

        var total = BuildAndSortTemp(source);
        NextStat = RecalculateLength;

        var pos = CalculateFirst14Groups(source, total, groups);

        (var bestBitsCount, var bestBitsCount15) = ComputeBestBitsForLastTwoGroups(source, pos);
        AddGroup(groups, 14, bestBitsCount);
        AddGroup(groups, 15, bestBitsCount15);

        AssignCodes(source, groups);
    }

    private static void AddGroup(int[] groups, int group, int bitsCount)
    {
        int j;
        for (j = group; j > 0 && bitsCount < groups[j - 1]; --j)
        {
            groups[j] = groups[j - 1];
        }

        groups[j] = bitsCount;
    }

    private int BuildAndSortTemp(Span<EncodingTempHuffStat> source)
    {
        var total = 0;
        for (var j = 0; j < EncodingGlobals.SymbolCount; j++)
        {
            source[j] = new EncodingTempHuffStat((short)j, Stat[j]);
            total += Stat[j];
            Stat[j] = (short)(Stat[j] >> 1);
        }

        EncodingUtilities.ShellSort(source);
        return total;
    }

    private static (int BitsCount, int Count, int Advanced) ComputeGroupBits(
        Span<EncodingTempHuffStat> source,
        int position,
        int averageGroup
    )
    {
        var i = 0;
        var n = 0;
        var nn = 0;

        var bitsCount = 0;
        while (true)
        {
            var over = 0;
            var itemsCount = 1 << bitsCount;

            if (position + i + itemsCount > EncodingGlobals.SymbolCount)
            {
                itemsCount = EncodingGlobals.SymbolCount - position;
                over = 1;
            }

            for (; i < itemsCount; ++i)
            {
                nn += source[position + i].N;
            }

            var shouldBreak = over != 0 || bitsCount >= 8 || nn > averageGroup;
            if (shouldBreak)
            {
                // Decide whether to keep current nBits or use the previous one without mutating the loop counter
                int chosenBits;
                if (bitsCount == 0 || int.Abs(n - averageGroup) > int.Abs(nn - averageGroup))
                {
                    n = nn;
                    chosenBits = bitsCount;
                }
                else
                {
                    chosenBits = bitsCount - 1;
                }

                var advanced = 1 << chosenBits;
                return (chosenBits, n, advanced);
            }

            n = nn;
            bitsCount++;
        }
    }

    private static int CalculateFirst14Groups(Span<EncodingTempHuffStat> source, int total, int[] groups)
    {
        var pos = 0;
        var totalCount = 0;
        for (var group = 0; group < 14; ++group)
        {
            var avgGroup = (total - totalCount) / (16 - group);
            (var bitsCount, var count, var advanced) = ComputeGroupBits(source, pos, avgGroup);
            AddGroup(groups, group, bitsCount);
            totalCount += count;
            pos += advanced;
        }

        return pos;
    }

    private static int SumRemaining(Span<EncodingTempHuffStat> source, int position)
    {
        var sum = 0;
        for (var j = position; j < EncodingGlobals.SymbolCount; ++j)
        {
            sum += source[j].N;
        }

        return sum;
    }

    private static int AccumulateUpTo(
        Span<EncodingTempHuffStat> source,
        int basePosition,
        int fromExclusive,
        int toExclusive
    )
    {
        var sum = 0;
        for (var t = fromExclusive; t < toExclusive; ++t)
        {
            sum += source[basePosition + t].N;
        }

        return sum;
    }

    private static int BitsToCover(int items)
    {
        // Minimal number of bits b such that (1 << b) >= items; capped by 9 to allow early pruning by caller.
        if (items <= 1)
        {
            return 0;
        }

        var bits = 0;
        var capacity = 1;
        while (capacity < items && bits <= 9)
        {
            capacity <<= 1;
            bits++;
        }

        return bits;
    }

    private static (int BestBits, int BestBits15) ComputeBestBitsForLastTwoGroups(
        Span<EncodingTempHuffStat> source,
        int position
    )
    {
        var bestBitsCount = 0;
        var bestBitsCount15 = 0;
        var best = int.MaxValue;

        var ii = 0;
        var nnn = 0;

        var left = SumRemaining(source, position);

        for (
            int numberOfBits = 0, nItems = 1;
            position + ii + nItems <= EncodingGlobals.SymbolCount;
            ++numberOfBits, nItems <<= 1
        )
        {
            // Accumulate from current ii up to nItems (exclusive)
            nnn += AccumulateUpTo(source, position, ii, nItems);
            ii = nItems;

            var nItems15 = EncodingGlobals.SymbolCount - (position + ii);
            var numberOfBits15 = BitsToCover(nItems15);

            if (numberOfBits > 8 || numberOfBits15 > 8)
            {
                continue;
            }

            var cost = (nnn * numberOfBits) + ((left - nnn) * numberOfBits15);
            if (cost < best)
            {
                best = cost;
                bestBitsCount = numberOfBits;
                bestBitsCount15 = numberOfBits15;
            }
            else
            {
                // PERF: costs will only increase after this point
                break;
            }
        }

        return (bestBitsCount, bestBitsCount15);
    }

    private void AssignCodes(Span<EncodingTempHuffStat> source, int[] groups)
    {
        var pos = 0;
        for (var j = 0; j < 16; ++j)
        {
            var bitsCount = groups[j];
            var itemsCount = 1 << bitsCount;
            var maxK = int.Min(itemsCount, EncodingGlobals.SymbolCount - pos);
            for (var k = 0; k < maxK; ++k)
            {
                int symbol = source[pos + k].I;
                SymbolTable[symbol].NumberOfBits = (short)(bitsCount + 4);
                SymbolTable[symbol].Code = (ushort)((j << bitsCount) | k);
            }

            pos += 1 << bitsCount;
        }
    }
}
