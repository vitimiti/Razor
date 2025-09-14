// -----------------------------------------------------------------------
// <copyright file="EncodingMatchOverItem.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal readonly struct EncodingMatchOverItem(int symbol, int numberOfBits, ushort bits)
{
    public int Symbol { get; } = symbol;

    public int NumberOfBits { get; } = numberOfBits;

    public ushort Bits { get; } = bits;
}
