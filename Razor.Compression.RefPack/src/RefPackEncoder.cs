// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.RefPack;

internal static class RefPackEncoder
{
    public static void Encode(BinaryWriter writer, byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        // Reset the stream to the beginning of the compressed data.
        writer.BaseStream.Position = 0;
        var uncompressedLength = buffer.Length;
        if (uncompressedLength > 0xFFFFFF)
        {
            writer.WriteUInt16BigEndian(0x90FB);
            writer.WriteUInt32BigEndian((uint)uncompressedLength);
        }
        else
        {
            writer.WriteUInt16BigEndian(0x10FB);
            writer.WriteUInt24BigEndian((uint)uncompressedLength);
        }

        RefCompress(writer, buffer, offset, count);
    }

    private static int HashAt(byte[] source, int index)
    {
        var first = source[index];
        var second = source[index + 1];
        var third = source[index + 2];
        return (((first << 8) | third) ^ (second << 4)) & 0xFFFF;
    }

    private static int MatchLength(
        byte[] source,
        int sourceIndex,
        int destinationIndex,
        int maxMatch
    )
    {
        var current = 0;
        while (
            current < maxMatch
            && source[sourceIndex + current] == source[destinationIndex + current]
        )
        {
            current++;
        }

        return current;
    }

    private static (int BackOffset, int BackLength, int BackCost) FindBestMatch(
        byte[] source,
        int cPtr,
        int fromStart,
        int remaining,
        int[] hashTable,
        int[] link
    )
    {
        var bOffset = 0;
        var bLength = 2;
        var bCost = 2;
        var mLength = int.Min(remaining, 1028);
        var hash = HashAt(source, cPtr);
        var hashOffset = hashTable[hash];
        var minHashOffset = int.Max(cPtr - fromStart - 0x1FFFF, 0);

        if (hashOffset < minHashOffset)
        {
            return (bOffset, bLength, bCost);
        }

        var bestGain = bLength - bCost;
        do
        {
            var tPtr = fromStart + hashOffset;

            // Guard: quick mismatch check to skip expensive matching.
            if (source[cPtr + bLength] != source[tPtr + bLength])
            {
                continue;
            }

            var tLength = MatchLength(source, cPtr, tPtr, mLength);
            if (tLength <= bLength)
            {
                continue;
            }

            var tOffset = (cPtr - 1) - tPtr;
            var tCost = tOffset switch
            {
                < 0x400 when tLength <= 10 => 2, // 2-byte int form
                < 0x4000 when tLength <= 67 => 3, // 3-byte int form
                _ => 4, // 4-byte int form
            };

            var candidateGain = tLength - tCost;
            if (candidateGain <= bestGain)
            {
                continue;
            }

            bLength = tLength;
            bCost = tCost;
            bOffset = tOffset;
            bestGain = candidateGain;

            if (bLength >= 1028)
            {
                break;
            }
        } while ((hashOffset = link[hashOffset & 0x1FFFF]) >= minHashOffset);

        return (bOffset, bLength, bCost);
    }

    private static void InsertIntoHashChain(
        byte[] source,
        int cPtr,
        int fromStart,
        int[] hashTable,
        int[] link
    )
    {
        var hash = HashAt(source, cPtr);
        var hashOffset = cPtr - fromStart;
        link[hashOffset & 0x1FFFF] = hashTable[hash];
        hashTable[hash] = hashOffset;
    }

    private static void EmitPendingLiteralBlocks(
        BinaryWriter writer,
        byte[] source,
        ref int rPtr,
        ref int literalRun
    )
    {
        while (literalRun > 3)
        {
            var tLength = int.Min(112, literalRun & (~3));
            literalRun -= tLength;
            writer.Write((byte)(0xE0 + (tLength >> 2) - 1));
            for (var i = 0; i < tLength; i++)
            {
                writer.Write(source[rPtr + i]);
            }

            rPtr += tLength;
        }
    }

    private static void EmitBestMatchCommand(
        BinaryWriter writer,
        byte[] source,
        int bCost,
        int bOffset,
        int bLength,
        ref int rPtr,
        ref int literalRun
    )
    {
        switch (bCost)
        {
            case 2: // 2-byte int form
            {
                var first = (byte)(
                    (((bOffset >> 8) & 0x1F) << 5)
                    | (((bLength - 3) & 0x07) << 2)
                    | (literalRun & 0x03)
                );

                var second = (byte)(bOffset & 0xFF);

                writer.Write(first);
                writer.Write(second);
                break;
            }
            case 3: // 3-byte int form
            {
                var first = (byte)(0x80 + (bLength - 4));
                var second = (byte)(((literalRun & 0x03) << 6) | ((bOffset >> 8) & 0x3F));
                var third = (byte)(bOffset & 0xFF);

                writer.Write(first);
                writer.Write(second);
                writer.Write(third);
                break;
            }
            default: // 4-byte very int form
            {
                var first = (byte)(
                    0xC0
                    | (((bOffset >> 16) & 0x01) << 4)
                    | ((((bLength - 5) >> 8) & 0x03) << 2)
                    | (literalRun & 0x03)
                );

                var second = (byte)((bOffset >> 8) & 0xFF);
                var third = (byte)(bOffset & 0xFF);
                var fourth = (byte)((bLength - 5) & 0xFF);

                writer.Write(first);
                writer.Write(second);
                writer.Write(third);
                writer.Write(fourth);
                break;
            }
        }

        if (literalRun == 0)
        {
            return;
        }

        for (var i = 0; i < literalRun; i++)
        {
            writer.Write(source[rPtr + i]);
        }

        rPtr += literalRun;
        literalRun = 0;
    }

    /// <summary>Updates the hash chains and the linked list used for finding matches during the compression process after a match has been found and processed.</summary>
    /// <param name="source">The input buffer containing the data being compressed.</param>
    /// <param name="cPtr">A reference to the current pointer in the buffer indicating the position being processed.</param>
    /// <param name="fromStart">The starting offset within the input buffer, used as a reference for calculating positions.</param>
    /// <param name="bLength">The length of the match for which the hash chains need to be updated.</param>
    /// <param name="hashTable">The hash table used for quick lookup of recent positions for matches.</param>
    /// <param name="link">The linked list structure storing previous positions mapped to specific hashes, enabling the chaining process for match discovery.</param>
    private static void UpdateHashChainsAfterMatch(
        byte[] source,
        ref int cPtr,
        int fromStart,
        int bLength,
        int[] hashTable,
        int[] link
    )
    {
        for (var i = 0; i < bLength; i++)
        {
            var newHash = HashAt(source, cPtr);
            var position = cPtr - fromStart;
            link[position & 0x1FFFF] = hashTable[newHash];
            hashTable[newHash] = position;
            cPtr++;
        }
    }

    /// <summary>Compresses data using the RefPack compression algorithm and writes the compressed output to the provided binary writer.</summary>
    /// <param name="writer">The <see cref="BinaryWriter" /> used to write the compressed data.</param>
    /// <param name="buffer">The input buffer containing the data to be compressed.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin compression.</param>
    /// <param name="count">The number of bytes to compress starting from the specified offset.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="count"/> is negative, or if the sum of <paramref name="offset"/> and <paramref name="count"/> exceeds the length of <paramref name="buffer"/>.</exception>
    private static void RefCompress(BinaryWriter writer, byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        var hashTable = new int[0x10000];
        var link = new int[0x20000];
        Array.Fill(hashTable, -1);

        var cPtr = offset;
        var rPtr = offset;
        var literalRun = 0;
        var length = count - 4;

        while (length >= 0)
        {
            var (bOffset, bLength, bCost) = FindBestMatch(
                buffer,
                cPtr,
                offset,
                length,
                hashTable,
                link
            );

            if (bCost >= bLength || length < 4)
            {
                InsertIntoHashChain(buffer, cPtr, offset, hashTable, link);

                literalRun++;
                cPtr++;
                length--;
            }
            else
            {
                EmitPendingLiteralBlocks(writer, buffer, ref rPtr, ref literalRun);

                EmitBestMatchCommand(
                    writer,
                    buffer,
                    bCost,
                    bOffset,
                    bLength,
                    ref rPtr,
                    ref literalRun
                );

                UpdateHashChainsAfterMatch(buffer, ref cPtr, offset, bLength, hashTable, link);

                rPtr = cPtr;
                length -= bLength;
            }
        }
    }
}
