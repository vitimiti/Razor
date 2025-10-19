// -----------------------------------------------------------------------
// <copyright file="RandomGenerator2.cs" company="Razor Project Authors">
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
/// It is nearly identical in function to the <see cref="RandomGenerator"/> class, but has the following improvements:
/// <list type="number">
/// <item>It generates random numbers very quickly. No multiplies are used in the algorithm.</item>
/// <item>The return value is a full 32 bits rather than 15 bits of the <see cref="RandomGenerator"/> class.</item>
/// <item>The bit pattern won't ever repeat (actually, it will repeat in about 10 to the 50th power times.)</item>
/// </list>
/// </remarks>
/// <warning>
/// This random number generator starts breaking down in 64 dimensions, behaving very badly in that domain.
/// </warning>
public class RandomGenerator2 : IRandomGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RandomGenerator2"/> class using the provided seed.
    /// </summary>
    /// <param name="seed">The seed value used to initialize the internal random table.</param>
    /// <remarks>
    /// The seed value is used to initialize the internal random table through a <see cref="RandomGenerator3"/> instance.
    /// </remarks>
    public RandomGenerator2(uint seed = 0)
    {
        RandomGenerator3 random = new(seed);
        for (var index = 0; index < Table.Count; index++)
        {
            Table[index] = random.GetNumber();
        }
    }

    /// <summary>
    /// Gets the number of significant bits produced by this generator (always 32).
    /// </summary>
    public int SignificantBits => 32;

    /// <summary>
    /// Gets or sets the first index into the internal state table.
    /// </summary>
    protected int Index1 { get; set; }

    /// <summary>
    /// Gets or sets the second index into the internal state table. Defaults to 103.
    /// </summary>
    protected int Index2 { get; set; } = 103;

    /// <summary>
    /// Gets the internal state table used by the generator. The table contains 250 integers.
    /// </summary>
    protected IList<int> Table { get; } = new int[250];

    /// <summary>
    /// Generates and returns the next 32-bit random number from the generator.
    /// </summary>
    /// <returns>A pseudo-random 32-bit integer.</returns>
    public int GetNumber()
    {
        Table[Index1] ^= Table[Index2];
        var value = Table[Index1];

        Index1++;
        Index2++;

        if (Index1 >= Table.Count)
        {
            Index1 = 0;
        }

        if (Index2 >= Table.Count)
        {
            Index2 = 0;
        }

        return value;
    }

    /// <summary>
    /// Returns a pseudo-random integer in the specified range [min, max].
    /// </summary>
    /// <param name="min">Inclusive minimum value of the range.</param>
    /// <param name="max">Inclusive maximum value of the range.</param>
    /// <returns>A pseudo-random integer between <paramref name="min"/> and <paramref name="max"/> inclusive.</returns>
    public int GetNumber(int min, int max) => RandomNumber.Pick(this, min, max);
}
