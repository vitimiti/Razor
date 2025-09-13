// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public static partial class ExtraMath
{
    private const int ArcTableSize = 0x0400;
    private const int SinTableSize = 0x0400;

    private static readonly float[] s_fastAcosTable = new float[ArcTableSize];
    private static readonly float[] s_fastAsinTable = new float[ArcTableSize];
    private static readonly float[] s_fastSinTable = new float[SinTableSize];

    public const float Sqrt2 = 1.41421356F;

    static ExtraMath()
    {
        for (var i = 0; i < ArcTableSize; i++)
        {
            var cv = (i - ArcTableSize / 2F) * (1F / (ArcTableSize / 2F));
            s_fastAcosTable[i] = float.Acos(cv);
            s_fastAsinTable[i] = float.Asin(cv);
        }

        for (var i = 0; i < SinTableSize; i++)
        {
            var vc = i * 2F * float.Pi / SinTableSize;
            s_fastSinTable[i] = float.Sin(vc);
        }
    }

    public static float InvSqrt(float value, bool fast = true)
    {
        if (!fast)
        {
            return 1F / float.Sqrt(value);
        }

        // Match IEEE behavior for special cases
        if (float.Abs(value) < float.Epsilon) // Basically 0F
        {
            return float.PositiveInfinity; // 1/sqrt(0) -> +Inf
        }

        if (value is < 0F or float.NaN)
        {
            return float.NaN;
        }

        if (float.IsPositiveInfinity(value))
        {
            return 0F; // 1/sqrt(Inf) -> 0
        }

        // Quake III fast inverse square root (no assembly)
        // Uses bit-level hack + two Newton-Raphson iterations for accuracy
        var x2 = value * .5F;
        var i = BitConverter.SingleToInt32Bits(value);
        i = 0x5F3759DF - (i >> 1);
        var y = BitConverter.Int32BitsToSingle(i);

        // 1st Newton-Raphson iteration
        y *= (1.5F - (x2 * y * y));
        // 2nd iteration: improves precision to near full float accuracy
        y *= (1.5F - (x2 * y * y));

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

        var idx0 = FastFloatToIntFloor(value);
        var idx1 = idx0 + 1;
        var frac = value - idx0;

        idx0 += ArcTableSize / 2;
        idx1 += ArcTableSize / 2;

        ArgumentOutOfRangeException.ThrowIfLessThan(idx0, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx0, ArcTableSize);
        ArgumentOutOfRangeException.ThrowIfLessThan(idx1, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx1, ArcTableSize);

        return (1F - frac) * s_fastAcosTable[idx0] + frac * s_fastAcosTable[idx1];
    }

    public static float FastSin(float value)
    {
        value *= SinTableSize / (2F * float.Pi);

        var idx0 = FastFloatToIntFloor(value);
        var idx1 = idx0 + 1;
        var frac = value - idx0;

        idx0 &= (SinTableSize - 1);
        idx1 &= (SinTableSize - 1);

        return (1F - frac) * s_fastSinTable[idx0] + frac * s_fastSinTable[idx1];
    }

    public static int FastFloatToIntFloor(float value)
    {
        var bits = BitConverter.SingleToInt32Bits(value);
        var sign = bits >> 31;
        var absBits = bits & 0x7FFFFFFF;

        // Handle NaN and Infinity
        if ((absBits & 0x7F800000) == 0x7F800000)
        {
            if ((absBits & 0x007FFFFF) != 0)
            {
                // NaN: match C# cast-to-int behavior (returns 0)
                return 0;
            }

            // Infinities clamp to int range
            return sign == 0 ? int.MaxValue : int.MinValue;
        }

        var exponent = (absBits >> 23) - 0x7F;

        switch (exponent)
        {
            // |value| < 1
            case < 0:
                // Floor(negative small) = -1, Floor(positive small) = 0, Floor(+/-0) = 0
                if (absBits == 0)
                {
                    return 0;
                }

                return sign != 0 ? -1 : 0;
            // Too large to fit into 32-bit int: clamp
            case >= 31:
                return sign == 0 ? int.MaxValue : int.MinValue;
        }

        var mantissa = absBits & 0x007FFFFF;

        // Compute integer magnitude r using the implicit leading 1
        // r = ((1<<exponent) * (mantissa | (1<<23))) >> 24
        var r = (int)(((uint)(mantissa | (1 << 23)) << 8) >> (31 - exponent));

        if (sign == 0)
        {
            return r; // positive numbers: just truncate toward zero which equals floor for non-negative
        }

        // Negative numbers: if there's a fractional part, subtract one more
        if (exponent >= 23)
        {
            // No fractional part possible
            return -r;
        }

        var fracMask = (1 << (23 - exponent)) - 1;
        var hasFraction = (mantissa & fracMask) != 0;

        return hasFraction ? -(r + 1) : -r;
    }

    public static int SystemRand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return NativeRand.Win32Rand();
        }

        if (
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
        )
        {
            return NativeRand.UnixRand();
        }

        // Fallback to .NET
        return Random.Shared.Next();
    }

    private static partial class NativeRand
    {
        [SupportedOSPlatform("windows")]
        [LibraryImport("msvcrt", EntryPoint = "rand")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int Win32Rand();

        // FEATURE: Add other UNIX platforms here?
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("linux")]
        [LibraryImport("libc.so", EntryPoint = "rand")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int UnixRand();
    }
}
