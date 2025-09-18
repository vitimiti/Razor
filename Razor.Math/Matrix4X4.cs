// -----------------------------------------------------------------------
// <copyright file="Matrix4X4.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
using Matrix4X4Row = (float Member1, float Member2, float Member3, float Member4);
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly

namespace Razor.Math;

/// <summary>Represents a 4x4 transformation matrix for 3D graphics operations including rotation, translation, scaling, and perspective transformations.</summary>
/// <remarks>This matrix uses homogeneous coordinates with a 4x4 format suitable for 3D graphics pipelines and mathematical operations.</remarks>
public class Matrix4X4 : IEqualityComparer<Matrix4X4>
{
    /// <summary>Initializes a new instance of the <see cref="Matrix4X4"/> class with default zero values.</summary>
    public Matrix4X4() { }

    /// <summary>Initializes a new instance of the <see cref="Matrix4X4"/> class by copying another matrix.</summary>
    /// <param name="other">The matrix to copy from.</param>
    public Matrix4X4([NotNull] Matrix4X4 other) => Row = other.Row;

    /// <summary>Initializes a new instance of the <see cref="Matrix4X4"/> class using four row vectors.</summary>
    /// <param name="r0">The first row vector.</param>
    /// <param name="r1">The second row vector.</param>
    /// <param name="r2">The third row vector.</param>
    /// <param name="r3">The fourth row vector.</param>
    public Matrix4X4(Vector4 r0, Vector4 r1, Vector4 r2, Vector4 r3) => Initialize(r0, r1, r2, r3);

    /// <summary>Initializes a new instance of the <see cref="Matrix4X4"/> class using four row tuples.</summary>
    /// <param name="r1">The first row as a tuple of four float values.</param>
    /// <param name="r2">The second row as a tuple of four float values.</param>
    /// <param name="r3">The third row as a tuple of four float values.</param>
    /// <param name="r4">The fourth row as a tuple of four float values.</param>
    public Matrix4X4(Matrix4X4Row r1, Matrix4X4Row r2, Matrix4X4Row r3, Matrix4X4Row r4) => Initialize(r1, r2, r3, r4);

    /// <summary>Gets the 4x4 identity matrix with ones on the diagonal and zeros elsewhere.</summary>
    /// <value>A matrix representing no transformation (identity transformation).</value>
    public static Matrix4X4 Identity => new((1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 1, 0), (0, 0, 0, 1));

    /// <summary>Gets the transpose of the matrix where rows and columns are swapped.</summary>
    /// <value>A new matrix that is the transpose of this matrix.</value>
    public Matrix4X4 Transpose =>
        new(
            new Vector4(Row[0][0], Row[1][0], Row[2][0], Row[3][0]),
            new Vector4(Row[0][1], Row[1][1], Row[2][1], Row[3][1]),
            new Vector4(Row[0][2], Row[1][2], Row[2][2], Row[3][2]),
            new Vector4(Row[0][3], Row[1][3], Row[2][3], Row[3][3])
        );

    /// <summary>Gets the inverse of the matrix using Gaussian elimination.</summary>
    /// <value>The inverse matrix if it exists.</value>
    /// <exception cref="NotSupportedException">Thrown in DEBUG builds as the current implementation is known to be incorrect.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the matrix is singular and cannot be inverted.</exception>
    /// <remarks>Warning: The current implementation has known issues and throws NotSupportedException in DEBUG builds.</remarks>
    [SuppressMessage(
        "ReSharper",
        "HeuristicUnreachableCode",
        Justification = "It is only unreachable when DEBUG is defined."
    )]
    public Matrix4X4 Inverse
    {
        get
        {
            // BUG: This is how it is originally implemented, may need to fix this
#if DEBUG
            throw new NotSupportedException("The inverse implementation does NOT work, re-implement!");
#endif
#if DEBUG
#pragma warning disable CS0162 // Unreachable code detected
#endif
            Matrix4X4 a = new(this);
            Matrix4X4 b = Identity;
            for (var i = 0; i < 4; i++)
            {
                var j = i;
                for (var k = i + 1; k < 4; k++)
                {
                    if (float.Abs(a[k][i]) > float.Abs(a[j][i]))
                    {
                        j = k;
                    }
                }

                (a.Row[j], a.Row[i]) = (a.Row[i], a.Row[j]);
                (b.Row[j], a.Row[i]) = (a.Row[i], a.Row[j]);

                if (float.Abs(a[i][i]) < float.Epsilon)
                {
                    throw new InvalidOperationException("Matrix is singular, cannot invert.");
                }

                b.Row[i] /= a.Row[i][i];
                a.Row[i] /= a.Row[i][i];

                for (var l = 0; l < 4; l++)
                {
                    if (l == i)
                    {
                        continue;
                    }

                    b.Row[l] -= a[l][i] * b.Row[i];
                    a.Row[l] -= a[l][i] * a.Row[i];
                }
            }
#if DEBUG
#pragma warning restore CS0162 // Unreachable code detected
#endif
        }
    }

    /// <summary>Gets the collection of row vectors that define the matrix structure.</summary>
    /// <value>A collection containing exactly four Vector4 instances representing the matrix rows.</value>
    protected Collection<Vector4> Row { get; } = [new(), new(), new(), new()];

    /// <summary>Gets or sets the row vector at the specified index.</summary>
    /// <param name="index">The zero-based row index (0-3).</param>
    /// <value>The Vector4 representing the specified matrix row.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the valid range (0-3).</exception>
    public Vector4 this[int index]
    {
        get => Row[index];
        set => Row[index] = value;
    }

    /// <summary>Adds two matrices together element-wise.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The sum of the two matrices.</returns>
    public static Matrix4X4 operator +(Matrix4X4 left, Matrix4X4 right) => Add(left, right);

    /// <summary>Subtracts one matrix from another element-wise.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The difference of the two matrices.</returns>
    public static Matrix4X4 operator -(Matrix4X4 left, Matrix4X4 right) => Subtract(left, right);

    /// <summary>Multiplies a matrix by a scalar value.</summary>
    /// <param name="matrix">The matrix to multiply.</param>
    /// <param name="scalar">The scalar value.</param>
    /// <returns>The scaled matrix.</returns>
    public static Matrix4X4 operator *(Matrix4X4 matrix, float scalar) => Multiply(matrix, scalar);

    /// <summary>Multiplies a scalar value by a matrix.</summary>
    /// <param name="scalar">The scalar value.</param>
    /// <param name="matrix">The matrix to multiply.</param>
    /// <returns>The scaled matrix.</returns>
    public static Matrix4X4 operator *(float scalar, Matrix4X4 matrix) => Multiply(matrix, scalar);

    /// <summary>Multiplies two matrices together using standard matrix multiplication.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The product of the two matrices.</returns>
    public static Matrix4X4 operator *(Matrix4X4 left, Matrix4X4 right) => Multiply(left, right);

    /// <summary>Multiplies a 4x4 matrix by a 3D matrix.</summary>
    /// <param name="matrix4X4">The 4x4 matrix.</param>
    /// <param name="matrix3D">The 3D matrix.</param>
    /// <returns>The product matrix.</returns>
    public static Matrix4X4 operator *(Matrix4X4 matrix4X4, Matrix3D matrix3D) => Multiply(matrix4X4, matrix3D);

    /// <summary>Multiplies a 3D matrix by a 4x4 matrix.</summary>
    /// <param name="matrix3D">The 3D matrix.</param>
    /// <param name="matrix4X4">The 4x4 matrix.</param>
    /// <returns>The product matrix.</returns>
    public static Matrix4X4 operator *(Matrix3D matrix3D, Matrix4X4 matrix4X4) => Multiply(matrix3D, matrix4X4);

    /// <summary>Multiplies a matrix by a 3D vector, treating the vector as a 4D vector with W=1.</summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <param name="vector">The 3D vector to transform.</param>
    /// <returns>The transformed 4D vector.</returns>
    public static Vector4 operator *(Matrix4X4 matrix, Vector3 vector) => Multiply(matrix, vector);

    /// <summary>Multiplies a matrix by a 4D vector.</summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <param name="vector">The 4D vector to transform.</param>
    /// <returns>The transformed 4D vector.</returns>
    public static Vector4 operator *(Matrix4X4 matrix, Vector4 vector) => Multiply(matrix, vector);

    /// <summary>Divides a matrix by a scalar value.</summary>
    /// <param name="matrix">The matrix to divide.</param>
    /// <param name="scalar">The scalar divisor.</param>
    /// <returns>The divided matrix.</returns>
    public static Matrix4X4 operator /(Matrix4X4 matrix, float scalar) => Divide(matrix, scalar);

    /// <summary>Determines whether two matrices are equal using reference and component comparison.</summary>
    /// <param name="left">The first matrix to compare.</param>
    /// <param name="right">The second matrix to compare.</param>
    /// <returns><c>true</c> if the matrices are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Matrix4X4 left, Matrix4X4 right) => object.Equals(left, right);

    /// <summary>Determines whether two matrices are not equal using reference and component comparison.</summary>
    /// <param name="left">The first matrix to compare.</param>
    /// <param name="right">The second matrix to compare.</param>
    /// <returns><c>true</c> if the matrices are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Matrix4X4 left, Matrix4X4 right) => !object.Equals(left, right);

    /// <summary>Adds two matrices together element-wise.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The sum of the two matrices.</returns>
    public static Matrix4X4 Add([NotNull] Matrix4X4 left, [NotNull] Matrix4X4 right) =>
        new(
            left.Row[0] + right.Row[0],
            left.Row[1] + right.Row[1],
            left.Row[2] + right.Row[2],
            left.Row[3] + right.Row[3]
        );

    /// <summary>Subtracts one matrix from another element-wise.</summary>
    /// <param name="left">The first matrix.</param>
    /// <param name="right">The second matrix.</param>
    /// <returns>The difference of the two matrices.</returns>
    public static Matrix4X4 Subtract([NotNull] Matrix4X4 left, [NotNull] Matrix4X4 right) =>
        new(
            left.Row[0] - right.Row[0],
            left.Row[1] - right.Row[1],
            left.Row[2] - right.Row[2],
            left.Row[3] - right.Row[3]
        );

    /// <summary>Multiplies a matrix by a scalar value.</summary>
    /// <param name="matrix">The matrix to multiply.</param>
    /// <param name="scalar">The scalar value.</param>
    /// <returns>The scaled matrix with all elements multiplied by the scalar.</returns>
    public static Matrix4X4 Multiply([NotNull] Matrix4X4 matrix, float scalar) =>
        new(matrix.Row[0] * scalar, matrix.Row[1] * scalar, matrix.Row[2] * scalar, matrix.Row[3] * scalar);

    /// <summary>Multiplies two matrices together using standard 4x4 matrix multiplication.</summary>
    /// <param name="left">The first matrix (left operand).</param>
    /// <param name="right">The second matrix (right operand).</param>
    /// <returns>The resulting matrix from the multiplication operation.</returns>
    /// <remarks>Each element of the result is computed as the dot product of the corresponding row and column.</remarks>
    public static Matrix4X4 Multiply([NotNull] Matrix4X4 left, [NotNull] Matrix4X4 right)
    {
        return new Matrix4X4(
            new Vector4(DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2), DefineRowColumn(0, 3)),
            new Vector4(DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2), DefineRowColumn(1, 3)),
            new Vector4(DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2), DefineRowColumn(2, 3)),
            new Vector4(DefineRowColumn(3, 0), DefineRowColumn(3, 1), DefineRowColumn(3, 2), DefineRowColumn(3, 3))
        );

        float DefineRowColumn(int row, int column) =>
            (left[row][0] * right[0][column])
            + (left[row][1] * right[1][column])
            + (left[row][2] * right[2][column])
            + (left[row][3] * right[3][column]);
    }

    /// <summary>Multiplies a 4x4 matrix by a 3D matrix, treating the 3D matrix as having an implicit fourth row of (0,0,0,1).</summary>
    /// <param name="matrix4X4">The 4x4 matrix.</param>
    /// <param name="matrix3D">The 3D matrix to multiply with.</param>
    /// <returns>The resulting 4x4 matrix from the multiplication.</returns>
    /// <remarks>The 3D matrix is treated as a 4x4 matrix with the fourth row being (0,0,0,1).</remarks>
    public static Matrix4X4 Multiply([NotNull] Matrix4X4 matrix4X4, [NotNull] Matrix3D matrix3D)
    {
        return new Matrix4X4(
            new Vector4(DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2), DefineRowColumn4(0, 3)),
            new Vector4(DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2), DefineRowColumn4(1, 3)),
            new Vector4(DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2), DefineRowColumn4(2, 3)),
            new Vector4(DefineRowColumn(3, 0), DefineRowColumn(3, 1), DefineRowColumn(3, 2), DefineRowColumn4(3, 3))
        );

        float DefineRowColumn(int row, int column) =>
            (matrix4X4[row][0] * matrix3D[0][column])
            + (matrix4X4[row][1] * matrix3D[1][column])
            + (matrix4X4[row][2] * matrix3D[2][column]);

        float DefineRowColumn4(int row, int column) =>
            (matrix4X4[row][0] * matrix3D[0][column])
            + (matrix4X4[row][1] * matrix3D[1][column])
            + (matrix4X4[row][2] * matrix3D[2][column])
            + matrix4X4[row][3];
    }

    /// <summary>Multiplies a 3D matrix by a 4x4 matrix, treating the 3D matrix as having an implicit fourth row of (0,0,0,1).</summary>
    /// <param name="matrix3D">The 3D matrix.</param>
    /// <param name="matrix4X4">The 4x4 matrix to multiply with.</param>
    /// <returns>The resulting 4x4 matrix from the multiplication.</returns>
    /// <remarks>The 3D matrix is treated as a 4x4 matrix with the fourth row being (0,0,0,1).</remarks>
    public static Matrix4X4 Multiply([NotNull] Matrix3D matrix3D, [NotNull] Matrix4X4 matrix4X4)
    {
        return new Matrix4X4(
            new Vector4(DefineRowColumn(0, 0), DefineRowColumn(0, 1), DefineRowColumn(0, 2), DefineRowColumn(0, 3)),
            new Vector4(DefineRowColumn(1, 0), DefineRowColumn(1, 1), DefineRowColumn(1, 2), DefineRowColumn(1, 3)),
            new Vector4(DefineRowColumn(2, 0), DefineRowColumn(2, 1), DefineRowColumn(2, 2), DefineRowColumn(2, 3)),
            new Vector4(DefineRowColumn(3, 0), DefineRowColumn(3, 1), DefineRowColumn(3, 2), DefineRowColumn(3, 3))
        );

        float DefineRowColumn(int row, int column) =>
            (matrix3D[row][0] * matrix4X4[0][column])
            + (matrix3D[row][1] * matrix4X4[1][column])
            + (matrix3D[row][2] * matrix4X4[2][column])
            + (matrix3D[row][3] * matrix4X4[3][column]);
    }

    /// <summary>Transforms a 3D vector by the matrix, treating it as a 4D vector with W=1.</summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <param name="vector">The 3D vector to transform.</param>
    /// <returns>The transformed 4D vector result.</returns>
    /// <remarks>This method treats the 3D vector as having a W component of 1, making it suitable for transforming points in homogeneous coordinates.</remarks>
    public static Vector4 Multiply([NotNull] Matrix4X4 matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector[0]) + (matrix[0][1] * vector[1]) + (matrix[0][2] * vector[2]) + (matrix[0][3] * 1F),
            (matrix[1][0] * vector[0]) + (matrix[1][1] * vector[1]) + (matrix[1][2] * vector[2]) + (matrix[1][3] * 1F),
            (matrix[2][0] * vector[0]) + (matrix[2][1] * vector[1]) + (matrix[2][2] * vector[2]) + (matrix[2][3] * 1F),
            (matrix[3][0] * vector[0]) + (matrix[3][1] * vector[1]) + (matrix[3][2] * vector[2]) + (matrix[3][3] * 1F)
        );

    /// <summary>Transforms a 4D vector by the matrix using standard matrix-vector multiplication.</summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <param name="vector">The 4D vector to transform.</param>
    /// <returns>The transformed 4D vector result.</returns>
    /// <remarks>This performs the standard matrix-vector multiplication where each component of the result vector is the dot product of the corresponding matrix row with the input vector.</remarks>
    public static Vector4 Multiply([NotNull] Matrix4X4 matrix, [NotNull] Vector4 vector)
    {
        var x =
            (matrix[0][0] * vector[0])
            + (matrix[0][1] * vector[1])
            + (matrix[0][2] * vector[2])
            + (matrix[0][3] * vector[3]);

        var y =
            (matrix[1][0] * vector[0])
            + (matrix[1][1] * vector[1])
            + (matrix[1][2] * vector[2])
            + (matrix[1][3] * vector[3]);

        var z =
            (matrix[2][0] * vector[0])
            + (matrix[2][1] * vector[1])
            + (matrix[2][2] * vector[2])
            + (matrix[2][3] * vector[3]);

        var w =
            (matrix[3][0] * vector[0])
            + (matrix[3][1] * vector[1])
            + (matrix[3][2] * vector[2])
            + (matrix[3][3] * vector[3]);

        return new Vector4(x, y, z, w);
    }

    /// <summary>Divides a matrix by a scalar value by multiplying by the reciprocal.</summary>
    /// <param name="matrix">The matrix to divide.</param>
    /// <param name="scalar">The scalar divisor.</param>
    /// <returns>The divided matrix with all elements divided by the scalar.</returns>
    /// <remarks>Division is performed by computing the reciprocal and multiplying for efficiency.</remarks>
    public static Matrix4X4 Divide([NotNull] Matrix4X4 matrix, float scalar)
    {
        var oScalar = 1F / scalar;
        return new Matrix4X4(
            matrix.Row[0] * oScalar,
            matrix.Row[1] * oScalar,
            matrix.Row[2] * oScalar,
            matrix.Row[3] * oScalar
        );
    }

    /// <summary>Transforms a 3D vector by the matrix and returns the result as a 3D vector, discarding the W component.</summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <param name="vector">The 3D vector to transform.</param>
    /// <returns>The transformed vector as a 3D vector with W component discarded.</returns>
    /// <remarks>This method performs perspective divide implicitly by discarding the W component and is suitable for transforming points that need to be projected back to 3D space.</remarks>
    public static Vector3 TransformVectorToVector3([NotNull] Matrix4X4 matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector.X) + (matrix[0][1] * vector.Y) + (matrix[0][2] * vector.Z) + matrix[0][3],
            (matrix[1][0] * vector.X) + (matrix[1][1] * vector.Y) + (matrix[1][2] * vector.Z) + matrix[1][3],
            (matrix[2][0] * vector.X) + (matrix[2][1] * vector.Y) + (matrix[2][2] * vector.Z) + matrix[2][3]
        );

    /// <summary>Transforms a 3D vector by the matrix, treating it as a 4D vector with W=1, and returns a 4D vector.</summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <param name="vector">The 3D vector to transform.</param>
    /// <returns>The transformed 4D vector with W component set to 1.</returns>
    /// <remarks>This method is suitable for transforming points in homogeneous coordinates where the W component should remain 1.</remarks>
    public static Vector4 TransformVectorToVector4([NotNull] Matrix4X4 matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector.X) + (matrix[0][1] * vector.Y) + (matrix[0][2] * vector.Z) + matrix[0][3],
            (matrix[1][0] * vector.X) + (matrix[1][1] * vector.Y) + (matrix[1][2] * vector.Z) + matrix[1][3],
            (matrix[2][0] * vector.X) + (matrix[2][1] * vector.Y) + (matrix[2][2] * vector.Z) + matrix[2][3],
            1F
        );

    /// <summary>Transforms a 4D vector by the matrix using standard matrix-vector multiplication.</summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <param name="vector">The 4D vector to transform.</param>
    /// <returns>The transformed 4D vector result.</returns>
    /// <remarks>This method preserves all four components of the input vector and is suitable for general homogeneous coordinate transformations.</remarks>
    public static Vector4 TransformVectorToVector4([NotNull] Matrix4X4 matrix, [NotNull] Vector4 vector)
    {
        var x =
            (matrix[0][0] * vector.X)
            + (matrix[0][1] * vector.Y)
            + (matrix[0][2] * vector.Z)
            + (matrix[0][3] * vector.W);

        var y =
            (matrix[1][0] * vector.X)
            + (matrix[1][1] * vector.Y)
            + (matrix[1][2] * vector.Z)
            + (matrix[1][3] * vector.W);

        var z =
            (matrix[2][0] * vector.X)
            + (matrix[2][1] * vector.Y)
            + (matrix[2][2] * vector.Z)
            + (matrix[2][3] * vector.W);

        var w =
            (matrix[3][0] * vector.X)
            + (matrix[3][1] * vector.Y)
            + (matrix[3][2] * vector.Z)
            + (matrix[3][3] * vector.W);

        return new Vector4(x, y, z, w);
    }

    /// <summary>Resets the matrix to the 4x4 identity matrix with ones on the diagonal and zeros elsewhere.</summary>
    /// <remarks>After calling this method, the matrix represents no transformation (identity transformation).</remarks>
    public void MakeIdentity()
    {
        Row[0].Set(1, 0, 0, 0);
        Row[1].Set(0, 1, 0, 0);
        Row[2].Set(0, 0, 1, 0);
        Row[3].Set(0, 0, 0, 1);
    }

    /// <summary>Initializes the matrix from a 3D matrix, extending it to 4x4 by adding a fourth row of (0,0,0,1).</summary>
    /// <param name="matrix3D">The 3D matrix to copy from.</param>
    /// <remarks>The resulting matrix preserves the 3D transformation and adds homogeneous coordinate support.</remarks>
    public void Initialize([NotNull] Matrix3D matrix3D) =>
        (Row[0], Row[1], Row[2], Row[3]) = (matrix3D[0], matrix3D[1], matrix3D[2], new Vector4(0, 0, 0, 1));

    /// <summary>Initializes the matrix using four row vectors.</summary>
    /// <param name="r0">The first row vector.</param>
    /// <param name="r1">The second row vector.</param>
    /// <param name="r2">The third row vector.</param>
    /// <param name="r3">The fourth row vector.</param>
    public void Initialize([NotNull] Vector4 r0, [NotNull] Vector4 r1, [NotNull] Vector4 r2, [NotNull] Vector4 r3) =>
        (Row[0], Row[1], Row[2], Row[3]) = (r0, r1, r2, r3);

    /// <summary>Initializes the matrix using four row tuples.</summary>
    /// <param name="r1">The first row as a tuple of four float values.</param>
    /// <param name="r2">The second row as a tuple of four float values.</param>
    /// <param name="r3">The third row as a tuple of four float values.</param>
    /// <param name="r4">The fourth row as a tuple of four float values.</param>
    public void Initialize(Matrix4X4Row r1, Matrix4X4Row r2, Matrix4X4Row r3, Matrix4X4Row r4)
    {
        Row[0].Set(r1.Member1, r1.Member2, r1.Member3, r1.Member4);
        Row[1].Set(r2.Member1, r2.Member2, r2.Member3, r2.Member4);
        Row[2].Set(r3.Member1, r3.Member2, r3.Member3, r3.Member4);
        Row[3].Set(r4.Member1, r4.Member2, r4.Member3, r4.Member4);
    }

    /// <summary>Initializes the matrix as an orthogonal projection matrix.</summary>
    /// <param name="sides">A tuple containing the left, right, bottom, and top clipping planes.</param>
    /// <param name="depth">A tuple containing the near and far clipping planes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when near depth is less than 0 or far depth is less than near depth.</exception>
    /// <remarks>Creates an orthogonal projection matrix suitable for rendering without perspective distortion.</remarks>
    public void InitializeOrthogonal(
        (float Left, float Right, float Bottom, float Top) sides,
        (float Near, float Far) depth
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(depth.Near, 0F);
        ArgumentOutOfRangeException.ThrowIfLessThan(depth.Far, depth.Near);

        MakeIdentity();
        Row[0][0] = 2F / (sides.Right - sides.Left);
        Row[0][3] = -(sides.Right + sides.Left) / (sides.Right - sides.Left);
        Row[1][1] = 2F / (sides.Top - sides.Bottom);
        Row[1][3] = -(sides.Top + sides.Bottom) / (sides.Top - sides.Bottom);
        Row[2][2] = -2F / (depth.Far - depth.Near);
        Row[2][3] = -(depth.Far + depth.Near) / (depth.Far - depth.Near);
    }

    /// <summary>Initializes the matrix as a perspective projection matrix using field of view angles.</summary>
    /// <param name="fov">A tuple containing horizontal and vertical field of view angles in radians.</param>
    /// <param name="depth">A tuple containing the near and far clipping planes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when near depth is less than 0 or far depth is less than near depth.</exception>
    /// <remarks>Creates a perspective projection matrix with the specified field of view, commonly used for 3D rendering.</remarks>
    public void InitializePerspective((float Horizontal, float Vertical) fov, (float Near, float Far) depth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(depth.Near, 0F);
        ArgumentOutOfRangeException.ThrowIfLessThan(depth.Far, depth.Near);

        MakeIdentity();
        Row[0][0] = 1F / float.Tan(fov.Horizontal * .5F);
        Row[1][1] = 1F / float.Tan(fov.Vertical * .5F);
        Row[2][2] = -(depth.Far + depth.Near) / (depth.Far - depth.Near);
        Row[2][3] = -(2F * depth.Far * depth.Near) / (depth.Far - depth.Near);
        Row[3][2] = -1F;
        Row[3][3] = 0F;
    }

    /// <summary>Initializes the matrix as a perspective projection matrix using frustum bounds.</summary>
    /// <param name="sides">A tuple containing the left, right, bottom, and top frustum bounds at the near plane.</param>
    /// <param name="depth">A tuple containing the near and far clipping planes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when near or far depth is less than or equal to 0.</exception>
    /// <remarks>Creates a perspective projection matrix with explicit frustum bounds, allowing for off-center projections.</remarks>
    public void InitializePerspective(
        (float Left, float Right, float Bottom, float Top) sides,
        (float Near, float Far) depth
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(depth.Near, 0F);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(depth.Far, 0F);

        MakeIdentity();
        Row[0][0] = 2F * depth.Near / (sides.Right - sides.Left);
        Row[0][2] = (sides.Right + sides.Left) / (sides.Right - sides.Left);
        Row[1][1] = 2F * depth.Near / (sides.Top - sides.Bottom);
        Row[1][2] = (sides.Top + sides.Bottom) / (sides.Top - sides.Bottom);
        Row[2][2] = -(depth.Far + depth.Near) / (depth.Far - depth.Near);
        Row[2][3] = -(2F * depth.Far * depth.Near) / (depth.Far - depth.Near);
        Row[3][2] = -1F;
        Row[3][3] = 0F;
    }

    /// <summary>Determines whether the specified object is equal to the current matrix.</summary>
    /// <param name="obj">The object to compare with the current matrix.</param>
    /// <returns><c>true</c> if the specified object is a Matrix4X4 and is equal to the current matrix; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is Matrix4X4 matrix && Equals(this, matrix);

    /// <summary>Determines whether two Matrix4X4 instances are equal by comparing all their row components.</summary>
    /// <param name="x">The first matrix to compare.</param>
    /// <param name="y">The second matrix to compare.</param>
    /// <returns><c>true</c> if both matrices are null, refer to the same instance, or have identical row components; otherwise, <c>false</c>.</returns>
    public bool Equals(Matrix4X4? x, Matrix4X4? y) =>
        ReferenceEquals(x, y)
        || (
            x is not null
            && y is not null
            && x.GetType() == y.GetType()
            && !x.Row.Where((row, index) => row != y.Row[index]).Any()
        );

    /// <summary>Returns a hash code for the current matrix based on its row components.</summary>
    /// <returns>A hash code value suitable for use in hashing algorithms and data structures.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns a hash code for the specified matrix based on its row components.</summary>
    /// <param name="obj">The matrix to compute a hash code for.</param>
    /// <returns>A hash code value for the specified matrix.</returns>
    public int GetHashCode([NotNull] Matrix4X4 obj) =>
        HashCode.Combine(
            obj.Row[0].GetHashCode(),
            obj.Row[1].GetHashCode(),
            obj.Row[2].GetHashCode(),
            obj.Row[3].GetHashCode()
        );

    /// <summary>Returns a string representation of the matrix in the format "({Row0}, {Row1}, {Row2}, {Row3})".</summary>
    /// <returns>A string that represents the matrix with all four rows displayed.</returns>
    public override string ToString() => $"({Row[0]}, {Row[1]}, {Row[2]}, {Row[3]})";

    /// <summary>Returns a string representation of the matrix using the specified format provider.</summary>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the matrix formatted according to the specified format provider.</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({Row[0].ToString(formatProvider)}, {Row[1].ToString(formatProvider)}, {Row[2].ToString(formatProvider)}, {Row[3].ToString(formatProvider)})";

    /// <summary>Returns a string representation of the matrix using the specified format and format provider.</summary>
    /// <param name="format">A numeric format string applied to each component.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the matrix with each component formatted according to the specified format and provider.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({Row[0].ToString(format, formatProvider)}, {Row[1].ToString(format, formatProvider)}, {Row[2].ToString(format, formatProvider)}, {Row[3].ToString(format, formatProvider)})";
}
