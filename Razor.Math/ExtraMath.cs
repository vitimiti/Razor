// -----------------------------------------------------------------------
// <copyright file="ExtraMath.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Razor.Math;

/// <summary>Provides additional mathematical utility functions not available in standard libraries.</summary>
public static partial class ExtraMath
{
    /// <summary>The square root of 2 (approximately 1.414213562).</summary>
    public const float Sqrt2 = 1.41421356F;

    private const int ArcTableSize = 1024;
    private const int SinTableSize = 1024;

    private static readonly float[] FastAcosTable = new float[ArcTableSize];
    private static readonly float[] FastSinTable = new float[SinTableSize];

    /// <summary>Initializes lookup tables for fast trigonometric functions.</summary>
    /// <remarks>This method populates internal tables used by <see cref="FastAcos(float)"/> and <see cref="FastSin(float)"/>. It should be called once during application initialization for optimal performance.</remarks>
    public static void Initialize()
    {
        for (var i = 0; i < ArcTableSize; i++)
        {
            var cv = (i - (ArcTableSize / 2F)) * (1F / (ArcTableSize / 2F));
            FastAcosTable[i] = float.Acos(cv);
            FastSinTable[i] = float.Sin(cv);
        }
    }

    /// <summary>Determines whether a floating-point value is valid (not NaN or infinite).</summary>
    /// <param name="value">The floating-point value to check.</param>
    /// <returns><see langword="true"/> if the value is neither NaN nor infinite; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    /// <summary>Converts a floating-point value to an integer using floor operation.</summary>
    /// <param name="value">The floating-point value to convert.</param>
    /// <returns>The largest integer less than or equal to the specified value.</returns>
    public static int FloatToIntFloor(float value) => (int)float.Floor(value);

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

    /// <summary>Computes an approximation of the inverse cosine using a lookup table.</summary>
    /// <param name="value">The input value, typically in the range [-1, 1].</param>
    /// <returns>The inverse cosine of the input value in radians.</returns>
    /// <remarks>For values with absolute value greater than 0.975, falls back to <see cref="float.Acos(float)"/> for accuracy. Uses linear interpolation between table entries for intermediate values.</remarks>
    public static float FastAcos(float value)
    {
        // Near -1 and +1, the table becomes too inaccurate
        if (float.Abs(value) > 0.975F)
        {
            return float.Acos(value);
        }

        value *= ArcTableSize / 2F;
        var idx0 = FloatToIntFloor(value);
        var idx1 = idx0 + 1;
        var frac = value - idx0;

        idx0 += ArcTableSize / 2;
        idx1 += ArcTableSize / 2;

        ArgumentOutOfRangeException.ThrowIfLessThan(idx0, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx0, ArcTableSize);

        ArgumentOutOfRangeException.ThrowIfLessThan(idx1, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx1, ArcTableSize);

        return 1F - (frac * FastAcosTable[idx0]) + (frac * FastAcosTable[idx1]);
    }

    /// <summary>Computes an approximation of the sine function using a lookup table.</summary>
    /// <param name="value">The angle in radians.</param>
    /// <returns>The sine of the specified angle.</returns>
    /// <remarks>Uses linear interpolation between precomputed table values for performance. The input value is automatically wrapped to the appropriate range.</remarks>
    public static float FastSin(float value)
    {
        value *= SinTableSize / (2F * float.Pi);

        var idx0 = FloatToIntFloor(value);
        var idx1 = idx0 + 1;
        var frac = value - idx0;

        idx0 &= SinTableSize - 1;
        idx1 &= SinTableSize - 1;

        return ((1F - frac) * FastSinTable[idx0]) + (frac * FastSinTable[idx1]);
    }

    /// <summary>Gets a platform-specific random integer using native system libraries.</summary>
    /// <returns>A random integer from the platform's native random number generator, or from <see cref="Random.Shared"/> if no platform-specific implementation is available.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>On Windows: Uses msvcrt.dll's rand() function</description></item>
    /// <item><description>On macOS: Uses libc's rand() function</description></item>
    /// <item><description>On Linux: Uses libc.so.6's rand() function</description></item>
    /// <item><description>On other platforms: Falls back to <see cref="Random.Shared"/></description></item>
    /// </list>
    /// </remarks>
    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "This is a wrapper for a native function and the fallback is fine as security is not a concern here."
    )]
    [SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "This would cause a large, concatenated chain of ternary operators, which are more difficult to read."
    )]
    [SuppressMessage(
        "ReSharper",
        "ConvertIfStatementToReturnStatement",
        Justification = "This would cause a large, concatenated chain of ternary operators, which are more difficult to read."
    )]
    public static int SysRand()
    {
        if (OperatingSystem.IsWindows())
        {
            return NativeImports.Win32Rand();
        }

        if (OperatingSystem.IsMacOS())
        {
            return NativeImports.OsxRand();
        }

        if (OperatingSystem.IsLinux())
        {
            return NativeImports.LinuxRand();
        }

        return Random.Shared.Next();
    }

    private static partial class NativeImports
    {
        [SupportedOSPlatform("windows")]
        [LibraryImport("msvcrt", EntryPoint = "rand")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static partial int Win32Rand();

        [SupportedOSPlatform("osx")]
        [LibraryImport("libc", EntryPoint = "rand")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static partial int OsxRand();

        [SupportedOSPlatform("linux")]
        [LibraryImport("libc.so.6", EntryPoint = "rand")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static partial int LinuxRand();
    }
}
