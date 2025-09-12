// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Razor.FileSystem.Chunks;

namespace Razor.FileSystem.SaveLoadSystems;

[PublicAPI]
public interface IPersistFactory
{
    uint ChunkId { get; }

    IPersistable Load(ChunkReader reader);
    void Save(ChunkWriter writer, IPersistable obj);
}
