// -----------------------------------------------------------------------
// <copyright file="Vector4.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a 4-dimensional vector with X, Y, Z, and W components.</summary>
/// <remarks>This class implements <see cref="IEqualityComparer{Vector4}"/> to provide comparison functionality between Vector4 instances.</remarks>
public class Vector4 : IEqualityComparer<Vector4>
{
    /// <summary>Initializes a new instance of the <see cref="Vector4"/> class with all components set to zero.</summary>
    public Vector4() { }

    /// <summary>Initializes a new instance of the <see cref="Vector4"/> class by copying values from another Vector4 instance.</summary>
    /// <param name="other">The Vector4 to copy from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
    public Vector4([NotNull] Vector4 other) => (X, Y, Z, W) = (other.X, other.Y, other.Z, other.W);

    /// <summary>Initializes a new instance of the <see cref="Vector4"/> class with the specified component values.</summary>
    /// <param name="x">The X component value.</param>
    /// <param name="y">The Y component value.</param>
    /// <param name="z">The Z component value.</param>
    /// <param name="w">The W component value.</param>
    public Vector4(float x, float y, float z, float w) => Set(x, y, z, w);

    /// <summary>Initializes a new instance of the <see cref="Vector4"/> class from an array of float values.</summary>
    /// <param name="values">An array containing at least 4 float values for X, Y, Z, and W components.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array contains fewer than 4 elements.</exception>
    public Vector4([NotNull] float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 4);
        (X, Y, Z, W) = (values[0], values[1], values[2], values[3]);
    }

    /// <summary>Gets or sets the X component of the vector.</summary>
    /// <value>The X coordinate as a float value.</value>
    public float X { get; set; }

    /// <summary>Gets or sets the Y component of the vector.</summary>
    /// <value>The Y coordinate as a float value.</value>
    public float Y { get; set; }

    /// <summary>Gets or sets the Z component of the vector.</summary>
    /// <value>The Z coordinate as a float value.</value>
    public float Z { get; set; }

    /// <summary>Gets or sets the W component of the vector.</summary>
    /// <value>The W coordinate as a float value.</value>
    public float W { get; set; }

    /// <summary>Gets a value indicating whether all components of the vector are valid (not NaN).</summary>
    /// <value><c>true</c> if all components are valid numbers; otherwise, <c>false</c>.</value>
    public bool IsValid => !float.IsNaN(X) && !float.IsNaN(Y) && !float.IsNaN(Z) && !float.IsNaN(W);

    /// <summary>Gets the Euclidean length (magnitude) of the vector.</summary>
    /// <value>The square root of the sum of squares of all components.</value>
    public float Length => float.Sqrt(Length2);

    /// <summary>Gets the squared length of the vector.</summary>
    /// <value>The sum of squares of all components without taking the square root.</value>
    /// <remarks>This is more efficient than <see cref="Length"/> when only comparing magnitudes.</remarks>
    public float Length2 => float.Pow(X, 2) + float.Pow(Y, 2) + float.Pow(Z, 2) + float.Pow(W, 2);

    /// <summary>Gets or sets the component at the specified index.</summary>
    /// <param name="index">The zero-based index of the component (0=X, 1=Y, 2=Z, 3=W).</param>
    /// <value>The value of the component at the specified index.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is not between 0 and 3.</exception>
    public float this[int index]
    {
        get =>
            index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => W,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be between 0 and 3."),
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
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be between 0 and 3.");
            }
        }
    }

    /// <summary>Adds two vectors component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new Vector4 with components that are the sum of the corresponding components of the input vectors.</returns>
    public static Vector4 operator +(Vector4 left, Vector4 right) => Add(left, right);

    /// <summary>Subtracts the second vector from the first vector component-wise.</summary>
    /// <param name="left">The vector to subtract from.</param>
    /// <param name="right">The vector to subtract.</param>
    /// <returns>A new Vector4 with components that are the difference of the corresponding components of the input vectors.</returns>
    public static Vector4 operator -(Vector4 left, Vector4 right) => Subtract(left, right);

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <returns>A new Vector4 with each component multiplied by the scalar value.</returns>
    public static Vector4 operator *(Vector4 value, float scale) => Multiply(value, scale);

    /// <summary>Multiplies a scalar value by a vector.</summary>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <param name="right">The vector to scale.</param>
    /// <returns>A new Vector4 with each component multiplied by the scalar value.</returns>
    public static Vector4 operator *(float scale, Vector4 right) => Multiply(scale, right);

    /// <summary>Calculates the dot product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product of the two vectors as a float value.</returns>
    public static float operator *(Vector4 left, Vector4 right) => Multiply(left, right);

    /// <summary>Divides the first vector by the second vector component-wise.</summary>
    /// <param name="left">The dividend vector.</param>
    /// <param name="right">The divisor vector.</param>
    /// <returns>A new Vector4 with each component of the first vector divided by the corresponding component of the second vector.</returns>
    public static Vector4 operator /(Vector4 left, Vector4 right) => Divide(left, right);

    /// <summary>Negates all components of the vector.</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A new Vector4 with all components negated.</returns>
    public static Vector4 operator -(Vector4 value) => Negate(value);

    /// <summary>Returns a copy of the vector unchanged.</summary>
    /// <param name="value">The vector to return.</param>
    /// <returns>The same vector instance unchanged.</returns>
    public static Vector4 operator +(Vector4 value) => Plus(value);

    /// <summary>Determines whether two vectors are equal.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><c>true</c> if the vectors are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Vector4 left, Vector4 right) => object.Equals(left, right);

    /// <summary>Determines whether two vectors are not equal.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><c>true</c> if the vectors are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Vector4 left, Vector4 right) => !object.Equals(left, right);

    /// <summary>Creates a new vector with all components negated.</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A new Vector4 with all components negated.</returns>
    public static Vector4 Negate([NotNull] Vector4 value) => new(-value.X, -value.Y, -value.Z, -value.W);

    /// <summary>Returns the vector unchanged.</summary>
    /// <param name="value">The vector to return.</param>
    /// <returns>The same vector instance unchanged.</returns>
    public static Vector4 Plus(Vector4 value) => value;

    /// <summary>Adds two vectors component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new Vector4 containing the sum of the input vectors' components.</returns>
    public static Vector4 Add([NotNull] Vector4 left, [NotNull] Vector4 right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    /// <summary>Subtracts the second vector from the first vector component-wise.</summary>
    /// <param name="left">The vector to subtract from.</param>
    /// <param name="right">The vector to subtract.</param>
    /// <returns>A new Vector4 containing the difference of the input vectors' components.</returns>
    public static Vector4 Subtract([NotNull] Vector4 left, [NotNull] Vector4 right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    /// <summary>Multiplies each component of the vector by a scalar value.</summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The scalar multiplier.</param>
    /// <returns>A new Vector4 with each component multiplied by the scalar value.</returns>
    public static Vector4 Multiply([NotNull] Vector4 value, float scale) =>
        new(value.X * scale, value.Y * scale, value.Z * scale, value.W * scale);

    /// <summary>Multiplies each component of the vector by a scalar value.</summary>
    /// <param name="scale">The scalar multiplier.</param>
    /// <param name="right">The vector to scale.</param>
    /// <returns>A new Vector4 with each component multiplied by the scalar value.</returns>
    public static Vector4 Multiply(float scale, Vector4 right) => Multiply(right, scale);

    /// <summary>Calculates the dot product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product as a float value.</returns>
    public static float Multiply(Vector4 left, Vector4 right) => DotProduct(left, right);

    /// <summary>Divides the first vector by the second vector component-wise.</summary>
    /// <param name="left">The dividend vector.</param>
    /// <param name="right">The divisor vector.</param>
    /// <returns>A new Vector4 with each component of the first vector divided by the corresponding component of the second vector.</returns>
    /// <remarks>This implementation uses the reciprocal of the X component of the divisor for all divisions, which may not be the intended behavior.</remarks>
    public static Vector4 Divide([NotNull] Vector4 left, [NotNull] Vector4 right)
    {
        var oK = 1F / right.X;
        return new Vector4(left.X * oK, left.Y * oK, left.Z * oK, left.W * oK);
    }

    /// <summary>Calculates the dot product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The sum of the products of corresponding components.</returns>
    public static float DotProduct([NotNull] Vector4 left, [NotNull] Vector4 right) =>
        (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);

    /// <summary>Performs linear interpolation between two vectors.</summary>
    /// <param name="left">The first vector (when amount is 0).</param>
    /// <param name="right">The second vector (when amount is 1).</param>
    /// <param name="amount">The interpolation factor between 0 and 1.</param>
    /// <returns>A new Vector4 representing the interpolated result.</returns>
    public static Vector4 Lerp([NotNull] Vector4 left, [NotNull] Vector4 right, float amount) =>
        new(
            left.X + ((right.X - left.X) * amount),
            left.Y + ((right.Y - left.Y) * amount),
            left.Z + ((right.Z - left.Z) * amount),
            left.W + ((right.W - left.W) * amount)
        );

    /// <summary>Sets all components of the vector to the specified values.</summary>
    /// <param name="x">The X component value.</param>
    /// <param name="y">The Y component value.</param>
    /// <param name="z">The Z component value.</param>
    /// <param name="w">The W component value.</param>
    public void Set(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);

    /// <summary>Normalizes the vector to have a length of 1.</summary>
    /// <remarks>If the vector's squared length is very close to zero (within epsilon), the vector remains unchanged to avoid division by zero.</remarks>
    public void Normalize()
    {
        var len2 = Length2;
        if (float.Abs(len2) >= float.Epsilon)
        {
            return;
        }

        var oLen = ExtraMath.InvSqrt(len2);
        X *= oLen;
        Y *= oLen;
        Z *= oLen;
        W *= oLen;
    }

    /// <summary>Determines whether the specified object is equal to the current Vector4.</summary>
    /// <param name="obj">The object to compare with the current Vector4.</param>
    /// <returns><c>true</c> if the specified object is equal to the current Vector4; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is Vector4 other && Equals(this, other);

    /// <summary>Determines whether two Vector4 instances are equal within floating-point epsilon tolerance.</summary>
    /// <param name="x">The first Vector4 to compare.</param>
    /// <param name="y">The second Vector4 to compare.</param>
    /// <returns><c>true</c> if the vectors are equal within epsilon tolerance; otherwise, <c>false</c>.</returns>
    public bool Equals(Vector4? x, Vector4? y) =>
        ReferenceEquals(x, y)
        || (
            x is not null
            && y is not null
            && x.GetType() == y.GetType()
            && float.Abs(x.X - y.X) < float.Epsilon
            && float.Abs(x.Y - y.Y) < float.Epsilon
            && float.Abs(x.Z - y.Z) < float.Epsilon
            && float.Abs(x.W - y.W) < float.Epsilon
        );

    /// <summary>Returns a hash code for the current Vector4.</summary>
    /// <returns>A hash code for the current Vector4 based on its component values.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns a hash code for the specified Vector4.</summary>
    /// <param name="obj">The Vector4 for which to get a hash code.</param>
    /// <returns>A hash code for the specified Vector4 based on its component values.</returns>
    public int GetHashCode([NotNull] Vector4 obj) => HashCode.Combine(obj.X, obj.Y, obj.Z, obj.W);

    /// <summary>Returns a string representation of the vector in the format "(X, Y, Z, W)".</summary>
    /// <returns>A string representation of the vector.</returns>
    public override string ToString() => $"({X}, {Y}, {Z}, {W})";

    /// <summary>Returns a string representation of the vector using the specified format provider.</summary>
    /// <param name="formatProvider">The format provider to use for formatting the component values.</param>
    /// <returns>A string representation of the vector with formatted component values.</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({X.ToString(formatProvider)}, {Y.ToString(formatProvider)}, {Z.ToString(formatProvider)}, {W.ToString(formatProvider)})";

    /// <summary>Returns a string representation of the vector using the specified format string and format provider.</summary>
    /// <param name="format">The format string to apply to each component.</param>
    /// <param name="formatProvider">The format provider to use for formatting the component values.</param>
    /// <returns>A string representation of the vector with formatted component values.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)}, {Z.ToString(format, formatProvider)}, {W.ToString(format, formatProvider)})";
}
