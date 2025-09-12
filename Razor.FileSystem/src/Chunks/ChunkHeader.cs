// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.FileSystem.Chunks;

[PublicAPI]
public struct ChunkHeader(uint chunkTypeFlags, uint chunkSize)
{
    private uint _chunkSize = chunkSize;

    public uint ChunkTypeFlags { get; set; } = chunkTypeFlags;

    public uint ChunkSize
    {
        get => _chunkSize & 0x7FFFFFFF;
        set
        {
            _chunkSize &= 0x80000000;
            _chunkSize |= value & 0x7FFFFFFF;
        }
    }

    public bool ContainsSubChunks
    {
        get => (_chunkSize & 0x80000000) != 0;
        set => _chunkSize = value ? _chunkSize | 0x80000000 : _chunkSize & 0x7FFFFFFF;
    }

    public void AddSize(uint size)
    {
        ChunkSize += size;
    }
}
