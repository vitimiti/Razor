// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.BinaryTree;

internal static class BinaryTreeDecoder
{
    public static long Decode(BinaryReader reader, byte[] buffer, int offset, int count)
    {
        if (!BinaryTreeDecoderUtilities.IsBinaryTreeCompressed(reader.BaseStream))
        {
            throw new ArgumentException(
                "The stream is not BinaryTree compressed data.",
                nameof(reader)
            );
        }

        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        // Reset the stream to the beginning of the compressed data.
        reader.BaseStream.Position = 0;
        var uncompressedSize = GetUncompressedSize(reader);
        if (uncompressedSize != BinaryTreeDecoderUtilities.GetUncompressedSize(reader.BaseStream))
        {
            throw new InvalidOperationException(
                "The uncompressed size does not match the expected value."
            );
        }

        // Don't write the uncompressed size to the buffer.
        // Start writing from the current stream position onwards.
        var toWrite = uint.Min((uint)count, uncompressedSize);
        var startOffset = offset;

        var clueTable = new sbyte[0x100];
        var leftNodes = new byte[0x100];
        var rightNodes = new byte[0x100];

        var clue = unchecked((sbyte)reader.ReadByte());
        clueTable[clue] = 1; // Mark clue as special
        var nodeCount = reader.ReadByte();
        InitializeTables(reader, clueTable, leftNodes, rightNodes, nodeCount);

        while (offset - startOffset < toWrite)
        {
            var node = reader.ReadByte();
            var currentClue = clueTable[node];
            if (TryAndProcessZeroClue(currentClue, node, buffer, ref offset))
            {
                continue;
            }

            if (
                TryAndProcessNegativeClue(
                    currentClue,
                    node,
                    buffer,
                    ref offset,
                    clueTable,
                    leftNodes,
                    rightNodes
                )
            )
            {
                continue;
            }

            if (TryAndProcessNode(node, buffer, ref offset))
            {
                continue;
            }

            break;
        }

        return offset - startOffset;
    }

    private static uint GetUncompressedSize(BinaryReader reader)
    {
        var packType = reader.ReadUInt16BigEndian();
        if (packType is 0x47FB)
        {
            // Add offset
            _ = reader.ReadUInt24BigEndian();
        }

        var length = reader.ReadUInt24BigEndian();
        return length;
    }

    private static void InitializeTables(
        BinaryReader reader,
        sbyte[] clueTable,
        byte[] leftNodes,
        byte[] rightNodes,
        int nodeCount
    )
    {
        for (var i = 0; i < nodeCount; i++)
        {
            var node = reader.ReadByte();
            leftNodes[node] = reader.ReadByte();
            rightNodes[node] = reader.ReadByte();
            clueTable[node] = -1;
        }
    }

    private static bool TryAndProcessZeroClue(
        sbyte currentClue,
        byte node,
        byte[] buffer,
        ref int offset
    )
    {
        if (currentClue != 0)
        {
            return false;
        }

        buffer[offset++] = node;
        return true;
    }

    private static void Chase(
        byte[] buffer,
        sbyte[] clueTable,
        byte[] leftNodes,
        byte[] rightNodes,
        ref int offset,
        byte node
    )
    {
        // Use the stack to avoid recursion.
        var stack = new Stack<byte>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();
            if (clueTable[currentNode] != 0)
            {
                // Push right first so left is processed first (LIFO).
                stack.Push(rightNodes[currentNode]);
                stack.Push(leftNodes[currentNode]);
            }
            else
            {
                buffer[offset++] = currentNode;
            }
        }
    }

    private static bool TryAndProcessNegativeClue(
        sbyte clue,
        byte node,
        byte[] buffer,
        ref int offset,
        sbyte[] clueTable,
        byte[] leftNodes,
        byte[] rightNodes
    )
    {
        if (clue >= 0)
        {
            return false;
        }

        Chase(buffer, clueTable, leftNodes, rightNodes, ref offset, leftNodes[node]);
        Chase(buffer, clueTable, leftNodes, rightNodes, ref offset, rightNodes[node]);
        return true;
    }

    private static bool TryAndProcessNode(byte node, byte[] buffer, ref int offset)
    {
        if (node == 0)
        {
            return false;
        }

        buffer[offset++] = node;
        return true;
    }
}
