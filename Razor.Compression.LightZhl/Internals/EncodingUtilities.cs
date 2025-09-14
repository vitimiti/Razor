// -----------------------------------------------------------------------
// <copyright file="EncodingUtilities.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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
