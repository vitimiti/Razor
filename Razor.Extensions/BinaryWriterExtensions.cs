// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Text;
using JetBrains.Annotations;

namespace Razor.Extensions;

[PublicAPI]
public static class BinaryWriterExtensions
{
    public static void WriteUInt16BigEndian(this BinaryWriter writer, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        writer.Write(bytes);
    }

    public static void WriteUInt24BigEndian(this BinaryWriter writer, uint value)
    {
        writer.Write((byte)(value >> 16));
        writer.Write((byte)(value >> 8));
        writer.Write((byte)value);
    }

    public static void WriteUInt32BigEndian(this BinaryWriter writer, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    public static void WriteNullTerminatedUtf8(
        this BinaryWriter writer,
        string value,
        Encoding encoding
    )
    {
        var bytes = encoding.GetBytes(value);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
