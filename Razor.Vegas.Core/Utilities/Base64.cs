// -----------------------------------------------------------------------
// <copyright file="Base64.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Razor.Vegas.Core.Utilities;

/// <summary>
/// Utilities for Base64 Content-Transfer-Encoding: encoding and decoding
/// routines adapted from a legacy C implementation.
/// </summary>
/// <remarks>
/// <para>
/// The Base64 encoding represents arbitrary sequences of octets using a
/// 64-character subset of US-ASCII so that 6 bits are represented per
/// printable character. The output is about 33% larger than the input.
/// </para>
/// <para>
/// Encoding maps 3 bytes (24 bits) of input into 4 Base64 characters (4 x 6 bits).
/// When fewer than 3 input bytes remain, the encoder pads the output with '='
/// so the decoder can reconstruct the original byte count. The decoder ignores
/// characters not part of the Base64 alphabet (for example, line breaks or spaces),
/// and treats '=' as an end-of-data marker.
/// </para>
/// <para>
/// These routines are intended for small, fast, allocation-free transforms
/// over spans of bytes; they do not add line breaks to the encoded output.
/// </para>
/// </remarks>
internal static partial class Base64
{
    private const byte Bad = 0xFE;
    private const byte End = 0xFF;
    private const string Pad = "=";
    private const string Encoder = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    private const int PacketChars = 4;

    /// <summary>
    /// Encodes the provided input bytes into Base64 text, writing the result
    /// into the supplied <paramref name="buffer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The encoder processes the input in 3-byte groups and writes 4 output
    /// characters per group. When fewer than three bytes remain, the output
    /// is padded with '=' characters so the decoder can recover the original
    /// byte count. Any trailing space in the destination buffer is filled with
    /// a NUL byte when there is remaining space after encoding.
    /// </para>
    /// <para>
    /// The caller is responsible for providing a destination buffer large
    /// enough to hold the encoded characters (typically 4 * ceil(sourceLength / 3)).
    /// </para>
    /// </remarks>
    /// <param name="data">Source bytes to encode.</param>
    /// <param name="buffer">Destination buffer that will receive the encoded characters.</param>
    /// <returns>The number of bytes written to <paramref name="buffer"/> (the encoded length).
    /// If <paramref name="data"/> or <paramref name="buffer"/> is empty, returns zero.</returns>
    public static int Encode(ReadOnlySpan<byte> data, Span<byte> buffer)
    {
        if (data.Length == 0 || buffer.Length == 0)
        {
            return 0;
        }

        var total = 0;
        var sourceIndex = 0;
        var bufferIndex = 0;
        var sourceLength = data.Length;
        var destinationLength = buffer.Length;
        while (sourceLength > 0 && destinationLength >= PacketChars)
        {
            PacketType packet = new();
            var pad = 0;
            packet.Clear();
            packet.C1 = data[sourceIndex++];
            sourceLength--;

            if (sourceLength > 0)
            {
                packet.C2 = data[sourceIndex++];
                sourceLength--;
            }
            else
            {
                pad++;
            }

            if (sourceLength > 0)
            {
                packet.C3 = data[sourceIndex++];
                sourceLength--;
            }
            else
            {
                pad++;
            }

            // Translate and write 4 characters of Base64 data. Pad with pad
            // characters if there is insufficient source data for a full packet.
            buffer[bufferIndex++] = (byte)Encoder[packet.O1];
            buffer[bufferIndex++] = (byte)Encoder[packet.O2];
            buffer[bufferIndex++] = pad < 2 ? (byte)Encoder[packet.O3] : (byte)Pad[0];
            buffer[bufferIndex++] = pad < 1 ? (byte)Encoder[packet.O4] : (byte)Pad[0];

            destinationLength -= PacketChars;
            total += PacketChars;
        }

        if (destinationLength > 0)
        {
            buffer[bufferIndex] = (byte)'\0';
        }

        return total;
    }

    /// <summary>
    /// Internal helper type representing a 3-byte input packet and its
    /// corresponding 4 output 6-bit codes used by the encoder/decoder.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct PacketType
    {
        /// <summary>
        /// Raw 32-bit container for the packet data.
        /// </summary>
        [FieldOffset(0)]
        public uint Raw;

        /// <summary>
        /// First source byte (C1).
        /// </summary>
        [FieldOffset(0)]
        public byte C1;

        /// <summary>
        /// Second source byte (C2).
        /// </summary>
        [FieldOffset(1)]
        public byte C2;

        /// <summary>
        /// Third source byte (C3).
        /// </summary>
        [FieldOffset(2)]
        public byte C3;

        /// <summary>
        /// Padding byte used for alignment in the raw container.
        /// </summary>
        [FieldOffset(3)]
        public byte Padding;

        [FieldOffset(0)]
        private uint _subcodeData;

        /// <summary>
        /// Gets or sets the first 6-bit output code (O1) produced from the
        /// 24-bit input. Bit positions differ by platform endianness.
        /// </summary>
        public byte O1
        {
            // Little endian O1 is at bits 18-23, big endian is at bits 0-5
            readonly get =>
                BitConverter.IsLittleEndian ? (byte)((_subcodeData >> 18) & 0x3F) : (byte)(_subcodeData & 0x3F);
            set =>
                _subcodeData = BitConverter.IsLittleEndian
                    ? (_subcodeData & 0xFF03FFFFU) | ((uint)(value & 0x3F) << 18)
                    : (_subcodeData & 0xFFFFFFC0U) | (uint)(value & 0x3F);
        }

        /// <summary>
        /// Gets or sets the second 6-bit output code (O2) produced from the input.
        /// </summary>
        public byte O2
        {
            // Little endian O2 is at bits 12-17, big endian is at bits 6-11
            readonly get =>
                BitConverter.IsLittleEndian ? (byte)((_subcodeData >> 12) & 0x3F) : (byte)((_subcodeData >> 6) & 0x3F);
            set =>
                _subcodeData = BitConverter.IsLittleEndian
                    ? (_subcodeData & 0xFFFC0FFFU) | ((uint)(value & 0x3F) << 12)
                    : (_subcodeData & 0xFFFFF03FU) | ((uint)(value & 0x3F) << 6);
        }

        /// <summary>
        /// Gets or sets the third 6-bit output code (O3) produced from the input.
        /// </summary>
        public byte O3
        {
            // Little endian O3 is at bits 6-11, big endian is at bits 12-17
            readonly get =>
                BitConverter.IsLittleEndian ? (byte)((_subcodeData >> 6) & 0x3F) : (byte)((_subcodeData >> 12) & 0x3F);
            set =>
                _subcodeData = BitConverter.IsLittleEndian
                    ? (_subcodeData & 0xFFFFF03FU) | ((uint)(value & 0x3F) << 6)
                    : (_subcodeData & 0xFFFC0FFFU) | ((uint)(value & 0x3F) << 12);
        }

        /// <summary>
        /// Gets or sets the fourth 6-bit output code (O4) produced from the input.
        /// </summary>
        public byte O4
        {
            // Little endian O4 is at bits 0-5, big endian is at bits 18-23
            readonly get =>
                BitConverter.IsLittleEndian ? (byte)(_subcodeData & 0x3F) : (byte)((_subcodeData >> 18) & 0x3F);
            set =>
                _subcodeData = BitConverter.IsLittleEndian
                    ? (_subcodeData & 0xFFFFFFC0U) | (uint)(value & 0x3F)
                    : (_subcodeData & 0xFF03FFFFU) | ((uint)(value & 0x3F) << 18);
        }

        /// <summary>
        /// Gets or sets the byte used to pad the subcode data (highest 8 bits).
        /// </summary>
        public byte SubCodePad
        {
            readonly get => (byte)((_subcodeData >> 24) & 0xFF);
            set => _subcodeData = (_subcodeData & 0x00FFFFFFU) | (uint)((value & 0xFF) << 24);
        }

        /// <summary>
        /// Clears the packet contents (sets raw container to zero).
        /// </summary>
        public void Clear() => Raw = 0;
    }

    /// <summary>
    /// Decodes Base64 text into its original bytes, writing the result into
    /// the provided <paramref name="buffer"/>.
    /// </summary>
    /// <remarks>
    /// The decoder ignores non-Base64 characters (for example, whitespace or
    /// line breaks) and treats the '=' character as an end-of-data marker.
    /// Decoding stops when the source is exhausted or '=' is encountered.
    /// </remarks>
    /// <param name="data">Source Base64-encoded bytes to decode.</param>
    /// <param name="buffer">Destination buffer that receives decoded bytes.</param>
    /// <returns>The number of bytes written into <paramref name="buffer"/>. Returns zero when input or output buffer lengths are zero.</returns>
    public static int Decode(ReadOnlySpan<byte> data, Span<byte> buffer)
    {
        if (data.Length == 0 || buffer.Length == 0)
        {
            return 0;
        }

        var total = 0;
        var sourceIndex = 0;
        var destinationIndex = 0;
        var sourceLength = data.Length;
        var destinationLength = buffer.Length;
        while (sourceLength > 0 && destinationLength > 0)
        {
            PacketType packet = new();
            packet.Clear();

            // Process input until a full packet has been accumulated or the
            // source is exhausted.
            var packetCount = 0;
            while (packetCount < PacketChars && sourceLength > 0)
            {
                var c = data[sourceIndex++];
                sourceLength--;

                var code = Decoder[c];

                // An unrecognized character is skipped.
                if (code is Bad)
                {
                    continue;
                }

                // The "=" character signifies the end of data regardless of
                // what the source buffer length value may be.
                if (code is End)
                {
                    sourceLength = 0;
                    break;
                }

                // A valid Base64 character was found so add it to the packet
                // data.
                switch (packetCount)
                {
                    case 0:
                        packet.O1 = code;
                        break;
                    case 1:
                        packet.O2 = code;
                        break;
                    case 2:
                        packet.O3 = code;
                        break;
                    case 3:
                        packet.O4 = code;
                        break;
                    default:
                        break;
                }

                packetCount++;
            }

            // A packet block is ready for output into the destination buffer.
            buffer[destinationIndex++] = packet.C1;
            destinationLength--;
            total++;
            if (destinationLength > 0 && packetCount > 2)
            {
                buffer[destinationIndex++] = packet.C2;
                destinationLength--;
                total++;
            }

            if (destinationLength > 0 && packetCount > 3)
            {
                buffer[destinationIndex++] = packet.C3;
                destinationLength--;
                total++;
            }
        }

        return total;
    }
}
