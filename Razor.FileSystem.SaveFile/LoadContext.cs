// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Razor.FileSystem.SaveFile;

/// <summary>Represents a context used during the loading of serialized objects, facilitating the resolution of references and deferred post-load operations.</summary>
public sealed class LoadContext
{
    private readonly Dictionary<int, ISerializableObject> _byId = [];
    private readonly List<ISerializableObject> _postLoad = [];

    /// <summary>Resolves a reference to an object of the specified type using its identifier, if available in the context.</summary>
    /// <typeparam name="T">The type of the object to resolve, which must implement <see cref="ISerializableObject"/>.</typeparam>
    /// <param name="id">The unique identifier of the object to resolve.</param>
    /// <returns>The object of the specified type if found; otherwise, null.</returns>
    public T? Resolve<T>(int id)
        where T : class, ISerializableObject => id == 0 ? null : ResolveSecondStep<T>(id);

    /// <summary>Defers the execution of the <see cref="ISerializableObject.OnPostLoad"/> method for the specified object until after all objects are loaded and references are resolved.</summary>
    /// <param name="obj">The object for which the post-load operation should be deferred.</param>
    public void DeferPostLoad(ISerializableObject obj) => _postLoad.Add(obj);

    /// <summary>Reads a reference identifier from the binary stream.</summary>
    /// <param name="reader">The binary reader from which to read the reference identifier.</param>
    /// <returns>The identifier of the reference read from the stream.</returns>
    public static int ReadRefId([NotNull] BinaryReader reader) => reader.ReadInt32();

    internal void RunPostLoad()
    {
        foreach (ISerializableObject obj in _postLoad)
        {
            obj.OnPostLoad(this);
        }

        _postLoad.Clear();
    }

    internal void Register(int id, ISerializableObject obj) => _byId[id] = obj;

    private T? ResolveSecondStep<T>(int id)
        where T : class, ISerializableObject => _byId.TryGetValue(id, out ISerializableObject? obj) ? (T)obj : null;
}
