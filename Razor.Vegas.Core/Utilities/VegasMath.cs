// -----------------------------------------------------------------------
// <copyright file="VegasMath.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Utilities;

/// <summary>
/// Provides small utility math helpers used across the project.
/// </summary>
/// <remarks>
/// Contains number-theory helpers such as greatest common divisor (GCD) and least common multiple (LCM).
/// </remarks>
public static class VegasMath
{
    /// <summary>
    /// Computes the greatest common divisor (GCD) of two non-negative integers using the Euclidean algorithm.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>The greatest common divisor of <paramref name="left"/> and <paramref name="right"/>. If both
    /// arguments are zero, the method returns zero.</returns>
    public static uint GreatestCommonDivisor(uint left, uint right)
    {
        // This uses the Euclidean algorithm.
        while (true)
        {
            if (right == 0)
            {
                return left;
            }

            var left1 = left;
            left = right;
            right = left1 % right;
        }
    }

    /// <summary>
    /// Computes the least common multiple (LCM) of two non-negative integers.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>The least common multiple of <paramref name="left"/> and <paramref name="right"/>.
    /// If either argument is zero, the result is zero. The computation uses the formula
    /// LCM(a,b) = a / GCD(a,b) * b (implemented as <c>left * right / GCD(left,right)</c> here).</returns>
    public static uint LeastCommonMultiple(uint left, uint right) => left * right / GreatestCommonDivisor(left, right);
}
