// -----------------------------------------------------------------------
// <copyright file="ChunkHeader.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Buffers.Binary;

namespace Razor.Vegas.Core.ChunkIo;

/// <summary>
/// Represents the header for a data chunk used by the I/O system.
/// The header encodes a chunk type and a chunk size; the high bit of
/// <see cref="ChunkSize"/> is reserved to indicate whether the chunk
/// is a sub-chunk.
/// </summary>
public class ChunkHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkHeader"/> class
    /// with default values (both <see cref="ChunkType"/> and
    /// <see cref="ChunkSize"/> are zero).
    /// </summary>
    public ChunkHeader() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkHeader"/> class
    /// with the specified chunk type and chunk size.
    /// </summary>
    /// <param name="chunkType">The numeric chunk type identifier.</param>
    /// <param name="chunkSize">The raw chunk size value (high bit may be used to flag sub-chunks).</param>
    public ChunkHeader(uint chunkType, uint chunkSize) => (ChunkType, ChunkSize) = (chunkType, chunkSize);

    /// <summary>
    /// Gets the size of a <see cref="ChunkHeader"/> in bytes.
    /// </summary>
    /// <remarks>
    /// This value is 8.
    /// </remarks>
    public static int ByteSize => sizeof(uint) * 2;

    /// <summary>
    /// Gets or sets the chunk type identifier.
    /// </summary>
    public uint ChunkType { get; set; }

    /// <summary>
    /// Gets or sets the raw chunk size value. The high (sign) bit of this
    /// value is used internally to indicate a sub-chunk; the remaining
    /// 31 bits represent the actual size.
    /// </summary>
    public uint ChunkSize { get; set; }

    /// <summary>
    /// Gets or sets the effective size of the chunk, excluding the
    /// high bit used to mark sub-chunks. Setting <see cref="Size"/>
    /// preserves the existing sub-chunk flag in <see cref="ChunkSize"/>.
    /// </summary>
    /// <remarks>
    /// Reading this property masks out the high bit of <see cref="ChunkSize"/>.
    /// Writing this property stores only the lower 31 bits into
    /// <see cref="ChunkSize"/>, preserving the current state of the high bit.
    /// </remarks>
    public uint Size
    {
        get => ChunkSize & 0x7FFFFFFF;
        set => ChunkSize = (ChunkSize & 0x80000000) | (value & 0x7FFFFFFF);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this header represents a sub-chunk.
    /// When <c>true</c>, the high bit of <see cref="ChunkSize"/> is set.
    /// </summary>
    public bool SubChunk
    {
        get => (ChunkSize & 0x80000000) != 0;
        set => ChunkSize = value ? ChunkSize | 0x80000000 : ChunkSize & 0x7FFFFFFF;
    }

    /// <summary>
    /// Creates a new <see cref="ChunkHeader"/> from the specified byte array.
    /// </summary>
    /// <param name="bytes">The byte array containing the chunk header.</param>
    /// <returns>A new <see cref="ChunkHeader"/> instance.</returns>
    /// <remarks>
    /// The byte array must contain at least 8 bytes.
    /// </remarks>
    public static ChunkHeader FromBuffer(ReadOnlySpan<byte> bytes) =>
        new(BinaryPrimitives.ReadUInt32LittleEndian(bytes), BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]));

    /// <summary>
    /// Adds <paramref name="size"/> to <see cref="Size"/>, preserving the
    /// sub-chunk flag in <see cref="ChunkSize"/>.
    /// </summary>
    /// <param name="size">The size to add to the current chunk size.</param>
    public void AddSize(uint size) => Size += size;

    /// <summary>
    /// Converts the current header to a byte array.
    /// </summary>
    /// <returns>A byte array containing the header data.</returns>
    /// <remarks>
    /// The returned array will contain 8 bytes.
    /// </remarks>
    public Span<byte> ToBuffer()
    {
        Span<byte> buffer = stackalloc byte[ByteSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, ChunkType);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[4..], ChunkSize);
        return buffer.ToArray();
    }
}
