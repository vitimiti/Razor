// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using MatrixRow = (float Member1, float Member2, float Member3);

namespace Razor.Math;

[PublicAPI]
public class Matrix3X3 : IEqualityComparer<Matrix3X3>
{
    protected Vector3[] Rows { get; set; }

    public static Matrix3X3 Identity => new((1F, 0F, 0F), (0F, 1F, 0F), (0F, 0F, 1F));

    public static Matrix3X3 RotateX90 => new((1F, 0F, 0F), (0F, 0F, -1F), (0F, 1F, 0F));

    public static Matrix3X3 RotateX180 => new((1F, 0F, 0F), (0F, -1F, 0F), (0F, 0F, -1F));

    public static Matrix3X3 RotateX270 => new((1F, 0F, 0F), (0F, 0F, 1F), (0F, -1F, 0F));

    public static Matrix3X3 RotateY90 => new((0F, 0F, 1F), (0F, 1F, 0F), (-1F, 0F, 0F));

    public static Matrix3X3 RotateY180 => new((-1F, 0F, 0F), (0F, 1F, 0F), (0F, 0F, -1F));

    public static Matrix3X3 RotateY270 => new((0F, -1F, 0F), (1F, 0F, 0F), (0F, 0F, 1F));

    public static Matrix3X3 RotateZ90 => new((0F, -1F, 0F), (1F, 0F, 0F), (0F, 0F, 1F));

    public static Matrix3X3 RotateZ180 => new((-1F, 0F, 0F), (0F, -1F, 0F), (0F, 0F, 1F));

    public static Matrix3X3 RotateZ270 => new((0F, 1F, 0F), (-1F, 0F, 0F), (0F, 0F, 1F));

    public float XRotation
    {
        get
        {
            var vec = this * new Vector3(0F, 1F, 0F);
            return float.Atan2(vec[2], vec[1]);
        }
    }

    public float YRotation
    {
        get
        {
            var vec = this * new Vector3(0F, 0F, 1F);
            return float.Atan2(vec[0], vec[2]);
        }
    }

    public float ZRotation
    {
        get
        {
            var vec = this * new Vector3(1F, 0F, 0F);
            return float.Atan2(vec[1], vec[0]);
        }
    }

    public Vector3 XVector => new(Rows[0][0], Rows[1][0], Rows[2][0]);
    public Vector3 YVector => new(Rows[0][1], Rows[1][1], Rows[2][1]);
    public Vector3 ZVector => new(Rows[0][2], Rows[1][2], Rows[2][2]);

    public Matrix3X3 Transposed =>
        new(
            new Vector3(Rows[0][0], Rows[1][0], Rows[2][0]),
            new Vector3(Rows[0][1], Rows[1][1], Rows[2][1]),
            new Vector3(Rows[0][2], Rows[1][2], Rows[2][2])
        );

    public Matrix3X3 Inverse
    {
        get
        {
            var matCopy = new Matrix3X3(this);
            var identity = Identity;
            for (var i = 0; i < 3; i++)
            {
                var j = i;
                for (var k = i + 1; k < 3; k++)
                {
                    if (float.Abs(matCopy[k][i]) > float.Abs(matCopy[j][i]))
                    {
                        j = k;
                    }
                }

                Vector3.Swap(ref matCopy.Rows[j], ref matCopy.Rows[i]);
                Vector3.Swap(ref identity.Rows[j], ref identity.Rows[i]);

                identity.Rows[i] /= matCopy.Rows[i][i];
                matCopy.Rows[i] /= matCopy.Rows[i][i];

                for (var l = 0; l < 3; l++)
                {
                    if (l == i)
                    {
                        continue;
                    }

                    identity.Rows[l] -= matCopy[l][i] * identity.Rows[i];
                    identity.Rows[l] -= matCopy[l][i] * identity.Rows[i];
                }
            }

            return identity;
        }
    }

    public float Determinant =>
        Rows[0][0] * (Rows[1][1] * Rows[2][2] - Rows[1][2] * Rows[2][1])
        - Rows[0][1] * (Rows[1][0] * Rows[2][2] - Rows[1][2] * Rows[2][0])
        - Rows[0][2] * (Rows[1][0] * Rows[2][1] - Rows[1][1] * Rows[2][0]);

    public bool IsOrthogonal
    {
        get
        {
            return Vector3.DotProduct(XVector, YVector) <= float.Epsilon
                && Vector3.DotProduct(YVector, ZVector) <= float.Epsilon
                && Vector3.DotProduct(ZVector, XVector) <= float.Epsilon
                && float.Abs(XVector.Length - 1F) <= float.Epsilon
                && float.Abs(YVector.Length - 1F) <= float.Epsilon
                && float.Abs(ZVector.Length - 1F) <= float.Epsilon;
        }
    }

    public Matrix3X3()
    {
        Rows = new Vector3[3];
    }

    public Matrix3X3(Matrix3X3 other)
    {
        Rows = [other.Rows[0], other.Rows[1], other.Rows[2]];
    }

    public Matrix3X3(Vector3 row0, Vector3 row1, Vector3 row2)
    {
        Rows = new Vector3[3];
        Set(row0, row1, row2);
    }

    public Matrix3X3(MatrixRow row1, MatrixRow row2, MatrixRow row3)
    {
        Rows = new Vector3[3];
        Set(row1, row2, row3);
    }

    public Matrix3X3(Vector3 axis, float angle)
    {
        Rows = new Vector3[3];
        Set(axis, angle);
    }

    public Matrix3X3(Vector3 axis, float sinAngle, float cosAngle)
    {
        Rows = new Vector3[3];
        Set(axis, sinAngle, cosAngle);
    }

    public Matrix3X3(Quaternion quaternion)
    {
        Rows = new Vector3[3];
        Set(quaternion);
    }

    public Matrix3X3(Matrix3D matrix3d)
    {
        Rows = new Vector3[3];
        Set(matrix3d);
    }

    public Matrix3X3(Matrix4X4 matrix4X4)
    {
        Rows = new Vector3[3];
        Set(matrix4X4);
    }

    public static Matrix3X3 Add(Matrix3X3 left, Matrix3X3 right)
    {
        return left + right;
    }

    public static Matrix3X3 Subtract(Matrix3X3 left, Matrix3X3 right)
    {
        return left - right;
    }

    public static Matrix3X3 Multiply(Matrix3D left, Matrix3X3 right)
    {
        return left * right;
    }

    public static Matrix3X3 Multiply(Matrix3X3 left, Matrix3D right)
    {
        return left * right;
    }

    public static Matrix3X3 Multiply(Matrix3X3 left, Matrix3X3 right)
    {
        return left * right;
    }

    public static Matrix3X3 CreateXRotationMatrix3(float sin, float cos)
    {
        return new Matrix3X3((1F, 0F, 0F), (0F, cos, -sin), (0F, sin, cos));
    }

    public static Matrix3X3 CreateXRotationMatrix3(float rad)
    {
        return CreateXRotationMatrix3(float.Sin(rad), float.Cos(rad));
    }

    public static Matrix3X3 CreateYRotationMatrix3(float sin, float cos)
    {
        return new Matrix3X3((cos, 0F, sin), (0F, 1F, 0F), (-sin, 0F, cos));
    }

    public static Matrix3X3 CreateYRotationMatrix3(float rad)
    {
        return CreateYRotationMatrix3(float.Sin(rad), float.Cos(rad));
    }

    public static Matrix3X3 CreateZRotationMatrix3(float sin, float cos)
    {
        return new Matrix3X3((cos, -sin, 0F), (sin, cos, 0F), (0F, 0F, 1F));
    }

    public static Matrix3X3 CreateZRotationMatrix3(float rad)
    {
        return CreateZRotationMatrix3(float.Sin(rad), float.Cos(rad));
    }

    public static Vector3 RotateVector(Matrix3X3 matrix, Vector3 vector)
    {
        return new Vector3(
            matrix[0][0] * vector.X + matrix[0][1] * vector.Y + matrix[0][2] * vector.Z,
            matrix[1][0] * vector.X + matrix[1][1] * vector.Y + matrix[1][2] * vector.Z,
            matrix[2][0] * vector.X + matrix[2][1] * vector.Y + matrix[2][2] * vector.Z
        );
    }

    public static Vector3 TransposeRotateVector(Matrix3X3 matrix, Vector3 vector)
    {
        return new Vector3(
            matrix[0][0] * vector.X + matrix[1][0] * vector.Y + matrix[2][0] * vector.Z,
            matrix[0][1] * vector.X + matrix[1][1] * vector.Y + matrix[2][1] * vector.Z,
            matrix[0][2] * vector.X + matrix[1][2] * vector.Y + matrix[2][2] * vector.Z
        );
    }

    public void Set(Vector3 row0, Vector3 row1, Vector3 row2)
    {
        Rows[0] = row0;
        Rows[1] = row1;
        Rows[2] = row2;
    }

    public void Set(MatrixRow row1, MatrixRow row2, MatrixRow row3)
    {
        Rows[0] = new Vector3(row1.Member1, row1.Member2, row1.Member3);
        Rows[1] = new Vector3(row2.Member1, row2.Member2, row2.Member3);
        Rows[2] = new Vector3(row3.Member1, row3.Member2, row3.Member3);
    }

    public void Set(Vector3 axis, float angle)
    {
        Set(axis, float.Sin(angle), float.Cos(angle));
    }

    public void Set(Vector3 axis, float sinAngle, float cosAngle)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(axis.Length2 - 1F, 0.001F);

        Rows[0]
            .Set(
                float.Pow(axis[0], 2) + cosAngle * (1F - float.Pow(axis[0], 2)),
                axis[0] * axis[1] * (1F - cosAngle) - axis[2] * sinAngle,
                axis[2] * axis[0] * (1F - cosAngle) + axis[1] * sinAngle
            );

        Rows[1]
            .Set(
                axis[0] * axis[1] * (1F - cosAngle) + axis[2] * sinAngle,
                float.Pow(axis[1], 2) + cosAngle * (1F - float.Pow(axis[1], 2)),
                axis[1] * axis[2] * (1F - cosAngle) - axis[0] * sinAngle
            );

        Rows[2]
            .Set(
                axis[2] * axis[0] * (1F - cosAngle) - axis[1] * sinAngle,
                axis[1] * axis[2] * (1F - cosAngle) + axis[0] * sinAngle,
                float.Pow(axis[2], 2) + cosAngle * (1F - float.Pow(axis[2], 2))
            );
    }

    public void Set(Matrix3D matrix3d)
    {
        Rows[0].Set(matrix3d[0][0], matrix3d[0][1], matrix3d[0][2]);
        Rows[1].Set(matrix3d[1][0], matrix3d[1][1], matrix3d[1][2]);
        Rows[2].Set(matrix3d[2][0], matrix3d[2][1], matrix3d[2][2]);
    }

    public void Set(Matrix4X4 matrix4X4)
    {
        Rows[0].Set(matrix4X4[0][0], matrix4X4[0][1], matrix4X4[0][2]);
        Rows[1].Set(matrix4X4[1][0], matrix4X4[1][1], matrix4X4[1][2]);
        Rows[2].Set(matrix4X4[2][0], matrix4X4[2][1], matrix4X4[2][2]);
    }

    public void Set(Quaternion quaternion)
    {
        Rows[0][0] = 1F - 2F * (float.Pow(quaternion[1], 2) + float.Pow(quaternion[2], 2));
        Rows[0][1] = 2F * (quaternion[0] * quaternion[1] - quaternion[2] * quaternion[3]);
        Rows[0][2] = 2F * (quaternion[2] * quaternion[0] + quaternion[1] * quaternion[3]);

        Rows[1][0] = 2F * (quaternion[0] * quaternion[1] + quaternion[2] * quaternion[3]);
        Rows[1][1] = 1F - 2F * (float.Pow(quaternion[2], 2) + float.Pow(quaternion[0], 2));
        Rows[1][2] = 2F * (quaternion[1] * quaternion[2] - quaternion[0] * quaternion[3]);

        Rows[2][0] = 2F * (quaternion[2] * quaternion[0] - quaternion[1] * quaternion[3]);
        Rows[2][1] = 2F * (quaternion[1] * quaternion[2] + quaternion[0] * quaternion[3]);
        Rows[2][2] = 1F - 2F * (float.Pow(quaternion[1], 2) + float.Pow(quaternion[0], 2));
    }

    public void MakeIdentity()
    {
        Rows[0] = new Vector3(1F, 0F, 0F);
        Rows[1] = new Vector3(0F, 1F, 0F);
        Rows[2] = new Vector3(0F, 0F, 1F);
    }

    public void RotateX(float theta)
    {
        RotateX(float.Sin(theta), float.Cos(theta));
    }

    public void RotateX(float sin, float cos)
    {
        var tmp1 = Rows[0][1];
        var tmp2 = Rows[0][2];
        Rows[0][1] = cos * tmp1 + sin * tmp2;
        Rows[0][2] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[1][1];
        tmp2 = Rows[1][2];
        Rows[1][1] = cos * tmp1 + sin * tmp2;
        Rows[1][2] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[2][1];
        tmp2 = Rows[2][2];
        Rows[2][1] = cos * tmp1 + sin * tmp2;
        Rows[2][2] = -sin * tmp1 + cos * tmp2;
    }

    public void RotateY(float theta)
    {
        RotateY(float.Sin(theta), float.Cos(theta));
    }

    public void RotateY(float sin, float cos)
    {
        var tmp1 = Rows[0][0];
        var tmp2 = Rows[0][2];
        Rows[0][0] = cos * tmp1 - sin * tmp2;
        Rows[0][2] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[1][0];
        tmp2 = Rows[1][2];
        Rows[1][0] = cos * tmp1 - sin * tmp2;
        Rows[1][2] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[2][0];
        tmp2 = Rows[2][2];
        Rows[2][0] = cos * tmp1 - sin * tmp2;
        Rows[2][2] = sin * tmp1 + cos * tmp2;
    }

    public void RotateZ(float theta)
    {
        RotateZ(float.Sin(theta), float.Cos(theta));
    }

    public void RotateZ(float sin, float cos)
    {
        var tmp1 = Rows[0][0];
        var tmp2 = Rows[0][1];
        Rows[0][0] = cos * tmp1 + sin * tmp2;
        Rows[0][1] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[1][0];
        tmp2 = Rows[1][1];
        Rows[1][0] = cos * tmp1 + sin * tmp2;
        Rows[1][1] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[2][0];
        tmp2 = Rows[2][1];
        Rows[2][0] = cos * tmp1 + sin * tmp2;
        Rows[2][1] = -sin * tmp1 + cos * tmp2;
    }

    public Vector3 RotateAxisAlignedBoxExtent(Vector3 extent)
    {
        Vector3 vec = new();
        for (var i = 0; i < 3; i++)
        {
            vec[i] = 0F;
            for (var j = 0; j < 3; j++)
            {
                vec[i] += float.Abs(Rows[i][j] * extent[j]);
            }
        }

        return vec;
    }

    public void ReOrthogonalize()
    {
        var x = XVector;
        var y = YVector;
        var z = Vector3.CrossProduct(x, y);
        y = Vector3.CrossProduct(z, x);

        var len = x.Length;
        if (len < float.Epsilon)
        {
            MakeIdentity();
            return;
        }

        x /= len;
        len = y.Length;
        if (len < float.Epsilon)
        {
            MakeIdentity();
            return;
        }

        y /= len;

        len = z.Length;
        if (len < float.Epsilon)
        {
            MakeIdentity();
            return;
        }

        z /= len;

        Rows[0][0] = x.X;
        Rows[0][1] = x.Y;
        Rows[0][2] = x.Z;

        Rows[1][0] = y.X;
        Rows[1][1] = y.Y;
        Rows[1][2] = y.Z;

        Rows[2][0] = z.X;
        Rows[2][1] = z.Y;
        Rows[2][2] = z.Z;
    }

    public override bool Equals(object? obj)
    {
        return obj is Matrix3X3 other && Equals(this, other);
    }

    public bool Equals(Matrix3X3? left, Matrix3X3? right)
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

        return left.Rows[0] == right.Rows[0]
            && left.Rows[1] == right.Rows[1]
            && left.Rows[2] == right.Rows[2];
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Matrix3X3 obj)
    {
        return obj.Rows.GetHashCode();
    }

    public override string ToString()
    {
        return $"({Rows[0]}, {Rows[1]}, {Rows[2]})";
    }

    public string ToString(string? format)
    {
        return $"({Rows[0].ToString(format)}, {Rows[1].ToString(format)}, {Rows[2].ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({Rows[0].ToString(format, formatProvider)}, {Rows[1].ToString(format, formatProvider)}, {Rows[2].ToString(format, formatProvider)})";
    }

    public (Vector3 Member1, Vector3 Member2, Vector3 Member3) Deconstruct()
    {
        return (Rows[0], Rows[1], Rows[2]);
    }

    public Vector3 this[int index] =>
        index switch
        {
            0 => Rows[0],
            1 => Rows[1],
            2 => Rows[2],
            _ => throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "Index must be between 0 and 2"
            ),
        };

    public static Matrix3X3 operator +(Matrix3X3 left, Matrix3X3 right)
    {
        return new Matrix3X3(
            left.Rows[0] + right.Rows[0],
            left.Rows[1] + right.Rows[1],
            left.Rows[2] + right.Rows[2]
        );
    }

    public static Matrix3X3 operator -(Matrix3X3 left, Matrix3X3 right)
    {
        return new Matrix3X3(
            left.Rows[0] - right.Rows[0],
            left.Rows[1] - right.Rows[1],
            left.Rows[2] - right.Rows[2]
        );
    }

    public static Matrix3X3 operator *(Matrix3X3 left, Matrix3X3 right)
    {
        return new Matrix3X3(
            new Vector3(DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2)),
            new Vector3(DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2)),
            new Vector3(DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2))
        );

        float DefineRowColumn(int row, int column)
        {
            return left[row][0] * right[0][column]
                + left[row][1] * right[1][column]
                + left[row][2] * right[2][column];
        }
    }

    public static Matrix3X3 operator *(Matrix3X3 value, float scalar)
    {
        return new Matrix3X3(
            value.Rows[0] * scalar,
            value.Rows[1] * scalar,
            value.Rows[2] * scalar
        );
    }

    public static Matrix3X3 operator *(float scalar, Matrix3X3 value)
    {
        return value * scalar;
    }

    public static Vector3 operator *(Matrix3X3 value, Vector3 vector)
    {
        return new Vector3(
            value[0][0] * vector[0] + value[0][1] * vector[1] + value[0][2] * vector[2],
            value[1][0] * vector[0] + value[1][1] * vector[1] + value[1][2] * vector[2],
            value[2][0] * vector[0] + value[2][1] * vector[1] + value[2][2] * vector[2]
        );
    }

    public static Matrix3X3 operator *(Matrix3D left, Matrix3X3 right)
    {
        return new Matrix3X3(
            (DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2)),
            (DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2)),
            (DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2))
        );

        float DefineRowColumn(int row, int column)
        {
            return left[row][0] * right[0][column]
                + left[row][1] * right[1][column]
                + left[row][2] * right[2][column];
        }
    }

    public static Matrix3X3 operator *(Matrix3X3 left, Matrix3D right)
    {
        return new Matrix3X3(
            (DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2)),
            (DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2)),
            (DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2))
        );

        float DefineRowColumn(int row, int column)
        {
            return left[row][0] * right[0][column]
                + left[row][1] * right[1][column]
                + left[row][2] * right[2][column];
        }
    }

    public static Matrix3X3 operator /(Matrix3X3 value, float scalar)
    {
        return new Matrix3X3(
            value.Rows[0] / scalar,
            value.Rows[1] / scalar,
            value.Rows[2] / scalar
        );
    }

    public static Matrix3X3 operator -(Matrix3X3 value)
    {
        return new Matrix3X3(-value.Rows[0], -value.Rows[1], -value.Rows[2]);
    }

    public static bool operator ==(Matrix3X3 left, Matrix3X3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Matrix3X3 left, Matrix3X3 right)
    {
        return !left.Equals(right);
    }
}
