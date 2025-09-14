// -----------------------------------------------------------------------
// <copyright file="DecodingDisplayPrefixItem.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal readonly struct DecodingDisplayPrefixItem(int numberOfBits, int display)
{
    public readonly int NumberOfBits = numberOfBits;
    public readonly int Display = display;
}
