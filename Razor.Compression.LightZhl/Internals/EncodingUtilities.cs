// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl.Internals;

internal static class EncodingUtilities
{
    private const int BufferMask = EncodingGlobals.BufferSize - 1;

    public static int Wrap(int position) => position & BufferMask;

    public static int Distance(int difference) => difference & BufferMask;

    public static void ShellSort(Span<EncodingTempHuffStat> source)
    {
        var length = source.Length;
        var gaps = new[] { 40, 13, 4, 1 };
        foreach (var h in gaps)
        {
            for (var i = h; i < length; ++i)
            {
                EncodingTempHuffStat v = source[i];
                var j = i;
                while (j >= h && v < source[j - h])
                {
                    source[j] = source[j - h];
                    j -= h;
                }

                source[j] = v;
            }
        }
    }
}
