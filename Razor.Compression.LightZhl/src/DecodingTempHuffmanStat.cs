// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl;

internal readonly struct DecodingTempHuffmanStat(short i, short n)
    : IComparable<DecodingTempHuffmanStat>,
        IComparable,
        IEquatable<DecodingTempHuffmanStat>
{
    public readonly short I = i;
    private readonly short _n = n;

    public bool Equals(DecodingTempHuffmanStat other)
    {
        return I == other.I && _n == other._n;
    }

    public override bool Equals(object? obj)
    {
        return obj is DecodingTempHuffmanStat other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(I, _n);
    }

    public int CompareTo(DecodingTempHuffmanStat other)
    {
        // Descendent by n, then descendent by i
        var compare = other._n - _n;
        if (compare != 0)
        {
            return compare;
        }

        return other.I - I;
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        return obj is DecodingTempHuffmanStat other
            ? CompareTo(other)
            : throw new ArgumentException(
                $"Object must be of type {nameof(DecodingTempHuffmanStat)}"
            );
    }

    public static bool operator ==(DecodingTempHuffmanStat left, DecodingTempHuffmanStat right)
    {
        return left.CompareTo(right) == 0;
    }

    public static bool operator !=(DecodingTempHuffmanStat left, DecodingTempHuffmanStat right)
    {
        return left.CompareTo(right) != 0;
    }

    public static bool operator <(DecodingTempHuffmanStat left, DecodingTempHuffmanStat right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(DecodingTempHuffmanStat left, DecodingTempHuffmanStat right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(DecodingTempHuffmanStat left, DecodingTempHuffmanStat right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(DecodingTempHuffmanStat left, DecodingTempHuffmanStat right)
    {
        return left.CompareTo(right) >= 0;
    }
}
