// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Compression;

[PublicAPI]
public enum CompressionType
{
    None,
    RefPack,
    NoxLzh,
    ZLib1,
    ZLib2,
    ZLib3,
    ZLib4,
    ZLib5,
    ZLib6,
    ZLib7,
    ZLib8,
    ZLib9,
    BinaryTree,
    HuffmanWithRunlength,
}
