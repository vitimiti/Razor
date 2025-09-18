// -----------------------------------------------------------------------
// <copyright file="Matrix3D.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Razor.Abstractions;
#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
using Matrix3DRow = (float Member1, float Member2, float Member3, float Member4);
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly

namespace Razor.Math;

/// <summary>Represents a 3x4 transformation matrix for 3D graphics operations including rotation, translation, and scaling.</summary>
/// <remarks>This matrix uses homogeneous coordinates with a 3x4 format where the fourth column represents translation.</remarks>
public class Matrix3D : IEqualityComparer<Matrix3D>
{
    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class with default zero values.</summary>
    public Matrix3D() { }

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class from an array of float values.</summary>
    /// <param name="values">Array containing at least 12 float values representing the matrix elements in row-major order.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array contains fewer than 12 elements.</exception>
    public Matrix3D([NotNull] float[] values) => Set(values);

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class using three row tuples.</summary>
    /// <param name="row1">First row containing four float values.</param>
    /// <param name="row2">Second row containing four float values.</param>
    /// <param name="row3">Third row containing four float values.</param>
    public Matrix3D(Matrix3DRow row1, Matrix3DRow row2, Matrix3DRow row3) => Set(row1, row2, row3);

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class from basis vectors and position.</summary>
    /// <param name="x">The X-axis vector defining the first column of the rotation matrix.</param>
    /// <param name="y">The Y-axis vector defining the second column of the rotation matrix.</param>
    /// <param name="z">The Z-axis vector defining the third column of the rotation matrix.</param>
    /// <param name="position">The position vector defining the translation.</param>
    public Matrix3D(Vector3 x, Vector3 y, Vector3 z, Vector3 position) => Set(x, y, z, position);

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class as a rotation matrix from an axis and angle.</summary>
    /// <param name="axis">The rotation axis vector (should be normalized).</param>
    /// <param name="angle">The rotation angle in radians.</param>
    public Matrix3D(Vector3 axis, float angle) => Set(axis, angle);

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class as a rotation matrix using precomputed sine and cosine values.</summary>
    /// <param name="axis">The rotation axis vector (must be normalized).</param>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public Matrix3D(Vector3 axis, float sin, float cos) => Set(axis, sin, cos);

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class from a 3x3 rotation matrix and position vector.</summary>
    /// <param name="rotation">The 3x3 rotation matrix.</param>
    /// <param name="position">The translation vector.</param>
    public Matrix3D(Matrix3X3 rotation, Vector3 position) => Set(rotation, position);

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class from a quaternion rotation and position vector.</summary>
    /// <param name="rotation">The quaternion representing the rotation.</param>
    /// <param name="position">The translation vector.</param>
    public Matrix3D(Quaternion rotation, Vector3 position) => Set(rotation, position);

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class as a translation matrix.</summary>
    /// <param name="position">The translation vector. Rotation components are set to identity.</param>
    public Matrix3D(Vector3 position) => Set(position);

    /// <summary>Initializes a new instance of the <see cref="Matrix3D"/> class by copying another matrix.</summary>
    /// <param name="other">The matrix to copy from.</param>
    public Matrix3D([NotNull] Matrix3D other) => Row = other.Row;

    /// <summary>Gets the 3x4 identity matrix with ones on the diagonal and zeros elsewhere.</summary>
    /// <value>A matrix representing no transformation (identity transformation).</value>
    public static Matrix3D Identity => new((1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 1, 0));

    /// <summary>Gets a matrix representing a 90-degree rotation around the X-axis.</summary>
    /// <value>A rotation matrix for 90 degrees around the X-axis.</value>
    public static Matrix3D RotateX90 => new((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0));

    /// <summary>Gets a matrix representing a 180-degree rotation around the X-axis.</summary>
    /// <value>A rotation matrix for 180 degrees around the X-axis.</value>
    public static Matrix3D RotateX180 => new((1, 0, 0, 0), (0, -1, 0, 0), (0, 0, -1, 0));

    /// <summary>Gets a matrix representing a 270-degree rotation around the X-axis.</summary>
    /// <value>A rotation matrix for 270 degrees around the X-axis.</value>
    public static Matrix3D RotateX270 => new((1, 0, 0, 0), (0, 0, 1, 0), (0, -1, 0, 0));

    /// <summary>Gets a matrix representing a 90-degree rotation around the Y-axis.</summary>
    /// <value>A rotation matrix for 90 degrees around the Y-axis.</value>
    public static Matrix3D RotateY90 => new((0, 0, 1, 0), (0, 1, 0, 0), (-1, 0, 0, 0));

    /// <summary>Gets a matrix representing a 180-degree rotation around the Y-axis.</summary>
    /// <value>A rotation matrix for 180 degrees around the Y-axis.</value>
    public static Matrix3D RotateY180 => new((-1, 0, 0, 0), (0, 1, 0, 0), (0, 0, -1, 0));

    /// <summary>Gets a matrix representing a 270-degree rotation around the Y-axis.</summary>
    /// <value>A rotation matrix for 270 degrees around the Y-axis.</value>
    public static Matrix3D RotateY270 => new((0, -1, 0, 0), (0, 1, 0, 0), (1, 0, 0, 0));

    /// <summary>Gets a matrix representing a 90-degree rotation around the Z-axis.</summary>
    /// <value>A rotation matrix for 90 degrees around the Z-axis.</value>
    public static Matrix3D RotateZ90 => new((0, -1, 0, 0), (1, 0, 0, 0), (0, 0, 1, 0));

    /// <summary>Gets a matrix representing a 180-degree rotation around the Z-axis.</summary>
    /// <value>A rotation matrix for 180 degrees around the Z-axis.</value>
    public static Matrix3D RotateZ180 => new((-1, 0, 0, 0), (0, -1, 0, 0), (0, 0, 1, 0));

    /// <summary>Gets a matrix representing a 270-degree rotation around the Z-axis.</summary>
    /// <value>A rotation matrix for 270 degrees around the Z-axis.</value>
    public static Matrix3D RotateZ270 => new((0, 1, 0, 0), (-1, 0, 0, 0), (0, 0, 1, 0));

    /// <summary>Gets or sets the translation vector (position) of the matrix.</summary>
    /// <value>The translation components as a Vector3 (X, Y, Z translation values).</value>
    /// <exception cref="ArgumentNullException">Thrown when setting to null.</exception>
    public Vector3 Translation
    {
        get => new(XTranslation, YTranslation, ZTranslation);
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            XTranslation = value.X;
            YTranslation = value.Y;
            ZTranslation = value.Z;
        }
    }

    /// <summary>Gets or sets the X translation component of the matrix.</summary>
    /// <value>The X translation value from the first row's fourth column.</value>
    public float XTranslation
    {
        get => Row[0][3];
        set => Row[0][3] = value;
    }

    /// <summary>Gets or sets the Y translation component of the matrix.</summary>
    /// <value>The Y translation value from the second row's fourth column.</value>
    public float YTranslation
    {
        get => Row[1][3];
        set => Row[1][3] = value;
    }

    /// <summary>Gets or sets the Z translation component of the matrix.</summary>
    /// <value>The Z translation value from the third row's fourth column.</value>
    public float ZTranslation
    {
        get => Row[2][3];
        set => Row[2][3] = value;
    }

    /// <summary>Gets the X-axis vector (first column) of the rotation matrix.</summary>
    /// <value>The X-axis basis vector extracted from the matrix columns.</value>
    public Vector3 XVector => new(Row[0][0], Row[1][0], Row[2][0]);

    /// <summary>Gets the Y-axis vector (second column) of the rotation matrix.</summary>
    /// <value>The Y-axis basis vector extracted from the matrix columns.</value>
    public Vector3 YVector => new(Row[0][1], Row[1][1], Row[2][1]);

    /// <summary>Gets the Z-axis vector (third column) of the rotation matrix.</summary>
    /// <value>The Z-axis basis vector extracted from the matrix columns.</value>
    public Vector3 ZVector => new(Row[0][2], Row[1][2], Row[2][2]);

    /// <summary>Gets the rotation angle around the X-axis in radians.</summary>
    /// <value>The X-axis rotation angle calculated using arctangent of matrix elements.</value>
    public float XRotation => float.Atan2(Row[2][1], Row[1][1]);

    /// <summary>Gets the rotation angle around the Y-axis in radians.</summary>
    /// <value>The Y-axis rotation angle calculated using arctangent of matrix elements.</value>
    public float YRotation => float.Atan2(Row[0][2], Row[2][2]);

    /// <summary>Gets the rotation angle around the Z-axis in radians.</summary>
    /// <value>The Z-axis rotation angle calculated using arctangent of matrix elements.</value>
    public float ZRotation => float.Atan2(Row[1][0], Row[0][0]);

    /// <summary>Gets a value indicating whether the matrix is orthogonal (represents a pure rotation without scaling).</summary>
    /// <value><c>true</c> if the matrix has orthonormal basis vectors; otherwise, <c>false</c>.</value>
    /// <remarks>An orthogonal matrix has perpendicular basis vectors of unit length, indicating no scaling or shearing.</remarks>
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
                && float.Abs(x.Length2 - 1F) <= float.Epsilon
                && float.Abs(y.Length2 - 1F) <= float.Epsilon
                && float.Abs(z.Length2 - 1F) <= float.Epsilon;
        }
    }

    /// <summary>Gets the collection of row vectors that define the matrix structure.</summary>
    /// <value>A collection containing exactly three Vector4 instances representing the matrix rows.</value>
    /// <remarks>Each row contains four components: three for rotation/scale and one for translation.</remarks>
    protected Collection<Vector4> Row { get; } = [new(), new(), new()];

    /// <summary>Gets or sets the row vector at the specified index.</summary>
    /// <param name="index">The zero-based row index (0-2).</param>
    /// <value>The Vector4 representing the specified matrix row.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the valid range (0-2).</exception>
    public Vector4 this[int index]
    {
        get => Row[index];
        set => Row[index] = value;
    }

    /// <summary>Multiplies two matrices together using standard matrix multiplication.</summary>
    /// <param name="left">The first matrix (left operand).</param>
    /// <param name="right">The second matrix (right operand).</param>
    /// <returns>The resulting matrix from the multiplication operation.</returns>
    public static Matrix3D operator *(Matrix3D left, Matrix3D right) => Multiply(left, right);

    /// <summary>Transforms a vector by multiplying it with the matrix (applies transformation).</summary>
    /// <param name="left">The transformation matrix.</param>
    /// <param name="right">The vector to transform.</param>
    /// <returns>The transformed vector.</returns>
    public static Vector3 operator *(Matrix3D left, Vector3 right) => Multiply(left, right);

    /// <summary>Determines whether two matrices are equal using reference and component comparison.</summary>
    /// <param name="left">The first matrix to compare.</param>
    /// <param name="right">The second matrix to compare.</param>
    /// <returns><c>true</c> if the matrices are equal; otherwise, <c>false</c>.</returns>
    [SuppressMessage(
        "csharpsquid",
        "S3875:\"operator==\" should not be overloaded on reference types",
        Justification = "This is a mathematical class."
    )]
    public static bool operator ==(Matrix3D left, Matrix3D right) => object.Equals(left, right);

    /// <summary>Determines whether two matrices are not equal using reference and component comparison.</summary>
    /// <param name="left">The first matrix to compare.</param>
    /// <param name="right">The second matrix to compare.</param>
    /// <returns><c>true</c> if the matrices are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Matrix3D left, Matrix3D right) => !object.Equals(left, right);

    /// <summary>Multiplies two matrices together using standard 3x4 matrix multiplication.</summary>
    /// <param name="left">The first matrix (left operand).</param>
    /// <param name="right">The second matrix (right operand).</param>
    /// <returns>The resulting transformation matrix from the multiplication operation.</returns>
    /// <remarks>This performs the mathematical matrix multiplication where each element of the result is computed as the dot product of the corresponding row and column.</remarks>
    public static Matrix3D Multiply([NotNull] Matrix3D left, [NotNull] Matrix3D right)
    {
        Matrix3D result = new();

        result[0][0] = (left[0][0] * right[0][0]) + (left[0][1] * right[1][0]) + (left[0][2] * right[2][0]);
        result[1][0] = (left[1][0] * right[0][0]) + (left[1][1] * right[1][0]) + (left[1][2] * right[2][0]);
        result[2][0] = (left[2][0] * right[0][0]) + (left[2][1] * right[1][0]) + (left[2][2] * right[2][0]);

        result[0][1] = (left[0][0] * right[0][1]) + (left[0][1] * right[1][1]) + (left[0][2] * right[2][1]);
        result[1][1] = (left[1][0] * right[0][1]) + (left[1][1] * right[1][1]) + (left[1][2] * right[2][1]);
        result[2][1] = (left[2][0] * right[0][1]) + (left[2][1] * right[1][1]) + (left[2][2] * right[2][1]);

        result[0][2] = (left[0][0] * right[0][2]) + (left[0][1] * right[1][2]) + (left[0][2] * right[2][2]);
        result[1][2] = (left[1][0] * right[0][2]) + (left[1][1] * right[1][2]) + (left[1][2] * right[2][2]);
        result[2][2] = (left[2][0] * right[0][2]) + (left[2][1] * right[1][2]) + (left[2][2] * right[2][2]);

        result[0][3] =
            (left[0][0] * right[0][3]) + (left[0][1] * right[1][3]) + (left[0][2] * right[2][3]) + left[0][3];

        result[1][3] =
            (left[1][0] * right[0][3]) + (left[1][1] * right[1][3]) + (left[1][2] * right[2][3]) + left[1][3];

        result[2][3] =
            (left[2][0] * right[0][3]) + (left[2][1] * right[1][3]) + (left[2][2] * right[2][3]) + left[2][3];

        return result;
    }

    /// <summary>Transforms a vector by applying the matrix transformation (rotation and translation).</summary>
    /// <param name="left">The transformation matrix.</param>
    /// <param name="right">The vector to transform.</param>
    /// <returns>The transformed vector with both rotation and translation applied.</returns>
    /// <remarks>This performs a full transformation including translation, equivalent to treating the vector as a point in homogeneous coordinates.</remarks>
    public static Vector3 Multiply([NotNull] Matrix3D left, [NotNull] Vector3 right) =>
        new(
            (left.Row[0].X * right.X) + (left.Row[0].Y * right.Y) + (left.Row[0].Z * right.Z) + left.Row[0].W,
            (left.Row[1].X * right.X) + (left.Row[1].Y * right.Y) + (left.Row[1].Z * right.Z) + left.Row[1].W,
            (left.Row[2].X * right.X) + (left.Row[2].Y * right.Y) + (left.Row[2].Z * right.Z) + left.Row[2].W
        );

    /// <summary>Transforms a vector by applying the full matrix transformation including translation.</summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <param name="vector">The vector to transform.</param>
    /// <returns>The fully transformed vector (rotation + translation).</returns>
    /// <remarks>This is equivalent to the <c>*</c> operator and <c>Multiply</c> method, treating the vector as a point.</remarks>
    public static Vector3 TransformVector([NotNull] Matrix3D matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector.X) + (matrix[0][1] * vector.Y) + (matrix[0][2] * vector.Z) + matrix[0][3],
            (matrix[1][0] * vector.X) + (matrix[1][1] * vector.Y) + (matrix[1][2] * vector.Z) + matrix[1][3],
            (matrix[2][0] * vector.X) + (matrix[2][1] * vector.Y) + (matrix[2][2] * vector.Z) + matrix[2][3]
        );

    /// <summary>Rotates a vector using only the rotation portion of the matrix (ignores translation).</summary>
    /// <param name="matrix">The transformation matrix (only rotation part is used).</param>
    /// <param name="vector">The vector to rotate.</param>
    /// <returns>The rotated vector without translation applied.</returns>
    /// <remarks>This treats the vector as a direction rather than a point, applying only rotation and scaling.</remarks>
    public static Vector3 RotateVector([NotNull] Matrix3D matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector.X) + (matrix[0][1] * vector.Y) + (matrix[0][2] * vector.Z),
            (matrix[1][0] * vector.X) + (matrix[1][1] * vector.Y) + (matrix[1][2] * vector.Z),
            (matrix[2][0] * vector.X) + (matrix[2][1] * vector.Y) + (matrix[2][2] * vector.Z)
        );

    /// <summary>Applies the inverse transformation to a vector (opposite of <see cref="TransformVector"/>).</summary>
    /// <param name="matrix">The transformation matrix to invert.</param>
    /// <param name="vector">The vector in transformed space.</param>
    /// <returns>The vector in original space before transformation.</returns>
    /// <remarks>This assumes the matrix is orthogonal and first subtracts translation, then applies inverse rotation.</remarks>
    public static Vector3 InverseTransformVector([NotNull] Matrix3D matrix, [NotNull] Vector3 vector)
    {
        Vector3 diff = new(vector.X - matrix[0][3], vector.Y - matrix[1][3], vector.Z - matrix[2][3]);
        return InverseRotateVector(matrix, diff);
    }

    /// <summary>Applies the inverse rotation to a vector using the transpose of the rotation matrix.</summary>
    /// <param name="matrix">The transformation matrix (only rotation part is used).</param>
    /// <param name="vector">The vector to inverse rotate.</param>
    /// <returns>The vector rotated by the inverse (transpose) of the rotation matrix.</returns>
    /// <remarks>This works correctly only if the rotation matrix is orthogonal (no scaling/shearing).</remarks>
    public static Vector3 InverseRotateVector([NotNull] Matrix3D matrix, [NotNull] Vector3 vector) =>
        new(
            (matrix[0][0] * vector.X) + (matrix[1][0] * vector.Y) + (matrix[2][0] * vector.Z),
            (matrix[0][1] * vector.X) + (matrix[1][1] * vector.Y) + (matrix[2][1] * vector.Z),
            (matrix[0][2] * vector.X) + (matrix[1][2] * vector.Y) + (matrix[2][2] * vector.Z)
        );

    /// <summary>Creates a Matrix3D from a native matrix inverse interface implementation.</summary>
    /// <param name="nativeMatrixInverse">The native matrix inverse implementation providing indexed access to matrix elements.</param>
    /// <returns>A new Matrix3D instance populated with values from the native matrix inverse.</returns>
    /// <remarks>This method is used to interface with external graphics APIs that provide their own matrix inverse implementations.</remarks>
    public static Matrix3D GetInverse([NotNull] INativeMatrixInverse nativeMatrixInverse)
    {
        Matrix3D result = new()
        {
            [0] = { [0] = nativeMatrixInverse[0, 0] },
            [0] = { [1] = nativeMatrixInverse[0, 1] },
            [0] = { [2] = nativeMatrixInverse[0, 2] },
            [0] = { [3] = nativeMatrixInverse[0, 3] },
            [1] = { [0] = nativeMatrixInverse[1, 0] },
            [1] = { [1] = nativeMatrixInverse[1, 1] },
            [1] = { [2] = nativeMatrixInverse[1, 2] },
            [1] = { [3] = nativeMatrixInverse[1, 3] },
            [2] = { [0] = nativeMatrixInverse[2, 0] },
            [2] = { [1] = nativeMatrixInverse[2, 1] },
            [2] = { [2] = nativeMatrixInverse[2, 2] },
            [2] = { [3] = nativeMatrixInverse[2, 3] },
        };

        return result;
    }

    /// <summary>Performs linear interpolation between two transformation matrices.</summary>
    /// <param name="left">The starting matrix (when factor is 0).</param>
    /// <param name="right">The ending matrix (when factor is 1).</param>
    /// <param name="factor">The interpolation factor between 0 and 1.</param>
    /// <returns>The interpolated transformation matrix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when factor is not between 0 and 1.</exception>
    /// <remarks>This method interpolates translation linearly and rotation using spherical linear interpolation (SLERP) via quaternions.</remarks>
    public static Matrix3D Lerp([NotNull] Matrix3D left, [NotNull] Matrix3D right, float factor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(factor, 0F);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(factor, 1F);

        var pos = Vector3.Lerp(left.Translation, right.Translation, factor);
        var rot = Quaternion.Slerp(Quaternion.Build(left), Quaternion.Build(right), factor);
        return new Matrix3D(rot, pos);
    }

    /// <summary>Attempts to solve a linear system using Gaussian elimination with the matrix as the coefficient matrix.</summary>
    /// <param name="system">The matrix representing the linear system to solve (modified in place).</param>
    /// <returns><c>true</c> if the system was successfully solved; <c>false</c> if the matrix is singular.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="system"/> is <c>null</c>.</exception>
    /// <remarks>This method modifies the input matrix in place and may fail if the matrix is singular (determinant near zero).</remarks>
    public static bool TrySolveLinearSystem(ref Matrix3D system)
    {
        ArgumentNullException.ThrowIfNull(system);

        if (float.Abs(system[0][0]) < float.Epsilon)
        {
            return false;
        }

        system[0] *= 1F / system[0][0];
        system[1] -= system[1][0] * system[0];
        system[2] -= system[2][0] * system[0];

        if (float.Abs(system[1][1]) < float.Epsilon)
        {
            return false;
        }

        system[1] *= 1F / system[1][1];
        system[2] -= system[2][1] * system[1];

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

    /// <summary>Computes the orthogonal inverse of the matrix assuming it represents an orthogonal transformation.</summary>
    /// <returns>The orthogonal inverse matrix.</returns>
    /// <remarks>This method is faster than general matrix inversion but only works correctly for orthogonal matrices (pure rotation + translation). The rotation part is transposed and translation is transformed accordingly.</remarks>
    public Matrix3D GetOrthogonalInverse()
    {
        Matrix3D result = new()
        {
            [0] = { [0] = Row[0][0] },
            [0] = { [1] = Row[1][0] },
            [0] = { [2] = Row[2][0] },
            [1] = { [0] = Row[0][1] },
            [1] = { [1] = Row[1][1] },
            [1] = { [2] = Row[2][1] },
            [2] = { [0] = Row[0][2] },
            [2] = { [1] = Row[1][2] },
            [2] = { [2] = Row[2][2] },
        };

        Vector3 translation = Translation;
        translation = result.RotateVector(translation);
        translation = -translation;

        result.Row[0][3] = translation[0];
        result.Row[1][3] = translation[1];
        result.Row[2][3] = translation[2];

        return result;
    }

    /// <summary>Sets the matrix values from an array of floats in row-major order.</summary>
    /// <param name="values">Array containing at least 12 float values representing the 3x4 matrix elements.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array contains fewer than 12 elements.</exception>
    /// <remarks>Values are assigned in row-major order: [0-3] to first row, [4-7] to second row, [8-11] to third row.</remarks>
    public void Set([NotNull] float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 12);
        Row[0].Set(values[0], values[1], values[2], values[3]);
        Row[1].Set(values[4], values[5], values[6], values[7]);
        Row[2].Set(values[8], values[9], values[10], values[11]);
    }

    /// <summary>Sets the matrix values using three row tuples.</summary>
    /// <param name="row1">First row containing four float values (Member1, Member2, Member3, Member4).</param>
    /// <param name="row2">Second row containing four float values.</param>
    /// <param name="row3">Third row containing four float values.</param>
    public void Set(Matrix3DRow row1, Matrix3DRow row2, Matrix3DRow row3)
    {
        Row[0].Set(row1.Member1, row1.Member2, row1.Member3, row1.Member4);
        Row[1].Set(row2.Member1, row2.Member2, row2.Member3, row2.Member4);
        Row[2].Set(row3.Member1, row3.Member2, row3.Member3, row3.Member4);
    }

    /// <summary>Sets the matrix from basis vectors and position vector.</summary>
    /// <param name="x">The X-axis basis vector (first column of rotation matrix).</param>
    /// <param name="y">The Y-axis basis vector (second column of rotation matrix).</param>
    /// <param name="z">The Z-axis basis vector (third column of rotation matrix).</param>
    /// <param name="position">The position vector (fourth column).</param>
    /// <remarks>The matrix is constructed with the basis vectors as columns and position as the translation component.</remarks>
    public void Set([NotNull] Vector3 x, [NotNull] Vector3 y, [NotNull] Vector3 z, [NotNull] Vector3 position)
    {
        Row[0].Set(x[0], y[0], z[0], position[0]);
        Row[1].Set(x[1], y[1], z[1], position[1]);
        Row[2].Set(x[2], y[2], z[2], position[2]);
    }

    /// <summary>Sets the matrix as a rotation matrix around the specified axis and angle.</summary>
    /// <param name="axis">The rotation axis vector (should be normalized).</param>
    /// <param name="angle">The rotation angle in radians.</param>
    public void Set([NotNull] Vector3 axis, float angle) => Set(axis, float.Sin(angle), float.Cos(angle));

    /// <summary>Sets the matrix as a rotation matrix using axis-angle representation with precomputed sine and cosine.</summary>
    /// <param name="axis">The rotation axis vector (must be normalized).</param>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the axis vector is not normalized (length not equal to 1).</exception>
    /// <remarks>This method uses Rodrigues' rotation formula to construct the rotation matrix. Translation is set to zero.</remarks>
    public void Set([NotNull] Vector3 axis, float sin, float cos)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(float.Abs(axis.Length2 - 1F), float.Epsilon);

        Row[0]
            .Set(
                float.Pow(axis[0], 2) + (cos * (1F - float.Pow(axis[0], 2))),
                (axis[0] * axis[1] * (1F - cos)) - (axis[2] * sin),
                (axis[2] * axis[0] * (1F - cos)) + (axis[1] * sin),
                0F
            );

        Row[1]
            .Set(
                axis[0] * axis[1] * (1F - cos) * axis[2] * sin,
                float.Pow(axis[1], 2) + (cos * (1F - float.Pow(axis[1], 2))),
                (axis[1] * axis[2] * (1F - cos)) - (axis[0] * sin),
                0F
            );

        Row[2]
            .Set(
                (axis[2] * axis[0] * (1F - cos)) - (axis[1] * sin),
                (axis[1] * axis[2] * (1F - cos)) + (axis[0] * sin),
                float.Pow(axis[2], 2) + (cos * (1F - float.Pow(axis[2], 2))),
                0F
            );
    }

    /// <summary>Sets the matrix as a translation-only matrix with identity rotation.</summary>
    /// <param name="position">The translation vector. Rotation is set to identity.</param>
    public void Set([NotNull] Vector3 position)
    {
        Row[0].Set(1F, 0F, 0F, position[0]);
        Row[1].Set(0F, 1F, 0F, position[1]);
        Row[2].Set(0F, 0F, 1F, position[2]);
    }

    /// <summary>Sets the matrix from a 3x3 rotation matrix and position vector.</summary>
    /// <param name="rotation">The 3x3 rotation matrix.</param>
    /// <param name="position">The translation vector.</param>
    public void Set([NotNull] Matrix3X3 rotation, [NotNull] Vector3 position)
    {
        Row[0].Set(rotation[0][0], rotation[0][1], rotation[0][2], position[0]);
        Row[1].Set(rotation[1][0], rotation[1][1], rotation[1][2], position[1]);
        Row[2].Set(rotation[2][0], rotation[2][1], rotation[2][2], position[2]);
    }

    /// <summary>Sets the matrix from a quaternion rotation and position vector.</summary>
    /// <param name="rotation">The quaternion representing the rotation.</param>
    /// <param name="position">The translation vector.</param>
    public void Set([NotNull] Quaternion rotation, Vector3 position)
    {
        SetRotation(rotation);
        Translation = position;
    }

    /// <summary>Resets the matrix to the identity transformation (no rotation, no translation).</summary>
    /// <remarks>This creates a 3x4 identity matrix with ones on the diagonal and zeros elsewhere.</remarks>
    public void MakeIdentity()
    {
        Row[0].Set(1F, 0F, 0F, 0F);
        Row[1].Set(0F, 1F, 0F, 0F);
        Row[2].Set(0F, 0F, 1F, 0F);
    }

    /// <summary>Applies a translation to the matrix by the specified amounts along each axis.</summary>
    /// <param name="x">Translation amount along the X-axis.</param>
    /// <param name="y">Translation amount along the Y-axis.</param>
    /// <param name="z">Translation amount along the Z-axis.</param>
    /// <remarks>The translation is applied in the coordinate system defined by the matrix's current orientation.</remarks>
    public void Translate(float x, float y, float z)
    {
        Row[0][3] += (Row[0][0] * x) + (Row[0][1] * y) + (Row[0][2] * z);
        Row[1][3] += (Row[1][0] * x) + (Row[1][1] * y) + (Row[1][2] * z);
        Row[2][3] += (Row[2][0] * x) + (Row[2][1] * y) + (Row[2][2] * z);
    }

    /// <summary>Applies a translation to the matrix by the specified translation vector.</summary>
    /// <param name="translation">The translation vector containing X, Y, and Z translation amounts.</param>
    public void Translate([NotNull] Vector3 translation) => Translate(translation.X, translation.Y, translation.Z);

    /// <summary>Applies a translation along the X-axis by the specified amount.</summary>
    /// <param name="x">Translation amount along the X-axis.</param>
    public void TranslateX(float x)
    {
        Row[0][3] += Row[0][0] * x;
        Row[1][3] += Row[1][0] * x;
        Row[2][3] += Row[2][0] * x;
    }

    /// <summary>Applies a translation along the Y-axis by the specified amount.</summary>
    /// <param name="y">Translation amount along the Y-axis.</param>
    public void TranslateY(float y)
    {
        Row[0][3] += Row[0][1] * y;
        Row[1][3] += Row[1][1] * y;
        Row[2][3] += Row[2][1] * y;
    }

    /// <summary>Applies a translation along the Z-axis by the specified amount.</summary>
    /// <param name="z">Translation amount along the Z-axis.</param>
    public void TranslateZ(float z)
    {
        Row[0][3] += Row[0][2] * z;
        Row[1][3] += Row[1][2] * z;
        Row[2][3] += Row[2][2] * z;
    }

    /// <summary>Applies a rotation around the X-axis by the specified angle.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    public void RotateX(float theta) => RotateX(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a rotation around the X-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>This method is more efficient when sine and cosine values are already computed.</remarks>
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

    /// <summary>Applies a rotation around the Y-axis by the specified angle.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    public void RotateY(float theta) => RotateY(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a rotation around the Y-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>This method is more efficient when sine and cosine values are already computed.</remarks>
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

    /// <summary>Applies a rotation around the Z-axis by the specified angle.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    public void RotateZ(float theta) => RotateZ(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a rotation around the Z-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>This method is more efficient when sine and cosine values are already computed.</remarks>
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

    /// <summary>Sets the rotation portion of the matrix from a 3x3 rotation matrix, preserving translation.</summary>
    /// <param name="rotation">The 3x3 rotation matrix to copy rotation values from.</param>
    public void SetRotation([NotNull] Matrix3X3 rotation)
    {
        Row[0][0] = rotation[0][0];
        Row[0][1] = rotation[0][1];
        Row[0][2] = rotation[0][2];

        Row[1][0] = rotation[1][0];
        Row[1][1] = rotation[1][1];
        Row[1][2] = rotation[1][2];

        Row[2][0] = rotation[2][0];
        Row[2][1] = rotation[2][1];
        Row[2][2] = rotation[2][2];
    }

    /// <summary>Sets the rotation portion of the matrix from a quaternion, preserving translation.</summary>
    /// <param name="rotation">The quaternion representing the desired rotation.</param>
    /// <remarks>This method converts the quaternion to a rotation matrix using standard quaternion-to-matrix conversion formulas.</remarks>
    public void SetRotation([NotNull] Quaternion rotation)
    {
        Row[0][0] = 1F - (2F * (float.Pow(rotation[1], 2) + float.Pow(rotation[2], 2)));
        Row[0][1] = 2F * ((rotation[0] * rotation[1]) - (rotation[2] * rotation[3]));
        Row[0][2] = 2F * ((rotation[2] * rotation[0]) + (rotation[1] * rotation[3]));

        Row[1][0] = 2F * ((rotation[0] * rotation[1]) + (rotation[2] * rotation[3]));
        Row[1][1] = 1F - (2F * (float.Pow(rotation[2], 2) + float.Pow(rotation[0], 2)));
        Row[1][2] = 2F * ((rotation[1] * rotation[2]) - (rotation[0] * rotation[3]));

        Row[2][0] = 2F * ((rotation[2] * rotation[0]) - (rotation[1] * rotation[3]));
        Row[2][1] = 2F * ((rotation[1] * rotation[2]) + (rotation[0] * rotation[3]));
        Row[2][2] = 1F - (2F * (float.Pow(rotation[1], 2) + float.Pow(rotation[0], 2)));
    }

    /// <summary>Rotates the specified vector using only the rotation portion of the matrix.</summary>
    /// <param name="vector">The vector to rotate.</param>
    /// <returns>The rotated vector without translation applied.</returns>
    /// <remarks>This treats the input as a direction vector rather than a point, applying only rotation and scaling.</remarks>
    public Vector3 RotateVector([NotNull] Vector3 vector) =>
        new(
            (Row[0][0] * vector[0]) + (Row[0][1] * vector[1]) + (Row[0][2] * vector[2]),
            (Row[1][0] * vector[0]) + (Row[1][1] * vector[1]) + (Row[1][2] * vector[2]),
            (Row[2][0] * vector[0]) + (Row[2][1] * vector[1]) + (Row[2][2] * vector[2])
        );

    /// <summary>Applies the inverse rotation to the specified vector using the transpose of the rotation matrix.</summary>
    /// <param name="vector">The vector to inverse rotate.</param>
    /// <returns>The vector rotated by the inverse of the matrix rotation.</returns>
    /// <remarks>This method assumes the rotation matrix is orthogonal (no scaling/shearing). For non-orthogonal matrices, use the general matrix inverse.</remarks>
    public Vector3 InverseRotateVector([NotNull] Vector3 vector) =>
        new(
            (Row[0][0] * vector[0]) + (Row[1][0] * vector[1]) + (Row[2][0] * vector[2]),
            (Row[0][1] * vector[0]) + (Row[1][1] * vector[1]) + (Row[2][1] * vector[2]),
            (Row[0][2] * vector[0]) + (Row[1][2] * vector[1]) + (Row[2][2] * vector[2])
        );

    /// <summary>Configures the matrix to represent a camera transformation looking from position toward target with specified roll.</summary>
    /// <param name="position">The camera position in world space.</param>
    /// <param name="target">The point the camera is looking at.</param>
    /// <param name="roll">The roll angle in radians around the view direction.</param>
    /// <remarks>This creates a view matrix suitable for camera positioning, with standard camera orientation conventions.</remarks>
    public void LookAt([NotNull] Vector3 position, [NotNull] Vector3 target, float roll)
    {
        var dx = target[0] - position[0];
        var dy = target[1] - position[1];
        var dz = target[2] - position[2];

        var len1 = float.Sqrt(float.Pow(dx, 2) + float.Pow(dy, 2) + float.Pow(dz, 2));
        var len2 = float.Sqrt(float.Pow(dx, 2) + float.Pow(dy, 2));

        float sinP;
        float cosP;
        if (float.Abs(len1) >= float.Epsilon)
        {
            sinP = dz / len1;
            cosP = len2 / len1;
        }
        else
        {
            sinP = 0F;
            cosP = 1F;
        }

        float sinY;
        float cosY;
        if (float.Abs(len2) >= float.Epsilon)
        {
            sinY = dy / len2;
            cosY = dx / len2;
        }
        else
        {
            sinY = 0F;
            cosY = 1F;
        }

        Row[0].X = 0;
        Row[0].Y = 0;
        Row[0].Z = -1;

        Row[1].X = -1;
        Row[1].Y = 0;
        Row[1].Z = 0;

        Row[2].X = 0;
        Row[2].Y = 1;
        Row[2].Z = 0;

        Row[0].W = position.X;
        Row[1].W = position.Y;
        Row[2].W = position.Z;

        RotateY(sinY, cosY);
        RotateX(sinP, cosP);
        RotateZ(-roll);
    }

    /// <summary>Builds a transformation matrix from a position and normalized direction vector.</summary>
    /// <param name="position">The position for the transformation.</param>
    /// <param name="direction">The direction vector (must be normalized to unit length).</param>
    /// <exception cref="ArgumentException">Thrown when the direction vector is not normalized.</exception>
    /// <remarks>This creates a transformation matrix where the local Z-axis aligns with the direction vector.</remarks>
    public void BuildTransformMatrix([NotNull] Vector3 position, [NotNull] Vector3 direction)
    {
        if (float.Abs(direction.Length2 - 1F) >= float.Epsilon)
        {
            throw new ArgumentException("Direction must be unitized.", nameof(direction));
        }

        var len2 = float.Sqrt(float.Pow(direction.X, 2) + float.Pow(direction.Y, 2));
        var sinP = direction.Z;

        float sinY;
        float cosY;
        if (float.Abs(len2) >= float.Epsilon)
        {
            sinY = direction.Y / len2;
            cosY = direction.X / len2;
        }
        else
        {
            sinY = 0F;
            cosY = 1F;
        }

        MakeIdentity();
        Translate(position);
        RotateZ(sinY, cosY);
        RotateY(-sinP, len2);
    }

    /// <summary>Configures the matrix for object transformation looking from position toward target with specified roll.</summary>
    /// <param name="position">The object position in world space.</param>
    /// <param name="target">The point the object is looking at.</param>
    /// <param name="roll">The roll angle in radians around the view direction.</param>
    /// <remarks>This is similar to <see cref="LookAt"/> but uses object-oriented conventions rather than camera conventions.</remarks>
    public void ObjLookAt([NotNull] Vector3 position, [NotNull] Vector3 target, float roll)
    {
        var dx = target[0] - position[0];
        var dy = target[1] - position[1];
        var dz = target[2] - position[2];

        var len1 = float.Sqrt(float.Pow(dx, 2) + float.Pow(dy, 2) + float.Pow(dz, 2));
        var len2 = float.Sqrt(float.Pow(dx, 2) + float.Pow(dy, 2));

        float sinP;
        float cosP;
        if (float.Abs(len1) >= float.Epsilon)
        {
            sinP = dz / len1;
            cosP = len2 / len1;
        }
        else
        {
            sinP = 0F;
            cosP = 1F;
        }

        float sinY;
        float cosY;
        if (float.Abs(len2) >= float.Epsilon)
        {
            sinY = dy / len2;
            cosY = dx / len2;
        }
        else
        {
            sinY = 0F;
            cosY = 1F;
        }

        MakeIdentity();
        Translate(position);
        RotateZ(sinY, cosY);
        RotateY(-sinP, cosP);
        RotateX(roll);
    }

    /// <summary>Applies uniform scaling to the matrix by multiplying all basis vectors by the scale factor.</summary>
    /// <param name="scale">The uniform scale factor to apply to all axes.</param>
    /// <remarks>Translation is not affected by scaling. Only the rotation/scale portion of the matrix is modified.</remarks>
    public void Scale(float scale)
    {
        Row[0][0] *= scale;
        Row[1][0] *= scale;
        Row[2][0] *= scale;

        Row[0][1] *= scale;
        Row[1][1] *= scale;
        Row[2][1] *= scale;

        Row[0][2] *= scale;
        Row[1][2] *= scale;
        Row[2][2] *= scale;
    }

    /// <summary>Applies non-uniform scaling to the matrix with different scale factors for each axis.</summary>
    /// <param name="x">The scale factor for the X-axis.</param>
    /// <param name="y">The scale factor for the Y-axis.</param>
    /// <param name="z">The scale factor for the Z-axis.</param>
    /// <remarks>Translation is not affected by scaling. Each axis can be scaled independently.</remarks>
    public void Scale(float x, float y, float z)
    {
        Row[0][0] *= x;
        Row[1][0] *= x;
        Row[2][0] *= x;

        Row[0][1] *= y;
        Row[1][1] *= y;
        Row[2][1] *= y;

        Row[0][2] *= z;
        Row[1][2] *= z;
        Row[2][2] *= z;
    }

    /// <summary>Applies scaling to the matrix using a vector containing scale factors for each axis.</summary>
    /// <param name="scale">Vector containing X, Y, and Z scale factors.</param>
    public void Scale([NotNull] Vector3 scale) => Scale(scale.X, scale.Y, scale.Z);

    /// <summary>Applies a pre-rotation around the X-axis by the specified angle.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    /// <remarks>Pre-rotations are applied in object space rather than world space, affecting the interpretation of subsequent transformations.</remarks>
    public void PreRotateX(float theta) => PreRotateX(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a pre-rotation around the X-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>Pre-rotations modify the coordinate system for subsequent transformations. This includes the translation component.</remarks>
    public void PreRotateX(float sin, float cos)
    {
        var temp1 = Row[1][0];
        var temp2 = Row[2][0];
        Row[1][0] = (cos * temp1) - (sin * temp2);
        Row[2][0] = (sin * temp1) + (cos * temp2);

        temp1 = Row[1][1];
        temp2 = Row[2][1];
        Row[1][1] = (cos * temp1) - (sin * temp2);
        Row[2][1] = (sin * temp1) + (cos * temp2);

        temp1 = Row[1][2];
        temp2 = Row[2][2];
        Row[1][2] = (cos * temp1) - (sin * temp2);
        Row[2][2] = (sin * temp1) + (cos * temp2);

        temp1 = Row[1][3];
        temp2 = Row[2][3];
        Row[1][3] = (cos * temp1) - (sin * temp2);
        Row[2][3] = (sin * temp1) + (cos * temp2);
    }

    /// <summary>Applies a pre-rotation around the Y-axis by the specified angle.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    /// <remarks>Pre-rotations are applied in object space rather than world space, affecting the interpretation of subsequent transformations.</remarks>
    public void PreRotateY(float theta) => PreRotateY(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a pre-rotation around the Y-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>Pre-rotations modify the coordinate system for subsequent transformations. This includes the translation component.</remarks>
    public void PreRotateY(float sin, float cos)
    {
        var temp1 = Row[0][0];
        var temp2 = Row[2][0];
        Row[0][0] = (cos * temp1) + (sin * temp2);
        Row[2][0] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[0][1];
        temp2 = Row[2][1];
        Row[0][1] = (cos * temp1) + (sin * temp2);
        Row[2][1] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[0][2];
        temp2 = Row[2][2];
        Row[0][2] = (cos * temp1) + (sin * temp2);
        Row[2][2] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[0][3];
        temp2 = Row[2][3];
        Row[0][3] = (cos * temp1) + (sin * temp2);
        Row[2][3] = (-sin * temp1) + (cos * temp2);
    }

    /// <summary>Applies a pre-rotation around the Z-axis by the specified angle.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    /// <remarks>Pre-rotations are applied in object space rather than world space, affecting the interpretation of subsequent transformations.</remarks>
    public void PreRotateZ(float theta) => PreRotateZ(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a pre-rotation around the Z-axis using precomputed sine and cosine values.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>Pre-rotations modify the coordinate system for subsequent transformations. This includes the translation component.</remarks>
    public void PreRotateZ(float sin, float cos)
    {
        var temp1 = Row[0][0];
        var temp2 = Row[1][0];
        Row[0][0] = (cos * temp1) - (sin * temp2);
        Row[1][0] = (sin * temp1) + (cos * temp2);

        temp1 = Row[0][1];
        temp2 = Row[1][1];
        Row[0][1] = (cos * temp1) - (sin * temp2);
        Row[1][1] = (sin * temp1) + (cos * temp2);

        temp1 = Row[0][2];
        temp2 = Row[1][2];
        Row[0][2] = (cos * temp1) - (sin * temp2);
        Row[1][2] = (sin * temp1) + (cos * temp2);

        temp1 = Row[0][3];
        temp2 = Row[1][3];
        Row[0][3] = (cos * temp1) - (sin * temp2);
        Row[1][3] = (sin * temp1) + (cos * temp2);
    }

    /// <summary>Applies a pre-rotation around the X-axis by the specified angle, excluding translation from the transformation.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    /// <remarks>This variant of pre-rotation affects only the rotation/scale portion of the matrix, leaving translation unchanged.</remarks>
    public void PreRotateXInPlace(float theta) => PreRotateXInPlace(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a pre-rotation around the X-axis using precomputed sine and cosine values, excluding translation.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>This method rotates only the basis vectors without affecting the translation component.</remarks>
    public void PreRotateXInPlace(float sin, float cos)
    {
        var temp1 = Row[1][0];
        var temp2 = Row[2][0];
        Row[1][0] = (cos * temp1) - (sin * temp2);
        Row[2][0] = (sin * temp1) + (cos * temp2);

        temp1 = Row[1][1];
        temp2 = Row[2][1];
        Row[1][1] = (cos * temp1) - (sin * temp2);
        Row[2][1] = (sin * temp1) + (cos * temp2);

        temp1 = Row[1][2];
        temp2 = Row[2][2];
        Row[1][2] = (cos * temp1) - (sin * temp2);
        Row[2][2] = (sin * temp1) + (cos * temp2);
    }

    /// <summary>Applies a pre-rotation around the Y-axis by the specified angle, excluding translation from the transformation.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    /// <remarks>This variant of pre-rotation affects only the rotation/scale portion of the matrix, leaving translation unchanged.</remarks>
    public void PreRotateYInPlace(float theta) => PreRotateYInPlace(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a pre-rotation around the Y-axis using precomputed sine and cosine values, excluding translation.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>This method rotates only the basis vectors without affecting the translation component.</remarks>
    public void PreRotateYInPlace(float sin, float cos)
    {
        var temp1 = Row[0][0];
        var temp2 = Row[2][0];
        Row[0][0] = (cos * temp1) + (sin * temp2);
        Row[2][0] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[0][1];
        temp2 = Row[2][1];
        Row[0][1] = (cos * temp1) + (sin * temp2);
        Row[2][1] = (-sin * temp1) + (cos * temp2);

        temp1 = Row[0][2];
        temp2 = Row[2][2];
        Row[0][2] = (cos * temp1) + (sin * temp2);
        Row[2][2] = (-sin * temp1) + (cos * temp2);
    }

    /// <summary>Applies a pre-rotation around the Z-axis by the specified angle, excluding translation from the transformation.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    /// <remarks>This variant of pre-rotation affects only the rotation/scale portion of the matrix, leaving translation unchanged.</remarks>
    public void PreRotateZInPlace(float theta) => PreRotateZInPlace(float.Sin(theta), float.Cos(theta));

    /// <summary>Applies a pre-rotation around the Z-axis using precomputed sine and cosine values, excluding translation.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    /// <remarks>This method rotates only the basis vectors without affecting the translation component.</remarks>
    public void PreRotateZInPlace(float sin, float cos)
    {
        var temp1 = Row[0][0];
        var temp2 = Row[1][0];
        Row[0][0] = (cos * temp1) - (sin * temp2);
        Row[1][0] = (sin * temp1) + (cos * temp2);

        temp1 = Row[0][1];
        temp2 = Row[1][1];
        Row[0][1] = (cos * temp1) - (sin * temp2);
        Row[1][1] = (sin * temp1) + (cos * temp2);

        temp1 = Row[0][2];
        temp2 = Row[1][2];
        Row[0][2] = (cos * temp1) - (sin * temp2);
        Row[1][2] = (sin * temp1) + (cos * temp2);
    }

    /// <summary>Adjusts the translation of the matrix by adding the specified translation vector.</summary>
    /// <param name="translation">The translation vector to add to the current translation.</param>
    /// <remarks>This is equivalent to calling <see cref="AdjustXTranslation"/>, <see cref="AdjustYTranslation"/>, and <see cref="AdjustZTranslation"/> individually.</remarks>
    public void AdjustTranslation([NotNull] Vector3 translation)
    {
        AdjustXTranslation(translation.X);
        AdjustYTranslation(translation.Y);
        AdjustZTranslation(translation.Z);
    }

    /// <summary>Adjusts the X translation component by adding the specified amount.</summary>
    /// <param name="x">The amount to add to the current X translation.</param>
    public void AdjustXTranslation(float x) => XTranslation += x;

    /// <summary>Adjusts the Y translation component by adding the specified amount.</summary>
    /// <param name="y">The amount to add to the current Y translation.</param>
    public void AdjustYTranslation(float y) => YTranslation += y;

    /// <summary>Adjusts the Z translation component by adding the specified amount.</summary>
    /// <param name="z">The amount to add to the current Z translation.</param>
    public void AdjustZTranslation(float z) => ZTranslation += z;

    /// <summary>Transforms an array of vectors using the matrix and returns the results as a collection.</summary>
    /// <param name="vectors">The array of vectors to transform.</param>
    /// <returns>A collection containing the transformed vectors.</returns>
    /// <remarks>Each vector is transformed using the full matrix transformation including translation.</remarks>
    public ICollection<Vector3> Multiply([NotNull] Vector3[] vectors)
    {
        var result = new Vector3[vectors.Length];
        for (var i = 0; i < vectors.Length; i++)
        {
            result[i] = new Vector3(
                (Row[0].X * vectors[i].X) + (Row[0].Y * vectors[i].Y) + (Row[0].Z * vectors[i].Z) + Row[0].W,
                (Row[1].X * vectors[i].X) + (Row[1].Y * vectors[i].Y) + (Row[1].Z * vectors[i].Z) + Row[1].W,
                (Row[2].X * vectors[i].X) + (Row[2].Y * vectors[i].Y) + (Row[2].Z * vectors[i].Z) + Row[2].W
            );
        }

        return result;
    }

    /// <summary>Copies the 3x3 rotation portion from a collection of float arrays, setting translation to zero.</summary>
    /// <param name="matrix">Collection containing at least 3 float arrays, each with at least 3 elements, representing a 3x3 matrix.</param>
    /// <remarks>The fourth column (translation) is set to zero. This is useful for importing rotation-only transformations.</remarks>
    public void Copy3X3Matrix([NotNull] IReadOnlyCollection<float[]> matrix)
    {
        Row[0][0] = matrix.ElementAt(0)[0];
        Row[0][1] = matrix.ElementAt(0)[1];
        Row[0][2] = matrix.ElementAt(0)[2];
        Row[0][3] = 0F;

        Row[1][0] = matrix.ElementAt(1)[0];
        Row[1][1] = matrix.ElementAt(1)[1];
        Row[1][2] = matrix.ElementAt(1)[2];
        Row[1][3] = 0F;

        Row[2][0] = matrix.ElementAt(2)[0];
        Row[2][1] = matrix.ElementAt(2)[1];
        Row[2][2] = matrix.ElementAt(2)[2];
        Row[2][3] = 0F;
    }

    /// <summary>Transforms an axis-aligned bounding box defined by minimum and maximum corners.</summary>
    /// <param name="min">The minimum corner of the axis-aligned bounding box.</param>
    /// <param name="max">The maximum corner of the axis-aligned bounding box.</param>
    /// <returns>A tuple containing the transformed minimum and maximum corners of the bounding box.</returns>
    /// <remarks>The result may not be axis-aligned after transformation. This method computes the actual extent of the transformed box.</remarks>
    public (Vector3 Min, Vector3 Max) TransformMinMaxAxisAlignedBox([NotNull] Vector3 min, [NotNull] Vector3 max)
    {
        Vector3 minResult = new();
        Vector3 maxResult = new();

        minResult.X = maxResult.X = Row[0][3];
        minResult.Y = maxResult.Y = Row[1][3];
        minResult.Z = maxResult.Z = Row[2][3];

        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                var temp0 = Row[i][j] * min[j];
                var temp1 = Row[i][j] * max[j];
                if (temp0 < temp1)
                {
                    minResult[i] += temp0;
                    maxResult[i] += temp1;
                }
                else
                {
                    minResult[i] += temp1;
                    maxResult[i] += temp0;
                }
            }
        }

        return (minResult, maxResult);
    }

    /// <summary>Transforms an axis-aligned bounding box defined by center and extent vectors.</summary>
    /// <param name="center">The center point of the axis-aligned bounding box.</param>
    /// <param name="extent">The extent (half-sizes) of the bounding box along each axis.</param>
    /// <returns>A tuple containing the transformed center and extent of the bounding box.</returns>
    /// <remarks>This is often more efficient than the min-max variant when working with center-extent representations.</remarks>
    public (Vector3 Center, Vector3 Extent) TransformCenterExtentAxisAlignedBox(
        [NotNull] Vector3 center,
        [NotNull] Vector3 extent
    )
    {
        Vector3 centerResult = new();
        Vector3 extentResult = new();
        for (var i = 0; i < 3; i++)
        {
            centerResult[i] = Row[i][3];
            extentResult[i] = 0F;
            for (var j = 0; j < 3; j++)
            {
                centerResult[i] += Row[i][j] * center[j];
                extentResult[i] += float.Abs(Row[i][j] * extent[j]);
            }
        }

        return (centerResult, extentResult);
    }

    /// <summary>Re-orthogonalizes the matrix to correct numerical drift and ensure it represents a valid rotation matrix.</summary>
    /// <remarks>Over time, floating-point operations can cause rotation matrices to lose orthogonality. This method uses Gram-Schmidt orthogonalization to restore proper orthonormal basis vectors. If orthogonalization fails, the matrix is reset to identity.</remarks>
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

        x *= 1F / len;
        len = y.Length;
        if (len < float.Epsilon)
        {
            MakeIdentity();
            return;
        }

        y *= 1F / len;
        len = z.Length;
        if (len < float.Epsilon)
        {
            MakeIdentity();
            return;
        }

        z *= 1F / len;

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
    /// <returns><c>true</c> if the specified object is a Matrix3D and is equal to the current matrix; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is Matrix3D matrix && Equals(this, matrix);

    /// <summary>Determines whether two Matrix3D instances are equal by comparing all their components.</summary>
    /// <param name="x">The first matrix to compare.</param>
    /// <param name="y">The second matrix to compare.</param>
    /// <returns><c>true</c> if both matrices are null, refer to the same instance, or have identical components; otherwise, <c>false</c>.</returns>
    public bool Equals(Matrix3D? x, Matrix3D? y) =>
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
    public int GetHashCode([NotNull] Matrix3D obj) => obj.Row.GetHashCode();

    /// <summary>Returns a string representation of the matrix in the format "({Row0}, {Row1}, {Row2})".</summary>
    /// <returns>A string that represents the matrix with all three rows displayed.</returns>
    public override string ToString() => $"({Row[0]}, {Row[1]}, {Row[2]})";

    /// <summary>Returns a string representation of the matrix using the specified format provider.</summary>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the matrix formatted according to the specified format provider.</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({Row[0].ToString(formatProvider)}, {Row[1].ToString(formatProvider)}, {Row[2].ToString(formatProvider)})";

    /// <summary>Returns a string representation of the matrix using the specified format and format provider.</summary>
    /// <param name="format">A numeric format string applied to each component.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the matrix with each component formatted according to the specified format and provider.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({Row[0].ToString(format, formatProvider)}, {Row[1].ToString(format, formatProvider)}, {Row[2].ToString(format, formatProvider)})";
}
