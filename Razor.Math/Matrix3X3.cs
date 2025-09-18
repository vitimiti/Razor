// -----------------------------------------------------------------------
// <copyright file="Matrix3X3.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
using Matrix3X3Row = (float Member1, float Member2, float Member3);
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly

namespace Razor.Math;

/// <summary>Represents a 3x3 matrix for 3D transformations and linear algebra operations.</summary>
public class Matrix3X3 : IEqualityComparer<Matrix3X3>
{
    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class with default values.</summary>
    public Matrix3X3() { }

    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class by copying another matrix.</summary>
    /// <param name="other">The matrix to copy.</param>
    public Matrix3X3([NotNull] Matrix3X3 other) => Row = other.Row;

    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class with three row vectors.</summary>
    /// <param name="r0">The first row vector.</param>
    /// <param name="r1">The second row vector.</param>
    /// <param name="r2">The third row vector.</param>
    public Matrix3X3(Vector3 r0, Vector3 r1, Vector3 r2) => Set(r0, r1, r2);

    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class with three row tuples.</summary>
    /// <param name="r0">The first row as a tuple.</param>
    /// <param name="r1">The second row as a tuple.</param>
    /// <param name="r2">The third row as a tuple.</param>
    public Matrix3X3(Matrix3X3Row r0, Matrix3X3Row r1, Matrix3X3Row r2) => Set(r0, r1, r2);

    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class as a rotation matrix from an axis and angle.</summary>
    /// <param name="axis">The rotation axis.</param>
    /// <param name="angle">The rotation angle in radians.</param>
    public Matrix3X3(Vector3 axis, float angle) => Set(axis, angle);

    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class as a rotation matrix from an axis and precomputed sine and cosine.</summary>
    /// <param name="axis">The rotation axis.</param>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public Matrix3X3(Vector3 axis, float sin, float cos) => Set(axis, sin, cos);

    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class from a quaternion.</summary>
    /// <param name="quaternion">The quaternion to convert.</param>
    public Matrix3X3(Quaternion quaternion) => Set(quaternion);

    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class from a 3D matrix.</summary>
    /// <param name="matrix3D">The 3D matrix to convert.</param>
    public Matrix3X3(Matrix3D matrix3D) => Set(matrix3D);

    /// <summary>Initializes a new instance of the <see cref="Matrix3X3"/> class from a 4x4 matrix.</summary>
    /// <param name="matrix4X4">The 4x4 matrix to convert.</param>
    public Matrix3X3(Matrix4X4 matrix4X4) => Set(matrix4X4);

    /// <summary>Gets the 3x3 identity matrix.</summary>
    /// <value>A matrix with ones on the diagonal and zeros elsewhere.</value>
    public static Matrix3X3 Identity => new(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1));

    /// <summary>Gets a matrix representing a 90-degree rotation around the X-axis.</summary>
    /// <value>A rotation matrix for 90 degrees around the X-axis.</value>
    public static Matrix3X3 RotateX90 => new((1, 0, 0), (0, 0, -1), (0, 1, 0));

    /// <summary>Gets a matrix representing a 180-degree rotation around the X-axis.</summary>
    /// <value>A rotation matrix for 180 degrees around the X-axis.</value>
    public static Matrix3X3 RotateX180 => new((1, 0, 0), (0, -1, 0), (0, 0, -1));

    /// <summary>Gets a matrix representing a 270-degree rotation around the X-axis.</summary>
    /// <value>A rotation matrix for 270 degrees around the X-axis.</value>
    public static Matrix3X3 RotateX270 => new((1, 0, 0), (0, 0, 1), (0, -1, 0));

    /// <summary>Gets a matrix representing a 90-degree rotation around the Y-axis.</summary>
    /// <value>A rotation matrix for 90 degrees around the Y-axis.</value>
    public static Matrix3X3 RotateY90 => new((0, 0, 1), (0, 1, 0), (-1, 0, 0));

    /// <summary>Gets a matrix representing a 180-degree rotation around the Y-axis.</summary>
    /// <value>A rotation matrix for 180 degrees around the Y-axis.</value>
    public static Matrix3X3 RotateY180 => new((-1, 0, 0), (0, 1, 0), (0, 0, -1));

    /// <summary>Gets a matrix representing a 270-degree rotation around the Y-axis.</summary>
    /// <value>A rotation matrix for 270 degrees around the Y-axis.</value>
    public static Matrix3X3 RotateY270 => new((0, 0, -1), (0, 1, 0), (1, 0, 0));

    /// <summary>Gets a matrix representing a 90-degree rotation around the Z-axis.</summary>
    /// <value>A rotation matrix for 90 degrees around the Z-axis.</value>
    public static Matrix3X3 RotateZ90 => new((0, -1, 0), (1, 0, 0), (0, 0, 1));

    /// <summary>Gets a matrix representing a 180-degree rotation around the Z-axis.</summary>
    /// <value>A rotation matrix for 180 degrees around the Z-axis.</value>
    public static Matrix3X3 RotateZ180 => new((-1, 0, 0), (0, -1, 0), (0, 0, 1));

    /// <summary>Gets a matrix representing a 270-degree rotation around the Z-axis.</summary>
    /// <value>A rotation matrix for 270 degrees around the Z-axis.</value>
    public static Matrix3X3 RotateZ270 => new((0, 1, 0), (-1, 0, 0), (0, 0, 1));

    /// <summary>Gets the transpose of the matrix.</summary>
    /// <value>A new matrix that is the transpose of this matrix.</value>
    public Matrix3X3 Transpose =>
        new((Row[0][0], Row[1][0], Row[2][0]), (Row[0][1], Row[1][1], Row[2][1]), (Row[0][2], Row[1][2], Row[2][2]));

    /// <summary>Gets the inverse of the matrix.</summary>
    /// <value>A new matrix that is the inverse of this matrix.</value>
    /// <exception cref="InvalidOperationException">Thrown when the matrix is singular and cannot be inverted.</exception>
    public Matrix3X3 Inverse
    {
        get
        {
            Matrix3X3 a = new(this);
            Matrix3X3 b = Identity;
            for (var i = 0; i < 3; i++)
            {
                var j = i;
                for (var k = i + 1; k < 3; k++)
                {
                    if (float.Abs(a[k][i]) > float.Abs(a[j][i]))
                    {
                        j = k;
                    }
                }

                (a.Row[j], a.Row[i]) = (a.Row[i], a.Row[j]);
                (b.Row[j], b.Row[i]) = (b.Row[i], b.Row[j]);

                if (float.Abs(a[i][i]) < float.Epsilon)
                {
                    throw new InvalidOperationException("Matrix is singular, cannot invert.");
                }

                b.Row[i] /= a.Row[i][i];
                a.Row[i] /= a.Row[i][i];
                for (var l = 0; l < 3; l++)
                {
                    if (l == i)
                    {
                        continue;
                    }

                    b.Row[l] -= a[l][i] * b.Row[i];
                    a.Row[l] -= a[l][i] * a.Row[i];
                }
            }

            return b;
        }
    }

    /// <summary>Gets the determinant of the matrix.</summary>
    /// <value>The determinant value of the matrix.</value>
    public float Determinant =>
        (Row[0][0] * ((Row[1][1] * Row[2][2]) - (Row[1][2] * Row[2][1])))
        - (Row[0][1] * ((Row[1][0] * Row[2][2]) - (Row[1][2] * Row[2][0])))
        - (Row[0][2] * ((Row[1][0] * Row[2][1]) - (Row[1][1] * Row[2][0])));

    /// <summary>Gets the rotation angle around the X-axis in radians.</summary>
    /// <value>The X-axis rotation angle in radians.</value>
    public float XRotation
    {
        get
        {
            Vector3 v = this * new Vector3(0, 1, 0);
            return float.Atan2(v[2], v[1]);
        }
    }

    /// <summary>Gets the rotation angle around the Y-axis in radians.</summary>
    /// <value>The Y-axis rotation angle in radians.</value>
    public float YRotation
    {
        get
        {
            Vector3 v = this * new Vector3(0, 0, 1);
            return float.Atan2(v[0], v[2]);
        }
    }

    /// <summary>Gets the rotation angle around the Z-axis in radians.</summary>
    /// <value>The Z-axis rotation angle in radians.</value>
    public float ZRotation
    {
        get
        {
            Vector3 v = this * new Vector3(1, 0, 0);
            return float.Atan2(v[1], v[0]);
        }
    }

    /// <summary>Gets the X-axis vector (first column) of the matrix.</summary>
    /// <value>The X-axis vector extracted from the matrix.</value>
    public Vector3 XVector => new(Row[0][0], Row[1][0], Row[2][0]);

    /// <summary>Gets the Y-axis vector (second column) of the matrix.</summary>
    /// <value>The Y-axis vector extracted from the matrix.</value>
    public Vector3 YVector => new(Row[0][1], Row[1][1], Row[2][1]);

    /// <summary>Gets the Z-axis vector (third column) of the matrix.</summary>
    /// <value>The Z-axis vector extracted from the matrix.</value>
    public Vector3 ZVector => new(Row[0][2], Row[1][2], Row[2][2]);

    /// <summary>Gets a value indicating whether the matrix is orthogonal.</summary>
    /// <value><c>true</c> if the matrix is orthogonal; otherwise, <c>false</c>.</value>
    /// <remarks>An orthogonal matrix has orthonormal basis vectors (perpendicular and unit length).</remarks>
    public bool IsOrthogonal
    {
        get
        {
            Vector3 x = new(Row[0].X, Row[0].Y, Row[0].Z);
            Vector3 y = new(Row[1].X, Row[1].Y, Row[1].Z);
            Vector3 z = new(Row[2].X, Row[2].Y, Row[2].Z);

            return Vector3.DotProduct(x, y) <= float.Epsilon
                && Vector3.DotProduct(y, z) <= float.Epsilon
                && Vector3.DotProduct(z, x) <= float.Epsilon
                && float.Abs(x.Length - 1F) <= float.Epsilon
                && float.Abs(y.Length - 1F) <= float.Epsilon
                && float.Abs(z.Length - 1F) <= float.Epsilon;
        }
    }

    /// <summary>Gets the collection of row vectors that define the matrix.</summary>
    /// <value>A collection of <see cref="Vector3"/> representing the rows of the matrix.</value>
    protected Collection<Vector3> Row { get; } = [new(), new(), new()];

    /// <summary>Gets or sets the row vector at the specified index.</summary>
    /// <param name="index">The zero-based index of the row (0-2).</param>
    /// <value>The row vector at the specified index.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the valid range (0-2).</exception>
    public Vector3 this[int index]
    {
        get => Row[index];
        set => Row[index] = value;
    }

    /// <summary>Explicitly converts a <see cref="Matrix3D"/> to a <see cref="Matrix3X3"/>.</summary>
    /// <param name="matrix3D">The matrix to convert.</param>
    /// <returns>A new <see cref="Matrix3X3"/> instance.</returns>
    public static explicit operator Matrix3X3(Matrix3D matrix3D) => FromMatrix3D(matrix3D);

    /// <summary>Explicitly converts a <see cref="Matrix4X4"/> to a <see cref="Matrix3X3"/>.</summary>
    /// <param name="matrix4X4">The matrix to convert.</param>
    /// <returns>A new <see cref="Matrix3X3"/> instance.</returns>
    public static explicit operator Matrix3X3(Matrix4X4 matrix4X4) => FromMatrix4X4(matrix4X4);

    /// <summary>Adds two matrices together.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The sum of the two matrices.</returns>
    public static Matrix3X3 operator +(Matrix3X3 left, Matrix3X3 right) => Add(left, right);

    /// <summary>Subtracts one matrix from another.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The difference of the two matrices.</returns>
    public static Matrix3X3 operator -(Matrix3X3 left, Matrix3X3 right) => Subtract(left, right);

    /// <summary>Multiplies a matrix by a scalar value.</summary>
    /// <param name="matrix">The matrix to multiply.</param>
    /// <param name="scalar">The scalar value.</param>
    /// <returns>The scaled matrix.</returns>
    public static Matrix3X3 operator *(Matrix3X3 matrix, float scalar) => Multiply(matrix, scalar);

    /// <summary>Multiplies a scalar value by a matrix.</summary>
    /// <param name="scalar">The scalar value.</param>
    /// <param name="matrix">The matrix to multiply.</param>
    /// <returns>The scaled matrix.</returns>
    public static Matrix3X3 operator *(float scalar, Matrix3X3 matrix) => Multiply(matrix, scalar);

    /// <summary>Multiplies two matrices together.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The product of the two matrices.</returns>
    public static Matrix3X3 operator *(Matrix3X3 left, Matrix3X3 right) => Multiply(left, right);

    /// <summary>Multiplies a matrix by a vector.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The transformed vector.</returns>
    public static Vector3 operator *(Matrix3X3 matrix, Vector3 vector) => Multiply(matrix, vector);

    /// <summary>Multiplies a 3x3 matrix by a 3D matrix.</summary>
    /// <param name="matrix3X3">The 3x3 matrix.</param>
    /// <param name="matrix3D">The 3D matrix.</param>
    /// <returns>The product matrix.</returns>
    public static Matrix3X3 operator *(Matrix3X3 matrix3X3, Matrix3D matrix3D) => Multiply(matrix3X3, matrix3D);

    /// <summary>Multiplies a 3D matrix by a 3x3 matrix.</summary>
    /// <param name="matrix3D">The 3D matrix.</param>
    /// <param name="matrix3X3">The 3x3 matrix.</param>
    /// <returns>The product matrix.</returns>
    public static Matrix3X3 operator *(Matrix3D matrix3D, Matrix3X3 matrix3X3) => Multiply(matrix3D, matrix3X3);

    /// <summary>Divides a matrix by a scalar value.</summary>
    /// <param name="matrix">The matrix to divide.</param>
    /// <param name="scalar">The scalar divisor.</param>
    /// <returns>The divided matrix.</returns>
    public static Matrix3X3 operator /(Matrix3X3 matrix, float scalar) => Divide(matrix, scalar);

    /// <summary>Negates all elements of a matrix.</summary>
    /// <param name="matrix">The matrix to negate.</param>
    /// <returns>The negated matrix.</returns>
    public static Matrix3X3 operator -(Matrix3X3 matrix) => Negate(matrix);

    /// <summary>Determines whether two matrices are equal.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns><c>true</c> if the matrices are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Matrix3X3 left, Matrix3X3 right) => object.Equals(left, right);

    /// <summary>Determines whether two matrices are not equal.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns><c>true</c> if the matrices are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Matrix3X3 left, Matrix3X3 right) => !object.Equals(left, right);

    /// <summary>Creates a new <see cref="Matrix3X3"/> from a <see cref="Matrix3D"/>.</summary>
    /// <param name="matrix3D">The source matrix.</param>
    /// <returns>A new <see cref="Matrix3X3"/> instance.</returns>
    public static Matrix3X3 FromMatrix3D([NotNull] Matrix3D matrix3D) => new(matrix3D);

    /// <summary>Creates a new <see cref="Matrix3X3"/> from a <see cref="Matrix4X4"/>.</summary>
    /// <param name="matrix4X4">The source matrix.</param>
    /// <returns>A new <see cref="Matrix3X3"/> instance.</returns>
    public static Matrix3X3 FromMatrix4X4([NotNull] Matrix4X4 matrix4X4) => new(matrix4X4);

    /// <summary>Adds two matrices together.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The sum of the two matrices.</returns>
    public static Matrix3X3 Add([NotNull] Matrix3X3 left, [NotNull] Matrix3X3 right) =>
        new(left.Row[0] + right.Row[0], left.Row[1] + right.Row[1], left.Row[2] + right.Row[2]);

    /// <summary>Subtracts one matrix from another.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The difference of the two matrices.</returns>
    public static Matrix3X3 Subtract([NotNull] Matrix3X3 left, [NotNull] Matrix3X3 right) =>
        new(left.Row[0] - right.Row[0], left.Row[1] - right.Row[1], left.Row[2] - right.Row[2]);

    /// <summary>Multiplies a matrix by a scalar value.</summary>
    /// <param name="matrix">The matrix to multiply.</param>
    /// <param name="scalar">The scalar value.</param>
    /// <returns>The scaled matrix.</returns>
    public static Matrix3X3 Multiply([NotNull] Matrix3X3 matrix, float scalar) =>
        new(matrix.Row[0] * scalar, matrix.Row[1] * scalar, matrix.Row[2] * scalar);

    /// <summary>Multiplies two matrices together.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The product of the two matrices.</returns>
    public static Matrix3X3 Multiply([NotNull] Matrix3X3 left, [NotNull] Matrix3X3 right)
    {
        return new Matrix3X3(
            new Vector3(DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2)),
            new Vector3(DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2)),
            new Vector3(DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2))
        );

        float DefineRowColumn(int row, int column) =>
            (left[row][0] * right[0][column]) + (left[row][1] * right[1][column]) + (left[row][2] * right[2][column]);
    }

    /// <summary>Multiplies a matrix by a vector.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="vector">The vector.</param>
    /// <returns>The transformed vector.</returns>
    public static Vector3 Multiply([NotNull] Matrix3X3 matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector[0]) + (matrix[0][1] * vector[1]) + (matrix[0][2] * vector[2]),
            (matrix[1][0] * vector[0]) + (matrix[1][1] * vector[1]) + (matrix[1][2] * vector[2]),
            (matrix[2][0] * vector[0]) + (matrix[2][1] * vector[1]) + (matrix[2][2] * vector[2])
        );

    /// <summary>Multiplies a 3x3 matrix by a 3D matrix.</summary>
    /// <param name="matrix3X3">The 3x3 matrix.</param>
    /// <param name="matrix3D">The 3D matrix.</param>
    /// <returns>The product matrix.</returns>
    public static Matrix3X3 Multiply([NotNull] Matrix3X3 matrix3X3, [NotNull] Matrix3D matrix3D)
    {
        return new Matrix3X3(
            new Vector3(DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2)),
            new Vector3(DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2)),
            new Vector3(DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2))
        );

        float DefineRowColumn(int row, int column) =>
            (matrix3X3[row][0] * matrix3D[0][column])
            + (matrix3X3[row][1] * matrix3D[1][column])
            + (matrix3X3[row][2] * matrix3D[2][column]);
    }

    /// <summary>Multiplies a 3D matrix by a 3x3 matrix.</summary>
    /// <param name="matrix3D">The 3D matrix.</param>
    /// <param name="matrix3X3">The 3x3 matrix.</param>
    /// <returns>The product matrix.</returns>
    public static Matrix3X3 Multiply([NotNull] Matrix3D matrix3D, [NotNull] Matrix3X3 matrix3X3)
    {
        return new Matrix3X3(
            new Vector3(DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2)),
            new Vector3(DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2)),
            new Vector3(DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2))
        );

        float DefineRowColumn(int row, int column) =>
            (matrix3D[0][row] * matrix3X3[0][column])
            + (matrix3D[1][row] * matrix3X3[1][column])
            + (matrix3D[2][row] * matrix3X3[2][column]);
    }

    /// <summary>Divides a matrix by a scalar value.</summary>
    /// <param name="matrix">The matrix to divide.</param>
    /// <param name="scalar">The scalar divisor.</param>
    /// <returns>The divided matrix.</returns>
    public static Matrix3X3 Divide([NotNull] Matrix3X3 matrix, float scalar) =>
        new(matrix.Row[0] / scalar, matrix.Row[1] / scalar, matrix.Row[2] / scalar);

    /// <summary>Negates all elements of a matrix.</summary>
    /// <param name="matrix">The matrix to negate.</param>
    /// <returns>The negated matrix.</returns>
    public static Matrix3X3 Negate([NotNull] Matrix3X3 matrix) => new(-matrix.Row[0], -matrix.Row[1], -matrix.Row[2]);

    /// <summary>Creates a rotation matrix around the X-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <returns>The X-axis rotation matrix.</returns>
    public static Matrix3X3 CreateXRotationMatrix3(float sin, float cos) =>
        new((1, 0, 0), (0, cos, -sin), (0, sin, cos));

    /// <summary>Creates a rotation matrix around the X-axis.</summary>
    /// <param name="rad">The rotation angle in radians.</param>
    /// <returns>The X-axis rotation matrix.</returns>
    public static Matrix3X3 CreateXRotationMatrix3(float rad) => CreateXRotationMatrix3(float.Sin(rad), float.Cos(rad));

    /// <summary>Creates a rotation matrix around the Y-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <returns>The Y-axis rotation matrix.</returns>
    public static Matrix3X3 CreateYRotationMatrix3(float sin, float cos) =>
        new((cos, 0, sin), (0, 1, 0), (-sin, 0, cos));

    /// <summary>Creates a rotation matrix around the Y-axis.</summary>
    /// <param name="rad">The rotation angle in radians.</param>
    /// <returns>The Y-axis rotation matrix.</returns>
    public static Matrix3X3 CreateYRotationMatrix3(float rad) => CreateYRotationMatrix3(float.Sin(rad), float.Cos(rad));

    /// <summary>Creates a rotation matrix around the Z-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <returns>The Z-axis rotation matrix.</returns>
    public static Matrix3X3 CreateZRotationMatrix3(float sin, float cos) =>
        new((cos, -sin, 0), (sin, cos, 0), (0, 0, 1));

    /// <summary>Creates a rotation matrix around the Z-axis.</summary>
    /// <param name="rad">The rotation angle in radians.</param>
    /// <returns>The Z-axis rotation matrix.</returns>
    public static Matrix3X3 CreateZRotationMatrix3(float rad) => CreateZRotationMatrix3(float.Sin(rad), float.Cos(rad));

    /// <summary>Rotates a vector using the specified matrix.</summary>
    /// <param name="matrix">The rotation matrix.</param>
    /// <param name="vector">The vector to rotate.</param>
    /// <returns>The rotated vector.</returns>
    public static Vector3 RotateVector([NotNull] Matrix3X3 matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector.X) + (matrix[0][1] * vector.Y) + (matrix[0][2] * vector.Z),
            (matrix[1][0] * vector.X) + (matrix[1][1] * vector.Y) + (matrix[1][2] * vector.Z),
            (matrix[2][0] * vector.X) + (matrix[2][1] * vector.Y) + (matrix[2][2] * vector.Z)
        );

    /// <summary>Rotates a vector using the transpose of the specified matrix.</summary>
    /// <param name="matrix">The rotation matrix.</param>
    /// <param name="vector">The vector to rotate.</param>
    /// <returns>The rotated vector using the transposed matrix.</returns>
    public static Vector3 TransposeRotateVector([NotNull] Matrix3X3 matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector.X) + (matrix[1][0] * vector.Y) + (matrix[2][0] * vector.Z),
            (matrix[0][1] * vector.X) + (matrix[1][1] * vector.Y) + (matrix[2][1] * vector.Z),
            (matrix[0][2] * vector.X) + (matrix[1][2] * vector.Y) + (matrix[2][2] * vector.Z)
        );

    /// <summary>Sets the matrix values using three row vectors.</summary>
    /// <param name="r0">The first row vector.</param>
    /// <param name="r1">The second row vector.</param>
    /// <param name="r2">The third row vector.</param>
    public void Set([NotNull] Vector3 r0, [NotNull] Vector3 r1, [NotNull] Vector3 r2) =>
        (Row[0], Row[1], Row[2]) = (r0, r1, r2);

    /// <summary>Sets the matrix values using three row tuples.</summary>
    /// <param name="r0">The first row as a tuple.</param>
    /// <param name="r1">The second row as a tuple.</param>
    /// <param name="r2">The third row as a tuple.</param>
    public void Set([NotNull] Matrix3X3Row r0, [NotNull] Matrix3X3Row r1, [NotNull] Matrix3X3Row r2) =>
        Set(
            new Vector3(r0.Member1, r0.Member2, r0.Member3),
            new Vector3(r1.Member1, r1.Member2, r1.Member3),
            new Vector3(r2.Member1, r2.Member2, r2.Member3)
        );

    /// <summary>Sets the matrix as a rotation matrix from an axis and angle.</summary>
    /// <param name="axis">The rotation axis.</param>
    /// <param name="angle">The rotation angle in radians.</param>
    public void Set(Vector3 axis, float angle) => Set(axis, float.Sin(angle), float.Cos(angle));

    /// <summary>Sets the matrix as a rotation matrix from an axis and precomputed sine and cosine.</summary>
    /// <param name="axis">The rotation axis (must be normalized).</param>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the axis is not normalized.</exception>
    public void Set([NotNull] Vector3 axis, float sin, float cos)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(float.Abs(axis.Length2 - 1F), float.Epsilon);

        Set(
            new Vector3(
                float.Pow(axis[0], 2) + (cos * (1F - float.Pow(axis[0], 2))),
                (axis[0] * axis[1] * (1F - cos)) - (axis[2] * sin),
                (axis[2] * axis[0] * (1F - cos)) + (axis[1] * sin)
            ),
            new Vector3(
                (axis[0] * axis[1] * (1F - cos)) + (axis[2] * sin),
                float.Pow(axis[1], 2) + (cos * (1F - float.Pow(axis[1], 2))),
                (axis[1] * axis[2] * (1F - cos)) - (axis[0] * sin)
            ),
            new Vector3(
                (axis[2] * axis[0] * (1F - cos)) - (axis[1] * sin),
                (axis[1] * axis[2] * (1F - cos)) + (axis[0] * sin),
                float.Pow(axis[2], 2) + (cos * (1F - float.Pow(axis[2], 2)))
            )
        );
    }

    /// <summary>Sets the matrix values from a 3D matrix.</summary>
    /// <param name="matrix3D">The 3D matrix to copy from.</param>
    public void Set([NotNull] Matrix3D matrix3D) =>
        Set(
            new Vector3(matrix3D[0][0], matrix3D[0][1], matrix3D[0][2]),
            new Vector3(matrix3D[1][0], matrix3D[1][1], matrix3D[1][2]),
            new Vector3(matrix3D[2][0], matrix3D[2][1], matrix3D[2][2])
        );

    /// <summary>Sets the matrix values from a 4x4 matrix.</summary>
    /// <param name="matrix4X4">The 4x4 matrix to copy from.</param>
    public void Set([NotNull] Matrix4X4 matrix4X4) =>
        Set(
            new Vector3(matrix4X4[0][0], matrix4X4[0][1], matrix4X4[0][2]),
            new Vector3(matrix4X4[1][0], matrix4X4[1][1], matrix4X4[1][2]),
            new Vector3(matrix4X4[2][0], matrix4X4[2][1], matrix4X4[2][2])
        );

    /// <summary>Sets the matrix values from a quaternion.</summary>
    /// <param name="quaternion">The quaternion to convert.</param>
    public void Set([NotNull] Quaternion quaternion) =>
        Set(
            new Vector3(
                1F - (2F * ((quaternion[1] * quaternion[1]) + (quaternion[2] * quaternion[2]))),
                2F * ((quaternion[0] * quaternion[1]) - (quaternion[2] * quaternion[3])),
                2F * ((quaternion[2] * quaternion[0]) + (quaternion[1] * quaternion[3]))
            ),
            new Vector3(
                2F * ((quaternion[0] * quaternion[1]) + (quaternion[2] * quaternion[3])),
                1F - (2.0f * ((quaternion[2] * quaternion[2]) + (quaternion[0] * quaternion[0]))),
                2F * ((quaternion[1] * quaternion[2]) - (quaternion[0] * quaternion[3]))
            ),
            new Vector3(
                2F * ((quaternion[2] * quaternion[0]) - (quaternion[1] * quaternion[3])),
                2F * ((quaternion[1] * quaternion[2]) + (quaternion[0] * quaternion[3])),
                1F - (2F * ((quaternion[1] * quaternion[1]) + (quaternion[0] * quaternion[0])))
            )
        );

    /// <summary>Sets the matrix to the identity matrix.</summary>
    public void MakeIdentity() => Set(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1));

    /// <summary>Applies a rotation around the X-axis to the matrix.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    public void RotateX(float theta) => RotateX(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a rotation around the X-axis to the matrix using precomputed sine and cosine.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public void RotateX(float sin, float cos)
    {
        var temp1 = Row[0][1];
        var temp2 = Row[0][2];
        Row[0][1] = (cos * temp1) + (sin * temp2);
        Row[0][2] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[1][1];
        temp2 = Row[1][2];
        Row[1][1] = (cos * temp1) + (sin * temp2);
        Row[1][2] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[2][1];
        temp2 = Row[2][2];
        Row[2][1] = (cos * temp1) + (sin * temp2);
        Row[2][2] = (-sin * temp1) + (cos * temp2);
    }

    /// <summary>Applies a rotation around the Y-axis to the matrix.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    public void RotateY(float theta) => RotateY(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a rotation around the Y-axis to the matrix using precomputed sine and cosine.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public void RotateY(float sin, float cos)
    {
        var temp1 = Row[0][0];
        var temp2 = Row[0][2];
        Row[0][0] = (cos * temp1) - (sin * temp2);
        Row[0][2] = (sin * temp1) + (cos * temp2);

        temp1 = Row[1][0];
        temp2 = Row[1][2];
        Row[1][0] = (cos * temp1) - (sin * temp2);
        Row[1][2] = (sin * temp1) + (cos * temp2);

        temp1 = Row[2][0];
        temp2 = Row[2][2];
        Row[2][0] = (cos * temp1) - (sin * temp2);
        Row[2][2] = (sin * temp1) + (cos * temp2);
    }

    /// <summary>Applies a rotation around the Z-axis to the matrix.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    public void RotateZ(float theta) => RotateZ(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a rotation around the Z-axis to the matrix using precomputed sine and cosine.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public void RotateZ(float sin, float cos)
    {
        var temp1 = Row[0][0];
        var temp2 = Row[0][1];
        Row[0][0] = (cos * temp1) + (sin * temp2);
        Row[0][1] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[1][0];
        temp2 = Row[1][1];
        Row[1][0] = (cos * temp1) + (sin * temp2);
        Row[1][1] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[2][0];
        temp2 = Row[2][1];
        Row[2][0] = (cos * temp1) + (sin * temp2);
        Row[2][1] = (-sin * temp1) + (cos * temp2);
    }

    /// <summary>Rotates an axis-aligned bounding box extent vector using the absolute values of the matrix elements.</summary>
    /// <param name="extent">The extent vector of the axis-aligned bounding box.</param>
    /// <returns>The transformed extent vector.</returns>
    /// <remarks>This method is useful for computing the extent of an axis-aligned bounding box after applying an arbitrary rotation.</remarks>
    public Vector3 RotateAxisAlignedBoxExtent([NotNull] Vector3 extent)
    {
        Vector3 result = new();
        for (var i = 0; i < 3; i++)
        {
            result[i] = 0F;
            for (var j = 0; j < 3; j++)
            {
                result[i] += float.Abs(Row[i][j] * extent[j]);
            }
        }

        return result;
    }

    /// <summary>Re-orthogonalizes the matrix to ensure it represents a valid rotation matrix.</summary>
    /// <remarks>This method corrects numerical drift that can occur after multiple matrix operations by ensuring the basis vectors are orthonormal. If the matrix cannot be orthogonalized, it is set to the identity matrix.</remarks>
    public void ReOrthogonalize()
    {
        Vector3 x = new(Row[0][0], Row[0][1], Row[0][2]);
        Vector3 y = new(Row[1][0], Row[1][1], Row[1][2]);

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

        Row[0][0] = x.X;
        Row[0][1] = x.Y;
        Row[0][2] = x.Z;

        Row[1][0] = y.X;
        Row[1][1] = y.Y;
        Row[1][2] = y.Z;

        Row[2][0] = z.X;
        Row[2][1] = z.Y;
        Row[2][2] = z.Z;
    }

    /// <summary>Determines whether the specified object is equal to the current matrix.</summary>
    /// <param name="obj">The object to compare with the current matrix.</param>
    /// <returns><c>true</c> if the specified object is equal to the current matrix; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is Matrix3X3 other && Equals(this, other);

    /// <summary>Determines whether two matrices are equal.</summary>
    /// <param name="x">The first matrix to compare.</param>
    /// <param name="y">The second matrix to compare.</param>
    /// <returns><c>true</c> if the matrices are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(Matrix3X3? x, Matrix3X3? y) =>
        ReferenceEquals(x, y)
        || (
            x is not null
            && y is not null
            && x.GetType() == y.GetType()
            && !x.Row.Where((row, index) => row != y.Row[index]).Any()
        );

    /// <summary>Returns a hash code for the current matrix.</summary>
    /// <returns>A hash code for the current matrix.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns a hash code for the specified matrix.</summary>
    /// <param name="obj">The matrix to get the hash code for.</param>
    /// <returns>A hash code for the specified matrix.</returns>
    public int GetHashCode([NotNull] Matrix3X3 obj) =>
        HashCode.Combine(obj.Row[0].GetHashCode(), obj.Row[1].GetHashCode(), obj.Row[2].GetHashCode());

    /// <summary>Returns a string representation of the matrix.</summary>
    /// <returns>A string representation of the matrix in the format "({Row0}, {Row1}, {Row2})".</returns>
    public override string ToString() => $"({Row[0]}, {Row[1]}, {Row[2]})";

    /// <summary>Returns a string representation of the matrix using the specified format provider.</summary>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the matrix formatted according to the specified format provider.</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({Row[0].ToString(formatProvider)}, {Row[1].ToString(formatProvider)}, {Row[2].ToString(formatProvider)})";

    /// <summary>Returns a string representation of the matrix using the specified format and format provider.</summary>
    /// <param name="format">A numeric format string.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the matrix formatted according to the specified format and format provider.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({Row[0].ToString(format, formatProvider)}, {Row[1].ToString(format, formatProvider)}, {Row[2].ToString(format, formatProvider)})";
}
