// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Razor.FileSystem.Chunks;
using Razor.FileSystem.SaveLoadSystems;

namespace Razor.FileSystem;

[PublicAPI]
public static class SaveLoadSystem
{
    private static readonly Dictionary<uint, IPersistFactory> s_factories = new();
    private static readonly Dictionary<ulong, object> s_pointerMap = new();
    private static readonly List<IPostLoadable> s_postLoadList = [];

    public static void RegisterFactory(IPersistFactory factory)
    {
        s_factories[factory.ChunkId] = factory;
    }

    public static void UnregisterFactory(uint chunkId)
    {
        _ = s_factories.Remove(chunkId);
    }

    public static void UnregisterFactory(IPersistFactory factory)
    {
        if (
            s_factories.TryGetValue(factory.ChunkId, out var registeredFactory)
            && ReferenceEquals(registeredFactory, factory)
        )
        {
            _ = s_factories.Remove(factory.ChunkId);
        }
    }

    public static void RegisterPointer(ulong oldPointer, object newObject)
    {
        s_pointerMap[oldPointer] = newObject;
    }

    public static T? GetMappedObject<T>(ulong pointer)
        where T : class
    {
        return s_pointerMap.TryGetValue(pointer, out var obj) ? obj as T : null;
    }

    public static void AddToPostLoadList(IPostLoadable obj)
    {
        if (obj.IsPostLoadRegistered)
        {
            return;
        }

        s_postLoadList.Add(obj);
        obj.IsPostLoadRegistered = true;
    }

    public static void ProcessPostLoad()
    {
        foreach (var obj in s_postLoadList)
        {
            obj.OnPostLoad();
            obj.IsPostLoadRegistered = false;
        }

        s_postLoadList.Clear();
        s_pointerMap.Clear();
    }

    public static IPersistable LoadObject(ChunkReader reader, uint chunkId)
    {
        return !s_factories.TryGetValue(chunkId, out var factory)
            ? throw new InvalidOperationException(
                $"No factory registered for chunk ID 0x{chunkId:X8}"
            )
            : factory.Load(reader);
    }

    public static void SaveObject(ChunkWriter writer, uint chunkId, IPersistable obj)
    {
        if (!s_factories.TryGetValue(chunkId, out var factory))
        {
            throw new InvalidOperationException(
                $"No factory registered for chunk ID 0x{chunkId:X8}"
            );
        }

        writer.WriteChunk(
            chunkId,
            chunkWriter =>
            {
                factory.Save(chunkWriter, obj);
            }
        );
    }
}
