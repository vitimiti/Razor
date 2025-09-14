// -----------------------------------------------------------------------
// <copyright file="BigArchiveEntry.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.FileSystem.BigFile;

/// <summary>Represents an entry in a big archive with its offset and size information.</summary>
/// <param name="Offset">The offset of the entry in the archive.</param>
/// <param name="Size">The size of the entry in bytes.</param>
public record struct BigArchiveEntry(uint Offset, uint Size);
