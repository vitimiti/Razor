// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.RefPack;

/// <summary>The mode of the <see cref="RefPackStream" />.</summary>
public enum RefPackMode
{
    /// <summary>The RefPack stream is being used to compress a file.</summary>
    Compression,

    /// <summary>
    ///     The RefPack stream is being used to decompress a file.
    /// </summary>
    Decompression,
}
