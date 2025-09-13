// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.FileSystem.SaveFile;

[PublicAPI]
public interface ISerializableObject
{
    void Write(BinaryWriter writer, SaveContext context);
    void Read(BinaryReader reader, LoadContext context);
    void OnPostLoad(LoadContext context);
}
