// -----------------------------------------------------------------------
// <copyright file="RandomGenerator4.cs" company="Razor Project Authors">
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
/// It is based on the paper "Mersenne Twister: A 623-Dimensionally Equidistributed Uniform Pseudo-Random Number Generator" by Makoto Matsumoto and Takuji Nishimura, ACM Transactions on Modeling and Computer Simulation, Vol 8, No 1, January 1998, pp 3--30.
/// <list type="number">
/// <item>The random number returned is very strongly random.</item>
/// <item>The return value is a full 32 bits rather than 15 bits of the <see cref="RandomGenerator"/> class.</item>
/// <item>The bit pattern won't repeat until 2 ^ 19937 - 1 times.</item>
/// </list>
/// </remarks>
/// <warning>
/// Do not use for cryptography. This random number generator is good for numerical simulations.
/// Optimized, it should be faster than native `rand()` implementations.
/// </warning>
/// <seealso href="https://en.wikipedia.org/wiki/Mersenne_Twister"/>
/// <seealso href="http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html"/>
/// <seealso href="http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/ARTICLES/mt.pdf"/>
/// <seealso href="http://www.math.keio.ac.jp/~matumoto/emt.hml"/>
public class RandomGenerator4 : IRandomGenerator
{
    // Period parameters
    private const int N = 624;
    private const int M = 397;

    // Constant vector a
    private const uint MatrixA = 0x9908B0DF;

    // Most significant w-r bits
    private const uint UpperMask = 0x80000000;

    // Least significant r bits
    private const uint LowerMask = 0x7FFFFFFF;

    // Tempering parameters
    private const uint TemperingMaskB = 0x9D2C5680;
    private const uint TemperingMaskC = 0xEFC60000;

    // mag01[x] = x * MATRIX_A  for x=0,1
    private static readonly uint[] _mag01 = [0x0, MatrixA];

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomGenerator4"/> class using the specified seed.
    /// </summary>
    /// <param name="seed">Seed value used to initialize the generator. If zero, a small non-zero default is used.</param>
    public RandomGenerator4(uint seed = 4357)
    {
        if (seed == 0)
        {
            seed = 4375;
        }

        Mt[0] = seed & 0xFFFFFFFF;
        for (Mti = 1; Mti < N; Mti++)
        {
            Mt[Mti] = (69069 * Mt[Mti - 1]) & 0xFFFFFFFF;
        }

        // Mti is set to N+1 by constructor
    }

    /// <summary>
    /// Gets the number of significant bits produced by this generator (always 32).
    /// </summary>
    public int SignificantBits => 32;

    /// <summary>
    /// Gets the internal state vector (MT) for the generator.
    /// </summary>
    protected uint[] Mt { get; } = new uint[624];

    /// <summary>
    /// Gets or sets the current index into the internal state vector.
    /// </summary>
    protected int Mti { get; set; }

    /// <summary>
    /// Generates and returns the next 32-bit pseudo-random integer from the generator.
    /// </summary>
    /// <returns>A 32-bit pseudo-random integer.</returns>
    public int GetNumber()
    {
        uint y;

        // Generate N words at one time
        if (Mti >= N)
        {
            int kk;
            for (kk = 0; kk < N - M; kk++)
            {
                y = (Mt[kk] & UpperMask) | (Mt[kk + 1] & LowerMask);
                Mt[kk] = Mt[kk + M] ^ (y >> 1) ^ _mag01[y & 0x1];
            }

            for (; kk < N - 1; kk++)
            {
                y = (Mt[kk] & UpperMask) | (Mt[kk + 1] & LowerMask);
                Mt[kk] = Mt[kk + (M - N)] ^ (y >> 1) ^ _mag01[y & 0x1];
            }

            y = (Mt[N - 1] & UpperMask) | (Mt[0] & LowerMask);
            Mt[N - 1] = Mt[M - 1] ^ (y >> 1) ^ _mag01[y & 0x1];

            Mti = 0;
        }

        y = Mt[Mti++];
        y ^= TemperingShiftU(y);
        y ^= TemperingShiftS(y) & TemperingMaskB;
        y ^= TemperingShiftT(y) & TemperingMaskC;
        y ^= TemperingShiftL(y);

        return unchecked((int)y);
    }

    /// <summary>
    /// Returns a pseudo-random integer in the specified inclusive range.
    /// </summary>
    /// <param name="min">Inclusive minimum value.</param>
    /// <param name="max">Inclusive maximum value.</param>
    /// <returns>A pseudo-random integer between <paramref name="min"/> and <paramref name="max"/> inclusive.</returns>
    public int GetNumber(int min, int max) => RandomNumber.Pick(this, min, max);

    /// <summary>
    /// Returns a single-precision floating-point random value in the interval [0,1).
    /// </summary>
    /// <returns>A float uniformly distributed in [0,1).</returns>
    public float GetFloat()
    {
        var x = GetNumber();
        var y = unchecked((uint)x);

        return y * 2.3283064370807973754314699618685e-10F;
    }

    private static uint TemperingShiftU(uint y) => y >> 11;

    private static uint TemperingShiftS(uint y) => y << 7;

    private static uint TemperingShiftT(uint y) => y << 15;

    private static uint TemperingShiftL(uint y) => y >> 18;
}
