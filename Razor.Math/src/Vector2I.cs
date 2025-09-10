// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Vector2I : IEqualityComparer<Vector2I>
{
    public int I { get; set; }
    public int J { get; set; }

    public Vector2I() { }

    public Vector2I(int i, int j)
    {
        Set(i, j);
    }

    public static void Swap(ref Vector2I left, ref Vector2I right)
    {
        (left, right) = (right, left);
    }

    public void Set(int i, int j)
    {
        I = i;
        J = j;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2I other && Equals(this, other);
    }

    public bool Equals(Vector2I? left, Vector2I? right)
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

        return left.I == right.I && left.J == right.J;
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Vector2I obj)
    {
        return HashCode.Combine(obj.I, obj.J);
    }

    public override string ToString()
    {
        return $"({I}, {J})";
    }

    public string ToString(string? format)
    {
        return $"({I.ToString(format)}, {J.ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({I.ToString(format, formatProvider)}, {J.ToString(format, formatProvider)})";
    }

    public (int I, int J) Deconstruct()
    {
        return (I, J);
    }

    public int this[int index]
    {
        get =>
            index switch
            {
                0 => I,
                1 => J,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "The index must be between 0 and 1."
                ),
            };
    }

    public static Vector2I operator +(Vector2I left, Vector2I right)
    {
        return new Vector2I(left.I + right.I, left.J + right.J);
    }

    public static Vector2I operator -(Vector2I left, Vector2I right)
    {
        return new Vector2I(left.I - right.I, left.J - right.J);
    }

    public static bool operator ==(Vector2I left, Vector2I right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2I left, Vector2I right)
    {
        return !left.Equals(right);
    }
}
