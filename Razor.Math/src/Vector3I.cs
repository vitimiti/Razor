// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Vector3I : IEqualityComparer<Vector3I>
{
    public int I { get; set; }
    public int J { get; set; }
    public int K { get; set; }

    public Vector3I() { }

    public Vector3I(int i, int j, int k)
    {
        I = i;
        J = j;
        K = k;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector3I other && Equals(this, other);
    }

    public bool Equals(Vector3I? left, Vector3I? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null)
        {
            return false;
        }

        if (right is null)
        {
            return false;
        }

        if (left.GetType() != right.GetType())
        {
            return false;
        }

        return left.I == right.I && left.J == right.J && left.K == right.K;
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Vector3I obj)
    {
        return HashCode.Combine(obj.I, obj.J, obj.K);
    }

    public override string ToString()
    {
        return $"({I}, {J}, {K})";
    }

    public string ToString(string? format)
    {
        return $"({I.ToString(format)}, {J.ToString(format)}, {K.ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({I.ToString(format, formatProvider)}, {J.ToString(format, formatProvider)}, {K.ToString(format, formatProvider)})";
    }

    public (int I, int J, int K) Deconstruct()
    {
        return (I, J, K);
    }

    public int this[int index]
    {
        get =>
            index switch
            {
                0 => I,
                1 => J,
                2 => K,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "The index must be between 0 and 2."
                ),
            };
        set
        {
            switch (index)
            {
                case 0:
                    I = value;
                    break;
                case 1:
                    J = value;
                    break;
                case 2:
                    K = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        index,
                        "The index must be between 0 and 2."
                    );
            }
        }
    }

    public static Vector3I operator +(Vector3I left, Vector3I right)
    {
        return new Vector3I(left.I + right.I, left.J + right.J, left.K + right.K);
    }

    public static Vector3I operator -(Vector3I left, Vector3I right)
    {
        return new Vector3I(left.I - right.I, left.J - right.J, left.K - right.K);
    }

    public static bool operator ==(Vector3I left, Vector3I right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3I left, Vector3I right)
    {
        return !left.Equals(right);
    }
}
