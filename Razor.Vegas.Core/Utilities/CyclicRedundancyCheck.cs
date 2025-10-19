// -----------------------------------------------------------------------
// <copyright file="CyclicRedundancyCheck.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Vegas.Core.Utilities;

/// <summary>
/// Provides helpers for calculating CRC-32 checksums for memory buffers and strings.
/// </summary>
/// <remarks>
/// The methods provide convenience overloads for byte spans and strings and perform
/// the standard CRC-32 (reflected) calculation using a precomputed table.
/// </remarks>
public static partial class CyclicRedundancyCheck
{
    /// <summary>
    /// Computes a CRC-32 checksum for the provided data buffer.
    /// </summary>
    /// <param name="data">The input bytes to compute the CRC over.</param>
    /// <param name="crc">An optional initial CRC value to continue a previous computation. Defaults to 0.</param>
    /// <returns>The computed CRC-32 value.</returns>
    public static uint Memory(ReadOnlySpan<byte> data, uint crc = 0)
    {
        // Invert previous CRC
        crc ^= 0xFFFFFFFF;
        foreach (var b in data)
        {
            // Calculate CRC for each byte
            crc = CalculateCrc32(b, crc);
        }

        // Invert thew new CRC and return it
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// Computes a CRC-32 checksum for the provided <see cref="string"/> using the low byte of each <see cref="char"/>.
    /// </summary>
    /// <param name="str">The input string to compute the CRC for.</param>
    /// <param name="crc">An optional initial CRC value to continue a previous computation. Defaults to 0.</param>
    /// <returns>The computed CRC-32 value.</returns>
    public static uint CaseSensitiveString([NotNull] string str, uint crc = 0)
    {
        // Invert previous CRC
        crc ^= 0xFFFFFFFF;

        // Calculate CRC for each character
        crc = str.Aggregate(crc, (current, c) => CalculateCrc32(c, current));

        // Invert thew new CRC and return it
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// Computes a case-insensitive CRC-32 checksum for the provided string by normalizing to uppercase invariant culture.
    /// </summary>
    /// <param name="str">The input string to compute the case-insensitive CRC for.</param>
    /// <param name="crc">An optional initial CRC value to continue a previous computation. Defaults to 0.</param>
    /// <returns>The computed CRC-32 value.</returns>
    public static uint CaseInsensitiveString([NotNull] string str, uint crc = 0) =>
        CaseSensitiveString(str.ToUpperInvariant(), crc);

    private static uint CalculateCrc32(uint b, uint crc) => Crc32Table[(crc ^ b) & 0xFF] ^ ((crc >> 8) & 0x00FFFFFF);
}
