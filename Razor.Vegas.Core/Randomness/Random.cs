// -----------------------------------------------------------------------
// <copyright file="Random.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Randomness;

/// <summary>
/// Randomness-related helpers and high-level convenience wrappers used by the Razor.Vegas.Core project.
/// </summary>
/// <remarks>
/// Provides convenience wrappers around a low-level generator (<see cref="RandomGenerator2"/>) to produce
/// 32-bit integers and floating point values. Integer methods return full 32-bit values or values constrained by the
/// supplied bounds; float methods return values in the requested interval inclusive.
/// </remarks>
public class Random
{
    private const int FloatRange = 0x1000;

    private readonly RandomGenerator2 _generator = new();

    /// <summary>
    /// Gets or sets a shared free random instance intended for non-deterministic uses (visual or audio effects) where synchronization is not required.
    /// </summary>
    public static Random? FreeRandom { get; set; }

    /// <summary>
    /// Returns a pseudo-random 32-bit signed integer.
    /// </summary>
    /// <returns>A pseudo-random Int32 value.</returns>
    public int GetInt32() => _generator.GetNumber();

    /// <summary>
    /// Returns a pseudo-random non-negative integer less than the specified <paramref name="max"/>.
    /// </summary>
    /// <param name="max">Exclusive upper bound for the generated value; must be greater than zero.</param>
    /// <returns>An integer in the range [0, <paramref name="max"/> - 1].</returns>
    public int GetInt32(int max)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(max, 0);
        return (_generator.GetNumber() & 0x7FFFFFFF) % max;
    }

    /// <summary>
    /// Returns a pseudo-random integer between <paramref name="min"/> and <paramref name="max"/> (both inclusive).
    /// </summary>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <returns>An integer in the inclusive range [<paramref name="min"/>, <paramref name="max"/>].</returns>
    public int GetInt32(int min, int max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        return GetInt32(max - min) + min;
    }

    /// <summary>
    /// Returns a pseudo-random single-precision floating point value in the interval [0, 1].
    /// </summary>
    /// <returns>A float in the interval [0, 1].</returns>
    public float GetFloat() => GetInt32(FloatRange + 1) / (float)FloatRange;

    /// <summary>
    /// Returns a pseudo-random single-precision floating point value between 0 and <paramref name="max"/> (both inclusive).
    /// </summary>
    /// <param name="max">Maximum value of the returned range.</param>
    /// <returns>A float in the interval [0, <paramref name="max"/>].</returns>
    public float GetFloat(float max) => GetFloat() * max;

    /// <summary>
    /// Returns a pseudo-random single-precision floating point value between <paramref name="min"/> and <paramref name="max"/> (both inclusive).
    /// </summary>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <returns>A float in the inclusive interval [<paramref name="min"/>, <paramref name="max"/>].</returns>
    public float GetFloat(float min, float max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        return (GetFloat() * (max - min)) + min;
    }
}
