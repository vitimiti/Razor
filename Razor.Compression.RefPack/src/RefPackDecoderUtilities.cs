// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Extensions;

namespace Razor.Compression.RefPack;

/// <summary>Utilities for the RefPack file format.</summary>
internal static class RefPackDecoderUtilities
{
    /// <summary>Checks whether a given stream is RefPack compressed data.</summary>
    /// <param name="stream">The stream to check the compression type.</param>
    /// <returns><see langword="true" /> if the stream is RefPack compressed data, otherwise <see langword="false" />.</returns>
    public static bool IsRefPack(Stream stream)
    {
        if (stream.Length < 2)
        {
            return false;
        }

        stream.Position = 0;
        using BinaryReader reader = new(stream, EncodingExtensions.Ansi, leaveOpen: true);
        var packType = reader.ReadUInt16BigEndian();
        return packType is 0x10FB or 0x11FB or 0x90FB or 0x91FB;
    }

    /// <summary>Gets the uncompressed size of the given RefPack compressed stream.</summary>
    /// <param name="stream">The RefPack compressed stream to get the uncompressed size from.</param>
    /// <returns>A new <see cref="uint" /> with the uncompressed size.</returns>
    /// <exception cref="ArgumentException">When the given <paramref name="stream" /> is not a RefPack stream.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When the given <paramref name="stream" /> is too small to contain the uncompressed size.</exception>
    public static uint GetUncompressedSize(Stream stream)
    {
        if (!IsRefPack(stream))
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
