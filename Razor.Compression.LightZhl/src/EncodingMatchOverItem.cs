// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl;

internal readonly struct EncodingMatchOverItem(int symbol, int numberOfBits, ushort bits)
{
    public int Symbol { get; } = symbol;
    public int NumberOfBits { get; } = numberOfBits;
    public ushort Bits { get; } = bits;
}
