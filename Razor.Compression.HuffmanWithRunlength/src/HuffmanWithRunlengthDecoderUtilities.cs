// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.HuffmanWithRunlength;

internal static class HuffmanWithRunlengthDecoderUtilities
{
    // csharpier-ignore
    private static ushort[] ValidPackTypes =>
    [
        0x30FB, 0x31FB, 0x32FB, 0x33FB, 0x34FB, 0x35FB, 0xB0FB, 0xB1FB, 0xB2FB, 0xB3FB, 0xB4FB, 0xB5FB
    ];

    public static bool IsHuffmanWithRunlengthCompressed(Stream stream)
    {
        if (stream.Length < 2)
        {
            return false;
        }

        stream.Position = 0;
        using BinaryReader reader = new(stream, EncodingExtensions.Ansi, leaveOpen: true);
        var packType = reader.ReadUInt16BigEndian();
        return ValidPackTypes.Contains(packType);
    }

    public static uint GetUncompressedSize(Stream stream)
    {
        if (!IsHuffmanWithRunlengthCompressed(stream))
        {
            throw new ArgumentException(
                "The stream is not HuffmanWithRunlength compressed data.",
                nameof(stream)
            );
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(stream.Length, 2);
        stream.Position = 0;
        using BinaryReader reader = new(stream, EncodingExtensions.Ansi, leaveOpen: true);
        var packType = reader.ReadUInt16BigEndian();
        var bytesToRead = (packType & 0x8000) != 0 ? 4 : 3;

        // Create an offset
        _ = reader.ReadBytes(bytesToRead == 4 ? 2 + bytesToRead : 2);
        return bytesToRead == 4 ? reader.ReadUInt32BigEndian() : reader.ReadUInt24BigEndian();
    }
}
