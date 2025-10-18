// -----------------------------------------------------------------------
// <copyright file="MicroChunkHeader.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.ChunkIo;

/// <summary>
/// Represents a compact header for a microdata chunk used by the I/O system.
/// This class stores a one-byte chunk type and a one-byte chunk size for
/// compact storage formats.
/// </summary>
public class MicroChunkHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MicroChunkHeader"/> class
    /// with default values (both <see cref="ChunkType"/> and
    /// <see cref="ChunkSize"/> are zero).
    /// </summary>
    public MicroChunkHeader() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroChunkHeader"/> class
    /// with the specified type and size values.
    /// </summary>
    /// <param name="chunkType">The numeric chunk type identifier (byte).</param>
    /// <param name="chunkSize">The chunk size in bytes (byte).</param>
    public MicroChunkHeader(byte chunkType, byte chunkSize) => (ChunkType, ChunkSize) = (chunkType, chunkSize);

    /// <summary>
    /// Gets or sets the chunk type identifier (one byte).
    /// </summary>
    public byte ChunkType { get; set; }

    /// <summary>
    /// Gets or sets the chunk size (one byte).
    /// </summary>
    public byte ChunkSize { get; set; }

    /// <summary>
    /// Creates a new <see cref="MicroChunkHeader"/> from the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer containing the chunk header.</param>
    /// <returns>A new <see cref="MicroChunkHeader"/> instance.</returns>
    /// <remarks>
    /// The buffer must contain at least 2 bytes.
    /// </remarks>
    public static MicroChunkHeader FromBuffer(ReadOnlySpan<byte> buffer) => new(buffer[0], buffer[1]);

    /// <summary>
    /// Adds the specified <paramref name="size"/> to the current <see cref="ChunkSize"/>.
    /// </summary>
    /// <param name="size">The value to add to the current <see cref="ChunkSize"/>.</param>
    public void AddSize(byte size) => ChunkSize += size;

    /// <summary>
    /// Converts the current header to a byte array.
    /// </summary>
    /// <returns>A byte array containing the header data.</returns>
    /// <remarks>
    /// The returned array will contain 2 bytes.
    /// </remarks>
    public Span<byte> ToBuffer()
    {
        Span<byte> buffer = [ChunkType, ChunkSize];
        return buffer.ToArray();
    }
}
