// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Razor.Extensions;

/// <summary>Provides extension methods for writing specific data formats with a BinaryWriter.</summary>
public static class BinaryWriterExtensions
{
    /// <summary>Writes a 16-bit unsigned integer to the current stream in big-endian format.</summary>
    /// <param name="writer">The BinaryWriter instance to write the data to.</param>
    /// <param name="value">The 16-bit unsigned integer to write in big-endian format.</param>
    public static void WriteUInt16BigEndian([NotNull] this BinaryWriter writer, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        writer.Write(bytes);
    }

    /// <summary>Writes a 24-bit unsigned integer to the current stream in big-endian format.</summary>
    /// <param name="writer">The BinaryWriter instance to write the data to.</param>
    /// <param name="value">The 24-bit unsigned integer to write in big-endian format.</param>
    public static void WriteUInt24BigEndian([NotNull] this BinaryWriter writer, uint value)
    {
        writer.Write((byte)(value >> 16));
        writer.Write((byte)(value >> 8));
        writer.Write((byte)value);
    }

    /// <summary>Writes a 32-bit unsigned integer to the current stream in big-endian format.</summary>
    /// <param name="writer">The BinaryWriter instance to write the data to.</param>
    /// <param name="value">The 32-bit unsigned integer to write in big-endian format.</param>
    public static void WriteUInt32BigEndian([NotNull] this BinaryWriter writer, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    /// <summary>Writes a UTF-8 encoded string followed by a null terminator to the current stream.</summary>
    /// <param name="writer">The BinaryWriter instance to write the data to.</param>
    /// <param name="value">The string to encode and write.</param>
    /// <param name="encoding">The Encoding to use for converting the string to bytes.</param>
    public static void WriteNullTerminatedUtf8(
        [NotNull] this BinaryWriter writer,
        string value,
        [NotNull] Encoding encoding
    )
    {
        var bytes = encoding.GetBytes(value);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
