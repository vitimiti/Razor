// -----------------------------------------------------------------------
// <copyright file="SaveContext.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Razor.FileSystem.SaveFile;

/// <summary>Provides functionalities for managing serialization contexts, including assigning unique IDs to serializable objects and maintaining a queue of pending objects for serialization.</summary>
public sealed class SaveContext
{
    private readonly Dictionary<ISerializableObject, int> _ids = new(
        ReferenceEqualityComparer<ISerializableObject>.Default
    );

    private readonly Queue<ISerializableObject> _pending = new();

    private int _nextId = 1;

    /// <summary>Gets the unique ID for the specified serializable object or assigns a new one if it does not already have an ID.</summary>
    /// <param name="obj">The serializable object for which to get or assign the ID. If null, returns 0.</param>
    /// <returns>The unique ID of the specified serializable object, or 0 if the object is null.</returns>
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

    /// <summary>Writes the ID reference of the specified serializable object to the binary writer, assigning a new ID if necessary.</summary>
    /// <param name="writer">The binary writer used to write the ID reference.</param>
    /// <param name="obj">The serializable object whose ID reference is to be written. Can be null.</param>
    public void WriteRef([NotNull] BinaryWriter writer, ISerializableObject? obj)
    {
        var id = GetOrAssignId(obj);
        writer.Write(id);
    }

    internal bool TryDequeue(out ISerializableObject? obj) => _pending.TryDequeue(out obj);

    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static ReferenceEqualityComparer<T> Default { get; } = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
