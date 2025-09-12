// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.FileSystem.Chunks;

[PublicAPI]
public struct ChunkHeader(uint chunkType, uint chunkSize)
{
    internal uint RawChunkSize { get; set; } = chunkSize;

    public uint ChunkType { get; set; } = chunkType;

    public uint ChunkSize
    {
        get => RawChunkSize & 0x7FFFFFFF;
        set
        {
            RawChunkSize &= 0x80000000;
            RawChunkSize |= value & 0x7FFFFFFF;
        }
    }

    public bool ContainsSubChunks
    {
        get => (RawChunkSize & 0x80000000) != 0;
        set => RawChunkSize = value ? RawChunkSize | 0x80000000 : RawChunkSize & 0x7FFFFFFF;
    }

    public void AddSize(uint size)
    {
        ChunkSize += size;
    }
}
