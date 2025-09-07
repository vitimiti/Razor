// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace Razor.Extensions;

/// <summary>Extensions for the <see cref="BinaryWriter" /> class.</summary>
public static class BinaryWriterExtensions
{
    /// <summary>Writes 2 bytes in big-endian form.</summary>
    /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
    /// <param name="value">The value to write as 2 big-endian bytes.</param>
    public static void WriteUInt16BigEndian(this BinaryWriter writer, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        writer.Write(bytes);
    }

    /// <summary>Writes 3 bytes in big-endian form.</summary>
    /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
    /// <param name="value">The value to write as 3 big-endian bytes.</param>
    public static void WriteUInt24BigEndian(this BinaryWriter writer, uint value)
    {
        writer.Write((byte)(value >> 16));
        writer.Write((byte)(value >> 8));
        writer.Write((byte)value);
    }

    /// <summary>Writes 4 bytes in big-endian form.</summary>
    /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
    /// <param name="value">The value to write as 4 big-endian bytes.</param>
    public static void WriteUInt32BigEndian(this BinaryWriter writer, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }
}
