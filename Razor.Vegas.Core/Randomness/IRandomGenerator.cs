// -----------------------------------------------------------------------
// <copyright file="IRandomGenerator.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Randomness;

/// <summary>
/// Interface for random number generators.
/// </summary>
public interface IRandomGenerator
{
    /// <summary>
    /// Gets the number of significant bits in the random number.
    /// </summary>
    int SignificantBits { get; }

    /// <summary>
    /// Gets a random number.
    /// </summary>
    /// <returns>A random number.</returns>
    int GetNumber();

    /// <summary>
    /// Gets a random number between the specified min and max values.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random number between the specified min and max values.</returns>
    int GetNumber(int min, int max);
}
