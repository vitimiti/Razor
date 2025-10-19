// -----------------------------------------------------------------------
// <copyright file="RandomGenerator3.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Randomness;

/// <summary>
/// This class functions like a "magic" number where it returns a different value every time it is read.
/// </summary>
/// <remarks>
/// It is nearly identical in function to the <see cref="RandomGenerator"/> and <see cref="RandomGenerator2"/> classes, but has the following improvements.
/// <list type="number">
/// <item>The random number returned is very strongly random. Approaching cryptographic quality.</item>
/// <item>The return value is a full 32 bits rather than 15 bits of the <see cref="RandomGenerator"/> class.</item>
/// <item>The bit pattern won't repeat until 2^32 times.</item>
/// </list>
/// </remarks>
/// <warning>
/// This random number generator starts breaking down in 3 dimensions, exhibiting a strange bias.
/// </warning>
public class RandomGenerator3(uint seed1 = 0, uint seed2 = 0) : IRandomGenerator
{
    /// <summary>
    /// Gets the number of significant bits produced by this generator (always 32).
    /// </summary>
    public int SignificantBits => 32;

    /// <summary>
    /// Gets the first static mix table used by the mixing function.
    /// </summary>
    protected static IList<int> Mix1 { get; } =
    [
        unchecked((int)0xBAA96887),
        0x1E17D32C,
        0x03BCDC3C,
        0x0F33D1B2,
        0x76A6491D,
        unchecked((int)0xC570D85D),
        unchecked((int)0xE382B1E3),
        0x78DB4362,
        0x7439A9D4,
        unchecked((int)0x9CEA8AC5),
        unchecked((int)0x89537C5C),
        0x2588F55D,
        0x415B5E1D,
        0x216E3D95,
        unchecked((int)0x85C662E7),
        0x5E8AB368,
        0x3EA5CC8C,
        unchecked((int)0xD26A0F74),
        unchecked((int)0xF3A9222B),
        0x48AAD7E4,
    ];

    /// <summary>
    /// Gets the second static mix table used by the mixing function.
    /// </summary>
    protected static IList<int> Mix2 { get; } =
    [
        0x4B0F3B58,
        unchecked((int)0xE874F0C3),
        0x6955C5A6,
        0x55A7CA46,
        0x4D9A9D86,
        unchecked((int)0xFE28A195),
        unchecked((int)0xB1CA7865),
        0x6B235751,
        unchecked((int)0x9A997A61),
        unchecked((int)0xAA6E95C8),
        unchecked((int)0xAAA98EE1),
        0x5AF9154C,
        unchecked((int)0xFC8E2263),
        0x390F5E8C,
        0x58FFD802,
        unchecked((int)0xAC0A5EBA),
        unchecked((int)0xAC4874F6),
        unchecked((int)0xA9DF0913),
        unchecked((int)0x86BE4C74),
        unchecked((int)0xED2C123B),
    ];

    /// <summary>
    /// Gets or sets the evolving seed (low word) used by the generator.
    /// </summary>
    protected int Seed { get; set; } = (int)seed1;

    /// <summary>
    /// Gets or sets the evolving index (high word) used by the generator.
    /// </summary>
    protected int Index { get; set; } = (int)seed2;

    /// <summary>
    /// Generates and returns the next 32-bit pseudo-random number.
    /// </summary>
    /// <returns>A pseudo-random 32-bit integer.</returns>
    public int GetNumber()
    {
        var loWord = Seed;
        var hiWord = Index++;
        for (var i = 0; i < 4; i++)
        {
            var hiHold = hiWord;
            var temp = hiHold ^ Mix1[i];
            var iTempLow = temp & 0xFFFF;
            var iTempHigh = temp >> 16;

            temp = (int)float.Pow(iTempLow, 2) + ~(int)float.Pow(iTempHigh, 2);
            temp = (temp >> 16) | (temp << 16);
            hiWord = loWord & ((temp & Mix2[i]) + (iTempLow * iTempHigh));
            loWord = hiHold;
        }

        return loWord;
    }

    /// <summary>
    /// Returns a pseudo-random integer in the specified inclusive range.
    /// </summary>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value.</param>
    /// <returns>A pseudo-random integer between <paramref name="min"/> and <paramref name="max"/> inclusive.</returns>
    public int GetNumber(int min, int max) => RandomNumber.Pick(this, min, max);
}
