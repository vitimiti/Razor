// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.BinaryTree;

internal static class BinaryTreeDecoderUtilities
{
    public static bool IsBinaryTreeCompressed(Stream stream)
    {
        if (stream.Length < 2)
        {
            return false;
        }

        stream.Position = 0;
        using BinaryReader reader = new(stream, EncodingExtensions.Ansi, leaveOpen: true);
        var packType = reader.ReadUInt16BigEndian();
        return packType is 0x46FB or 0x47FB;
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
        var offset = packType is 0x46FB ? 2 : 5;

        // Create an offset
        _ = reader.ReadBytes(offset);
        return reader.ReadUInt24BigEndian();
    }
}
