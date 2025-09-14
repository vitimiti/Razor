// -----------------------------------------------------------------------
// <copyright file="DecodingGroup.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal readonly struct DecodingGroup(int numberOfBits, int position)
{
    public readonly int NumberOfBits = numberOfBits;
    public readonly int Position = position;
}
