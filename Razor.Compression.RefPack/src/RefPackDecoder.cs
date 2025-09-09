// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.RefPack;

internal static class RefPackDecoder
{
    private const string ExcessiveLiteralRunError = "Literal run exceeds requested output size.";

    private const string EofError = "Unexpected end of compressed data while reading literals.";

    private const string ExcessiveCopyError = "Back-reference copy exceeds requested output size.";

    public static long Decode(BinaryReader reader, byte[] buffer, int offset, int count)
    {
        if (!RefPackDecoderUtilities.IsRefPackCompressed(reader.BaseStream))
        {
            throw new ArgumentException("The stream is not a RefPack stream.", nameof(reader));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + count);

        // Reset the stream to the beginning of the compressed data.
        reader.BaseStream.Position = 0;
        var uncompressedSize = GetUncompressedSize(reader);
        var expectedSize = RefPackDecoderUtilities.GetUncompressedSize(reader.BaseStream);
        if (uncompressedSize != expectedSize)
        {
            throw new InvalidOperationException(
                $"The uncompressed size of {uncompressedSize} bytes does not match the expected value of {expectedSize} bytes."
            );
        }

        // Don't write the uncompressed size to the buffer.
        // Start writing from the current stream position onwards.
        var toWrite = uint.Min((uint)count, uncompressedSize);
        var startOffset = offset;

        while (offset - startOffset < toWrite)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                throw new EndOfStreamException("Unexpected end of compressed data.");
            }

            var first = reader.ReadByte();
            var remaining = toWrite - (offset - startOffset);
            if (TryAndProcessShortForm(reader, first, buffer, ref offset, remaining))
            {
                continue;
            }

            if (TryAndProcessIntForm(reader, first, buffer, ref offset, remaining))
            {
                continue;
            }

            if (TryAndProcessVeryIntForm(reader, first, buffer, ref offset, remaining))
            {
                continue;
            }

            if (TryAndProcessLiteralRun(reader, first, buffer, ref offset, remaining))
            {
                continue;
            }

            ProcessEofLiteralRun(reader, buffer, first, ref offset, remaining);

            break;
        }

        return offset - startOffset;
    }

    private static uint GetUncompressedSize(BinaryReader reader)
    {
        var type = reader.ReadUInt16BigEndian();
        var bytesToRead = (type & 0x8000) != 0 ? 4 : 3;
        var skipBytes = (type & 0x100) != 0;
        if (skipBytes)
        {
            // Skip the offset
            _ = bytesToRead == 4 ? reader.ReadUInt32BigEndian() : reader.ReadUInt24BigEndian();
        }

        var length = bytesToRead == 4 ? reader.ReadUInt32BigEndian() : reader.ReadUInt24BigEndian();
        return length;
    }

    private static bool TryAndProcessShortForm(
        BinaryReader reader,
        byte first,
        byte[] buffer,
        ref int offset,
        long remaining
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(remaining);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + remaining);

        if ((first & 0x80) != 0)
        {
            return false;
        }

        var second = reader.ReadByte();
        var literalRun = first & 3;
        if (literalRun > remaining)
        {
            throw new InvalidDataException(ExcessiveLiteralRunError);
        }

        for (var i = 0; i < literalRun; i++)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                throw new EndOfStreamException(EofError);
            }

            buffer[offset++] = reader.ReadByte();
        }

        remaining -= literalRun;
        var distance = ((first & 0x60) << 3) + second;
        var referenceIndex = offset - 1 - distance;
        if (referenceIndex < 0 || referenceIndex >= offset)
        {
            throw new InvalidDataException("Invalid back-reference in short form.");
        }

        var copyLength = ((first & 0x1C) >> 2) + 3;
        if (copyLength > remaining)
        {
            throw new InvalidDataException(ExcessiveCopyError);
        }

        for (var i = 0; i < copyLength; i++)
        {
            buffer[offset++] = buffer[referenceIndex++];
        }

        return true;
    }

    /// <exception cref="EndOfStreamException">Thrown when an unexpected end of stream occurs while reading the compressed data.</exception>
    private static bool TryAndProcessIntForm(
        BinaryReader reader,
        byte first,
        byte[] buffer,
        ref int offset,
        long remaining
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(remaining);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + remaining);

        if ((first & 0x40) != 0)
        {
            return false;
        }

        var second = reader.ReadByte();
        var third = reader.ReadByte();
        var literalRun = second >> 6;
        if (literalRun > remaining)
        {
            throw new InvalidDataException(ExcessiveLiteralRunError);
        }

        for (var i = 0; i < literalRun; i++)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                throw new EndOfStreamException(EofError);
            }

            buffer[offset++] = reader.ReadByte();
        }

        remaining -= literalRun;
        var distance = ((second & 0x3F) << 8) + third;
        var referenceIndex = offset - 1 - distance;
        if (referenceIndex < 0 || referenceIndex >= offset)
        {
            throw new InvalidDataException("Invalid back-reference in int form.");
        }

        var copyLength = (first & 0x3F) + 4;
        if (copyLength > remaining)
        {
            throw new InvalidDataException(ExcessiveCopyError);
        }

        for (var i = 0; i < copyLength; i++)
        {
            buffer[offset++] = buffer[referenceIndex++];
        }

        return true;
    }

    /// <summary>Attempts to process a "very int form" encoding in the RefPack compressed data.</summary>
    /// <param name="reader">The binary reader to read the compressed data.</param>
    /// <param name="first">The first byte of the current encoding to evaluate.</param>
    /// <param name="buffer">The buffer to write the decompressed data to.</param>
    /// <param name="offset">A reference to the current offset in the buffer where data is to be written.</param>
    /// <param name="remaining">The number of bytes remaining to be processed in the buffer.</param>
    /// <returns><see langword="true" /> if the form was processed, otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the given <paramref name="offset" /> or <paramref name="remaining" /> is negative or when the given <paramref name="offset" /> + <paramref name="remaining" /> is greater than the length of the given <paramref name="buffer" />.</exception>
    /// <exception cref="InvalidDataException">Thrown when the compressed data contains invalid literal runs, back-references, or copy lengths.</exception>
    /// <exception cref="EndOfStreamException">Thrown when an unexpected end of stream occurs while reading the compressed data.</exception>
    private static bool TryAndProcessVeryIntForm(
        BinaryReader reader,
        byte first,
        byte[] buffer,
        ref int offset,
        long remaining
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(remaining);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + remaining);

        if ((first & 0x20) != 0)
        {
            return false;
        }

        var second = reader.ReadByte();
        var third = reader.ReadByte();
        var fourth = reader.ReadByte();
        var literalRun = first & 3;
        if (literalRun > remaining)
        {
            throw new InvalidDataException(ExcessiveLiteralRunError);
        }

        for (var i = 0; i < literalRun; i++)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                throw new EndOfStreamException(EofError);
            }

            buffer[offset++] = reader.ReadByte();
        }

        remaining -= literalRun;
        var distance = ((first & 0x10) >> 4 << 16) + (second << 8) + third;
        var referenceIndex = offset - 1 - distance;
        if (referenceIndex < 0 || referenceIndex >= offset)
        {
            throw new InvalidDataException("Invalid back-reference in very int form.");
        }

        var copyLength = ((first & 0x0C) >> 2 << 8) + fourth + 5;
        if (copyLength > remaining)
        {
            throw new InvalidDataException(ExcessiveCopyError);
        }

        for (var i = 0; i < copyLength; i++)
        {
            buffer[offset++] = buffer[referenceIndex++];
        }

        return true;
    }

    /// <summary>Attempts to process a literal run encoding in the RefPack compressed data.</summary>
    /// <param name="reader">The binary reader to read the compressed data.</param>
    /// <param name="first">The first byte of the current encoding to evaluate.</param>
    /// <param name="buffer">The buffer to write the decompressed data to.</param>
    /// <param name="offset">A reference to the current offset in the buffer where data is to be written.</param>
    /// <param name="remaining">The number of bytes remaining to be processed in the buffer.</param>
    /// <returns><see langword="true" /> if the literal run was processed, otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the given <paramref name="offset" /> or <paramref name="remaining" /> is negative or when the given <paramref name="offset" /> + <paramref name="remaining" /> is greater than the length of the given <paramref name="buffer" />.</exception>
    /// <exception cref="InvalidDataException">Thrown when the compressed data contains invalid literal runs, back-references, or copy lengths.</exception>
    /// <exception cref="EndOfStreamException">Thrown when an unexpected end of stream occurs while reading the compressed data.</exception>
    private static bool TryAndProcessLiteralRun(
        BinaryReader reader,
        byte first,
        byte[] buffer,
        ref int offset,
        long remaining
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(remaining);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset + remaining);

        var literalRun = ((first & 0x1F) << 2) + 4;
        if (literalRun > 112)
        {
            return false;
        }

        if (literalRun > remaining)
        {
            throw new InvalidDataException(ExcessiveLiteralRunError);
        }

        for (var i = 0; i < literalRun; i++)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                throw new EndOfStreamException(EofError);
            }

            buffer[offset++] = reader.ReadByte();
        }

        return true;
    }

    /// <summary>Processes the end of file literal run in the RefPack compressed data.</summary>
    /// <param name="reader">The reader containing the RefPack compressed data.</param>
    /// <param name="buffer">The buffer where the decompressed data will be written.</param>
    /// <param name="first">The first byte of the current RefPack literal run.</param>
    /// <param name="offset">The current offset in the buffer where data will be written.</param>
    /// <param name="remaining">The number of bytes remaining to be processed in the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the given <paramref name="offset" /> or the given <paramref name="remaining" /> are negative, or when the given <paramref name="offset" /> is greater than the length of the given <paramref name="buffer" />.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the end of the stream is unexpectedly reached while processing the literal run.</exception>
    private static void ProcessEofLiteralRun(
        BinaryReader reader,
        byte[] buffer,
        byte first,
        ref int offset,
        long remaining
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(remaining);
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, offset);

        var literalRun = first & 3;
        var toCopy = long.Min(literalRun, remaining);
        for (var i = 0L; i < toCopy; i++)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                throw new EndOfStreamException(EofError);
            }

            buffer[offset++] = reader.ReadByte();
        }
    }
}
