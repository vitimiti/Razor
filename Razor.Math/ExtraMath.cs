// -----------------------------------------------------------------------
// <copyright file="ExtraMath.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Math;

/// <summary>Provides additional mathematical utility functions not available in standard libraries.</summary>
public static class ExtraMath
{
    /// <summary>Determines whether a floating-point value is valid (not NaN or infinite).</summary>
    /// <param name="value">The floating-point value to check.</param>
    /// <returns><see langword="true"/> if the value is neither NaN nor infinite; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    /// <summary>Computes the inverse square root of a floating-point value using either fast approximation or standard calculation.</summary>
    /// <param name="value">The value to compute the inverse square root for.</param>
    /// <param name="fast">If <see langword="true"/>, uses the fast Quake III algorithm with Newton-Raphson refinement; if <see langword="false"/>, uses standard calculation.</param>
    /// <returns>The inverse square root of the input value (1 / sqrt(value)).</returns>
    /// <remarks>The fast implementation is based on the famous Quake III algorithm with three Newton-Raphson iterations for improved precision.</remarks>
    public static float InvSqrt(float value, bool fast = true)
    {
        if (!fast)
        {
            return 1F / float.Sqrt(value);
        }

        // Fast inverse square root implementation based on the original C++ assembly code
        // Using the original magic number and algorithm structure
        const float oneAndHalf = 1.5F;
        const uint magic = 0xBE6EB508; // Original magic number from the C++ code

        var a = value;
        unsafe
        {
            var aInt = *(uint*)&a;

            // Original algorithm: eax = 0xbe6eb508 - aInt, then aInt = aInt - 0x800000, then eax >>= 1
            var r0 = magic - aInt;
            aInt -= 0x800000; // a/2 -> this modifies our 'a' value (Y0)
            r0 >>= 1; // First approximation R0

            a = *(float*)&aInt; // Y0 = a/2
            var r = *(float*)&r0; // R0

            // First Newton-Raphson iteration: r1 = 1.5 - (r*r*y0)
            var y1 = r * r * a; // r*r*y0
            var r1 = oneAndHalf - y1;

            // Second iteration: y2 = y1*r1*r1, x2 = x1*r1, r2 = 1.5 - y2
            var y2 = y1 * r1 * r1;
            var x2 = r * r1; // x1*r1 (r was our initial x1)
            var r2 = oneAndHalf - y2;

            // Third iteration: y3 = y2*r2*r2, x3 = x2*r2, r3 = 1.5 - y3
            var y3 = y2 * r2 * r2;
            var x3 = x2 * r2;
            var r3 = oneAndHalf - y3;

            // Final result: x3 * r3
            return x3 * r3;
        }
    }
}
