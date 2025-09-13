// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.RefPack.Internals;

internal static class RefPackDecoderUtilities
{
    // csharpier-ignore
    private static ushort[] ValidPackTypes => [0x10FB, 0x11FB, 0x90FB, 0x91FB];

    public static bool IsRefPackCompressed(Stream stream)
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
        if (!IsRefPackCompressed(stream))
        {
            throw new ArgumentException(
                "The stream is not RefPack compressed data.",
                nameof(stream)
            );
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(stream.Length, 2);

        stream.Position = 0;
        using BinaryReader reader = new(stream, EncodingExtensions.Ansi, leaveOpen: true);
        var packType = reader.ReadUInt16BigEndian();
        var bytesToRead = (packType & 0x8000) != 0 ? 4 : 3;

        // Create an offset
        _ = reader.ReadBytes((packType & 0x100) != 0 ? 2 + bytesToRead : 2);
        return bytesToRead == 4 ? reader.ReadUInt32BigEndian() : reader.ReadUInt24BigEndian();
    }
}
