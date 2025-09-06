// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.RefPack;

/// <summary>
///     Provides functionality to compress data using the RefPack compression
///     algorithm.
/// </summary>
internal static class RefPackEncoder
{
    /// <summary>
    ///     Encodes and compresses a specified range of data from the provided
    ///     buffer into a stream using the RefPack compression algorithm.
    /// </summary>
    /// <param name="stream">
    ///     The target stream where the encoded and compressed data will be
    ///     written.
    /// </param>
    /// <param name="buffer">
    ///     The input buffer containing the data to be encoded and compressed.
    /// </param>
    /// <param name="offset">
    ///     The zero-based byte offset in the buffer at which to begin reading
    ///     data.
    /// </param>
    /// <param name="count">
    ///     The number of bytes to read from the buffer starting from the
    ///     specified offset.
    /// </param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    ///     Thrown if <paramref name="offset"/> or <paramref name="count"/> is
    ///     negative, or if the sum of <paramref name="offset"/> and
    ///     <paramref name="count"/> exceeds the length of
    ///     <paramref name="buffer"/>.
    /// </exception>
    public static void Encode(Stream stream, byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        using BinaryWriter writer = new(stream, EncodingExtensions.Ansi, leaveOpen: true);
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

    /// <summary>
    ///     Calculates a hash value for the specified position in the source
    ///     array using a combination of the bytes at the given index and its
    ///     subsequent positions.
    /// </summary>
    /// <param name="source">
    ///     The byte array from which the hash value will be calculated.
    /// </param>
    /// <param name="index">
    ///     The zero-based index in the source array from which to start
    ///     generating the hash value.
    /// </param>
    /// <returns>
    ///     An integer representing the calculated hash value derived from the
    ///     specified position in the source array.
    /// </returns>
    private static int HashAt(byte[] source, int index)
    {
        var first = source[index];
        var second = source[index + 1];
        var third = source[index + 2];
        return (((first << 8) | third) ^ (second << 4)) & 0xFFFF;
    }

    /// <summary>
    ///     Determines the maximum match length between two regions of a source
    ///     buffer, starting at the specified indices and limited by a maximum
    ///     permitted match length.
    /// </summary>
    /// <param name="source">
    ///     The source buffer in which the comparison will be performed.
    /// </param>
    /// <param name="sourceIndex">
    ///     The starting index of the first region within the source buffer.
    /// </param>
    /// <param name="destinationIndex">
    ///     The starting index of the second region within the source buffer.
    /// </param>
    /// <param name="maxMatch">
    ///     The maximum number of bytes to compare between the two regions.
    /// </param>
    /// <returns>
    ///     The length of the match between the two regions in bytes, up to the
    ///     specified maximum match length.
    /// </returns>
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

    /// <summary>
    ///     Finds the best match for a sequence in the given source buffer by
    ///     analyzing possible matches based on the current position, hash
    ///     values, and a linked list for fast lookups, and calculates the
    ///     optimal parameters for compression.
    /// </summary>
    /// <param name="source">
    ///     The source buffer containing the data to search for a match.
    /// </param>
    /// <param name="cPtr">
    ///     The current position in the source buffer where the search begins.
    /// </param>
    /// <param name="fromStart">
    ///     The starting position in the source buffer used as a reference for
    ///     relative offsets.
    /// </param>
    /// <param name="remaining">
    ///     The number of bytes remaining to be processed in the source buffer
    ///     starting from <paramref name="cPtr"/>.
    /// </param>
    /// <param name="hashTable">
    ///     An integer array used as a hash table for quick lookup of potential
    ///     matches in the source buffer.
    /// </param>
    /// <param name="link">
    ///     A linked list structure represented as an integer array for
    ///     resolving hash collisions and chaining entries in the hash table.
    /// </param>
    /// <returns>
    ///     A tuple consisting of:
    ///     <list type="bullet">
    ///         <item><c>bOffset</c>: The relative offset of the best match.</item>
    ///         <item><c>bLength</c>: The length of the best match.</item>
    ///         <item><c>bCost</c>: The cost in bytes to encode the match.</item>
    ///     </list>
    /// </returns>
    private static (int bOffset, int bLength, int bCost) FindBestMatch(
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

    /// <summary>
    ///     Inserts a position into the hash chain structure, updating hash
    ///     table and link entries used for efficient data compression and
    ///     matching.
    /// </summary>
    /// <param name="source">
    ///     The byte array containing the data to be processed and linked into
    ///     the hash chain.
    /// </param>
    /// <param name="cPtr">
    ///     The index of the current data position in the source array to be
    ///     added to the hash chain.
    /// </param>
    /// <param name="fromStart">
    ///     The starting index in the source array, used to compute the relative
    ///     position within the hash chain.
    /// </param>
    /// <param name="hashTable">
    ///     The hash table that maintains an array of hash values, enabling
    ///     rapid lookups of data positions.
    /// </param>
    /// <param name="link">
    ///     The array that represents the chain of links, where each position
    ///     refers to the previous position associated with the same hash value.
    /// </param>
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

    /// <summary>
    ///     Writes any pending literal blocks from the source buffer to the
    ///     writer, breaking them into manageable chunks if necessary.
    /// </summary>
    /// <param name="writer">
    ///     The writer to which the compressed literal blocks will be written.
    /// </param>
    /// <param name="source">
    ///     The source buffer containing the literal data to be written.
    /// </param>
    /// <param name="rPtr">
    ///     A reference to the current read position in the source buffer.
    /// </param>
    /// <param name="literalRun">
    ///     A reference to the count of contiguous literal bytes yet to be
    ///     written from the source buffer.
    /// </param>
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

    /// <summary>
    ///     Emits the best match command for a compressed data block into the
    ///     provided binary writer, encoding the match information and its
    ///     associated literals.
    /// </summary>
    /// <param name="writer">
    ///     The binary writer to which the encoded match command and associated literals are written.
    /// </param>
    /// <param name="source">
    ///     The source buffer containing the data to be compressed.
    /// </param>
    /// <param name="bCost">
    ///     The cost metric associated with the best match, used to determine
    ///     the encoding format of the command.
    /// </param>
    /// <param name="bOffset">
    ///     The offset of the best match from the current position in the source
    ///     buffer.
    /// </param>
    /// <param name="bLength">
    ///     The length of the best match found in the source buffer.
    /// </param>
    /// <param name="rPtr">
    ///     A reference to the current read pointer in the source buffer, which
    ///     is updated to reflect the processed data.
    /// </param>
    /// <param name="literalRun">
    ///     A reference to the count of consecutive unmatched literals to be
    ///     emitted, which is reset to zero once the literals are written.
    /// </param>
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

    /// <summary>
    ///     Updates the hash chains and the linked list used for finding matches
    ///     during the compression process after a match has been found and
    ///     processed.
    /// </summary>
    /// <param name="source">
    ///     The input buffer containing the data being compressed.
    /// </param>
    /// <param name="cPtr">
    ///     A reference to the current pointer in the buffer indicating the
    ///     position being processed.
    /// </param>
    /// <param name="fromStart">
    ///     The starting offset within the input buffer, used as a reference for
    ///     calculating positions.
    /// </param>
    /// <param name="bLength">
    ///     The length of the match for which the hash chains need to be
    ///     updated.
    /// </param>
    /// <param name="hashTable">
    ///     The hash table used for quick lookup of recent positions for
    ///     matches.
    /// </param>
    /// <param name="link">
    ///     The linked list structure storing previous positions mapped to
    ///     specific hashes, enabling the chaining process for match discovery.
    /// </param>
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

    /// <summary>
    ///     Compresses data using the RefPack compression algorithm and writes
    ///     the compressed output to the provided binary writer.
    /// </summary>
    /// <param name="writer">
    ///     The <see cref="BinaryWriter" /> used to write the compressed data.
    /// </param>
    /// <param name="buffer">
    ///     The input buffer containing the data to be compressed.
    /// </param>
    /// <param name="offset">
    ///     The zero-based byte offset in the buffer at which to begin
    ///     compression.
    /// </param>
    /// <param name="count">
    ///     The number of bytes to compress starting from the specified offset.
    /// </param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    ///     Thrown if <paramref name="offset"/> or <paramref name="count"/> is
    ///     negative, or if the sum of <paramref name="offset"/> and
    ///     <paramref name="count"/> exceeds the length of
    ///     <paramref name="buffer"/>.
    /// </exception>
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
