// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.FileSystem.Io;

[PublicAPI]
public record struct IoQuaternion()
{
    public float[] Q { get; init; } = new float[4];
}
