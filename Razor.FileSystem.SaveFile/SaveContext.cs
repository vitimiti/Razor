// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Razor.FileSystem.SaveFile;

[PublicAPI]
public sealed class SaveContext
{
    private readonly Dictionary<ISerializableObject, int> _ids = new(
        ReferenceEqualityComparer<ISerializableObject>.Default
    );

    private readonly Queue<ISerializableObject> _pending = new();

    private int _nextId = 1;

    public int GetOrAssignId(ISerializableObject? obj)
    {
        if (obj is null)
        {
            return 0;
        }

        if (_ids.TryGetValue(obj, out var id))
        {
            return id;
        }

        id = _nextId++;
        _ids[obj] = id;
        _pending.Enqueue(obj);
        return id;
    }

    public void WriteRef(BinaryWriter writer, ISerializableObject? obj)
    {
        var id = GetOrAssignId(obj);
        writer.Write(id);
    }

    internal bool TryDequeue(out ISerializableObject? obj)
    {
        return _pending.TryDequeue(out obj);
    }

    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static ReferenceEqualityComparer<T> Default { get; } = new();

        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
