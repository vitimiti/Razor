// -----------------------------------------------------------------------
// <copyright file="RandomGenerator.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Randomness;

/// <summary>
/// This class functions like a "magic" <see cref="int"/> value that returns a random number every time it is read.
/// To set the "random seed" for this, just assign a number to the constructor.
/// Take note that although this class will return an <see cref="int"/>, the actual significance of the random number is
/// limited to 15 bits (0..32767.)
/// </summary>
/// <param name="seed">The seed value used to initialize the internal random table.</param>
public class RandomGenerator(uint seed = 0) : IRandomGenerator
{
    /// <summary>
    /// The magic multiplier used to generate the random number.
    /// </summary>
    protected const uint KMultiplier = 0x41C64E6D;

    /// <summary>
    /// The magic additive used to generate the random number.
    /// </summary>
    protected const uint KAdditive = 0x00003039;

    /// <summary>
    /// The number of bits to throw away from the end of the random number.
    /// </summary>
    protected const int ThrowAwayBits = 10;

    /// <summary>
    /// Gets the number of significant bits in the random number.
    /// </summary>
    public int SignificantBits => 15;

    /// <summary>
    /// Gets or sets the seed value.
    /// </summary>
    protected uint Seed { get; set; } = seed;

    /// <summary>
    /// Gets the next random number.
    /// </summary>
    /// <returns>The next random number.</returns>
    public int GetNumber()
    {
        // Transform the seed value into the next number in the sequence.
        Seed = (Seed * KMultiplier) + KAdditive;
        return (int)((Seed >> ThrowAwayBits) & ~(~0 << SignificantBits));
    }

    /// <summary>
    /// Gets a random number between the specified min and max values.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random number between the specified min and max values.</returns>
    public int GetNumber(int min, int max) => RandomNumber.Pick(this, min, max);
}
