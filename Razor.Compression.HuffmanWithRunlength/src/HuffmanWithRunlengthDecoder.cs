// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.HuffmanWithRunlength;

internal static class HuffmanWithRunlengthDecoder
{
    public static long Decode(BinaryReader reader, byte[] buffer, int offset, int count)
    {
        if (
            !HuffmanWithRunlengthDecoderUtilities.IsHuffmanWithRunlengthCompressed(
                reader.BaseStream
            )
        )
        {
            throw new ArgumentException(
                "The stream is not a HuffmanWithRunlength stream.",
                nameof(reader)
            );
        }

        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        // Reset the stream to the beginning of the compressed data.
        reader.BaseStream.Position = 0;
        var compressed = reader.ReadBytes((int)reader.BaseStream.Length);
        var decompressed = Decompress(compressed, out var actualUncompressedSize);
        var expectedSize = HuffmanWithRunlengthDecoderUtilities.GetUncompressedSize(
            reader.BaseStream
        );

        if (
            actualUncompressedSize != expectedSize
            || actualUncompressedSize != decompressed.Length
            || decompressed.Length != expectedSize
        )
        {
            throw new InvalidOperationException(
                $"The uncompressed size of {actualUncompressedSize} ({actualUncompressedSize}) bytes does not match the expected value of {expectedSize} bytes."
            );
        }

        // The original code decompresses everything, and then it copies it into
        // the destination buffer, so we do the same.
        var toWrite = int.Min(count, (int)actualUncompressedSize);
        var startOffset = offset;

        Buffer.BlockCopy(decompressed, 0, buffer, offset, toWrite);
        offset += toWrite;

        return offset - startOffset;
    }

    private static uint GetBits(ref DecoderState decoderState, int value)
    {
        uint currentValue = 0;
        if (value != 0)
        {
            currentValue = decoderState.Bits >> (32 - value);
            decoderState.Bits <<= value;
            decoderState.BitsLeft -= value;
        }

        if (decoderState.BitsLeft >= 0)
        {
            return currentValue;
        }

        Get16Bits(ref decoderState);
        decoderState.Bits = decoderState.BitsUnshifted << (-decoderState.BitsLeft);
        decoderState.BitsLeft += 16;
        return currentValue;
    }

    private static void Get16Bits(ref DecoderState decoderState)
    {
        var first = decoderState.Pack[decoderState.SourceIndex];
        var second = decoderState.Pack[decoderState.SourceIndex + 1];
        decoderState.BitsUnshifted = (uint)(first | (int)(decoderState.BitsUnshifted << 8));
        decoderState.BitsUnshifted = (uint)(second | (int)(decoderState.BitsUnshifted << 8));
        decoderState.SourceIndex += 2;
    }

    private static uint GetNumber(ref DecoderState decoderState)
    {
        if ((int)decoderState.Bits < 0)
        {
            var value3 = GetBits(ref decoderState, 3);
            return value3 - 4;
        }

        uint value;
        var count = 2;
        if (decoderState.Bits >> 16 != 0)
        {
            do
            {
                decoderState.Bits <<= 1;
                ++count;
            } while ((int)decoderState.Bits >= 0);

            decoderState.Bits <<= 1;
            decoderState.BitsLeft -= (count - 1);
            _ = GetBits(ref decoderState, 0);
        }
        else
        {
            do
            {
                ++count;
                value = GetBits(ref decoderState, 1);
            } while (value == 0);
        }
        if (count > 16)
        {
            value = GetBits(ref decoderState, count - 16);
            var value1 = GetBits(ref decoderState, 16);
            return (value1 | (value << 16)) + (uint)((1 << count) - 4);
        }

        value = GetBits(ref decoderState, count);
        return value + (uint)((1 << count) - 4);
    }

    private static void SetArray(byte[] arr, int start, byte value, int count)
    {
        var end = start + count;
        for (var i = start; i < end; i++)
        {
            arr[i] = value;
        }
    }

    private static int RecoverClueLength(
        byte[] codeTableLocal,
        int[] bitNumberTableLocal,
        int mostBitsLocal,
        byte clueLocal
    )
    {
        var index = 0;
        for (var bitsLen = 1; bitsLen <= mostBitsLocal; bitsLen++)
        {
            var count = bitNumberTableLocal[bitsLen];
            for (var j = 0; j < count; j++, index++)
            {
                if (codeTableLocal[index] == clueLocal)
                {
                    return bitsLen;
                }
            }
        }

        return 0;
    }

    private static int ReadHeader(
        ref DecoderState decoderState,
        uint headerType,
        out uint normalizedType
    )
    {
        int localLength;
        var typeLocal = headerType;

        if ((typeLocal & 0x8000) != 0)
        {
            if ((typeLocal & 0x100) != 0)
            {
                _ = GetBits(ref decoderState, 16);
                _ = GetBits(ref decoderState, 16);
            }

            typeLocal &= unchecked((uint)~0x100);

            var value = GetBits(ref decoderState, 16);
            localLength = (int)GetBits(ref decoderState, 16);
            localLength |= (int)(value << 16);
        }
        else
        {
            if ((typeLocal & 0x100) != 0)
            {
                _ = GetBits(ref decoderState, 8);
                _ = GetBits(ref decoderState, 16);
            }

            typeLocal &= unchecked((uint)(~0x100));

            var value = GetBits(ref decoderState, 8);
            localLength = (int)GetBits(ref decoderState, 16);
            localLength |= (int)(value << 16);
        }

        normalizedType = typeLocal;
        return localLength;
    }

    private static byte InitializeDeltaAndCompareTables(
        ref DecoderState decoderState,
        int[] bitNumberTableLocal,
        uint[] deltaTableLocal,
        uint[] compareTableLocal,
        out int mostBitsLocal,
        out int charsCount
    )
    {
        // Clue byte
        var localClue = (byte)GetBits(ref decoderState, 8);

        charsCount = 0;
        var bitsCountLocal = 1;
        var baseCompare = 0U;

        int bitNumber;
        uint compare;
        do
        {
            baseCompare <<= 1;
            deltaTableLocal[bitsCountLocal] = baseCompare - (uint)charsCount;

            bitNumber = (int)GetNumber(ref decoderState); // number of codes of n bits
            bitNumberTableLocal[bitsCountLocal] = bitNumber;

            charsCount += bitNumber;
            baseCompare += (uint)bitNumber;

            compare = 0;
            if (bitNumber != 0)
            {
                compare = (baseCompare << (16 - bitsCountLocal)) & 0xffff;
            }

            compareTableLocal[bitsCountLocal++] = compare;
        } while (bitNumber == 0 || compare != 0);

        compareTableLocal[bitsCountLocal - 1] = 0xffffffff; // force match on most bits
        mostBitsLocal = bitsCountLocal - 1;
        return localClue;
    }

    private static void FillCodeTableFromLeaps(
        ref DecoderState decoderState,
        int charsCount,
        byte[] codeTableLocal
    )
    {
        var leap = new sbyte[256];
        byte nextChar = 0xFF;
        for (var i = 0; i < charsCount; ++i)
        {
            var leapDelta = (int)GetNumber(ref decoderState);
            ++leapDelta;
            do
            {
                unchecked
                {
                    ++nextChar;
                }

                if (leap[nextChar] == 0)
                {
                    --leapDelta;
                }
            } while (leapDelta != 0);

            leap[nextChar] = 1;
            codeTableLocal[i] = nextChar;
        }
    }

    private static byte LengthForCode(byte code, byte clue, int lengthIfNotClue)
    {
        return (byte)(code == clue ? 96 : lengthIfNotClue);
    }

    private static void WriteRepeatedEntries(
        ref QuickTables quickTables,
        byte code,
        byte length,
        int repeat
    )
    {
        for (var i = 0; i < repeat; ++i)
        {
            quickTables.QuickCode[quickTables.QuickCodePointer++] = code;
            quickTables.QuickLength[quickTables.QuickLengthPointer++] = length;
        }
    }

    private static void HandleClueEncounter(
        ref CodeStream codeStream,
        byte localClue,
        int bitsLocal,
        int numberBitEntries,
        ref QuickTables quickTables,
        ref int clueLengthLocal
    )
    {
        var nextCode = codeStream.Next();
        var nextLength = bitsLocal;

        if (nextCode == localClue)
        {
            clueLengthLocal = bitsLocal;
            nextLength = 96;
        }

        WriteRepeatedEntries(ref quickTables, nextCode, (byte)nextLength, numberBitEntries);
    }

    private static void FillQuickLookupTables(
        int[] bitNumberTableLocal,
        byte[] codeTableLocal,
        byte localClue,
        int mostBitsLocal,
        byte[] quickCodeTableLocal,
        byte[] quickLengthTableLocal,
        ref int clueLengthLocal
    )
    {
        var codeStream = new CodeStream(codeTableLocal);
        var quick = new QuickTables
        {
            QuickCode = quickCodeTableLocal,
            QuickLength = quickLengthTableLocal,
            QuickCodePointer = 0,
            QuickLengthPointer = 0,
        };

        var limitBits = Math.Min(mostBitsLocal, 8);
        for (var bitsLocal = 1; bitsLocal <= limitBits; ++bitsLocal)
        {
            var bitNumberLocal = bitNumberTableLocal[bitsLocal];
            var numberBitEntries = 1 << (8 - bitsLocal);
            for (var n = 0; n < bitNumberLocal; n++)
            {
                var nextCode = codeStream.Next();
                if (nextCode == localClue)
                {
                    HandleClueEncounter(
                        ref codeStream,
                        localClue,
                        bitsLocal,
                        numberBitEntries,
                        ref quick,
                        ref clueLengthLocal
                    );
                }

                var finalLength = LengthForCode(nextCode, localClue, bitsLocal);
                WriteRepeatedEntries(ref quick, nextCode, finalLength, numberBitEntries);
            }
        }
    }

    private static void EnsureClueLengthIfUnset(
        byte[] codeTableLocal,
        int[] bitNumberTableLocal,
        int mostBitsLocal,
        byte localClue,
        byte[] quickLengthTableLocal,
        ref int clueLengthLocal
    )
    {
        if (clueLengthLocal != 0)
        {
            return;
        }

        for (var i = 0; i < 256; i++)
        {
            if (quickLengthTableLocal[i] != 96)
            {
                continue;
            }

            clueLengthLocal = RecoverClueLength(
                codeTableLocal,
                bitNumberTableLocal,
                mostBitsLocal,
                localClue
            );

            break;
        }
    }

    private static (byte Clue, int MostBits, int ClueLength) BuildHuffmanTables(
        ref DecoderState decoderState,
        int[] bitNumberTableLocal,
        uint[] deltaTableLocal,
        uint[] compareTableLocal,
        byte[] codeTableLocal,
        byte[] quickCodeTableLocal,
        byte[] quickLengthTableLocal
    )
    {
        // Build delta/compare tables and get clue, mostBits and charsCount
        var localClue = InitializeDeltaAndCompareTables(
            ref decoderState,
            bitNumberTableLocal,
            deltaTableLocal,
            compareTableLocal,
            out var mostBitsLocal,
            out var charsCount
        );

        // Build code table from leap stream
        FillCodeTableFromLeaps(ref decoderState, charsCount, codeTableLocal);

        // Init quick length table
        SetArray(quickLengthTableLocal, 0, 64, 256);

        // Fill quick tables for first 8 bits and discover possible clue length
        var clueLengthLocal = 0;
        FillQuickLookupTables(
            bitNumberTableLocal,
            codeTableLocal,
            localClue,
            mostBitsLocal,
            quickCodeTableLocal,
            quickLengthTableLocal,
            ref clueLengthLocal
        );

        // Ensure clue length is known if any entry indicates clue
        EnsureClueLengthIfUnset(
            codeTableLocal,
            bitNumberTableLocal,
            mostBitsLocal,
            localClue,
            quickLengthTableLocal,
            ref clueLengthLocal
        );

        return (localClue, mostBitsLocal, clueLengthLocal);
    }

    private static void EmitQuickSymbols(
        ref DecoderState decoderState,
        byte[] destination,
        ref int destinationIndex,
        byte[] quickCodeTableLocal,
        byte[] quickLengthTableLocal,
        out int bitsCountLocal
    )
    {
        bitsCountLocal = quickLengthTableLocal[decoderState.Bits >> 24];
        decoderState.BitsLeft -= bitsCountLocal;
        while (decoderState.BitsLeft >= 0)
        {
            destination[destinationIndex++] = quickCodeTableLocal[decoderState.Bits >> 24];
            decoderState.Bits <<= bitsCountLocal;

            bitsCountLocal = quickLengthTableLocal[decoderState.Bits >> 24];
            decoderState.BitsLeft -= bitsCountLocal;
        }
    }

    private static int DetermineBitsCount(
        ref DecoderState decoderState,
        int bitsCountLocal,
        ref HuffmanContext huffmanContext
    )
    {
        if (bitsCountLocal != 96)
        {
            var compareLocal = decoderState.Bits >> 16;
            var bits = 8;
            do
            {
                ++bits;
            } while (compareLocal >= huffmanContext.CompareTable[bits]);

            return bits;
        }

        if (huffmanContext.ClueLength == 0)
        {
            huffmanContext.ClueLength = RecoverClueLength(
                huffmanContext.CodeTable,
                huffmanContext.BitNumberTable,
                huffmanContext.MostBits,
                huffmanContext.Clue
            );
        }

        return huffmanContext.ClueLength;
    }

    private static byte ReadCode(
        ref DecoderState decoderState,
        int bitsCountLocal,
        uint[] deltaTableLocal,
        byte[] codeTableLocal
    )
    {
        var seg = decoderState.Bits >> (32 - bitsCountLocal);
        decoderState.Bits <<= bitsCountLocal;
        decoderState.BitsLeft -= bitsCountLocal;
        return codeTableLocal[seg - deltaTableLocal[bitsCountLocal]];
    }

    private static void RefillBitsIfNeeded(ref DecoderState decoderState)
    {
        if (decoderState.BitsLeft >= 0)
        {
            return;
        }

        Get16Bits(ref decoderState);
        decoderState.Bits = decoderState.BitsUnshifted << -decoderState.BitsLeft;
        decoderState.BitsLeft += 16;
    }

    private static bool TryFastPath(
        ref DecoderState decoderState,
        byte[] destination,
        ref int destinationIndex,
        byte[] quickCodeTableLocal,
        ref int bitsCountLocal
    )
    {
        decoderState.BitsLeft += 16;

        if (decoderState.BitsLeft >= 0)
        {
            destination[destinationIndex++] = quickCodeTableLocal[decoderState.Bits >> 24];
            Get16Bits(ref decoderState);
            decoderState.Bits = decoderState.BitsUnshifted << (16 - decoderState.BitsLeft);
            return true;
        }

        decoderState.BitsLeft = decoderState.BitsLeft - 16 + bitsCountLocal;
        return false;
    }

    private static bool TryWriteNonClueOrRefill(
        ref DecoderState decoderState,
        byte code,
        byte clueLocal,
        byte[] destination,
        ref int destinationIndex
    )
    {
        if (code != clueLocal && decoderState.BitsLeft >= 0)
        {
            destination[destinationIndex++] = code;
            return true;
        }

        RefillBitsIfNeeded(ref decoderState);

        if (code == clueLocal)
        {
            return false;
        }

        destination[destinationIndex++] = code;
        return true;
    }

    private static bool TryProcessRunOrEof(
        ref DecoderState decoderState,
        byte[] destination,
        ref int destinationIndex
    )
    {
        var runLength = (int)GetNumber(ref decoderState);
        if (runLength != 0)
        {
            var dest = destinationIndex + runLength;
            var prev = destination[destinationIndex - 1];
            while (destinationIndex < dest)
            {
                destination[destinationIndex++] = prev;
            }

            return true; // continue decoding loop
        }

        // End Of File bit
        var vBit = GetBits(ref decoderState, 1);
        if (vBit != 0)
        {
            return false; // signal EOF to caller
        }

        var explicitByte = (byte)GetBits(ref decoderState, 8);
        destination[destinationIndex++] = explicitByte;
        return true;
    }

    private static int DecodeStream(
        ref DecoderState decoderState,
        byte[] destination,
        int destinationIndex,
        ref DecodeContext decodeContext
    )
    {
        while (true)
        {
            EmitQuickSymbols(
                ref decoderState,
                destination,
                ref destinationIndex,
                decodeContext._quickCodeTable,
                decodeContext._quickLengthTable,
                out var bitsCountLocal
            );

            if (
                TryFastPath(
                    ref decoderState,
                    destination,
                    ref destinationIndex,
                    decodeContext._quickCodeTable,
                    ref bitsCountLocal
                )
            )
            {
                continue;
            }

            bitsCountLocal = DetermineBitsCount(
                ref decoderState,
                bitsCountLocal,
                ref decodeContext._huffman
            );

            var code = ReadCode(
                ref decoderState,
                bitsCountLocal,
                decodeContext._deltaTable,
                decodeContext._huffman.CodeTable
            );
            if (
                TryWriteNonClueOrRefill(
                    ref decoderState,
                    code,
                    decodeContext._huffman.Clue,
                    destination,
                    ref destinationIndex
                )
            )
            {
                continue;
            }

            if (TryProcessRunOrEof(ref decoderState, destination, ref destinationIndex))
            {
                continue;
            }

            break;
        }

        return destinationIndex;
    }

    private static void PostProcessByType(uint processedType, byte[] bufferLocal, int length)
    {
        switch (processedType)
        {
            case 0x32FB or 0xB2FB:
            {
                var i = 0;
                var j = 0;
                while (j < length)
                {
                    i += bufferLocal[j];
                    bufferLocal[j] = (byte)i;
                    j++;
                }

                break;
            }
            case 0x34FB or 0xB4FB:
            {
                var i = 0;
                var nextCharInner = 0;
                var j = 0;
                while (j < length)
                {
                    i += bufferLocal[j];
                    nextCharInner += i;
                    bufferLocal[j] = (byte)nextCharInner;
                    j++;
                }

                break;
            }
        }
    }

    private static byte[] Decompress(byte[] packBuffer, out uint actualLength)
    {
        actualLength = 0;
        byte[] unpackBuffer = [];

        if (packBuffer.Length == 0)
        {
            return unpackBuffer;
        }

        // Initialize bit-reader state
        var state = new DecoderState
        {
            Pack = packBuffer,
            SourceIndex = 0,
            BitsUnshifted = 0U,
            BitsLeft = -16,
            Bits = 0U,
        };

        _ = GetBits(ref state, 0);

        // Header
        var type = GetBits(ref state, 16);
        var length = ReadHeader(ref state, type, out type);
        actualLength = (uint)length;

        // Output buffer and tables
        unpackBuffer = new byte[length];
        var destinationIndex = 0;

        var bitNumberTable = new int[16];
        var deltaTable = new uint[16];
        var compareTable = new uint[16];
        var codeTable = new byte[256];
        var quickCodeTable = new byte[256];
        var quickLengthTable = new byte[256];

        // Build tables
        var (clue, mostBits, clueLength) = BuildHuffmanTables(
            ref state,
            bitNumberTable,
            deltaTable,
            compareTable,
            codeTable,
            quickCodeTable,
            quickLengthTable
        );

        // Prepare decode context
        var huffmanCtx = new HuffmanContext
        {
            CompareTable = compareTable,
            CodeTable = codeTable,
            BitNumberTable = bitNumberTable,
            MostBits = mostBits,
            Clue = clue,
            ClueLength = clueLength,
        };

        var decodeCtx = new DecodeContext
        {
            _deltaTable = deltaTable,
            _quickCodeTable = quickCodeTable,
            _quickLengthTable = quickLengthTable,
            _huffman = huffmanCtx,
        };

        // Decode stream
        destinationIndex = DecodeStream(ref state, unpackBuffer, destinationIndex, ref decodeCtx);

        // Post-processing
        PostProcessByType(type, unpackBuffer, length);

        if (destinationIndex == length)
        {
            return unpackBuffer;
        }

        var trimmed = new byte[destinationIndex];
        Buffer.BlockCopy(unpackBuffer, 0, trimmed, 0, destinationIndex);
        return trimmed;
    }

    private struct DecoderState
    {
        public byte[] Pack { get; init; }
        public int SourceIndex { get; set; }
        public uint BitsUnshifted { get; set; }
        public int BitsLeft { get; set; }
        public uint Bits { get; set; }
    }

    private struct CodeStream(byte[] codeTable)
    {
        private int _index = 0;

        public byte Next() => codeTable[_index++];
    }

    private struct QuickTables
    {
        public byte[] QuickCode { get; init; }
        public byte[] QuickLength { get; init; }
        public int QuickCodePointer { get; set; }
        public int QuickLengthPointer { get; set; }
    }

    private struct HuffmanContext
    {
        public uint[] CompareTable { get; init; }
        public byte[] CodeTable { get; init; }
        public int[] BitNumberTable { get; init; }
        public int MostBits { get; init; }
        public byte Clue { get; init; }
        public int ClueLength { get; set; }
    }

    private struct DecodeContext
    {
        public uint[] _deltaTable;
        public byte[] _quickCodeTable;
        public byte[] _quickLengthTable;
        public HuffmanContext _huffman;
    }
}
