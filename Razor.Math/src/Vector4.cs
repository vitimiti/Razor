// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Vector4 : IEqualityComparer<Vector4>
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public float this[int index]
    {
        get =>
            index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => W,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "The index must be between 0 and 4."
                ),
            };
        set
        {
            switch (index)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                case 2:
                    Z = value;
                    break;
                case 3:
                    W = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        index,
                        "The index must be between 0 and 4."
                    );
            }
        }
    }

    public float Length2 => float.Pow(X, 2) + float.Pow(Y, 2) + float.Pow(Z, 2) + float.Pow(W, 2);
    public float Length => float.Sqrt(Length2);

    public bool IsValid =>
        ExtraMath.IsValid(X)
        && ExtraMath.IsValid(Y)
        && ExtraMath.IsValid(Z)
        && ExtraMath.IsValid(W);

    public Vector4() { }

    public Vector4(float x, float y, float z, float w)
    {
        Set(x, y, z, w);
    }

    public Vector4(float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 4);

        X = values[0];
        Y = values[1];
        Z = values[2];
        W = values[3];
    }

    public static float DotProduct(Vector4 left, Vector4 right)
    {
        return left * right;
    }

    public static Vector4 Normalize(Vector4 vector)
    {
        var len2 = vector.Length2;
        if (float.Abs(len2) < float.Epsilon) // Basically 0F
        {
            return new Vector4(0F, 0F, 0F, 0F);
        }

        var oLen = ExtraMath.InvSqrt(len2);
        return vector * oLen;
    }

    public static void Swap(ref Vector4 left, ref Vector4 right)
    {
        (left, right) = (right, left);
    }

    public static Vector4 Lerp(Vector4 left, Vector4 right, float alpha)
    {
        return new Vector4(
            left.X + (right.X - left.X) * alpha,
            left.Y + (right.Y - left.Y) * alpha,
            left.Z + (right.Z - left.Z) * alpha,
            left.W + (right.W - left.W) * alpha
        );
    }

    public void Set(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public void Normalize()
    {
        var len2 = Length2;
        if (float.Abs(len2) < float.Epsilon) // Basically 0F
        {
            return;
        }

        var oLen = ExtraMath.InvSqrt(len2);
        X *= oLen;
        Y *= oLen;
        Z *= oLen;
        W *= oLen;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector4 other && Equals(this, other);
    }

    public bool Equals(Vector4? x, Vector4? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null)
        {
            return false;
        }

        if (y is null)
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return float.Abs(x.X - y.X) < float.Epsilon
            && float.Abs(x.Y - y.Y) < float.Epsilon
            && float.Abs(x.Z - y.Z) < float.Epsilon
            && float.Abs(x.W - y.W) < float.Epsilon;
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Vector4 obj)
    {
        return HashCode.Combine(obj.X, obj.Y, obj.Z, obj.W);
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z}, {W})";
    }

    public string ToString(string? format)
    {
        return $"({X.ToString(format)}, {Y.ToString(format)}, {Z.ToString(format)}, {W.ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)}, {Z.ToString(format, formatProvider)}, {W.ToString(format, formatProvider)})";
    }

    public (float X, float Y, float Z, float W) Deconstruct()
    {
        return (X, Y, Z, W);
    }

    public static Vector4 operator +(Vector4 x, Vector4 y)
    {
        return new Vector4(x.X + y.X, x.Y + y.Y, x.Z + y.Z, x.W + y.W);
    }

    public static Vector4 operator -(Vector4 x, Vector4 y)
    {
        return new Vector4(x.X - y.X, x.Y - y.Y, x.Z - y.Z, x.W - y.W);
    }

    public static float operator *(Vector4 x, Vector4 y)
    {
        return x.X * y.X + x.Y * y.Y + x.Z * y.Z + x.W * y.W;
    }

    public static Vector4 operator *(Vector4 obj, float scalar)
    {
        return new Vector4(obj.X * scalar, obj.Y * scalar, obj.Z * scalar, obj.W * scalar);
    }

    public static Vector4 operator *(float scalar, Vector4 obj)
    {
        return obj * scalar;
    }

    public static Vector4 operator /(Vector4 obj, float scalar)
    {
        var oScalar = 1F / scalar;
        return obj * oScalar;
    }

    public static Vector4 operator -(Vector4 obj)
    {
        return new Vector4(-obj.X, -obj.Y, -obj.Z, -obj.W);
    }

    public static Vector4 operator +(Vector4 obj)
    {
        return obj;
    }

    public static bool operator ==(Vector4 x, Vector4 y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(Vector4 x, Vector4 y)
    {
        return !x.Equals(y);
    }
}
