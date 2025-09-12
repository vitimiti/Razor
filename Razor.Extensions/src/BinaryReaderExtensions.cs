// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Text;
using JetBrains.Annotations;

namespace Razor.Extensions;

[PublicAPI]
public static class BinaryReaderExtensions
{
    public static ushort ReadUInt16BigEndian(this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(2);
        return BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    public static uint ReadUInt24BigEndian(this BinaryReader reader)
    {
        var byte1 = reader.ReadByte();
        var byte2 = reader.ReadByte();
        var byte3 = reader.ReadByte();
        return (uint)((byte1 << 16) | (byte2 << 8) | byte3);
    }

    public static uint ReadUInt32BigEndian(this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    public static string ReadNullTerminatedUtf8(
        this BinaryReader reader,
        Encoding encoding,
        int maxLen = 4096
    )
    {
        using var ms = new MemoryStream();
        for (var i = 0; i < maxLen; i++)
        {
            var b = reader.ReadByte();
            if (b == 0)
            {
                break;
            }

            ms.WriteByte(b);
        }

        return encoding.GetString(ms.ToArray());
    }
}
