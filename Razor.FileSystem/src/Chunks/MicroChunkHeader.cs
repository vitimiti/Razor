// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.FileSystem.Chunks;

[PublicAPI]
public class MicroChunkHeader(byte chunkType, uint chunkSize)
{
    public byte ChunkType { get; set; } = chunkType;
    public uint ChunkSize { get; set; } = chunkSize;

    public MicroChunkHeader()
        : this(0, 0) { }

    public void AddSize(uint size)
    {
        ChunkSize += size;
    }
}
