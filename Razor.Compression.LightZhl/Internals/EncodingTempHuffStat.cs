// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl.Internals;

internal readonly struct EncodingTempHuffStat(short i, short n)
    : IComparable<EncodingTempHuffStat>,
        IComparable,
        IEquatable<EncodingTempHuffStat>
{
    public short I { get; } = i;
    public short N { get; } = n;

    public bool Equals(EncodingTempHuffStat other) => I == other.I && N == other.N;

    public override bool Equals(object? obj) => obj is EncodingTempHuffStat other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(I, N);

    public int CompareTo(EncodingTempHuffStat other)
    {
        var cmp = other.N - N;
        return (cmp != 0) ? cmp : other.I - I;
    }

    public int CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            EncodingTempHuffStat other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(EncodingTempHuffStat)}"),
        };

    public static bool operator ==(EncodingTempHuffStat x, EncodingTempHuffStat y) => x.CompareTo(y) == 0;

    public static bool operator !=(EncodingTempHuffStat x, EncodingTempHuffStat y) => x.CompareTo(y) != 0;

    public static bool operator <(EncodingTempHuffStat x, EncodingTempHuffStat y) => x.CompareTo(y) < 0;

    public static bool operator >(EncodingTempHuffStat x, EncodingTempHuffStat y) => x.CompareTo(y) > 0;

    public static bool operator <=(EncodingTempHuffStat x, EncodingTempHuffStat y) => x.CompareTo(y) <= 0;

    public static bool operator >=(EncodingTempHuffStat x, EncodingTempHuffStat y) => x.CompareTo(y) >= 0;
}
