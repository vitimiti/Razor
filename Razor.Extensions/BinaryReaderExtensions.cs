// -----------------------------------------------------------------------
// <copyright file="BinaryReaderExtensions.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Razor.Extensions;

/// <summary>Provides extension methods for the <see cref="BinaryReader"/> class to handle big-endian and null-terminated string data.</summary>
public static class BinaryReaderExtensions
{
    /// <summary>Reads a 16-bit unsigned integer from the current stream in big-endian format.</summary>
    /// <param name="reader">The <see cref="BinaryReader"/> instance to read from.</param>
    /// <returns>The 16-bit unsigned integer read from the stream, in big-endian format.</returns>
    public static ushort ReadUInt16BigEndian([NotNull] this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(2);
        return BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    /// <summary>Reads a 24-bit unsigned integer from the current stream in big-endian format.</summary>
    /// <param name="reader">The <see cref="BinaryReader"/> instance to read from.</param>
    /// <returns>The 24-bit unsigned integer read from the stream, in big-endian format.</returns>
    public static uint ReadUInt24BigEndian([NotNull] this BinaryReader reader)
    {
        var byte1 = reader.ReadByte();
        var byte2 = reader.ReadByte();
        var byte3 = reader.ReadByte();
        return (uint)((byte1 << 16) | (byte2 << 8) | byte3);
    }

    /// <summary>Reads a 32-bit unsigned integer from the current stream in big-endian format.</summary>
    /// <param name="reader">The <see cref="BinaryReader"/> instance to read from.</param>
    /// <returns>The 32-bit unsigned integer read from the stream, in big-endian format.</returns>
    public static uint ReadUInt32BigEndian([NotNull] this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    /// <summary>Reads a null-terminated UTF-8 encoded string from the current stream.</summary>
    /// <param name="reader">The <see cref="BinaryReader"/> instance to read from.</param>
    /// <param name="encoding">The <see cref="Encoding"/> to use for decoding the string.</param>
    /// <param name="maxLen">The maximum length of the string to read, in bytes. Default is 4096.</param>
    /// <returns>The null-terminated string read from the stream.</returns>
    public static string ReadNullTerminatedUtf8(
        [NotNull] this BinaryReader reader,
        [NotNull] Encoding encoding,
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
