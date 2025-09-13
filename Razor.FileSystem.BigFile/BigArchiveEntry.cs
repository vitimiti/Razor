// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.FileSystem.BigFile;

/// <summary>Represents an entry in a big archive with its offset and size information.</summary>
/// <param name="Offset">The offset of the entry in the archive.</param>
/// <param name="Size">The size of the entry in bytes.</param>
public record struct BigArchiveEntry(uint Offset, uint Size);
