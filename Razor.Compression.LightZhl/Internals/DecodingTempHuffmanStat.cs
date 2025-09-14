// -----------------------------------------------------------------------
// <copyright file="DecodingTempHuffmanStat.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal readonly struct DecodingTempHuffmanStat(short i, short n)
    : IComparable<DecodingTempHuffmanStat>,
        IComparable,
        IEquatable<DecodingTempHuffmanStat>
{
    public readonly short I = i;
    private readonly short _n = n;

    public static bool operator ==(DecodingTempHuffmanStat x, DecodingTempHuffmanStat y) => x.Equals(y);

    public static bool operator !=(DecodingTempHuffmanStat x, DecodingTempHuffmanStat y) => !x.Equals(y);

    public static bool operator <(DecodingTempHuffmanStat x, DecodingTempHuffmanStat y) => x.CompareTo(y) < 0;

    public static bool operator >(DecodingTempHuffmanStat x, DecodingTempHuffmanStat y) => x.CompareTo(y) > 0;

    public static bool operator <=(DecodingTempHuffmanStat x, DecodingTempHuffmanStat y) => x.CompareTo(y) <= 0;

    public static bool operator >=(DecodingTempHuffmanStat x, DecodingTempHuffmanStat y) => x.CompareTo(y) >= 0;

    public bool Equals(DecodingTempHuffmanStat other) => I == other.I && _n == other._n;

    public override bool Equals(object? obj) => obj is DecodingTempHuffmanStat other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(I, _n);

    public int CompareTo(DecodingTempHuffmanStat other)
    {
        // Descendent by n, then descendent by i
        var compare = other._n - _n;
        return compare != 0 ? compare : other.I - I;
    }

    public int CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            DecodingTempHuffmanStat other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(DecodingTempHuffmanStat)}"),
        };
}
