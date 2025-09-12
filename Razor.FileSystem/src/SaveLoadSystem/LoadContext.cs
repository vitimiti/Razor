// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.FileSystem.SaveLoadSystem;

[PublicAPI]
public sealed class LoadContext
{
    private readonly Dictionary<int, ISerializableObject> _byId = new();
    private readonly List<ISerializableObject> _postLoad = [];

    public T? Resolve<T>(int id)
        where T : class, ISerializableObject
    {
        if (id == 0)
            return null;
        return _byId.TryGetValue(id, out var obj) ? (T)obj : null;
    }

    public void DeferPostLoad(ISerializableObject obj)
    {
        _postLoad.Add(obj);
    }

    public static int ReadRefId(BinaryReader reader)
    {
        return reader.ReadInt32();
    }

    internal void RunPostLoad()
    {
        foreach (var obj in _postLoad)
        {
            obj.OnPostLoad(this);
        }

        _postLoad.Clear();
    }

    internal void Register(int id, ISerializableObject obj)
    {
        _byId[id] = obj;
    }
}
