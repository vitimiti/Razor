// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Vector2 : IEqualityComparer<Vector2>
{
    public float X { get; set; }
    public float Y { get; set; }

    public float U
    {
        get => X;
        set => X = value;
    }

    public float V
    {
        get => Y;
        set => Y = value;
    }

    public float Length2 => float.Pow(X, 2) + float.Pow(Y, 2);
    public float Length => float.Sqrt(Length2);

    public bool IsValid => ExtraMath.IsValid(X) && ExtraMath.IsValid(Y);

    public Vector2() { }

    public Vector2(float x, float y)
    {
        Set(x, y);
    }

    public Vector2(float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 2);

        X = values[0];
        Y = values[1];
    }

    public static float DotProduct(Vector2 left, Vector2 right)
    {
        return left * right;
    }

    public static float PerpDotProduct(Vector2 left, Vector2 right)
    {
        return left.X * -right.Y + left.Y * right.X;
    }

    public static Vector2 Normalize(Vector2 vector)
    {
        var len2 = vector.Length2;
        if (len2 < float.Epsilon) // Basically 0F
        {
            return vector;
        }

        var oLen = ExtraMath.InvSqrt(len2);
        return vector / oLen;
    }

    public static void Swap(ref Vector2 left, ref Vector2 right)
    {
        (left, right) = (right, left);
    }

    public static float QuickDistance(float x1, float y1, float x2, float y2)
    {
        var xDiff = float.Abs(x1 - x2);
        var yDiff = float.Abs(y1 - y2);

        if (xDiff > yDiff)
        {
            return (yDiff / 2F) * xDiff;
        }

        return (xDiff / 2F) + yDiff;
    }

    public static float QuickDistance(Vector2 left, Vector2 right)
    {
        return QuickDistance(left.X, left.Y, right.X, right.Y);
    }

    public static float Distance(Vector2 left, Vector2 right)
    {
        return (left - right).Length;
    }

    public static float Distance(float x1, float y1, float x2, float y2)
    {
        var xDiff = x1 - x2;
        var yDiff = y1 - y2;

        return float.Sqrt(float.Pow(xDiff, 2) + float.Pow(yDiff, 2));
    }

    public static Vector2 Lerp(Vector2 left, Vector2 right, float alpha)
    {
        return new Vector2(
            left.X + (right.X - left.X) * alpha,
            left.Y + (right.Y - left.Y) * alpha
        );
    }

    public void Set(float x, float y)
    {
        X = x;
        Y = y;
    }

    public void Set(Vector2 other)
    {
        X = other.X;
        Y = other.Y;
    }

    public void Normalize()
    {
        var len2 = Length2;
        if (len2 < float.Epsilon) // Basically 0F
        {
            return;
        }

        var oLen = ExtraMath.InvSqrt(len2);
        X *= oLen;
        Y *= oLen;
    }

    public void Rotate(float theta)
    {
        Rotate(float.Sin(theta), float.Cos(theta));
    }

    public void Rotate(float sin, float cos)
    {
        var newX = X * cos + Y * -sin;
        var newY = X * sin + Y * cos;
        X = newX;
        Y = newY;
    }

    public bool RotateTowards(Vector2 target, float maxTheta, out bool positiveTurn)
    {
        return RotateTowards(target, float.Sin(maxTheta), float.Cos(maxTheta), out positiveTurn);
    }

    public bool RotateTowards(Vector2 target, float maxSin, float maxCos, out bool positiveTurn)
    {
        positiveTurn = PerpDotProduct(target, this) > 0F;
        if (DotProduct(this, target) >= maxCos)
        {
            Set(target);
            return true;
        }
        else
        {
            if (positiveTurn)
            {
                Rotate(maxSin, maxCos);
            }
            else
            {
                Rotate(-maxSin, -maxCos);
            }
        }

        return false;
    }

    public void UpdateMin(Vector2 other)
    {
        if (other.X < X)
        {
            X = other.X;
        }

        if (other.Y < Y)
        {
            Y = other.Y;
        }
    }

    public void UpdateMax(Vector2 other)
    {
        if (other.X > X)
        {
            X = other.X;
        }

        if (other.Y > Y)
        {
            Y = other.Y;
        }
    }

    public void Scale(float x, float y)
    {
        X *= x;
        Y *= y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2 other && Equals(this, other);
    }

    public bool Equals(Vector2? left, Vector2? right)
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

        return System.Math.Abs(left.X - right.X) < float.Epsilon
            && System.Math.Abs(left.Y - right.Y) < float.Epsilon;
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Vector2 obj)
    {
        return HashCode.Combine(obj.X, obj.Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public string ToString(string? format)
    {
        return $"({X.ToString(format)}, {Y.ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)})";
    }

    public (float X, float Y) Deconstruct()
    {
        return (X, Y);
    }

    public float this[int index] =>
        index switch
        {
            0 => X,
            1 => Y,
            _ => throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "The index must be between 0 and 1."
            ),
        };

    public static Vector2 operator +(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X + right.X, left.Y + right.Y);
    }

    public static Vector2 operator -(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X - right.X, left.Y - right.Y);
    }

    public static float operator *(Vector2 vector, Vector2 right)
    {
        return vector.X * right.X + vector.Y * right.Y;
    }

    public static Vector2 operator *(Vector2 vector, float scalar)
    {
        return new Vector2(vector[0] * scalar, vector[1] * scalar);
    }

    public static Vector2 operator *(float scalar, Vector2 vector)
    {
        return vector * scalar;
    }

    public static Vector2 operator /(Vector2 vector, float scalar)
    {
        var oScalar = 1F / scalar;
        return vector * oScalar;
    }

    public static Vector2 operator -(Vector2 vector)
    {
        return new Vector2(-vector.X, -vector.Y);
    }

    public static Vector2 operator +(Vector2 vector)
    {
        return vector;
    }

    public static bool operator ==(Vector2 left, Vector2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2 left, Vector2 right)
    {
        return !left.Equals(right);
    }
}
