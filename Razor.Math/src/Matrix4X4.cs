// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using MatrixRow = (float Member1, float Member2, float Member3, float Member4);

namespace Razor.Math;

[PublicAPI]
public class Matrix4X4 : IEqualityComparer<Matrix4X4>
{
    protected Vector4[] Rows { get; }

    public static Matrix4X4 Identity
    {
        get
        {
            Matrix4X4 result = new();
            result.MakeIdentity();
            return result;
        }
    }

    public Matrix4X4 Transposed =>
        new(
            new Vector4(Rows[0][0], Rows[1][0], Rows[2][0], Rows[3][0]),
            new Vector4(Rows[0][1], Rows[1][1], Rows[2][1], Rows[3][1]),
            new Vector4(Rows[0][2], Rows[1][2], Rows[2][2], Rows[3][2]),
            new Vector4(Rows[0][3], Rows[1][3], Rows[2][3], Rows[3][3])
        );

    public Matrix4X4 Inverse
    {
        get
        {
            Matrix4X4 current = new(this);
            var result = Identity;
            for (var i = 0; i < Rows.Length; i++)
            {
                var j = i;
                for (var k = i + 1; k < Rows.Length; k++)
                {
                    if (float.Abs(current[k][i]) > float.Abs(current[j][i]))
                    {
                        j = k;
                    }
                }

                Vector4.Swap(ref current.Rows[j], ref current.Rows[i]);
                Vector4.Swap(ref result.Rows[j], ref result.Rows[i]);

                // Essentially current[i][i] == 0F
                if (float.Abs(current[i][i]) < float.Epsilon)
                {
                    throw new InvalidOperationException(
                        "The matrix is singular and cannot be inverted."
                    );
                }

                result.Rows[i] /= current[i][i];
                current.Rows[i] /= current[i][i];

                for (var l = 0; l < Rows.Length; l++)
                {
                    if (l == i)
                    {
                        continue;
                    }

                    result.Rows[l] -= current[l][i] * result.Rows[i];
                    current.Rows[l] -= current.Rows[l][i] * current.Rows[i];
                }
            }

            return result;
        }
    }

    public Matrix4X4()
    {
        Rows = new Vector4[4];
    }

    public Matrix4X4(Matrix4X4 other)
    {
        Rows = [other.Rows[0], other.Rows[1], other.Rows[2], other.Rows[3]];
    }

    public Matrix4X4(Matrix3D matrix)
    {
        Rows = new Vector4[4];
        Initialize(matrix);
    }

    public Matrix4X4(Vector4 r0, Vector4 r1, Vector4 r2, Vector4 r3)
    {
        Rows = new Vector4[4];
        Initialize(r0, r1, r2, r3);
    }

    public Matrix4X4(MatrixRow row1, MatrixRow row2, MatrixRow row3, MatrixRow row4)
    {
        Rows = new Vector4[4];
        Initialize(row1, row2, row3, row4);
    }

    public static Matrix4X4 Add(Matrix4X4 left, Matrix4X4 right)
    {
        return left + right;
    }

    public static Matrix4X4 Subtract(Matrix4X4 left, Matrix4X4 right)
    {
        return left - right;
    }

    public static Matrix4X4 Multiply(Matrix4X4 left, Matrix4X4 right)
    {
        return left * right;
    }

    public static Matrix4X4 Multiply(Matrix3D left, Matrix4X4 right)
    {
        return left * right;
    }

    public static Matrix4X4 Multiply(Matrix4X4 left, Matrix3D right)
    {
        return left * right;
    }

    public static Vector3 TransformVector3ToVector3(Matrix4X4 matrix, Vector3 vector)
    {
        return new Vector3(
            matrix[0][0] * vector.X
                + matrix[0][1] * vector.Y
                + matrix[0][2] * vector.Z
                + matrix[0][3],
            matrix[1][0] * vector.X
                + matrix[1][1] * vector.Y
                + matrix[1][2] * vector.Z
                + matrix[1][3],
            matrix[2][0] * vector.X
                + matrix[2][1] * vector.Y
                + matrix[2][2] * vector.Z
                + matrix[2][3]
        );
    }

    public static Vector4 TransformVector3ToVector4(Matrix4X4 matrix, Vector3 vector)
    {
        return new Vector4(
            matrix[0][0] * vector.X
                + matrix[0][1] * vector.Y
                + matrix[0][2] * vector.Z
                + matrix[0][3],
            matrix[1][0] * vector.X
                + matrix[1][1] * vector.Y
                + matrix[1][2] * vector.Z
                + matrix[1][3],
            matrix[2][0] * vector.X
                + matrix[2][1] * vector.Y
                + matrix[2][2] * vector.Z
                + matrix[2][3],
            1F
        );
    }

    public static Vector4 TransformVector4ToVector4(Matrix4X4 matrix, Vector4 vector)
    {
        return new Vector4(
            matrix[0][0] * vector.X
                + matrix[0][1] * vector.Y
                + matrix[0][2] * vector.Z
                + matrix[0][3] * vector.W,
            matrix[1][0] * vector.X
                + matrix[1][1] * vector.Y
                + matrix[1][2] * vector.Z
                + matrix[1][3] * vector.W,
            matrix[2][0] * vector.X
                + matrix[2][1] * vector.Y
                + matrix[2][2] * vector.Z
                + matrix[2][3] * vector.W,
            matrix[3][0] * vector.X
                + matrix[3][1] * vector.Y
                + matrix[3][2] * vector.Z
                + matrix[3][3] * vector.W
        );
    }

    public void MakeIdentity()
    {
        Rows[0].Set(1F, 0F, 0F, 0F);
        Rows[1].Set(0F, 1F, 0F, 0F);
        Rows[2].Set(0F, 0F, 1F, 0F);
        Rows[3].Set(0F, 0F, 0F, 1F);
    }

    public void Initialize(Matrix3D matrix)
    {
        Rows[0] = matrix[0];
        Rows[1] = matrix[1];
        Rows[2] = matrix[2];
        Rows[3] = new Vector4(0F, 0F, 0F, 1F);
    }

    public void Initialize(Vector4 r0, Vector4 r1, Vector4 r2, Vector4 r3)
    {
        Rows[0] = r0;
        Rows[1] = r1;
        Rows[2] = r2;
        Rows[3] = r3;
    }

    public void Initialize(MatrixRow row1, MatrixRow row2, MatrixRow row3, MatrixRow row4)
    {
        Rows[0].Set(row1.Member1, row1.Member2, row1.Member3, row1.Member4);
        Rows[1].Set(row2.Member1, row2.Member2, row2.Member3, row2.Member4);
        Rows[2].Set(row3.Member1, row3.Member2, row3.Member3, row3.Member4);
        Rows[3].Set(row4.Member1, row4.Member2, row4.Member3, row4.Member4);
    }

    public void InitializeOrthogonal(
        float left,
        float right,
        float bottom,
        float top,
        float zNear,
        float zFar
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(zNear, 0F);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(zFar, zNear);

        MakeIdentity();
        Rows[0][0] = 2F / (right - left);
        Rows[0][3] = -(right + left) / (right - left);
        Rows[1][1] = 2F / (top - bottom);
        Rows[1][3] = -(top + bottom) / (top - bottom);
        Rows[2][2] = -2F / (zFar - zNear);
        Rows[2][3] = -(zFar + zNear) / (zFar - zNear);
    }

    public void InitializePerspective(float hFov, float vFov, float zNear, float zFar)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(zNear, 0F);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(zFar, zNear);

        MakeIdentity();
        Rows[0][0] = 1F / float.Tan(hFov * .5F);
        Rows[1][1] = 1F / float.Tan(vFov * .5F);
        Rows[2][2] = -(zFar + zNear) / (zFar - zNear);
        Rows[2][3] = -(2F * zFar * zNear) / (zFar - zNear);
        Rows[3][2] = -1F;
        Rows[3][3] = 0F;
    }

    public void InitializePerspective(
        float left,
        float right,
        float bottom,
        float top,
        float zNear,
        float zFar
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(zNear, 0F);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(zFar, 0F);

        MakeIdentity();
        Rows[0][0] = 2F * zNear / (right - left);
        Rows[0][2] = (right + left) / (right - left);
        Rows[1][1] = 2F * zNear / (top - bottom);
        Rows[1][2] = (top + bottom) / (top - bottom);
        Rows[2][2] = -(zFar + zNear) / (zFar - zNear);
        Rows[2][3] = -(2F * zFar * zNear) / (zFar - zNear);
        Rows[3][2] = -1F;
        Rows[3][3] = 0F;
    }

    public override bool Equals(object? obj)
    {
        return obj is Matrix4X4 matrix && Equals(this, matrix);
    }

    public bool Equals(Matrix4X4? left, Matrix4X4? right)
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

        return !Rows.Where((_, i) => left.Rows[i] != right.Rows[i]).Any();
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Matrix4X4 obj)
    {
        return obj.Rows.GetHashCode();
    }

    public override string ToString()
    {
        return $"({Rows[0]}, {Rows[1]}, {Rows[2]}, {Rows[3]})";
    }

    public string ToString(string? format)
    {
        return $"({Rows[0].ToString(format)}, {Rows[1].ToString(format)}, {Rows[2].ToString(format)}, {Rows[3].ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({Rows[0].ToString(format, formatProvider)}, {Rows[1].ToString(format, formatProvider)}, {Rows[2].ToString(format, formatProvider)}, {Rows[3].ToString(format, formatProvider)})";
    }

    public (Vector4 Row1, Vector4 Row2, Vector4 Row3, Vector4 Row4) Deconstruct()
    {
        return (Rows[0], Rows[1], Rows[2], Rows[3]);
    }

    public Vector4 this[int index]
    {
        get =>
            index switch
            {
                0 => Rows[0],
                1 => Rows[1],
                2 => Rows[2],
                3 => Rows[3],
                _ => throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Index must be between 0 and 3."
                ),
            };
        set
        {
            switch (index)
            {
                case 0:
                    Rows[0] = value;
                    break;
                case 1:
                    Rows[1] = value;
                    break;
                case 2:
                    Rows[2] = value;
                    break;
                case 3:
                    Rows[3] = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        index,
                        "The index must be between 0 and 3."
                    );
            }
        }
    }

    public static Matrix4X4 operator +(Matrix4X4 left, Matrix4X4 right)
    {
        return new Matrix4X4(
            left.Rows[0] + right.Rows[0],
            left.Rows[1] + right.Rows[1],
            left.Rows[2] + right.Rows[2],
            left.Rows[3] + right.Rows[3]
        );
    }

    public static Matrix4X4 operator -(Matrix4X4 left, Matrix4X4 right)
    {
        return new Matrix4X4(
            left.Rows[0] - right.Rows[0],
            left.Rows[1] - right.Rows[1],
            left.Rows[2] - right.Rows[2],
            left.Rows[3] - right.Rows[3]
        );
    }

    public static Matrix4X4 operator *(Matrix4X4 left, Matrix4X4 right)
    {
        return new Matrix4X4(
            new Vector4(
                DefineRowColumn(0, 0),
                DefineRowColumn(0, 1),
                DefineRowColumn(0, 2),
                DefineRowColumn(0, 3)
            ),
            new Vector4(
                DefineRowColumn(1, 0),
                DefineRowColumn(1, 1),
                DefineRowColumn(1, 2),
                DefineRowColumn(1, 3)
            ),
            new Vector4(
                DefineRowColumn(2, 0),
                DefineRowColumn(2, 1),
                DefineRowColumn(2, 2),
                DefineRowColumn(2, 3)
            ),
            new Vector4(
                DefineRowColumn(3, 0),
                DefineRowColumn(3, 1),
                DefineRowColumn(3, 2),
                DefineRowColumn(3, 3)
            )
        );

        float DefineRowColumn(int row, int column)
        {
            return left[row][0] * right[0][column]
                + left[row][1] * right[1][column]
                + left[row][2] * right[2][column]
                + left[row][3] * right[3][column];
        }
    }

    public static Matrix4X4 operator *(Matrix4X4 matrix, float scalar)
    {
        return new Matrix4X4(
            matrix.Rows[0] * scalar,
            matrix.Rows[1] * scalar,
            matrix.Rows[2] * scalar,
            matrix.Rows[3] * scalar
        );
    }

    public static Matrix4X4 operator *(float scalar, Matrix4X4 matrix)
    {
        return matrix * scalar;
    }

    public static Matrix4X4 operator *(Matrix4X4 left, Matrix3D right)
    {
        return new Matrix4X4(
            new Vector4(
                DefineRowColumn(0, 0),
                DefineRowColumn(0, 1),
                DefineRowColumn(0, 2),
                DefineRowColumnLast(0, 3)
            ),
            new Vector4(
                DefineRowColumn(1, 0),
                DefineRowColumn(1, 1),
                DefineRowColumn(1, 2),
                DefineRowColumnLast(1, 3)
            ),
            new Vector4(
                DefineRowColumn(2, 0),
                DefineRowColumn(2, 1),
                DefineRowColumn(2, 2),
                DefineRowColumnLast(2, 3)
            ),
            new Vector4(
                DefineRowColumn(3, 0),
                DefineRowColumn(3, 1),
                DefineRowColumn(3, 2),
                DefineRowColumnLast(3, 3)
            )
        );

        float DefineRowColumn(int row, int column)
        {
            return left[row][0] * right[0][column]
                + left[row][1] * right[1][column]
                + left[row][2] * right[2][column];
        }

        float DefineRowColumnLast(int row, int column)
        {
            return left[row][0] * right[0][column]
                + left[row][1] * right[1][column]
                + left[row][2] * right[2][column]
                + left[row][3];
        }
    }

    public static Matrix4X4 operator *(Matrix3D left, Matrix4X4 right)
    {
        return new Matrix4X4(
            new Vector4(
                DefineRowColumn(0, 0),
                DefineRowColumn(0, 1),
                DefineRowColumn(0, 2),
                DefineRowColumn(0, 3)
            ),
            new Vector4(
                DefineRowColumn(1, 0),
                DefineRowColumn(1, 1),
                DefineRowColumn(1, 2),
                DefineRowColumn(1, 3)
            ),
            new Vector4(
                DefineRowColumn(2, 0),
                DefineRowColumn(2, 1),
                DefineRowColumn(2, 2),
                DefineRowColumn(2, 3)
            ),
            new Vector4(right[3][0], right[3][1], right[3][2], right[3][3])
        );

        float DefineRowColumn(int row, int column)
        {
            return left[row][0] * right[0][column]
                + left[row][1] * right[1][column]
                + left[row][2] * right[2][column]
                + left[row][3] * right[3][column];
        }
    }

    public static Vector4 operator *(Matrix4X4 matrix, Vector3 vector)
    {
        return new Vector4(
            matrix[0][0] * vector[0]
                + matrix[0][1] * vector[1]
                + matrix[0][2] * vector[2]
                + matrix[0][3],
            matrix[1][0] * vector[0]
                + matrix[1][1] * vector[1]
                + matrix[1][2] * vector[2]
                + matrix[1][3],
            matrix[2][0] * vector[0]
                + matrix[2][1] * vector[1]
                + matrix[2][2] * vector[2]
                + matrix[2][3],
            matrix[3][0] * vector[0]
                + matrix[3][1] * vector[1]
                + matrix[3][2] * vector[2]
                + matrix[3][3]
        );
    }

    public static Vector4 operator *(Matrix4X4 matrix, Vector4 vector)
    {
        return new Vector4(
            matrix[0][0] * vector[0]
                + matrix[0][1] * vector[1]
                + matrix[0][2] * vector[2]
                + matrix[0][3] * vector[3],
            matrix[1][0] * vector[0]
                + matrix[1][1] * vector[1]
                + matrix[1][2] * vector[2]
                + matrix[1][3] * vector[3],
            matrix[2][0] * vector[0]
                + matrix[2][1] * vector[1]
                + matrix[2][2] * vector[2]
                + matrix[2][3] * vector[3],
            matrix[3][0] * vector[0]
                + matrix[3][1] * vector[1]
                + matrix[3][2] * vector[2]
                + matrix[3][3] * vector[3]
        );
    }

    public static Matrix4X4 operator /(Matrix4X4 matrix, float scalar)
    {
        var oOd = 1F / scalar;
        return new Matrix4X4(
            matrix.Rows[0] * oOd,
            matrix.Rows[1] * oOd,
            matrix.Rows[2] * oOd,
            matrix.Rows[3] * oOd
        );
    }

    public static Matrix4X4 operator -(Matrix4X4 matrix)
    {
        return new Matrix4X4(-matrix.Rows[0], -matrix.Rows[1], -matrix.Rows[2], -matrix.Rows[3]);
    }

    public static bool operator ==(Matrix4X4 left, Matrix4X4 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Matrix4X4 left, Matrix4X4 right)
    {
        return !left.Equals(right);
    }
}
