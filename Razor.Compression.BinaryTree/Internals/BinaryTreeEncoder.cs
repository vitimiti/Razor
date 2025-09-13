// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.BinaryTree.Internals;

internal static class BinaryTreeEncoder
{
    private const int Codes = 0x100;
    private const int BigNumber = 32_000;
    private const int Slopage = 0x4000;

    public static void Encode(BinaryWriter writer, byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        // Reset the stream to the beginning of the compressed data.
        writer.BaseStream.Position = 0;
        writer.WriteUInt16BigEndian(0x46FB);
        writer.WriteUInt24BigEndian((uint)count);

        EncodeContext context = new()
        {
            Writer = writer,
            Length = (uint)count,
            Masks = { [0] = 0 },
            PackBits = 0,
            WorkPattern = 0,
            PointerLength = 0,
        };

        for (var i = 1; i < 17; i++)
        {
            context.Masks[i] = (context.Masks[i - 1] << 1) + 1;
        }

        var buffer1Size = count * 3 / 2 + Slopage;
        var buffer2Size = count * 3 / 2 + Slopage;
        context.Buffer1 = new byte[buffer1Size];
        context.Buffer2 = new byte[buffer2Size];

        context.BufferBase = new byte[count];
        Buffer.BlockCopy(buffer, offset, context.BufferBase, 0, count);
        context.BufferEndExclusive = count;

        const uint Passes = 256;
        const uint MultiMax = 32;
        TreePack(ref context, Passes, MultiMax);
    }

    private static void WriteBits(ref EncodeContext context, uint bitPattern, int length)
    {
        if (length > 16)
        {
            WriteBits(ref context, bitPattern >> 16, length - 16);
            WriteBits(ref context, bitPattern & 0xFFFF, 16);
            return;
        }

        context.PackBits += length;
        context.WorkPattern += (bitPattern & context.Masks[length]) << (24 - context.PackBits);
        while (context.PackBits >= 7)
        {
            var b = (byte)(context.WorkPattern >> 16);
            context.Writer.Write(b);

            context.WorkPattern <<= 8;
            context.PackBits -= 8;
            context.PointerLength++;
        }
    }

    private static void AdjacentCount(byte[] source, int start, int endExclusive, short[] count)
    {
        if (endExclusive - start <= 1)
        {
            return;
        }

        int i = source[start];
        for (var position = start + 1; position < endExclusive; position++)
        {
            i = ((i << 8) | source[position]) & 0xFFFF;
            count[i]++;
        }
    }

    private static void ClearCount(byte[] tryQueue, short[] count)
    {
        for (var i = 0; i < 256; i++)
        {
            if (tryQueue[i] == 0)
            {
                continue;
            }

            tryQueue[i] = 1;
            var baseIndex = i * 256;
            for (var j = 0; j < 256; j++)
            {
                count[baseIndex + j] = 0;
            }
        }
    }

    private static void JoinNodes(
        ref EncodeContext context,
        byte[] cluePointer,
        byte[] rightPointer,
        byte[] joinPointer,
        int clue
    )
    {
        var source = context.BufferBase;
        var destination = ReferenceEquals(source, context.Buffer1)
            ? context.Buffer2
            : context.Buffer1;
        var sourceIndex = 0;
        var bend = context.BufferEndExclusive;
        var destinationIndex = 0;

        while (sourceIndex <= bend)
        {
            byte b;
            do
            {
                b = GetAtSourceIndex(sourceIndex);
                destination[destinationIndex++] = b;
                sourceIndex++;
            } while (cluePointer[b] == 0);

            var pointerIndex = b;
            switch (cluePointer[pointerIndex])
            {
                case 1:
                {
                    if (
                        sourceIndex < bend
                        && GetAtSourceIndex(sourceIndex) == rightPointer[pointerIndex]
                    )
                    {
                        destination[destinationIndex - 1] = joinPointer[pointerIndex];
                        sourceIndex++;
                    }

                    break;
                }
                case 3:
                    destination[destinationIndex - 1] = (byte)clue;
                    destination[destinationIndex++] = pointerIndex;
                    break;
                default:
                {
                    if (sourceIndex <= bend)
                    {
                        destination[destinationIndex++] = GetAtSourceIndex(sourceIndex);
                        sourceIndex++;
                    }

                    break;
                }
            }
        }

        context.BufferBase = destination;
        context.BufferEndExclusive = destinationIndex - 2;

        return;

        byte GetAtSourceIndex(int index)
        {
            return index == bend ? (byte)clue : source[index];
        }
    }

    private static bool IsCandidate(short count, uint threshold, byte tryQueueEntry)
    {
        return count > threshold && tryQueueEntry != 0;
    }

    private static uint InsertBest(
        uint[] bestNumber,
        uint[] bestValue,
        uint bestSize,
        int index,
        uint value
    )
    {
        var k = bestSize;
        while (bestValue[k - 1] < value)
        {
            bestNumber[k] = bestNumber[k - 1];
            bestValue[k] = bestValue[k - 1];
            k--;
        }

        bestNumber[k] = (uint)index;
        bestValue[k] = value;

        if (bestSize < 0x30)
        {
            bestSize++;
        }

        return bestSize;
    }

    private static uint TrimBestSize(uint bestSize, uint[] bestValue, int ratio)
    {
        while (bestValue[bestSize - 1] < (bestValue[1] / (uint)ratio))
        {
            bestSize--;
        }

        return bestSize;
    }

    private static uint ComputeThreshold(uint bestSize, uint[] bestValue, int ratio)
    {
        return bestSize < 0x30 ? bestValue[1] / (uint)ratio : bestValue[bestSize - 1];
    }

    private static uint FindBest(
        short[] countPointer,
        byte[] tryQueue,
        uint[] bestNumber,
        uint[] bestValue,
        int ratio
    )
    {
        var bestSize = 1U;
        var threshold = 3U;
        var position = 0U;

        for (var i = 0; i < 256; i++)
        {
            if (tryQueue[i] == 0)
            {
                position += 256;
                continue;
            }

            for (var j = 0; j < 256; j++)
            {
                var index = (int)(position + (uint)j);
                var count = countPointer[index];

                if (!IsCandidate(count, threshold, tryQueue[j]))
                {
                    continue;
                }

                var value = (uint)count;

                bestSize = InsertBest(bestNumber, bestValue, bestSize, index, value);
                bestSize = TrimBestSize(bestSize, bestValue, ratio);
                threshold = ComputeThreshold(bestSize, bestValue, ratio);
            }

            position += 256;
        }

        return bestSize;
    }

    private static void InitializeCount2(EncodeContext context, uint[] count2)
    {
        Array.Clear(count2);
        for (var i = 0; i < context.BufferEndExclusive; i++)
        {
            count2[context.BufferBase[i]]++;
        }

        // Don't use 0 for clue or node, so that it is reserved.
        count2[0] = BigNumber;
    }

    private static void InitializeQueues(
        uint[] count2,
        byte[] tryQueue,
        byte[] freeQueue,
        EncodeContext context
    )
    {
        Array.Clear(context.ClueQueue);
        for (var i = 0; i < Codes; i++)
        {
            freeQueue[i] = 1;
            tryQueue[i] = (byte)(count2[i] > 3 ? 1 : 0);
        }
    }

    private static void InitializeSortPointers(uint[] sortPointer)
    {
        for (var i = 0; i < Codes; i++)
        {
            sortPointer[i] = (uint)i;
        }
    }

    private static void BubbleSortByFrequency(uint[] count2, uint[] sortPointer)
    {
        var swapped = true;
        while (swapped)
        {
            swapped = false;
            for (var i = 1; i < Codes; i++)
            {
                var first = sortPointer[i];
                var second = sortPointer[i - 1];
                var firstCount = count2[first];
                var secondCount = count2[second];

                // Keep stable order when counts equal
                var inOrder =
                    firstCount > secondCount || (firstCount == secondCount && first <= second);
                if (inOrder)
                {
                    continue;
                }

                sortPointer[i] = second;
                sortPointer[i - 1] = first;
                swapped = true;
            }
        }
    }

    private static bool IsPairCandidate(TreePackState state, int leftNode, int rightNode)
    {
        return state.TryQueue[leftNode] == 1 && state.TryQueue[rightNode] == 1;
    }

    private static void AdvanceToNextFreePointer(TreePackState state)
    {
        while (
            state.FreePointer < Codes && state.FreeQueue[state.SortPointer[state.FreePointer]] == 0
        )
        {
            state.FreePointer++;
        }
    }

    private static void ApplyJoin(
        ref EncodeContext context,
        ref TreePackState state,
        int leftNode,
        int rightNode,
        int joinNode
    )
    {
        // Update queues and clue table
        state.FreeQueue[joinNode] = 0;
        state.TryQueue[joinNode] = 2;
        context.ClueQueue[joinNode] = 3;

        state.FreeQueue[leftNode] = 0;
        state.TryQueue[leftNode] = 2;
        context.ClueQueue[leftNode] = 1;
        context.Right[leftNode] = (byte)rightNode;
        context.Join[leftNode] = (byte)joinNode;

        state.FreeQueue[rightNode] = 0;
        state.TryQueue[rightNode] = 2;

        // Record tree
        state.BtNode[state.BtSize] = (uint)joinNode;
        state.BtLeft[state.BtSize] = (uint)leftNode;
        state.BtRight[state.BtSize] = (uint)rightNode;
        state.BtSize++;
    }

    private static uint TryJoinBestPairs(
        ref EncodeContext context,
        uint multiMax,
        ref TreePackState state,
        uint bestSize
    )
    {
        var j = 1U;

        for (var i = 1U; i < bestSize; i++)
        {
            var leftNode = (int)((state.BestNumber[i] >> 8) & 255);
            var rightNode = (int)(state.BestNumber[i] & 255);

            if (!IsPairCandidate(state, leftNode, rightNode))
            {
                continue;
            }

            AdvanceToNextFreePointer(state);
            if (state.FreePointer >= Codes)
            {
                continue;
            }

            var joinNode = (int)state.SortPointer[state.FreePointer];
            var cost = 3U + state.Count2[joinNode];
            var save = state.BestValue[i];
            if (cost >= save)
            {
                continue;
            }

            state.BestJoin[j] = (byte)joinNode;
            state.BestNumber[j] = state.BestNumber[i];
            j++;

            ApplyJoin(ref context, ref state, leftNode, rightNode, joinNode);

            if (j > multiMax)
            {
                break;
            }
        }

        return j;
    }

    private static void RunMainPass(
        ref EncodeContext context,
        ref uint passes,
        uint multiMax,
        ref TreePackState state,
        int clue
    )
    {
        var doMore = passes;
        while (doMore != 0)
        {
            ClearCount(state.TryQueue, state.Count);
            AdjacentCount(context.BufferBase, 0, context.BufferEndExclusive, state.Count);

            var bestSize = FindBest(
                state.Count,
                state.TryQueue,
                state.BestNumber,
                state.BestValue,
                state.Ratio
            );
            doMore = 0;
            if (bestSize <= 1)
            {
                continue;
            }

            var j = TryJoinBestPairs(ref context, multiMax, ref state, bestSize);

            bestSize = j;
            if (bestSize <= 1)
            {
                continue;
            }

            JoinNodes(ref context, context.ClueQueue, context.Right, context.Join, clue);

            for (var i = 1U; i < bestSize; i++)
            {
                var leftNode = (int)((state.BestNumber[i] >> 8) & 255);
                int joinNode = state.BestJoin[i];
                context.ClueQueue[leftNode] = 0;
                context.ClueQueue[joinNode] = 0;
            }

            doMore = --passes;
        }
    }

    private static void WriteHeaderAndTables(
        ref EncodeContext context,
        int clue,
        uint btSize,
        uint[] btNode,
        uint[] btLeft,
        uint[] btRight
    )
    {
        WriteBits(ref context, (uint)clue, 8);
        WriteBits(ref context, btSize, 8);

        for (var i = 0U; i < btSize; i++)
        {
            WriteBits(ref context, btNode[i], 8);
            WriteBits(ref context, btLeft[i], 8);
            WriteBits(ref context, btRight[i], 8);
        }
    }

    private static void WritePayloadAndFooter(ref EncodeContext context, int clue)
    {
        for (var i = 0; i < context.BufferEndExclusive; i++)
        {
            WriteBits(ref context, context.BufferBase[i], 8);
        }

        WriteBits(ref context, (uint)clue, 8);
        WriteBits(ref context, 0, 8);

        // Flush bits (write 7 zeros)
        WriteBits(ref context, 0, 7);
    }

    private static void TreePack(ref EncodeContext context, uint passes, uint multiMax)
    {
        var state = new TreePackState();

        Array.Copy(context.BufferBase, 0, context.Buffer1, 0, context.Length);
        context.BufferBase = context.Buffer1;
        context.BufferEndExclusive = (int)context.Length;

        InitializeCount2(context, state.Count2);
        InitializeQueues(state.Count2, state.TryQueue, state.FreeQueue, context);
        InitializeSortPointers(state.SortPointer);
        BubbleSortByFrequency(state.Count2, state.SortPointer);

        // Pick clue byte
        var clue = (int)state.SortPointer[state.FreePointer++];
        state.FreeQueue[clue] = 0;
        state.TryQueue[clue] = 0;

        if (state.Count2[clue] != 0)
        {
            context.ClueQueue[clue] = 3;
            JoinNodes(ref context, context.ClueQueue, context.Right, context.Join, clue);
        }

        context.ClueQueue[clue] = 2;
        // Best arrays
        state.BestValue[0] = uint.MaxValue;

        // Main pass
        RunMainPass(ref context, ref passes, multiMax, ref state, clue);

        WriteHeaderAndTables(
            ref context,
            clue,
            state.BtSize,
            state.BtNode,
            state.BtLeft,
            state.BtRight
        );

        WritePayloadAndFooter(ref context, clue);
    }

    private sealed class EncodeContext
    {
        public int PackBits { get; set; }
        public uint WorkPattern { get; set; }
        public int PointerLength { get; set; }
        public uint Length { get; init; }
        public int BufferEndExclusive { get; set; }

        public uint[] Masks { get; } = new uint[0x11];
        public byte[] ClueQueue { get; } = new byte[Codes];
        public byte[] Right { get; } = new byte[Codes];
        public byte[] Join { get; } = new byte[Codes];
        public byte[] Buffer1 { get; set; } = [];
        public byte[] Buffer2 { get; set; } = [];
        public byte[] BufferBase { get; set; } = [];

        public required BinaryWriter Writer { get; init; }
    }

    private sealed class TreePackState
    {
        public short[] Count { get; } = new short[0x10000];
        public uint[] Count2 { get; } = new uint[Codes];
        public byte[] TryQueue { get; } = new byte[Codes];
        public byte[] FreeQueue { get; } = new byte[Codes];
        public byte[] BestJoin { get; } = new byte[Codes];
        public uint[] BestNumber { get; } = new uint[Codes];
        public uint[] BestValue { get; } = new uint[Codes];
        public uint[] BtNode { get; } = new uint[Codes];
        public uint[] BtLeft { get; } = new uint[Codes];
        public uint[] BtRight { get; } = new uint[Codes];
        public uint[] SortPointer { get; } = new uint[Codes];

        public uint FreePointer { get; set; }
        public uint BtSize { get; set; }
        public int Ratio { get; } = 2;
    }
}
