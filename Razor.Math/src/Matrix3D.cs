// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Razor.Generics;
using MatrixRow = (float Member1, float Member2, float Member3, float Member4);

namespace Razor.Math;

[PublicAPI]
public class Matrix3D : IEqualityComparer<Matrix3D>
{
    protected Vector4[] Rows { get; }

    public static Matrix3D Identity => new((1F, 0F, 0F, 0F), (0F, 1F, 0F, 0F), (0F, 0F, 1F, 0F));

    public static Matrix3D RotateX90 => new((1F, 0F, 0F, 0F), (0F, 0F, -1F, 0F), (0F, 1F, 0F, 0F));

    public static Matrix3D RotateX180 =>
        new((1F, 0F, 0F, 0F), (0F, -1F, 0F, 0F), (0F, 0F, -1F, 0F));

    public static Matrix3D RotateX270 => new((1F, 0F, 0F, 0F), (0F, 0F, 1F, 0F), (0F, -1F, 0F, 0F));

    public static Matrix3D RotateY90 => new((0F, 0F, 1F, 0F), (0F, 1F, 0F, 0F), (-1F, 0F, 0F, 0F));

    public static Matrix3D RotateY180 =>
        new((-1F, 0F, 0F, 0F), (0F, 1F, 0F, 0F), (0F, 0F, -1F, 0F));

    public static Matrix3D RotateY270 => new((0F, 0F, -1F, 0F), (0F, 1F, 0F, 0F), (1F, 0F, 0F, 0F));

    public static Matrix3D RotateZ90 => new((0F, -1F, 0F, 0F), (1F, 0F, 0F, 0F), (0F, 0F, 1F, 0F));

    public static Matrix3D RotateZ180 =>
        new((-1F, 0F, 0F, 0F), (0F, -1F, 0F, 0F), (0F, 0F, 1F, 0F));

    public static Matrix3D RotateZ270 => new((0F, 1F, 0F, 0F), (-1F, 0F, 0F, 0F), (0F, 0F, 1F, 0F));

    public bool IsOrthogonal
    {
        get
        {
            var x = XVector;
            var y = YVector;
            var z = ZVector;

            return Vector3.DotProduct(x, y) <= float.Epsilon
                && Vector3.DotProduct(y, z) <= float.Epsilon
                && Vector3.DotProduct(z, x) <= float.Epsilon
                && float.Abs(x.Length2 - 1F) <= float.Epsilon
                && float.Abs(y.Length2 - 1F) <= float.Epsilon
                && float.Abs(z.Length2 - 1F) <= float.Epsilon;
        }
    }

    public Vector3 Translation
    {
        get => new(Rows[0][3], Rows[1][3], Rows[2][3]);
        set
        {
            Rows[0][3] = value[0];
            Rows[1][3] = value[1];
            Rows[2][3] = value[2];
        }
    }

    public float XTranslation
    {
        get => Rows[0][3];
        set => Rows[0][3] = value;
    }

    public float YTranslation
    {
        get => Rows[1][3];
        set => Rows[1][3] = value;
    }

    public float ZTranslation
    {
        get => Rows[2][3];
        set => Rows[2][3] = value;
    }

    public Vector3 XVector => new(Rows[0][0], Rows[1][0], Rows[2][0]);
    public Vector3 YVector => new(Rows[0][1], Rows[1][1], Rows[2][1]);
    public Vector3 ZVector => new(Rows[0][2], Rows[1][2], Rows[2][2]);

    public float XRotation => float.Atan2(Rows[2][1], Rows[1][1]);

    public float YRotation => float.Atan2(Rows[0][2], Rows[2][2]);

    public float ZRotation => float.Atan2(Rows[1][0], Rows[0][0]);

    public Matrix3D OrthogonalInverse
    {
        get
        {
            Matrix3D result = new();

            result.Rows[0][0] = Rows[0][0];
            result.Rows[0][1] = Rows[1][0];
            result.Rows[0][2] = Rows[2][0];

            result.Rows[1][0] = Rows[0][1];
            result.Rows[1][1] = Rows[1][1];
            result.Rows[1][2] = Rows[2][1];

            result.Rows[2][0] = Rows[0][2];
            result.Rows[2][1] = Rows[1][2];
            result.Rows[2][2] = Rows[2][2];

            var translation = Translation;
            translation = result.RotateVector3(translation);
            translation = -translation;

            result.Rows[0][3] = translation[0];
            result.Rows[1][3] = translation[1];
            result.Rows[2][3] = translation[2];

            return result;
        }
    }

    public Vector4 this[int index]
    {
        get =>
            index switch
            {
                0 => Rows[0],
                1 => Rows[1],
                2 => Rows[2],
                _ => throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Index must be between 0 and 2."
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
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        index,
                        "Index must be between 0 and 2."
                    );
            }
        }
    }

    public Matrix3D()
    {
        Rows = new Vector4[3];
    }

    public Matrix3D(float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 12);

        Rows = new Vector4[3];
        Set(values);
    }

    public Matrix3D(MatrixRow row1, MatrixRow row2, MatrixRow row3)
    {
        Rows = new Vector4[3];
        Set(row1, row2, row3);
    }

    public Matrix3D(Vector3 x, Vector3 y, Vector3 z, Vector3 position)
    {
        Rows = new Vector4[3];
        Set(x, y, z, position);
    }

    public Matrix3D(Vector3 axis, float angle)
    {
        Rows = new Vector4[3];
        Set(axis, angle);
    }

    public Matrix3D(Vector3 axis, float sin, float cos)
    {
        Rows = new Vector4[3];
        Set(axis, sin, cos);
    }

    public Matrix3D(Matrix3X3 rotation, Vector3 position)
    {
        Rows = new Vector4[3];
        Set(rotation, position);
    }

    public Matrix3D(Quaternion rotation, Vector3 position)
    {
        Rows = new Vector4[3];
        Set(rotation, position);
    }

    public Matrix3D(Vector3 position)
    {
        Rows = new Vector4[3];
        Set(position);
    }

    public Matrix3D(Matrix3D other)
    {
        Rows = other.Rows;
    }

    public static Matrix3D Multiply(Matrix3D left, Matrix3D right)
    {
        return left * right;
    }

    public static Vector3 TransformVector3(Matrix3D matrix, Vector3 vector)
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

    public static Vector3 InverseTransformVector3(Matrix3D matrix, Vector3 vector)
    {
        Vector3 diff = new(
            vector.X - matrix[0][3],
            vector.Y - matrix[1][3],
            vector.Z - matrix[2][3]
        );

        return InverseRotateVector3(matrix, diff);
    }

    public static Vector3 RotateVector3(Matrix3D matrix, Vector3 vector)
    {
        return matrix.RotateVector3(vector);
    }

    public static Vector3 InverseRotateVector3(Matrix3D matrix, Vector3 vector)
    {
        return matrix.InverseRotateVector3(vector);
    }

    public static Matrix3D GetInverse(INativeMatrixInverse nativeMatrixInverse)
    {
        Matrix3D result = new();

        result.Rows[0][0] = nativeMatrixInverse[0, 0];
        result.Rows[0][1] = nativeMatrixInverse[0, 1];
        result.Rows[0][2] = nativeMatrixInverse[0, 2];
        result.Rows[0][3] = nativeMatrixInverse[0, 3];

        result.Rows[1][0] = nativeMatrixInverse[1, 0];
        result.Rows[1][1] = nativeMatrixInverse[1, 1];
        result.Rows[1][2] = nativeMatrixInverse[1, 2];
        result.Rows[1][3] = nativeMatrixInverse[1, 3];

        result.Rows[2][0] = nativeMatrixInverse[2, 0];
        result.Rows[2][1] = nativeMatrixInverse[2, 1];
        result.Rows[2][2] = nativeMatrixInverse[2, 2];
        result.Rows[2][3] = nativeMatrixInverse[2, 3];

        return result;
    }

    public static Matrix3D Lerp(Matrix3D left, Matrix3D right, float factor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(factor, 0F);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(factor, 1F);

        Matrix3D result = new();
        var position = Vector3.Lerp(left.Translation, right.Translation, factor);
        var rotation = Quaternion.Slerp(Quaternion.Build(left), Quaternion.Build(right), factor);
        result.Set(rotation, position);
        return result;
    }

    public static bool SolveLinearSystem(ref Matrix3D system)
    {
        // Essentially system[0][0] == 0F
        if (float.Abs(system[0][0]) < float.Epsilon)
        {
            return false;
        }

        system[0] *= 1F / system[0][0];
        system[1] -= system[1][0] * system[0];
        system[2] -= system[2][0] * system[0];

        // Essentially system[1][1] == 0F
        if (float.Abs(system[1][1]) < float.Epsilon)
        {
            return false;
        }

        system[1] *= 1F / system[1][1];
        system[2] -= system[2][1] * system[1];

        // Essentially system[2][2] == 0F
        if (float.Abs(system[2][2]) < float.Epsilon)
        {
            return false;
        }

        system[2] *= 1F / system[2][2];

        system[1] -= system[1][2] * system[2];
        system[0] -= system[0][2] * system[2];

        system[0] -= system[0][1] * system[1];
        return true;
    }

    public void Set(float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 12);

        Rows[0].Set(values[0], values[1], values[2], values[3]);
        Rows[1].Set(values[4], values[5], values[6], values[7]);
        Rows[2].Set(values[8], values[9], values[10], values[11]);
    }

    public void Set(MatrixRow row1, MatrixRow row2, MatrixRow row3)
    {
        Rows[0].Set(row1.Member1, row1.Member2, row1.Member3, row1.Member4);
        Rows[1].Set(row2.Member1, row2.Member2, row2.Member3, row2.Member4);
        Rows[2].Set(row3.Member1, row3.Member2, row3.Member3, row3.Member4);
    }

    public void Set(Vector3 x, Vector3 y, Vector3 z, Vector3 position)
    {
        Rows[0].Set(x[0], y[0], z[0], position[0]);
        Rows[1].Set(x[1], y[1], z[1], position[1]);
        Rows[2].Set(x[2], y[2], z[2], position[2]);
    }

    public void Set(Vector3 axis, float angle)
    {
        Set(axis, float.Sin(angle), float.Cos(angle));
    }

    public void Set(Vector3 axis, float sin, float cos)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(axis.Length2 - 1F, 0.001F);

        Rows[0]
            .Set(
                float.Pow(axis[0], 2) + cos * (1F - float.Pow(axis[0], 2)),
                axis[0] * axis[1] * (1F - cos) - axis[2] * sin,
                axis[2] * axis[0] * (1F - cos) + axis[1] * sin,
                0F
            );

        Rows[1]
            .Set(
                axis[0] * axis[1] * (1F - cos) + axis[2] * sin,
                float.Pow(axis[1], 2) + cos * (1F - float.Pow(axis[1], 2)),
                axis[1] * axis[2] * (1F - cos) - axis[0] * sin,
                0F
            );

        Rows[1]
            .Set(
                axis[2] * axis[0] * (1F - cos) - axis[1] * sin,
                axis[1] * axis[2] * (1F - cos) + axis[0] * sin,
                float.Pow(axis[2], 2) + cos * (1F - float.Pow(axis[2], 2)),
                0F
            );
    }

    public void Set(Vector3 position)
    {
        Rows[0].Set(1F, 0F, 0F, position[0]);
        Rows[1].Set(0F, 1F, 0F, position[1]);
        Rows[2].Set(0F, 0F, 1F, position[2]);
    }

    public void Set(Matrix3X3 rotation, Vector3 position)
    {
        Rows[0].Set(rotation[0][0], rotation[0][1], rotation[0][2], position[0]);
        Rows[1].Set(rotation[1][0], rotation[1][1], rotation[1][2], position[1]);
        Rows[2].Set(rotation[2][0], rotation[2][1], rotation[2][2], position[2]);
    }

    public void Set(Quaternion rotation, Vector3 position)
    {
        SetRotation(rotation);
        Translation = position;
    }

    public void SetRotation(Matrix3X3 rotation)
    {
        Rows[0][0] = rotation[0][0];
        Rows[0][1] = rotation[0][1];
        Rows[0][2] = rotation[0][2];

        Rows[1][0] = rotation[1][0];
        Rows[1][1] = rotation[1][1];
        Rows[1][2] = rotation[1][2];

        Rows[2][0] = rotation[2][0];
        Rows[2][1] = rotation[2][1];
        Rows[2][2] = rotation[2][2];
    }

    public void SetRotation(Quaternion rotation)
    {
        Rows[0][0] = 1F - 2F * (rotation[1] * rotation[1] + rotation[2] * rotation[2]);
        Rows[0][1] = 2F * (rotation[0] * rotation[1] - rotation[2] * rotation[3]);
        Rows[0][2] = 2F * (rotation[2] * rotation[0] + rotation[1] * rotation[3]);

        Rows[1][0] = 2F * (rotation[0] * rotation[1] + rotation[2] * rotation[3]);
        Rows[1][1] = 1F - 2F * (rotation[2] * rotation[2] + rotation[0] * rotation[0]);
        Rows[1][2] = 2F * (rotation[1] * rotation[2] - rotation[0] * rotation[3]);

        Rows[2][0] = 2F * (rotation[2] * rotation[0] - rotation[1] * rotation[3]);
        Rows[2][1] = 2F * (rotation[1] * rotation[2] + rotation[0] * rotation[3]);
        Rows[2][2] = 1F - 2F * (rotation[1] * rotation[1] + rotation[0] * rotation[0]);
    }

    public void Translate(float x, float y, float z)
    {
        Rows[0][3] += Rows[0][0] * x + Rows[0][1] * y + Rows[0][2] * z;
        Rows[1][3] += Rows[1][0] * x + Rows[1][1] * y + Rows[1][2] * z;
        Rows[2][3] += Rows[2][0] * x + Rows[2][1] * y + Rows[2][2] * z;
    }

    public void Translate(Vector3 translation)
    {
        Rows[0][3] +=
            Rows[0][0] * translation[0] + Rows[0][1] * translation[1] + Rows[0][2] * translation[2];

        Rows[1][3] +=
            Rows[1][0] * translation[0] + Rows[1][1] * translation[1] + Rows[1][2] * translation[2];

        Rows[2][3] +=
            Rows[2][0] * translation[0] + Rows[2][1] * translation[1] + Rows[2][2] * translation[2];
    }

    public void TranslateX(float x)
    {
        Rows[0][3] += Rows[0][0] * x;
        Rows[1][3] += Rows[1][0] * x;
        Rows[2][3] += Rows[2][0] * x;
    }

    public void TranslateY(float y)
    {
        Rows[0][3] += Rows[1][1] * y;
        Rows[1][3] += Rows[1][1] * y;
        Rows[2][3] += Rows[2][1] * y;
    }

    public void TranslateZ(float z)
    {
        Rows[0][3] += Rows[2][2] * z;
        Rows[1][3] += Rows[2][2] * z;
        Rows[2][3] += Rows[2][2] * z;
    }

    public void AdjustTranslation(Vector3 translation)
    {
        Rows[0][3] += translation[0];
        Rows[1][3] += translation[1];
        Rows[2][3] += translation[2];
    }

    public void AdjustXTranslation(float translation)
    {
        Rows[0][3] += translation;
    }

    public void AdjustYTranslation(float translation)
    {
        Rows[1][3] += translation;
    }

    public void AdjustZTranslation(float translation)
    {
        Rows[2][3] += translation;
    }

    public void MakeIdentity()
    {
        Rows[0].Set(1F, 0F, 0F, 0F);
        Rows[1].Set(0F, 1F, 0F, 0F);
        Rows[2].Set(0F, 0F, 1F, 0F);
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

    public void Scale(float scale)
    {
        Rows[0][0] *= scale;
        Rows[1][0] *= scale;
        Rows[2][0] *= scale;

        Rows[0][1] *= scale;
        Rows[1][1] *= scale;
        Rows[2][1] *= scale;

        Rows[0][2] *= scale;
        Rows[1][2] *= scale;
        Rows[2][2] *= scale;
    }

    public void Scale(float x, float y, float z)
    {
        Rows[0][0] *= x;
        Rows[1][0] *= x;
        Rows[2][0] *= x;

        Rows[0][1] *= y;
        Rows[1][1] *= y;
        Rows[2][1] *= y;

        Rows[0][2] *= z;
        Rows[1][2] *= z;
        Rows[2][2] *= z;
    }

    public void Scale(Vector3 scale)
    {
        Scale(scale.X, scale.Y, scale.Z);
    }

    public Vector3 RotateVector3(Vector3 vector)
    {
        return new Vector3(
            Rows[0][0] * vector.X + Rows[0][1] * vector.Y + Rows[0][2] * vector.Z,
            Rows[1][0] * vector.X + Rows[1][1] * vector.Y + Rows[1][2] * vector.Z,
            Rows[2][0] * vector.X + Rows[2][1] * vector.Y + Rows[2][2] * vector.Z
        );
    }

    public Vector3 InverseRotateVector3(Vector3 vector)
    {
        return new Vector3(
            Rows[0][0] * vector.X + Rows[1][0] * vector.Y + Rows[2][0] * vector.Z,
            Rows[0][1] * vector.X + Rows[1][1] * vector.Y + Rows[2][1] * vector.Z,
            Rows[0][2] * vector.X + Rows[1][2] * vector.Y + Rows[2][2] * vector.Z
        );
    }

    public void LookAt(Vector3 position, Vector3 target, float roll)
    {
        var dx = target[0] - position[0];
        var dy = target[1] - position[1];
        var dz = target[2] - position[2];

        var len1 = float.Sqrt(float.Pow(dx, 2) + float.Pow(dy, 2) + float.Pow(dz, 2));
        var len2 = float.Sqrt(float.Pow(dx, 2) + float.Pow(dy, 2));

        float sinPosition;
        float cosPosition;
        // Essentially len1 != 0F
        if (float.Abs(len1) > float.Epsilon)
        {
            sinPosition = dz / len1;
            cosPosition = len2 / len1;
        }
        else
        {
            sinPosition = 0F;
            cosPosition = 1F;
        }

        float sinYaw;
        float cosYaw;
        // Essentially len2 != 0F
        if (float.Abs(len2) > float.Epsilon)
        {
            sinYaw = dy / len2;
            cosYaw = dx / len2;
        }
        else
        {
            sinYaw = 0F;
            cosYaw = 1F;
        }

        Rows[0].X = 0F;
        Rows[0].Y = 0F;
        Rows[0].Z = -1F;

        Rows[1].X = -1F;
        Rows[1].Y = 0F;
        Rows[1].Z = 0F;

        Rows[2].X = 0F;
        Rows[2].Y = 1F;
        Rows[2].Z = 0F;

        Rows[0].W = position.X;
        Rows[1].W = position.Y;
        Rows[2].W = position.Z;

        RotateY(sinYaw, cosYaw);
        RotateX(sinPosition, cosPosition);
        RotateZ(-roll);
    }

    public void ObjectLookAt(Vector3 position, Vector3 target, float roll)
    {
        var dx = target[0] - position[0];
        var dy = target[1] - position[1];
        var dz = target[2] - position[2];

        var len1 = float.Sqrt(float.Pow(dx, 2) + float.Pow(dy, 2) + float.Pow(dz, 2));
        var len2 = float.Sqrt(float.Pow(dx, 2) + float.Pow(dy, 2));

        float sinPosition;
        float cosPosition;
        // Essentially len1 != 0F
        if (float.Abs(len1) > float.Epsilon)
        {
            sinPosition = dz / len1;
            cosPosition = len2 / len1;
        }
        else
        {
            sinPosition = 0F;
            cosPosition = 1F;
        }

        float sinYaw;
        float cosYaw;
        // Essentially len2 != 0F
        if (float.Abs(len2) > float.Epsilon)
        {
            sinYaw = dy / len2;
            cosYaw = dx / len2;
        }
        else
        {
            sinYaw = 0F;
            cosYaw = 1F;
        }

        MakeIdentity();
        Translate(position);
        RotateZ(sinYaw, cosYaw);
        RotateY(-sinPosition, cosPosition);
        RotateX(roll);
    }

    public void PreRotateX(float theta)
    {
        PreRotateX(float.Sin(theta), float.Cos(theta));
    }

    public void PreRotateX(float sin, float cos)
    {
        var tmp1 = Rows[1][0];
        var tmp2 = Rows[2][1];
        Rows[1][0] = cos * tmp1 - sin * tmp2;
        Rows[2][1] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[1][1];
        tmp2 = Rows[2][1];
        Rows[1][1] = cos * tmp1 - sin * tmp2;
        Rows[2][1] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[1][2];
        tmp2 = Rows[2][2];
        Rows[1][2] = cos * tmp1 - sin * tmp2;
        Rows[2][2] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[1][3];
        tmp2 = Rows[2][3];
        Rows[1][3] = cos * tmp1 - sin * tmp2;
        Rows[2][3] = sin * tmp1 + cos * tmp2;
    }

    public void PreRotateY(float theta)
    {
        PreRotateY(float.Sin(theta), float.Cos(theta));
    }

    public void PreRotateY(float sin, float cos)
    {
        var tmp1 = Rows[0][0];
        var tmp2 = Rows[2][0];
        Rows[0][0] = cos * tmp1 + sin * tmp2;
        Rows[2][0] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][1];
        tmp2 = Rows[2][1];
        Rows[0][1] = cos * tmp1 + sin * tmp2;
        Rows[2][1] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][2];
        tmp2 = Rows[2][2];
        Rows[0][2] = cos * tmp1 + sin * tmp2;
        Rows[2][2] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][3];
        tmp2 = Rows[2][3];
        Rows[0][3] = cos * tmp1 + sin * tmp2;
        Rows[2][3] = -sin * tmp1 + cos * tmp2;
    }

    public void PreRotateZ(float theta)
    {
        PreRotateZ(float.Sin(theta), float.Cos(theta));
    }

    public void PreRotateZ(float sin, float cos)
    {
        var tmp1 = Rows[0][0];
        var tmp2 = Rows[1][0];
        Rows[0][0] = cos * tmp1 - sin * tmp2;
        Rows[1][0] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][1];
        tmp2 = Rows[1][1];
        Rows[0][1] = cos * tmp1 - sin * tmp2;
        Rows[1][1] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][2];
        tmp2 = Rows[1][2];
        Rows[0][2] = cos * tmp1 - sin * tmp2;
        Rows[1][2] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][3];
        tmp2 = Rows[1][3];
        Rows[0][3] = cos * tmp1 - sin * tmp2;
        Rows[1][3] = sin * tmp1 + cos * tmp2;
    }

    public void InPlacePreRotateX(float theta)
    {
        InPlacePreRotateX(float.Sin(theta), float.Cos(theta));
    }

    public void InPlacePreRotateX(float sin, float cos)
    {
        var tmp1 = Rows[1][0];
        var tmp2 = Rows[2][0];
        Rows[1][0] = cos * tmp1 - sin * tmp2;
        Rows[2][0] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[1][1];
        tmp2 = Rows[2][1];
        Rows[1][1] = cos * tmp1 - sin * tmp2;
        Rows[2][1] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[1][2];
        tmp2 = Rows[2][2];
        Rows[1][2] = cos * tmp1 - sin * tmp2;
        Rows[2][2] = sin * tmp1 + cos * tmp2;
    }

    public void InPlacePreRotateY(float theta)
    {
        InPlacePreRotateY(float.Sin(theta), float.Cos(theta));
    }

    public void InPlacePreRotateY(float sin, float cos)
    {
        var tmp1 = Rows[0][0];
        var tmp2 = Rows[2][0];
        Rows[0][0] = cos * tmp1 + sin * tmp2;
        Rows[2][0] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][1];
        tmp2 = Rows[2][1];
        Rows[0][1] = cos * tmp1 + sin * tmp2;
        Rows[2][1] = -sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][2];
        tmp2 = Rows[2][2];
        Rows[0][2] = cos * tmp1 + sin * tmp2;
        Rows[2][2] = -sin * tmp1 + cos * tmp2;
    }

    public void InPlacePreRotateZ(float theta)
    {
        InPlacePreRotateZ(float.Sin(theta), float.Cos(theta));
    }

    public void InPlacePreRotateZ(float sin, float cos)
    {
        var tmp1 = Rows[0][0];
        var tmp2 = Rows[1][0];
        Rows[0][0] = cos * tmp1 - sin * tmp2;
        Rows[1][0] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][1];
        tmp2 = Rows[1][1];
        Rows[0][1] = cos * tmp1 - sin * tmp2;
        Rows[1][1] = sin * tmp1 + cos * tmp2;

        tmp1 = Rows[0][2];
        tmp2 = Rows[1][2];
        Rows[0][2] = cos * tmp1 - sin * tmp2;
        Rows[1][2] = sin * tmp1 + cos * tmp2;
    }

    public void MultiplyInPlace(Matrix3D left, Matrix3D right)
    {
        if (ReferenceEquals(this, left))
        {
            throw new InvalidOperationException("Cannot multiply in place a matrix by itself.");
        }

        var tmp1 = right.Rows[0].X;
        var tmp2 = right.Rows[1].X;
        var tmp3 = right.Rows[2].X;

        Rows[0].X = SubMultiply(left.Rows[0], tmp1, tmp2, tmp3);
        Rows[1].X = SubMultiply(left.Rows[1], tmp1, tmp2, tmp3);
        Rows[2].X = SubMultiply(left.Rows[2], tmp1, tmp2, tmp3);

        tmp1 = right.Rows[0].Y;
        tmp2 = right.Rows[1].Y;
        tmp3 = right.Rows[2].Y;

        Rows[0].Y = SubMultiply(left.Rows[0], tmp1, tmp2, tmp3);
        Rows[1].Y = SubMultiply(left.Rows[1], tmp1, tmp2, tmp3);
        Rows[2].Y = SubMultiply(left.Rows[2], tmp1, tmp2, tmp3);

        tmp1 = right.Rows[0].Z;
        tmp2 = right.Rows[1].Z;
        tmp3 = right.Rows[2].Z;

        Rows[0].Z = SubMultiply(left.Rows[0], tmp1, tmp2, tmp3);
        Rows[1].Z = SubMultiply(left.Rows[1], tmp1, tmp2, tmp3);
        Rows[2].Z = SubMultiply(left.Rows[2], tmp1, tmp2, tmp3);

        tmp1 = right.Rows[0].W;
        tmp2 = right.Rows[1].W;
        tmp3 = right.Rows[2].W;

        Rows[0].W = SubMultiply(left.Rows[0], tmp1, tmp2, tmp3) + left.Rows[0].W;
        Rows[1].W = SubMultiply(left.Rows[1], tmp1, tmp2, tmp3) + left.Rows[1].W;
        Rows[2].W = SubMultiply(left.Rows[2], tmp1, tmp2, tmp3) + left.Rows[2].W;
    }

    public void PreMultiply(Matrix3D other)
    {
        if (ReferenceEquals(this, other))
        {
            throw new InvalidOperationException("Cannot pre-multiply a matrix by itself.");
        }

        MultiplyInPlace(other, this);
    }

    public void PostMultiply(Matrix3D other)
    {
        if (ReferenceEquals(this, other))
        {
            throw new InvalidOperationException("Cannot post-multiply a matrix by itself.");
        }

        var tmpX = SubMultiply(Rows[0], other.Rows[0].X, other.Rows[1].X, other.Rows[2].X);
        var tmpY = SubMultiply(Rows[0], other.Rows[0].Y, other.Rows[1].Y, other.Rows[1].Z);
        var tmpZ = SubMultiply(Rows[0], other.Rows[0].Z, other.Rows[1].Z, other.Rows[2].Z);
        var tmpW = SubMultiply(Rows[0], other.Rows[0].W, other.Rows[1].W, other.Rows[2].W);

        Rows[0].X = tmpX;
        Rows[0].Y = tmpY;
        Rows[0].Z = tmpZ;
        Rows[0].W += tmpW;

        tmpX = SubMultiply(Rows[1], other.Rows[0].X, other.Rows[1].X, other.Rows[2].X);
        tmpY = SubMultiply(Rows[1], other.Rows[0].Y, other.Rows[1].Y, other.Rows[2].Y);
        tmpZ = SubMultiply(Rows[1], other.Rows[0].Z, other.Rows[1].Z, other.Rows[2].Z);
        tmpW = SubMultiply(Rows[1], other.Rows[0].W, other.Rows[1].W, other.Rows[2].W);

        Rows[1].X = tmpX;
        Rows[1].Y = tmpY;
        Rows[1].Z = tmpZ;
        Rows[1].W += tmpW;

        tmpX = SubMultiply(Rows[2], other.Rows[0].X, other.Rows[1].X, other.Rows[2].X);
        tmpY = SubMultiply(Rows[2], other.Rows[0].Y, other.Rows[1].Y, other.Rows[2].Y);
        tmpZ = SubMultiply(Rows[2], other.Rows[0].Z, other.Rows[1].Z, other.Rows[2].Z);
        tmpW = SubMultiply(Rows[2], other.Rows[0].W, other.Rows[1].W, other.Rows[2].W);

        Rows[2].X = tmpX;
        Rows[2].Y = tmpY;
        Rows[2].Z = tmpZ;
        Rows[2].W += tmpW;
    }

    public Vector3 MultiplyVector3(Vector3 vector)
    {
        return this * vector;
    }

    public Vector3[] MultiplyVector3Array(Vector3[] vectors)
    {
        var result = new List<Vector3>(vectors.Length);
        result.AddRange(vectors.Select(vector => this * vector));
        return result.ToArray();
    }

    public void BuildTransformMatrix(Vector3 position, Vector3 direction)
    {
        var len2 = float.Sqrt(float.Pow(direction.X, 2) + float.Pow(direction.Y, 2));
        var sinPosition = direction.Z;
        var cosPosition = len2;

        float sinYaw;
        float cosYaw;
        // Essentially len2 != 0F
        if (float.Abs(len2) > float.Epsilon)
        {
            sinYaw = direction.Y / len2;
            cosYaw = direction.X / len2;
        }
        else
        {
            sinYaw = 0F;
            cosYaw = 1F;
        }

        MakeIdentity();
        Translate(position);
        RotateZ(sinYaw, cosYaw);
        RotateY(-sinPosition, cosPosition);
    }

    public void Copy3X3Matrix(Matrix3X3 matrix)
    {
        Rows[0][0] = matrix[0][0];
        Rows[0][1] = matrix[0][1];
        Rows[0][2] = matrix[0][2];
        Rows[0][3] = 0F;

        Rows[1][0] = matrix[1][0];
        Rows[1][1] = matrix[1][1];
        Rows[1][2] = matrix[1][2];
        Rows[1][3] = 0F;

        Rows[2][0] = matrix[2][0];
        Rows[2][1] = matrix[2][1];
        Rows[2][2] = matrix[2][2];
        Rows[2][3] = 0F;
    }

    public (Vector3 Min, Vector3 Max) TransformAxisAlignedBox(Vector3 min, Vector3 max)
    {
        Vector3 resultMin = new();
        Vector3 resultMax = new();

        resultMin.X = resultMax.X = Rows[0][3];
        resultMin.Y = resultMax.Y = Rows[1][3];
        resultMin.Z = resultMax.Z = Rows[2][3];

        for (var i = 0; i < Rows.Length; i++)
        {
            for (var j = 0; j < Rows[i].Length; j++)
            {
                var tmp0 = Rows[i][j] * min[j];
                var tmp1 = Rows[i][j] * max[j];
                if (tmp0 < tmp1)
                {
                    resultMin[i] += tmp0;
                    resultMax[i] += tmp1;
                }
                else
                {
                    resultMin[i] += tmp1;
                    resultMax[i] += tmp0;
                }
            }
        }

        return (resultMin, resultMax);
    }

    public (Vector3 Center, Vector3 Extent) TransformCenterExtentAxisAlignedBox(
        Vector3 center,
        Vector3 extent
    )
    {
        Vector3 resultCenter = new();
        Vector3 resultExtent = new();
        for (var i = 0; i < Rows.Length; i++)
        {
            resultCenter[i] = Rows[i][3];
            resultExtent[i] = 0F;
            for (var j = 0; j < Rows[i].Length; j++)
            {
                resultCenter[i] += Rows[i][j] * center[j];
                resultExtent[i] += float.Abs(Rows[i][j] * extent[j]);
            }
        }

        return (resultCenter, resultExtent);
    }

    public void ReOrthogonalize()
    {
        var x = XVector;
        var y = YVector;

        var z = Vector3.CrossProduct(x, y);
        y = Vector3.CrossProduct(z, x);

        var len = x.Length;
        if (float.Abs(len) < float.Epsilon)
        {
            MakeIdentity();
            return;
        }

        x *= 1F / len;
        len = y.Length;
        if (float.Abs(len) < float.Epsilon)
        {
            MakeIdentity();
            return;
        }

        y *= 1F / len;
        len = z.Length;
        if (float.Abs(len) < float.Epsilon)
        {
            MakeIdentity();
            return;
        }

        z *= 1F / len;

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
        return obj is Matrix3D other && Equals(this, other);
    }

    public bool Equals(Matrix3D? x, Matrix3D? y)
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

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return Enumerable.Range(0, x.Rows.Length).All(i => x[i] == y[i]);
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Matrix3D obj)
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

    public (Vector4 Member1, Vector4 Member2, Vector4 Member3) Deconstruct()
    {
        return (Rows[0], Rows[1], Rows[2]);
    }

    private static float SubMultiply(Vector4 row, float tmp1, float tmp2, float tmp3)
    {
        return row.X * tmp1 + row.Y * tmp2 + row.Z * tmp3;
    }

    public static Matrix3D operator *(Matrix3D x, Matrix3D y)
    {
        Matrix3D result = new();

        var tmp1 = y[0][0];
        var tmp2 = y[1][0];
        var tmp3 = y[2][0];
        result[0][0] = x[0][0] * tmp1 + x[0][1] * tmp2 + x[0][2] * tmp3;
        result[1][0] = x[1][0] * tmp1 + x[1][1] * tmp2 + x[1][2] * tmp3;
        result[2][0] = x[2][0] * tmp1 + x[2][1] * tmp2 + x[2][2] * tmp3;

        tmp1 = y[0][1];
        tmp2 = y[1][1];
        tmp3 = y[2][1];
        result[0][1] = x[0][0] * tmp1 + x[0][1] * tmp2 + x[0][2] * tmp3;
        result[1][1] = x[1][0] * tmp1 + x[1][1] * tmp2 + x[1][2] * tmp3;
        result[2][1] = x[2][0] * tmp1 + x[2][1] * tmp2 + x[2][2] * tmp3;

        tmp1 = y[0][2];
        tmp2 = y[1][2];
        tmp3 = y[2][2];
        result[0][2] = x[0][0] * tmp1 + x[0][1] * tmp2 + x[0][2] * tmp3;
        result[1][2] = x[1][0] * tmp1 + x[1][1] * tmp2 + x[1][2] * tmp3;
        result[2][2] = x[2][0] * tmp1 + x[2][1] * tmp2 + x[2][2] * tmp3;

        tmp1 = y[0][3];
        tmp2 = y[1][3];
        tmp3 = y[2][3];
        result[0][3] = x[0][0] * tmp1 + x[0][1] * tmp2 + x[0][2] * tmp3 + x[0][3];
        result[1][3] = x[1][0] * tmp1 + x[1][1] * tmp2 + x[1][2] * tmp3 + x[1][3];
        result[2][3] = x[2][0] * tmp1 + x[2][1] * tmp2 + x[2][2] * tmp3 + x[2][3];

        return result;
    }

    public static Vector3 operator *(Matrix3D matrix, Vector3 vector)
    {
        return new Vector3(
            matrix.Rows[0].X * vector.X
                + matrix.Rows[0].Y * vector.Y
                + matrix.Rows[0].Z * vector.Z
                + matrix.Rows[0].W,
            matrix.Rows[1].X * vector.X
                + matrix.Rows[1].Y * vector.Y
                + matrix.Rows[1].Z * vector.Z
                + matrix.Rows[1].W,
            matrix.Rows[2].X * vector.X
                + matrix.Rows[2].Y * vector.Y
                + matrix.Rows[2].Z * vector.Z
                + matrix.Rows[2].W
        );
    }

    public static Vector3 operator *(Vector3 vector, Matrix3D matrix)
    {
        return matrix * vector;
    }

    [SuppressMessage(
        "csharpsquid",
        "S3875:operator==\" should not be overloaded on reference types",
        Justification = "This is a mathematical class that can be equated, but cannot have addition or substraction."
    )]
    public static bool operator ==(Matrix3D x, Matrix3D y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(Matrix3D x, Matrix3D y)
    {
        return !x.Equals(y);
    }
}
