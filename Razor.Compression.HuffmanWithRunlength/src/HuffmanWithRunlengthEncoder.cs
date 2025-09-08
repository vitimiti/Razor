// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.HuffmanWithRunlength;

internal static class HuffmanWithRunlengthEncoder
{
    private const int BigNumber = 32_000;
    private const int TreeSize = 520;
    private const int NumberOfCodes = 256;
    private const int MaxBits = 16;
    private const int RepeatTable = 252;

    public static void Encode(BinaryWriter writer, byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        // Reset the stream to the beginning of the compressed data.
        writer.BaseStream.Position = 0;

        // We only compress the requested slice [offset, offset+count).
        var source = new byte[count];
        Buffer.BlockCopy(buffer, offset, source, 0, count);

        var compressed = PackFile(source, source.Length);
        writer.Write(compressed);
    }

    private static void WriteBits(EncodeContext context, uint bitPattern, uint len)
    {
        // Replace recursion with an explicit segment stack to process left-to-right.
        Span<uint> masks = context.Masks;
        var segments = new Stack<(uint pattern, uint len)>();

        // Break the long pattern into 16-bit chunks, push in reverse so we pop in correct order.
        while (len > 16)
        {
            segments.Push((bitPattern & 0xFFFF, 16));
            bitPattern >>= 16;
            len -= 16;
        }
        segments.Push((bitPattern, len));

        while (segments.Count > 0)
        {
            var (pat, ln) = segments.Pop();

            context.PackBits += ln;
            context.WorkPattern += (pat & masks[(int)ln]) << (24 - (int)context.PackBits);
            while (context.PackBits > 7)
            {
                var b = (byte)(context.WorkPattern >> 16);
                context.OutBytes.Add(b);
                context.WorkPattern <<= 8;
                context.PackBits -= 8;
                context.PackedLength++;
            }
        }
    }

    private static void TreeChase(EncodeContext context, uint node, uint bits)
    {
        // Iterative traversal (explicit stack) instead of recursion.
        var stack = new Stack<(uint node, uint bits)>();
        stack.Push((node, bits));

        while (stack.Count > 0)
        {
            var (n, d) = stack.Pop();
            if (n < NumberOfCodes)
            {
                context.BitsArray[n] = d;
            }
            else
            {
                // Push right then left so left is processed first if desired...
                // order does not matter for lengths, only correctness of d+1.
                stack.Push((context.TreeRight[n], d + 1));
                stack.Push((context.TreeLeft[n], d + 1));
            }
        }
    }

    private static int BuildFrequencyLists(
        EncodeContext context,
        Span<uint> listCount,
        Span<uint> listPointer
    )
    {
        var i1 = 0;
        listCount[i1++] = 0;

        for (var i = 0U; i < NumberOfCodes; i++)
        {
            context.BitsArray[i] = 99;
            if (context.Count[i] == 0)
            {
                continue;
            }

            listCount[i1] = context.Count[i];
            listPointer[i1++] = i;
        }

        return i1;
    }

    private static (NodePair First, NodePair Second) FindTwoSmallest(
        Span<uint> listCount,
        int count
    )
    {
        // Find indices of the two smallest values in listCount[0..count-1]
        var idx1 = -1;
        var idx2 = -1;
        var v1 = uint.MaxValue;
        var v2 = uint.MaxValue;

        for (var i = 0; i < count; i++)
        {
            var v = listCount[i];
            if (v < v1)
            {
                v2 = v1;
                idx2 = idx1;

                v1 = v;
                idx1 = i;
            }
            else if (v < v2)
            {
                v2 = v;
                idx2 = i;
            }
        }

        return (new NodePair(unchecked((uint)idx1), v1), new NodePair(unchecked((uint)idx2), v2));
    }

    private static void MergeNodes(
        EncodeContext context,
        ref MergeState state,
        in NodePair first,
        in NodePair second
    )
    {
        context.TreeLeft[state.Nodes] = state.ListPointer[(int)first.Ptr];
        context.TreeRight[state.Nodes] = state.ListPointer[(int)second.Ptr];

        state.ListCount[(int)first.Ptr] = first.Val + second.Val;
        state.ListPointer[(int)first.Ptr] = state.Nodes;

        state.ListCount[(int)second.Ptr] = state.ListCount[--state.I1];
        state.ListPointer[(int)second.Ptr] = state.ListPointer[state.I1];

        ++state.Nodes;
    }

    private static void MakeTree(EncodeContext context)
    {
        uint nodes = NumberOfCodes;

        Span<uint> listCount = stackalloc uint[NumberOfCodes + 2];
        Span<uint> listPointer = stackalloc uint[NumberOfCodes + 2];

        var i1 = BuildFrequencyLists(context, listCount, listPointer);
        context.Codes = (uint)(i1 - 1);

        if (i1 <= 2)
        {
            TreeChase(context, listPointer[(int)context.Codes], 1);
            return;
        }

        var state = new MergeState(listCount, listPointer, ref nodes, ref i1);
        while (i1 > 2)
        {
            var (first, second) = FindTwoSmallest(listCount, i1);
            MergeNodes(context, ref state, in first, in second);
        }

        TreeChase(context, nodes - 1, 0);
    }

    private static int ComputeBaseMin(EncodeContext context, uint remaining)
    {
        return remaining switch
        {
            0 => 0,
            >= RepeatTable => 20,
            _ => (int)context.BitsArray[context.Clue] + 3 + (int)context.RepeatBits[remaining] * 2,
        };
    }

    private static int MinRep(EncodeContext context, uint remaining, uint r)
    {
        // Non-recursive equivalent of the original MinRep using an explicit stack.
        var stack = new Stack<MinRepFrame>();
        stack.Push(
            new MinRepFrame
            {
                Remaining = remaining,
                R = r,
                State = 0,
            }
        );

        var lastResult = 0;
        while (stack.Count > 0)
        {
            var frame = stack.Pop();
            if (frame.R == 0)
            {
                lastResult = ComputeBaseMin(context, frame.Remaining);
                continue;
            }

            switch (frame.State)
            {
                case 0:
                    // Need MinRep(remaining, r-1) first.
                    frame.State = 1;
                    stack.Push(frame); // resume after first child
                    stack.Push(
                        new MinRepFrame
                        {
                            Remaining = frame.Remaining,
                            R = frame.R - 1,
                            State = 0,
                        }
                    );

                    continue;

                case 1:
                    // We have MinRep(remaining, r-1) in lastResult.
                    frame.Min = lastResult;

                    // If this r-bucket is unusable, keep min and return to parent.
                    if (context.Count[context.Clue + frame.R] == 0)
                    {
                        lastResult = frame.Min;
                        continue;
                    }

                    // Otherwise compute MinRep(remaining % r, r-1) and combine later.
                    var use = frame.Remaining / frame.R; // uint
                    var newRemaining = frame.Remaining - (use * frame.R);

                    frame.Use = use;
                    frame.State = 2;
                    stack.Push(frame); // resume after second child
                    stack.Push(
                        new MinRepFrame
                        {
                            Remaining = newRemaining,
                            R = frame.R - 1,
                            State = 0,
                        }
                    );

                    continue;

                case 2:
                    // Combine second child with accumulated cost and compare
                    {
                        var min1 =
                            lastResult
                            + (int)context.BitsArray[context.Clue + frame.R] * (int)frame.Use;

                        lastResult = Math.Min(min1, frame.Min);
                    }

                    break;
            }
        }

        return lastResult;
    }

    private static void WriteNumber(EncodeContext context, uint number)
    {
        uint destinationPointerHuffman;
        uint destinationBase;
        switch (number)
        {
            case < RepeatTable:
                destinationPointerHuffman = context.RepeatBits[number];
                destinationBase = context.RepeatBase[number];
                break;
            case < 508:
                destinationPointerHuffman = 6;
                destinationBase = 252;
                break;
            case < 1020:
                destinationPointerHuffman = 7;
                destinationBase = 508;
                break;
            case < 2044:
                destinationPointerHuffman = 8;
                destinationBase = 1020;
                break;
            case < 4092:
                destinationPointerHuffman = 9;
                destinationBase = 2044;
                break;
            case < 8188:
                destinationPointerHuffman = 10;
                destinationBase = 4092;
                break;
            case < 16380:
                destinationPointerHuffman = 11;
                destinationBase = 8188;
                break;
            case < 32764:
                destinationPointerHuffman = 12;
                destinationBase = 16380;
                break;
            case < 65532:
                destinationPointerHuffman = 13;
                destinationBase = 32764;
                break;
            case < 131068:
                destinationPointerHuffman = 14;
                destinationBase = 65532;
                break;
            case < 262140:
                destinationPointerHuffman = 15;
                destinationBase = 131068;
                break;
            case < 524288:
                destinationPointerHuffman = 16;
                destinationBase = 262140;
                break;
            case < 1048576:
                destinationPointerHuffman = 17;
                destinationBase = 524288;
                break;
            default:
                destinationPointerHuffman = 18;
                destinationBase = 1048576;
                break;
        }

        WriteBits(context, 0x00000001, destinationPointerHuffman + 1);
        WriteBits(context, number - destinationBase, destinationPointerHuffman + 2);
    }

    private static void WriteExp(EncodeContext context, uint code)
    {
        WriteBits(context, context.PatternArray[context.Clue], context.BitsArray[context.Clue]);
        WriteNumber(context, 0);
        WriteBits(context, code, 9);
    }

    private static void WriteCode(EncodeContext context, uint code)
    {
        if (code == context.Clue)
        {
            WriteExp(context, code);
        }
        else
        {
            WriteBits(context, context.PatternArray[code], context.BitsArray[code]);
        }
    }

    private static void Init(EncodeContext context)
    {
        var i = 0U;
        while (i < 4)
        {
            context.RepeatBits[i] = 0;
            context.RepeatBase[i++] = 0;
        }

        while (i < 12)
        {
            context.RepeatBits[i] = 1;
            context.RepeatBase[i++] = 4;
        }

        while (i < 28)
        {
            context.RepeatBits[i] = 2;
            context.RepeatBase[i++] = 12;
        }

        while (i < 60)
        {
            context.RepeatBits[i] = 3;
            context.RepeatBase[i++] = 28;
        }

        while (i < 124)
        {
            context.RepeatBits[i] = 4;
            context.RepeatBase[i++] = 60;
        }

        while (i < 252)
        {
            context.RepeatBits[i] = 5;
            context.RepeatBase[i++] = 124;
        }
    }

    private static uint Pass1CollectCounts(EncodeContext context, byte[] b)
    {
        var idx = 0;
        var prev = 256U;
        while (idx < b.Length)
        {
            uint cur = b[idx++];
            if (cur == prev)
            {
                var reps = 0U;
                var limit = int.Min(idx + 30_000, b.Length);
                while (cur == prev && idx < limit)
                {
                    reps++;
                    cur = b[idx++];
                }

                if (reps < 255)
                {
                    context.Count[512 + reps]++;
                }
                else
                {
                    context.Count[512]++;
                }
            }

            context.Count[cur]++;
            context.Count[((cur + 256 - prev) & 255) + 256]++;
            prev = cur;
        }

        if (context.Count[512] == 0)
        {
            context.Count[512]++;
        }

        // Return least frequent byte index among 0..255
        var leastIdx = 0U;
        for (var i = 1U; i < 256U; i++)
        {
            if (context.Count[i] < context.Count[leastIdx])
            {
                leastIdx = i;
            }
        }

        return leastIdx;
    }

    private static void FindClueBytes(EncodeContext context, uint leastCountIndex)
    {
        context.Clues = 0;
        context.DClues = 0;

        uint nextI;
        var i3 = leastCountIndex;
        for (var i = 0U; i < NumberOfCodes; i = nextI)
        {
            var zeroRun = 0U;
            if (context.Count[i] < context.Count[i3])
            {
                i3 = i;
            }

            var j = i;
            while (j < 256 && context.Count[j] == 0)
            {
                zeroRun++;
                j++;
            }

            nextI = j + 1;
            if (zeroRun < context.DClues)
            {
                continue;
            }

            context.DClue = i;
            context.DClues = zeroRun;
            if (context.DClues < context.Clues)
            {
                continue;
            }

            context.DClue = context.Clue;
            context.DClues = context.Clues;
            context.Clue = i;
            context.Clues = zeroRun;
        }
    }

    private static void ForceClueIfRequested(EncodeContext context, uint opt, uint leastCountIndex)
    {
        if ((opt & 32) == 0 || context.Clues != 0)
        {
            return;
        }

        context.Clues = 1;
        context.Clue = leastCountIndex;
    }

    private static void ApplyOptClueSettings(EncodeContext context, uint opt)
    {
        if ((~opt & 2) != 0)
        {
            if (context.Clues > 1)
            {
                context.Clues = 1;
            }

            if ((~opt & 1) != 0)
            {
                context.Clues = 0;
            }
        }

        if ((~opt & 4) != 0)
        {
            context.DClues = 0;
            return;
        }

        if (context.DClues > 10)
        {
            var tmpClue = context.Clue;
            var tmpClues = context.Clues;

            context.Clue = context.DClue;
            context.Clues = context.DClues;
            context.DClue = tmpClue;
            context.DClues = tmpClues;
        }

        if ((context.Clues * 4) >= context.DClues)
        {
            return;
        }

        context.Clues = context.DClues / 4;
        context.DClues = context.DClues - context.Clues;
        context.Clue = context.DClue + context.DClues;
    }

    private static uint PrepareDeltaClues(EncodeContext context)
    {
        uint threshold = 0;
        if (context.DClues == 0)
        {
            return threshold;
        }

        context.MinDelta = -(int)(context.DClues / 2);
        context.MaxDelta = (int)context.DClues + context.MinDelta;
        threshold = context.Length / 25;

        for (uint i = 1; i <= (uint)context.MaxDelta; i++)
        {
            if (context.Count[256 + i] > threshold)
            {
                context.Count[context.DClue + (i - 1) * 2] = context.Count[256 + i];
            }
        }

        for (uint i = 1; i <= (uint)(-context.MinDelta); i++)
        {
            if (context.Count[512 - i] > threshold)
            {
                context.Count[context.DClue + (i - 1) * 2 + 1] = context.Count[512 - i];
            }
        }

        var last = 0U;
        for (uint i = 0; i < context.DClues; i++)
        {
            if (context.Count[context.DClue + i] != 0)
            {
                last = i;
            }
        }

        var di = (int)context.DClues - (int)last - 1;
        context.DClues -= (uint)di;
        if (context.Clue == context.DClue + context.DClues + (uint)di)
        {
            context.Clue -= (uint)di;
            context.Clues += (uint)di;
        }

        context.MinDelta = -(int)(context.DClues / 2);
        context.MaxDelta = (int)context.DClues + context.MinDelta;
        return threshold;
    }

    private static void CopyRepClueBytes(EncodeContext context)
    {
        if (context.Clues == 0)
        {
            return;
        }

        for (uint i = 0; i < context.Clues; i++)
        {
            context.Count[context.Clue + i] = context.Count[512 + i];
        }
    }

    private static void RemoveImpliedRepCluesIfAny(EncodeContext context)
    {
        if (context.Clues <= 1)
        {
            return;
        }

        for (uint i = 1; i < context.Clues; i++)
        {
            var i1C = i - 1;
            if (i1C > 8)
            {
                i1C = 8;
            }

            if (context.Count[context.Clue + i] == 0)
            {
                continue;
            }

            var minRep = MinRep(context, i, i1C);
            if (
                minRep <= context.BitsArray[context.Clue + i]
                || context.Count[context.Clue + i] * (minRep - context.BitsArray[context.Clue + i])
                    < (i / 2)
            )
            {
                context.Count[context.Clue + i] = 0;
            }
        }
    }

    private static uint GetRepeatNumber(EncodeContext context, uint[] count2, uint rep)
    {
        // If no clues or clue not present in count2, cannot encode as a number here.
        if (context.Clues == 0 || count2[context.Clue] == 0)
        {
            return BigNumber;
        }

        // Baseline 20 if the repeat length exceeds the repeat table capability.
        if (rep >= RepeatTable)
        {
            return 20;
        }

        // Encodable repeat number cost.
        return context.BitsArray[context.Clue] + 3 + context.RepeatBits[rep] * 2;
    }

    private static uint GetRepeatIndexOrBig(EncodeContext context, uint[] count2, uint rep)
    {
        // If only one clue (the repeat number), index encoding is not possible.
        if (context.Clues <= 1)
        {
            return BigNumber;
        }

        var remaining = rep;
        uint cost = 0;
        for (var i = context.Clues - 1; i != 0; i--)
        {
            if (count2[context.Clue + i] == 0)
            {
                continue;
            }

            var rep1 = remaining / i;
            cost += rep1 * context.BitsArray[context.Clue + i];
            remaining -= rep1 * i;
        }

        return remaining != 0 ? BigNumber : cost;
    }

    private static void DistributeRepeatToClueBuckets(
        EncodeContext context,
        uint[] count2,
        uint rep
    )
    {
        var remaining = rep;
        for (var i = context.Clues - 1; i != 0; i--)
        {
            if (count2[context.Clue + i] == 0)
            {
                continue;
            }

            var rep1 = remaining / i;
            remaining -= rep1 * i;
            context.Count[context.Clue + i] += rep1;
        }
    }

    private static void EvaluateRunAndUpdateCounts(
        EncodeContext context,
        uint[] count2,
        uint prev,
        uint rep
    )
    {
        var numberCode = rep * context.BitsArray[prev];

        // Compute alternative encodings costs
        var repeatNumber = GetRepeatNumber(context, count2, rep);
        var repeatIndex = GetRepeatIndexOrBig(context, count2, rep);

        // Prefer the cheapest option
        if (numberCode <= repeatNumber && numberCode <= repeatIndex)
        {
            context.Count[prev] += rep;
            return;
        }

        if (repeatNumber < repeatIndex)
        {
            context.Count[context.Clue]++;
            return;
        }

        // Fallback to indexing via clue buckets
        DistributeRepeatToClueBuckets(context, count2, rep);
    }

    private static bool TrySelectDeltaCodeIndex(
        EncodeContext context,
        uint[] count2,
        uint prev,
        uint cur,
        uint threshold,
        out uint codeIndex
    )
    {
        codeIndex = 0;
        if (context.DClues == 0)
        {
            return false;
        }

        var di = (int)cur - (int)prev;
        if (di > context.MaxDelta || di < context.MinDelta)
        {
            return false;
        }

        var diIndex = ((int)cur - (int)prev - 1) * 2 + (int)context.DClue;
        if (cur < prev)
        {
            diIndex = ((int)prev - (int)cur - 1) * 2 + (int)context.DClue + 1;
        }

        var diu = (uint)diIndex;
        if (count2[diu] <= threshold)
        {
            return false;
        }

        var preferDelta =
            count2[cur] < 4
            || context.BitsArray[diu] < context.BitsArray[cur]
            || context.BitsArray[diu] == context.BitsArray[cur]
                && context.Count[diu] > context.Count[cur];

        if (!preferDelta)
        {
            return false;
        }

        codeIndex = (uint)(
            ((int)cur - (int)prev) >= 0
                ? ((int)cur - (int)prev - 1) * 2 + (int)context.DClue
                : ((int)prev - (int)cur - 1) * 2 + (int)context.DClue + 1
        );

        return true;
    }

    private static void SecondPassRefineCounts(EncodeContext context, byte[] b, uint threshold)
    {
        var count2 = new uint[NumberOfCodes];
        for (var i = 0; i < NumberOfCodes; i++)
        {
            count2[i] = context.Count[i];
            context.Count[i] = 0;
            context.Count[256 + i] = 0;
            context.Count[512 + i] = 0;
        }

        var pos = 0;
        uint prev = 256;
        while (pos < b.Length)
        {
            uint cur = b[pos++];
            if (cur == prev)
            {
                uint rep = 0;
                var limit = int.Min(pos + 30_000, b.Length);
                while (cur == prev && pos < limit)
                {
                    cur = b[pos++];
                    rep++;
                }

                EvaluateRunAndUpdateCounts(context, count2, prev, rep);
            }

            if (TrySelectDeltaCodeIndex(context, count2, prev, cur, threshold, out var codeIndex))
            {
                context.Count[codeIndex]++;
            }
            else
            {
                context.Count[cur]++;
            }

            prev = cur;
        }
    }

    private static (
        uint LongestIndex,
        uint SecondLongestIndex,
        uint LongestLength
    ) FindTwoLongestCodes(EncodeContext context)
    {
        uint longestLen = 0;
        uint i2 = 0; // longest index
        uint i3Max = 0; // second-longest index

        for (var i = 0U; i < NumberOfCodes; i++)
        {
            if (context.Count[i] == 0)
            {
                continue;
            }

            var len = context.BitsArray[i];
            if (len < longestLen)
            {
                continue;
            }

            i3Max = i2;
            i2 = i;
            longestLen = len;
        }

        return (i2, i3Max, longestLen);
    }

    private static uint FindBestShorterThan(EncodeContext context, uint chainsaw)
    {
        uint i1Short = 0;

        // Find first candidate
        while (i1Short < NumberOfCodes)
        {
            if (context.Count[i1Short] != 0 && context.BitsArray[i1Short] < chainsaw)
            {
                break;
            }

            i1Short++;
        }

        // Improve candidate if possible
        for (var i = i1Short; i < NumberOfCodes; i++)
        {
            if (context.Count[i] == 0)
            {
                continue;
            }

            var len = context.BitsArray[i];
            if (len < chainsaw && len > context.BitsArray[i1Short])
            {
                i1Short = i;
            }
        }

        return i1Short;
    }

    private static void ClipBranches(EncodeContext context, uint chainsaw)
    {
        uint longest = 99;
        while (longest > chainsaw)
        {
            var (i2, i3Max, maxLen) = FindTwoLongestCodes(context);
            longest = maxLen;
            if (longest <= chainsaw)
            {
                break;
            }

            var i1Short = FindBestShorterThan(context, chainsaw);

            var newLen = context.BitsArray[i1Short] + 1;
            context.BitsArray[i1Short] = newLen;
            context.BitsArray[i2] = newLen;
            context.BitsArray[i3Max] = context.BitsArray[i3Max] - 1;

            longest = 99;
        }
    }

    private static void EnforceHuffmanOption(EncodeContext context, uint opt)
    {
        if ((~opt & 8) == 0)
        {
            return;
        }

        for (var i = 0; i < NumberOfCodes; i++)
        {
            context.BitsArray[i] = 8;
        }
    }

    private static void CountBitNumbers(EncodeContext context)
    {
        Array.Clear(context.BitNumber, 0, context.BitNumber.Length);
        for (var i = 0; i < NumberOfCodes; i++)
        {
            var bits = context.BitsArray[i];
            if (bits <= MaxBits)
            {
                context.BitNumber[bits]++;
            }
        }
    }

    private static void SortCodesAndAssignPatterns(EncodeContext context)
    {
        var most = 0U;
        var k = 0U;
        for (var bits = 1U; bits <= MaxBits; bits++)
        {
            if (context.BitNumber[bits] == 0)
            {
                continue;
            }

            for (uint code = 0; code < NumberOfCodes; code++)
            {
                if (context.BitsArray[code] == bits)
                {
                    context.SortPointer[k++] = code;
                }
            }

            most = bits;
        }

        context.MostBits = most;

        // Assign canonical bit patterns
        var pattern = 0U;
        var curBits = 0U;
        for (var idx2 = 0U; idx2 < context.Codes; idx2++)
        {
            var code = context.SortPointer[idx2];
            while (curBits < context.BitsArray[code])
            {
                curBits++;
                pattern <<= 1;
            }

            context.PatternArray[code] = pattern;
            pattern++;
        }
    }

    private static void Analysis(EncodeContext context, uint opt, uint chainsaw)
    {
        Array.Clear(context.Count);

        var b = context.Buffer;

        // Pass 1: collect counts (byte, delta, run-length metadata) and find least frequent byte index
        var leastCountIndex = Pass1CollectCounts(context, b);

        // Find clue bytes
        FindClueBytes(context, leastCountIndex);

        // Force a clue byte if requested by opt bit 32 (kept set in opt below)
        ForceClueIfRequested(context, opt, leastCountIndex);

        // Disable/split clues based on opt bits
        ApplyOptClueSettings(context, opt);

        // Copy delta clue bytes and compute threshold
        var threshold = PrepareDeltaClues(context);

        // Copy rep clue bytes
        CopyRepClueBytes(context);

        // First approximation tree
        MakeTree(context);

        // Remove implied rep clues if they are not worthwhile
        RemoveImpliedRepCluesIfAny(context);

        // Pass 2: refine counts using decisions
        SecondPassRefineCounts(context, b, threshold);

        if ((opt & 32) != 0)
        {
            context.Count[context.Clue]++;
        }

        // Second approximation tree
        MakeTree(context);

        // Chainsaw IV branch clipping
        ClipBranches(context, chainsaw);

        // If huffman inhibited (opt bit 8 off), force 8-bit codes
        EnforceHuffmanOption(context, opt);

        // Count bit numbers
        CountBitNumbers(context);

        // Sort, assign canonical bit patterns
        SortCodesAndAssignPatterns(context);
    }

    private static void ResetQueueLeapCodes(EncodeContext context)
    {
        for (uint i = 0; i < NumberOfCodes; i++)
        {
            context.QueueLeapCode[i] = 0;
        }
    }

    private static void EmitBitNumberCounts(EncodeContext context)
    {
        for (uint i = 1; i <= context.MostBits; i++)
        {
            WriteNumber(context, context.BitNumber[i]);
        }
    }

    private static void EmitLeapfrogDeltas(EncodeContext context)
    {
        var i2 = 255U;
        var idx = 0U;
        while (idx < context.Codes)
        {
            var code = context.SortPointer[idx];
            var di = -1;
            do
            {
                i2 = (i2 + 1) & 255;
                if (context.QueueLeapCode[i2] == 0)
                {
                    di++;
                }
            } while (code != i2);

            context.QueueLeapCode[i2] = 1;
            WriteNumber(context, (uint)di);
            idx++;
        }
    }

    private static bool TryComputeRepeatNumberCost(EncodeContext context, uint rep, out uint cost)
    {
        // Requires a clue symbol to exist and be present.
        if (context.Clues == 0 || context.Count[context.Clue] == 0)
        {
            cost = BigNumber;
            return false;
        }

        // Baseline if outside the compact repeat table.
        if (rep >= RepeatTable)
        {
            cost = 20;
            return true;
        }

        cost = context.BitsArray[context.Clue] + 3 + context.RepeatBits[rep] * 2;
        return true;
    }

    private static uint ComputeRepeatIndexCost(EncodeContext context, uint rep)
    {
        if (context.Clues <= 1)
        {
            return BigNumber;
        }

        var remaining = rep;
        var cost = 0U;
        for (var i = context.Clues - 1; i != 0; i--)
        {
            if (context.Count[context.Clue + i] == 0)
            {
                continue;
            }

            var take = remaining / i;
            cost += take * context.BitsArray[context.Clue + i];
            remaining -= take * i;
        }

        return remaining != 0 ? BigNumber : cost;
    }

    private static void EmitIndexViaClueBuckets(EncodeContext context, uint rep)
    {
        var remaining = rep;
        for (var i = context.Clues - 1; i != 0; i--)
        {
            if (context.Count[context.Clue + i] == 0)
            {
                continue;
            }

            var take = remaining / i;
            remaining -= take * i;
            for (var k = 0U; k < take; k++)
            {
                WriteCode(context, context.Clue + i);
            }
        }
    }

    private static void WriteCodeRepeated(EncodeContext context, uint code, uint count)
    {
        for (var i = 0U; i < count; i++)
        {
            WriteCode(context, code);
        }
    }

    private static void EncodeRun(EncodeContext context, uint prev, uint rep)
    {
        const uint RlAdjust = 1U;

        var codeNumber = rep * context.BitsArray[prev];

        // Compute alternative costs (number form and index form).
        var hasRepeatNumber = TryComputeRepeatNumberCost(context, rep, out var repeatNumber);
        if (!hasRepeatNumber)
        {
            repeatNumber = BigNumber;
        }

        var repeatIndex = ComputeRepeatIndexCost(context, rep);

        // Choose cheapest encoding.
        if (codeNumber <= repeatNumber && codeNumber <= repeatIndex)
        {
            WriteCodeRepeated(context, prev, rep);
            return;
        }

        if (repeatNumber < repeatIndex)
        {
            WriteBits(context, context.PatternArray[context.Clue], context.BitsArray[context.Clue]);
            WriteNumber(context, rep - RlAdjust);
            return;
        }

        // Index via clue buckets
        EmitIndexViaClueBuckets(context, rep);
    }

    private static bool TryWriteDelta(EncodeContext context, uint prev, uint cur)
    {
        if (context.DClues == 0)
        {
            return false;
        }

        var di = (int)cur - (int)prev;
        if (di > context.MaxDelta || di < context.MinDelta)
        {
            return false;
        }

        var diIndex = ((int)cur - (int)prev - 1) * 2 + (int)context.DClue;
        if (cur < prev)
        {
            diIndex = ((int)prev - (int)cur - 1) * 2 + (int)context.DClue + 1;
        }

        var diu = (uint)diIndex;
        if (context.BitsArray[diu] >= context.BitsArray[cur])
        {
            return false;
        }

        WriteBits(context, context.PatternArray[diu], context.BitsArray[diu]);
        return true;
    }

    private static void WriteEofAndFlush(EncodeContext context)
    {
        // EOF marker: [clue] 0gn [10]
        WriteBits(context, context.PatternArray[context.Clue], context.BitsArray[context.Clue]);
        WriteNumber(context, 0);
        WriteBits(context, 2, 2);

        // Flush bits
        WriteBits(context, 0, 7);
    }

    private static void Pack(EncodeContext context)
    {
        ResetQueueLeapCodes(context);
        EmitBitNumberCounts(context);
        EmitLeapfrogDeltas(context);

        if (context.Clues == 0)
        {
            context.Clue = BigNumber;
        }

        // Main payload (writes encoded bytes)
        var pos = 0;
        var prev = 256U;
        while (pos < context.Buffer.Length)
        {
            uint cur = context.Buffer[pos++];
            if (cur == prev)
            {
                var rep = 0U;
                var limit = int.Min(pos + 30_000, context.Buffer.Length);
                while (cur == prev && pos < limit)
                {
                    cur = context.Buffer[pos++];
                    rep++;
                }

                EncodeRun(context, prev, rep);
            }

            var wroteDelta = TryWriteDelta(context, prev, cur);
            prev = cur;
            if (!wroteDelta)
            {
                WriteCode(context, cur);
            }
        }

        WriteEofAndFlush(context);
    }

    private static byte[] PackFile(byte[] input, int uncompressedLength)
    {
        EncodeContext context = new()
        {
            PackBits = 0,
            WorkPattern = 0,
            Masks = { [0] = 0 },
        };

        for (var i = 1; i < 17; i++)
        {
            context.Masks[i] = (context.Masks[i - 1] << 1) + 1;
        }

        Init(context);

        // Input
        context.Buffer = input;
        context.FLength = input.Length;
        context.Length = (uint)context.FLength;

        // Output init
        context.PackBits = 0;
        context.WorkPattern = 0;
        context.PackedLength = 0;

        const uint Opt = 57U | 49U; // same as the original code

        // Build model
        Analysis(context, Opt, chainsaw: 15);

        // Write standard header (fb6/fb4 family), matching original implementation
        if (uncompressedLength > 0xFFFFFF)
        {
            // 32-bit header
            ushort packType = 0xB0FB;
            if (uncompressedLength == input.Length)
            {
                WriteBits(context, packType, 16);
            }
            else
            {
                packType = 0xB1FB;
                WriteBits(context, packType, 16);
                WriteBits(context, (uint)uncompressedLength, 32);
            }

            WriteBits(context, (uint)input.Length, 32);
        }
        else
        {
            // 24-bit header
            ushort packType = 0x30FB;
            if (uncompressedLength == input.Length)
            {
                WriteBits(context, packType, 16);
            }
            else
            {
                packType = 0x31FB;
                WriteBits(context, packType, 16);
                WriteBits(context, (uint)uncompressedLength, 24);
            }

            WriteBits(context, (uint)input.Length, 24);
        }

        // Payload
        Pack(context);

        return context.OutBytes.ToArray();
    }

    private sealed class EncodeContext
    {
        public byte[] QueueLeapCode { get; } = new byte[NumberOfCodes];
        public uint[] Count { get; } = new uint[768];
        public uint[] BitNumber { get; } = new uint[MaxBits + 1];
        public uint[] RepeatBits { get; } = new uint[RepeatTable];
        public uint[] RepeatBase { get; } = new uint[RepeatTable];
        public uint[] TreeLeft { get; } = new uint[TreeSize];
        public uint[] TreeRight { get; } = new uint[TreeSize];
        public uint[] BitsArray { get; } = new uint[NumberOfCodes];
        public uint[] PatternArray { get; } = new uint[NumberOfCodes];
        public uint[] Masks { get; } = new uint[17];

        public uint PackBits { get; set; }
        public uint WorkPattern { get; set; }

        // Input buffer
        public byte[] Buffer { get; set; } = [];
        public int FLength { get; set; }
        public uint MostBits { get; set; }
        public uint Codes { get; set; }
        public uint Clue { get; set; }
        public uint DClue { get; set; }
        public uint Clues { get; set; }
        public uint DClues { get; set; }
        public int MinDelta { get; set; }
        public int MaxDelta { get; set; }
        public uint PackedLength { get; set; }
        public uint Length { get; set; }
        public uint[] SortPointer { get; } = new uint[NumberOfCodes];

        // Output sink (byte accumulator)
        public List<byte> OutBytes { get; } = new(1024);
    }

    private readonly struct NodePair(uint ptr, uint val)
    {
        public uint Ptr { get; } = ptr;
        public uint Val { get; } = val;
    }

    private ref struct MergeState
    {
        public readonly Span<uint> ListCount;
        public readonly Span<uint> ListPointer;
        public readonly ref uint Nodes;
        public readonly ref int I1;

        public MergeState(Span<uint> listCount, Span<uint> listPointer, ref uint nodes, ref int i1)
        {
            ListCount = listCount;
            ListPointer = listPointer;
            Nodes = ref nodes;
            I1 = ref i1;
        }
    }

    private struct MinRepFrame
    {
        public uint Remaining { get; init; }
        public uint R { get; init; }
        public byte State { get; set; } // 0: before first child, 1: after first child, 2: after second child
        public int Min { get; set; } // result from MinRep(remaining, r-1)
        public uint Use { get; set; } // remaining / r
    }
}
