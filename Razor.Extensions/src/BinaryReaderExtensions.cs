// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace Razor.Extensions;

/// <summary>Extensions for the <see cref="BinaryReader" /> class.</summary>
public static class BinaryReaderExtensions
{
    /// <summary>Reads 3 bytes in big-endian form.</summary>
    /// <param name="reader">
    ///     The <see cref="BinaryReader" /> to read from.
    /// </param>
    /// <returns>
    ///     A new <see cref="uint" /> with the 3 bytes in big-endian form.
    /// </returns>
    public static uint ReadUInt24BigEndian(this BinaryReader reader)
    {
        var byte1 = reader.ReadByte();
        var byte2 = reader.ReadByte();
        var byte3 = reader.ReadByte();
        return (uint)((byte1 << 16) | (byte2 << 8) | byte3);
    }

    /// <summary>Reads 4 bytes in big-endian form.</summary>
    /// <param name="reader">
    ///     The <see cref="BinaryReader" /> to read from.
    /// </param>
    /// <returns>
    ///     A new <see cref="uint" /> with the 4 bytes in big-endian form.
    /// </returns>
    public static uint ReadUInt32BigEndian(this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }
}
