// -----------------------------------------------------------------------
// <copyright file="RandomNumber.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Vegas.Core.Randomness;

/// <summary>
/// Provides functionality to pick random numbers from a range.
/// </summary>
/// <remarks>
/// This class is a port of the <c>RandomNumber</c> class from the legacy
/// <c>Razor.Vegas.Core</c> library.
/// </remarks>
internal static class RandomNumber
{
    /// <summary>
    /// Picks a random number from the specified range.
    /// </summary>
    /// <param name="generator">The random number generator to use.</param>
    /// <param name="min">The minimum value of the range.</param>
    /// <param name="max">The maximum value of the range.</param>
    /// <returns>A random number from the specified range.</returns>
    public static int Pick([NotNull] IRandomGenerator generator, int min, int max)
    {
        // Test for the shortcut case where the range is null and thus
        // the number to return is actually implicit from the
        // parameters.
        if (min == max)
        {
            return min;
        }

        // Ensure that the min and max range values are in proper order.
        if (min > max)
        {
            (min, max) = (max, min);
        }

        // Find the highest bit that fits within the magnitued of the
        // range of random numbers desired. Notice that the scan is
        // limited to the range of significant bits returned by the
        // random number algorithm.
        var magnitude = max - min;
        var highBit = generator.SignificantBits - 1;
        while ((magnitude & (1 << highBit)) == 0 && highBit > 0)
        {
            highBit--;
        }

        // Create a full bit mask pattern that has all bits set that just
        // barely covers the magnitude of the number range desired.
        var mask = ~(~0 << (highBit + 1));

        // Keep picking random numbers until it fits within the magnitude desired.
        // With a good random number generator, it will have to perform this loop
        // an average of one and a half times.
        var pick = magnitude + 1;
        while (pick > magnitude)
        {
            pick = generator.GetNumber() & mask;
        }

        // Finally, bias the random number pick to the start of the range
        // requested.
        return pick + min;
    }
}
