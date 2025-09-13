// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.BinaryTree;

internal static class BinaryTreeDecoderUtilities
{
    // csharpier-ignore
    private static ushort[] ValidPackTypes => [0x46FB, 0x47FB];

    public static bool IsBinaryTreeCompressed(Stream stream)
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
        if (!IsBinaryTreeCompressed(stream))
        {
            throw new ArgumentException(
                "The stream is not BinaryTree compressed data.",
                nameof(stream)
            );
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(stream.Length, 2);

        stream.Position = 0;
        using BinaryReader reader = new(stream, EncodingExtensions.Ansi, leaveOpen: true);
        var packType = reader.ReadUInt16BigEndian();

        // Create an offset
        _ = reader.ReadBytes(packType is 0x46FB ? 2 : 5);
        return reader.ReadUInt24BigEndian();
    }
}
