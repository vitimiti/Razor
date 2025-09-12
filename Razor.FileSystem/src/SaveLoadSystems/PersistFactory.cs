// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Razor.FileSystem.Chunks;

namespace Razor.FileSystem.SaveLoadSystems;

[PublicAPI]
public class PersistFactory<T> : IPersistFactory, IDisposable
    where T : IPersistable, new()
{
    private const uint ObjectPointerChunkId = 0x00100100;
    private const uint ObjectDataChunkId = 0x00100101;

    private bool _disposed;

    public uint ChunkId { get; }

    public PersistFactory(uint chunkId)
    {
        ChunkId = chunkId;
        SaveLoadSystem.RegisterFactory(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        SaveLoadSystem.UnregisterFactory(ChunkId);

        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PersistFactory()
    {
        Dispose(false);
    }

    public IPersistable Load(ChunkReader reader)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var newObj = new T();
        var oldPointer = 0UL;

        reader.ReadChunks(
            (chunkInfo, chunkReader) =>
            {
                switch (chunkInfo.ChunkId)
                {
                    case ObjectPointerChunkId:
                        oldPointer = chunkReader.Read<ulong>();
                        break;
                    case ObjectDataChunkId:
                        newObj.Load(chunkReader);
                        break;
                }
            }
        );

        SaveLoadSystem.RegisterPointer(oldPointer, newObj);
        return newObj;
    }

    public void Save(ChunkWriter writer, IPersistable obj)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var objPointer = (ulong)obj.GetHashCode();
        writer.WriteChunk(
            ObjectPointerChunkId,
            chunkWriter =>
            {
                chunkWriter.Write(objPointer);
            }
        );

        writer.WriteChunk(
            ObjectDataChunkId,
            chunkWriter =>
            {
                obj.Save(chunkWriter);
            }
        );
    }

    IPersistable IPersistFactory.Load(ChunkReader reader) => Load(reader);

    void IPersistFactory.Save(ChunkWriter writer, IPersistable obj) => Save(writer, obj);
}
