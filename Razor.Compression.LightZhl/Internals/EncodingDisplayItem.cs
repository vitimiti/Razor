// -----------------------------------------------------------------------
// <copyright file="EncodingDisplayItem.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal readonly struct EncodingDisplayItem(int numberOfBits, ushort bits)
{
    public int NumberOfBits { get; } = numberOfBits;

    public ushort Bits { get; } = bits;
}
