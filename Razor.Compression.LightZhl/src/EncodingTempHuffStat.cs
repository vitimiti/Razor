// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl;

internal readonly struct EncodingTempHuffStat(short i, short n)
    : IComparable<EncodingTempHuffStat>,
        IComparable,
        IEquatable<EncodingTempHuffStat>
{
    public short I { get; } = i;
    public short N { get; } = n;

    public bool Equals(EncodingTempHuffStat other)
    {
        return I == other.I && N == other.N;
    }

    public override bool Equals(object? obj)
    {
        return obj is EncodingTempHuffStat other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(I, N);
    }

    public int CompareTo(EncodingTempHuffStat other)
    {
        var cmp = other.N - N;
        return (cmp != 0) ? cmp : other.I - I;
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        return obj is EncodingTempHuffStat other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(EncodingTempHuffStat)}");
    }

    public static bool operator ==(EncodingTempHuffStat left, EncodingTempHuffStat right)
    {
        return left.CompareTo(right) == 0;
    }

    public static bool operator !=(EncodingTempHuffStat left, EncodingTempHuffStat right)
    {
        return left.CompareTo(right) != 0;
    }

    public static bool operator <(EncodingTempHuffStat left, EncodingTempHuffStat right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(EncodingTempHuffStat left, EncodingTempHuffStat right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(EncodingTempHuffStat left, EncodingTempHuffStat right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(EncodingTempHuffStat left, EncodingTempHuffStat right)
    {
        return left.CompareTo(right) >= 0;
    }
}
