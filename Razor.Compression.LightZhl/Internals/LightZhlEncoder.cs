// -----------------------------------------------------------------------
// <copyright file="LightZhlEncoder.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Compression.LightZhl.Internals;

internal static class LightZhlEncoder
{
    public static int CalculateMaxCompressedSize(int rawSz) => rawSz + (rawSz >> 1) + 32;

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "We want to return false in case of an exception."
    )]
    public static bool Encode(ReadOnlySpan<byte> src, Span<byte> dst, out int written)
    {
        var comp = new Compressor();
        try
        {
            written = comp.Compress(dst, src);
            return true;
        }
        catch
        {
            written = 0;
            return false;
        }
    }
}
