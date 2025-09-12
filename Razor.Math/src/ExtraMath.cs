// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public static class ExtraMath
{
    private const int ArcTableSize = 0x0400;

    private static readonly float[] s_fastAcosTable = new float[ArcTableSize];

    public const float Sqrt2 = 1.41421356F;

    static ExtraMath() { }

    public static float InvSqrt(float value, bool fast = true)
    {
        if (!fast)
        {
            return 1F / float.Sqrt(value);
        }

        switch (value)
        {
            // Match IEEE behavior for special cases
            case < float.Epsilon: // Basically 0F
                return float.PositiveInfinity; // 1/sqrt(0) -> +Inf
            case < 0f or float.NaN:
                return float.NaN;
        }

        if (float.IsPositiveInfinity(value))
        {
            return 0f; // 1/sqrt(Inf) -> 0
        }

        // Quake III fast inverse square root (no assembly)
        // Uses bit-level hack + two Newton-Raphson iterations for accuracy
        var x2 = value * 0.5f;
        var i = BitConverter.SingleToInt32Bits(value);
        i = 0x5F3759DF - (i >> 1);
        var y = BitConverter.Int32BitsToSingle(i);

        // 1st Newton-Raphson iteration
        y *= (1.5f - (x2 * y * y));
        // 2nd iteration: improves precision to near full float accuracy
        y *= (1.5f - (x2 * y * y));

        return y;
    }

    public static bool IsValid(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static float FastAcos(float value)
    {
        if (float.Abs(value) > 0.975F)
        {
            return float.Acos(value);
        }

        value *= ArcTableSize / 2F;

        var idx0 = (int)float.Floor(value);
        var idx1 = idx0 + 1;
        var frac = value - idx0;

        idx0 += ArcTableSize / 2;
        idx1 += ArcTableSize / 2;

        ArgumentOutOfRangeException.ThrowIfLessThan(idx0, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx0, ArcTableSize);
        ArgumentOutOfRangeException.ThrowIfLessThan(idx1, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx1, ArcTableSize);

        throw new NotImplementedException();
        // return (1F - frac) *
    }
}
