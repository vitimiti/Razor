// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl;

internal static class LightZhlEncoder
{
    public static int CalcMaxCompressedSize(int rawSz) => rawSz + (rawSz >> 1) + 32;

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
