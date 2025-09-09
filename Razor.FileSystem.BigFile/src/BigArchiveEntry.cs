// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.FileSystem.BigFile;

[PublicAPI]
public record struct BigArchiveEntry(uint Offset, uint Size);
