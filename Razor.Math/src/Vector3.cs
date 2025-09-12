// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Vector3 : IEqualityComparer<Vector3>
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public float Length2 => float.Pow(X, 2) + float.Pow(Y, 2) + float.Pow(Z, 2);

    public float Length => float.Sqrt(Length2);

    public float QuickLength
    {
        get
        {
            var max = float.Abs(X);
            var mid = float.Abs(Y);
            var min = float.Abs(Z);

            if (max < mid)
            {
                (max, mid) = (mid, max);
            }

            if (max < min)
            {
                (max, min) = (min, max);
            }

            if (mid < min)
            {
                (mid, min) = (min, mid);
            }

            return max + (11F / 32F) * mid + (1F / 4F) * min;
        }
    }

    public bool IsValid => ExtraMath.IsValid(X) && ExtraMath.IsValid(Y) && ExtraMath.IsValid(Z);

    public Vector3() { }

    public Vector3(float x, float y, float z)
    {
        Set(x, y, z);
    }

    public Vector3(float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 3);

        X = values[0];
        Y = values[1];
        Z = values[2];
    }

    public static float DotProduct(Vector3 left, Vector3 right)
    {
        return left * right;
    }

    public static Vector3 CrossProduct(Vector3 left, Vector3 right)
    {
        return new Vector3(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X
        );
    }

    public static float CrossProductX(Vector3 left, Vector3 right)
    {
        return left.Y * right.Z - left.Z * right.Y;
    }

    public static float CrossProductY(Vector3 left, Vector3 right)
    {
        return left.Z * right.X - left.X * right.Z;
    }

    public static float CrossProductZ(Vector3 left, Vector3 right)
    {
        return left.X * right.Y - left.Y * right.X;
    }

    public static Vector3 Normalize(Vector3 vector)
    {
        var len2 = vector.Length2;
        if (float.Abs(len2) < float.Epsilon) // Basically 0F
        {
            return vector;
        }

        var oLen = ExtraMath.InvSqrt(len2);
        return vector * oLen;
    }

    public static void Swap(ref Vector3 left, ref Vector3 right)
    {
        (left, right) = (right, left);
    }

    public static Vector3 Add(Vector3 left, Vector3 right)
    {
        return left + right;
    }

    public static Vector3 Subtract(Vector3 left, Vector3 right)
    {
        return left - right;
    }

    public static float FindXAtY(float y, Vector3 left, Vector3 right)
    {
        return left.X + ((y - left.Y) * ((right.X - left.X) / (right.Y - left.Y)));
    }

    public static float FindXAtZ(float z, Vector3 left, Vector3 right)
    {
        return left.X + ((z - left.Z) * ((right.X - left.X) / (right.Z - left.Z)));
    }

    public static float FindYAtX(float x, Vector3 left, Vector3 right)
    {
        return left.Y + ((x - left.X) * ((right.Y - left.Y) / (right.X - left.X)));
    }

    public static float FindYAtZ(float z, Vector3 left, Vector3 right)
    {
        return left.Y + ((z - left.Z) * ((right.Y - left.Y) / (right.Z - left.Z)));
    }

    public static float FindZAtX(float x, Vector3 left, Vector3 right)
    {
        return left.Z + ((x - left.X) * ((right.Z - left.Z) / (right.X - left.X)));
    }

    public static float FindZAtY(float y, Vector3 left, Vector3 right)
    {
        return left.Z + ((y - left.Y) * ((right.Z - left.Z) / (right.Y - left.Y)));
    }

    public static float QuickDistance(Vector3 left, Vector3 right)
    {
        return (left - right).QuickLength;
    }

    public static float Distance(Vector3 left, Vector3 right)
    {
        return (left - right).Length;
    }

    public static Vector3 Lerp(Vector3 left, Vector3 right, float alpha)
    {
        return new Vector3(
            left.X + (right.X - left.X) * alpha,
            left.Y + (right.Y - left.Y) * alpha,
            left.Z + (right.Z - left.Z) * alpha
        );
    }

    public void Set(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Set(Vector3 other)
    {
        X = other.X;
        Y = other.Y;
        Z = other.Z;
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
    }

    public void Scale(Vector3 scale)
    {
        X *= scale.X;
        Y *= scale.Y;
        Z *= scale.Z;
    }

    public void RotateX(float angle)
    {
        RotateX(float.Sin(angle), float.Cos(angle));
    }

    public void RotateX(float sin, float cos)
    {
        var tmpY = Y;
        var tmpZ = Z;

        Y = cos * tmpY - sin * tmpZ;
        Z = sin * tmpY + cos * tmpZ;
    }

    public void RotateY(float angle)
    {
        RotateY(float.Sin(angle), float.Cos(angle));
    }

    public void RotateY(float sin, float cos)
    {
        var tmpX = X;
        var tmpZ = Z;

        X = cos * tmpX + sin * tmpZ;
        Z = -sin * tmpX + cos * tmpZ;
    }

    public void RotateZ(float angle)
    {
        RotateZ(float.Sin(angle), float.Cos(angle));
    }

    public void RotateZ(float sin, float cos)
    {
        var tmpX = X;
        var tmpY = Y;

        X = cos * tmpX - sin * tmpY;
        Y = sin * tmpX + cos * tmpY;
    }

    public void UpdateMin(Vector3 other)
    {
        if (other.X < X)
        {
            X = other.X;
        }

        if (other.Y < Y)
        {
            Y = other.Y;
        }

        if (other.Z < Z)
        {
            Z = other.Z;
        }
    }

    public void UpdateMax(Vector3 other)
    {
        if (other.X > X)
        {
            X = other.X;
        }

        if (other.Y > Y)
        {
            Y = other.Y;
        }

        if (other.Z > Z)
        {
            Z = other.Z;
        }
    }

    public void CapAbsoluteTo(Vector3 other)
    {
        CapAbsoluteToXComponent(other);
        CapAbsoluteToYComponent(other);
        CapAbsoluteToZComponent(other);
    }

    public uint ToAbgr()
    {
        return (uint)(
            255 << 24 | (long)(Z * 255F) << 16 | (long)(Y * 255F) << 8 | (long)(X * 255F)
        );
    }

    public uint ToArgb()
    {
        return (uint)(
            255 << 24 | (long)(X * 255F) << 16 | (long)(Y * 255F) << 8 | (long)(Z * 255F)
        );
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector3 other && Equals(this, other);
    }

    public bool Equals(Vector3? left, Vector3? right)
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

        return float.Abs(left.X - right.X) < float.Epsilon
            && float.Abs(left.Y - right.Y) < float.Epsilon
            && float.Abs(left.Z - right.Z) < float.Epsilon;
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Vector3 obj)
    {
        return HashCode.Combine(obj.X, obj.Y, obj.Z);
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }

    public string ToString(string? format)
    {
        return $"({X.ToString(format)}, {Y.ToString(format)}, {Z.ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)}, {Z.ToString(format, formatProvider)})";
    }

    public (float X, float Y, float Z) Deconstruct()
    {
        return (X, Y, Z);
    }

    public float this[int index]
    {
        get =>
            index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
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
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                case 2:
                    Z = value;
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

    public static Vector3 operator +(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static Vector3 operator -(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static float operator *(Vector3 left, Vector3 right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    public static Vector3 operator *(Vector3 vector, float scalar)
    {
        return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }

    public static Vector3 operator *(float scalar, Vector3 vector)
    {
        return vector * scalar;
    }

    public static Vector3 operator /(Vector3 vector, float scalar)
    {
        return new Vector3(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);
    }

    public static Vector3 operator -(Vector3 vector)
    {
        return new Vector3(-vector.X, -vector.Y, -vector.Z);
    }

    public static Vector3 operator +(Vector3 vector)
    {
        return vector;
    }

    public static bool operator ==(Vector3 left, Vector3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3 left, Vector3 right)
    {
        return !left.Equals(right);
    }

    private void CapAbsoluteToXComponent(Vector3 other)
    {
        if (X > 0)
        {
            if (other.X < X)
            {
                X = other.X;
            }
        }
        else
        {
            if (-other.X > X)
            {
                X = -other.X;
            }
        }
    }

    private void CapAbsoluteToYComponent(Vector3 other)
    {
        if (Y > 0)
        {
            if (other.Y < Y)
            {
                Y = other.Y;
            }
        }
        else
        {
            if (-other.Y > Y)
            {
                Y = -other.Y;
            }
        }
    }

    private void CapAbsoluteToZComponent(Vector3 other)
    {
        if (Z > 0)
        {
            if (other.Z < Z)
            {
                Z = other.Z;
            }
        }
        else
        {
            if (-other.Z > Z)
            {
                Z = -other.Z;
            }
        }
    }
}
