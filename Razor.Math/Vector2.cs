// -----------------------------------------------------------------------
// <copyright file="Vector2.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a 2D vector with X and Y components, providing mathematical operations and utility methods for 2D vector calculations.</summary>
public class Vector2 : IEqualityComparer<Vector2>
{
    /// <summary>Initializes a new instance of the <see cref="Vector2"/> class with default values (0, 0).</summary>
    public Vector2() { }

    /// <summary>Initializes a new instance of the <see cref="Vector2"/> class by copying values from another vector.</summary>
    /// <param name="other">The vector to copy values from.</param>
    public Vector2([NotNull] Vector2 other) => Set(other);

    /// <summary>Initializes a new instance of the <see cref="Vector2"/> class with specified X and Y components.</summary>
    /// <param name="x">The X component of the vector.</param>
    /// <param name="y">The Y component of the vector.</param>
    public Vector2(float x, float y) => Set(x, y);

    /// <summary>Initializes a new instance of the <see cref="Vector2"/> class from an array of float values.</summary>
    /// <param name="values">An array containing at least 2 float values, where the first two elements represent X and Y components.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array contains fewer than 2 elements.</exception>
    public Vector2([NotNull] float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 2);
        (X, Y) = (values[0], values[1]);
    }

    /// <summary>Gets or sets the X component of the vector.</summary>
    public float X { get; set; }

    /// <summary>Gets or sets the Y component of the vector.</summary>
    public float Y { get; set; }

    /// <summary>Gets or sets the U component of the vector (alias for X component, commonly used for texture coordinates).</summary>
    public float U
    {
        get => X;
        set => X = value;
    }

    /// <summary>Gets or sets the V component of the vector (alias for Y component, commonly used for texture coordinates).</summary>
    public float V
    {
        get => Y;
        set => Y = value;
    }

    /// <summary>Gets the length (magnitude) of the vector.</summary>
    public float Length => float.Sqrt(Length2);

    /// <summary>Gets the squared length of the vector, which is more efficient to calculate than the actual length.</summary>
    public float Length2 => float.Pow(X, 2) + float.Pow(Y, 2);

    /// <summary>Gets a value indicating whether the vector contains valid components (not NaN or infinite).</summary>
    public bool IsValid => ExtraMath.IsValid(X) && ExtraMath.IsValid(Y);

    /// <summary>Gets or sets the vector component at the specified index.</summary>
    /// <param name="index">The zero-based index of the component (0 for X, 1 for Y).</param>
    /// <returns>The component value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is not 0 or 1.</exception>
    public float this[int index]
    {
        get =>
            index switch
            {
                0 => X,
                1 => Y,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The index must be between 0 and 1."),
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, "The index must be between 0 and 1.");
            }
        }
    }

    /// <summary>Adds two vectors component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new vector representing the sum of the two input vectors.</returns>
    public static Vector2 operator +(Vector2 left, Vector2 right) => Add(left, right);

    /// <summary>Subtracts the second vector from the first vector component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector to subtract.</param>
    /// <returns>A new vector representing the difference between the two input vectors.</returns>
    public static Vector2 operator -(Vector2 left, Vector2 right) => Subtract(left, right);

    /// <summary>Computes the dot product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public static float operator *(Vector2 left, Vector2 right) => Multiply(left, right);

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="value">The vector to multiply.</param>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <returns>A new vector with each component multiplied by the scalar.</returns>
    public static Vector2 operator *(Vector2 value, float scale) => Multiply(value, scale);

    /// <summary>Multiplies a scalar value by a vector.</summary>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <param name="value">The vector to multiply.</param>
    /// <returns>A new vector with each component multiplied by the scalar.</returns>
    public static Vector2 operator *(float scale, Vector2 value) => Multiply(value, scale);

    /// <summary>Divides a vector by a scalar value.</summary>
    /// <param name="value">The vector to divide.</param>
    /// <param name="scale">The scalar value to divide by.</param>
    /// <returns>A new vector with each component divided by the scalar.</returns>
    public static Vector2 operator /(Vector2 value, float scale) => Divide(value, scale);

    /// <summary>Negates a vector (multiplies each component by -1).</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A new vector with negated components.</returns>
    public static Vector2 operator -(Vector2 value) => Negate(value);

    /// <summary>Returns the same vector unchanged (unary plus operator).</summary>
    /// <param name="value">The vector to return.</param>
    /// <returns>The same vector instance.</returns>
    public static Vector2 operator +(Vector2 value) => Plus(value);

    /// <summary>Determines whether two vectors are equal within floating-point epsilon tolerance.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Vector2 left, Vector2 right) => object.Equals(left, right);

    /// <summary>Determines whether two vectors are not equal within floating-point epsilon tolerance.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Vector2 left, Vector2 right) => !object.Equals(left, right);

    /// <summary>Negates a vector (multiplies each component by -1).</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A new vector with negated components.</returns>
    public static Vector2 Negate([NotNull] Vector2 value) => new(-value.X, -value.Y);

    /// <summary>Returns the same vector unchanged.</summary>
    /// <param name="value">The vector to return.</param>
    /// <returns>The same vector instance.</returns>
    public static Vector2 Plus(Vector2 value) => value;

    /// <summary>Adds two vectors component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new vector representing the sum of the two input vectors.</returns>
    public static Vector2 Add([NotNull] Vector2 left, [NotNull] Vector2 right) =>
        new(left.X + right.X, left.Y + right.Y);

    /// <summary>Subtracts the second vector from the first vector component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector to subtract.</param>
    /// <returns>A new vector representing the difference between the two input vectors.</returns>
    public static Vector2 Subtract([NotNull] Vector2 left, [NotNull] Vector2 right) =>
        new(left.X - right.X, left.Y - right.Y);

    /// <summary>Computes the dot product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public static float Multiply(Vector2 left, Vector2 right) => DotProduct(left, right);

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="value">The vector to multiply.</param>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <returns>A new vector with each component multiplied by the scalar.</returns>
    public static Vector2 Multiply([NotNull] Vector2 value, float scale) => new(value.X * scale, value.Y * scale);

    /// <summary>Multiplies a scalar value by a vector.</summary>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <param name="value">The vector to multiply.</param>
    /// <returns>A new vector with each component multiplied by the scalar.</returns>
    public static Vector2 Multiply(float scale, Vector2 value) => Multiply(value, scale);

    /// <summary>Divides a vector by a scalar value.</summary>
    /// <param name="value">The vector to divide.</param>
    /// <param name="scale">The scalar value to divide by.</param>
    /// <returns>A new vector with each component divided by the scalar.</returns>
    public static Vector2 Divide([NotNull] Vector2 value, float scale)
    {
        scale = 1F / scale;
        return new Vector2(value.X * scale, value.Y * scale);
    }

    /// <summary>Computes the dot product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product of the two vectors (left.X * right.X + left.Y * right.Y).</returns>
    public static float DotProduct([NotNull] Vector2 left, [NotNull] Vector2 right) =>
        (left.X * right.X) + (left.Y * right.Y);

    /// <summary>Computes the perpendicular dot product (cross product in 2D) of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The perpendicular dot product of the two vectors (left.X * -right.Y + left.Y * right.X).</returns>
    public static float PerpendicularDotProduct([NotNull] Vector2 left, [NotNull] Vector2 right) =>
        (left.X * -right.Y) + (left.Y * right.X);

    /// <summary>Normalizes a vector to unit length.</summary>
    /// <param name="value">The vector to normalize.</param>
    /// <returns>A new vector with the same direction but unit length, or zero vector if the input has zero length.</returns>
    public static Vector2 Normalize([NotNull] Vector2 value)
    {
        var len2 = value.Length2;
        if (float.Abs(len2) >= float.Epsilon)
        {
            return new Vector2(0F, 0F);
        }

        var oLen = ExtraMath.InvSqrt(len2);
        return value / oLen;
    }

    /// <summary>Computes an approximate distance between two points using a fast algorithm that avoids square root calculation.</summary>
    /// <param name="x1">The X coordinate of the first point.</param>
    /// <param name="y1">The Y coordinate of the first point.</param>
    /// <param name="x2">The X coordinate of the second point.</param>
    /// <param name="y2">The Y coordinate of the second point.</param>
    /// <returns>An approximate distance between the two points.</returns>
    public static float QuickDistance(float x1, float y1, float x2, float y2)
    {
        var xDiff = float.Abs(x1 - x2);
        var yDiff = float.Abs(y1 - y2);
        return xDiff > yDiff ? yDiff / 2F * xDiff : xDiff / 2F * yDiff;
    }

    /// <summary>Computes an approximate distance between two vectors using a fast algorithm that avoids square root calculation.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>An approximate distance between the two vectors.</returns>
    public static float QuickDistance([NotNull] Vector2 left, [NotNull] Vector2 right) =>
        QuickDistance(left.X, left.Y, right.X, right.Y);

    /// <summary>Computes the exact Euclidean distance between two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The Euclidean distance between the two vectors.</returns>
    public static float Distance([NotNull] Vector2 left, [NotNull] Vector2 right)
    {
        Vector2 temp = new(left - right);
        return temp.Length;
    }

    /// <summary>Computes the exact Euclidean distance between two points.</summary>
    /// <param name="x1">The X coordinate of the first point.</param>
    /// <param name="y1">The Y coordinate of the first point.</param>
    /// <param name="x2">The X coordinate of the second point.</param>
    /// <param name="y2">The Y coordinate of the second point.</param>
    /// <returns>The Euclidean distance between the two points.</returns>
    public static float Distance(float x1, float y1, float x2, float y2)
    {
        var xDiff = x1 - x2;
        var yDiff = y1 - y2;
        return float.Sqrt(float.Pow(xDiff, 2) + float.Pow(yDiff, 2));
    }

    /// <summary>Performs linear interpolation between two vectors.</summary>
    /// <param name="left">The starting vector (when amount is 0).</param>
    /// <param name="right">The ending vector (when amount is 1).</param>
    /// <param name="amount">The interpolation factor between 0 and 1.</param>
    /// <returns>A new vector representing the interpolated result.</returns>
    public static Vector2 Lerp([NotNull] Vector2 left, [NotNull] Vector2 right, float amount) =>
        new(left.X + ((right.X - left.X) * amount), left.Y + ((right.Y - left.Y) * amount));

    /// <summary>Sets the X and Y components of this vector to the specified values.</summary>
    /// <param name="x">The new X component value.</param>
    /// <param name="y">The new Y component value.</param>
    public void Set(float x, float y) => (X, Y) = (x, y);

    /// <summary>Sets the X and Y components of this vector to match another vector.</summary>
    /// <param name="other">The vector to copy values from.</param>
    public void Set([NotNull] Vector2 other) => (X, Y) = (other.X, other.Y);

    /// <summary>Normalizes this vector to unit length in-place.</summary>
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
    }

    /// <summary>Rotates this vector by the specified angle in radians.</summary>
    /// <param name="theta">The rotation angle in radians.</param>
    public void Rotate(float theta) => Rotate(float.Sin(theta), float.Cos(theta));

    /// <summary>Rotates this vector using pre-calculated sine and cosine values for better performance.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public void Rotate(float sin, float cos)
    {
        var newX = (X * cos) + (Y * -sin);
        var newY = (X * sin) + (Y * cos);
        X = newX;
        Y = newY;
    }

    /// <summary>Rotates this vector towards a target vector by a maximum angle, clamping the rotation if the target is reached.</summary>
    /// <param name="target">The target vector to rotate towards.</param>
    /// <param name="maxTheta">The maximum rotation angle in radians.</param>
    /// <param name="positiveTurn">Outputs whether the rotation was in the positive direction.</param>
    /// <returns><see langword="true"/> if the target was reached; otherwise, <see langword="false"/>.</returns>
    public bool RotateTowards(Vector2 target, float maxTheta, out bool positiveTurn) =>
        RotateTowards(target, float.Sin(maxTheta), float.Cos(maxTheta), out positiveTurn);

    /// <summary>Rotates this vector towards a target vector using pre-calculated sine and cosine values, clamping the rotation if the target is reached.</summary>
    /// <param name="target">The target vector to rotate towards.</param>
    /// <param name="maxSin">The sine of the maximum rotation angle.</param>
    /// <param name="maxCos">The cosine of the maximum rotation angle.</param>
    /// <param name="positiveTurn">Outputs whether the rotation was in the positive direction.</param>
    /// <returns><see langword="true"/> if the target was reached; otherwise, <see langword="false"/>.</returns>
    public bool RotateTowards(Vector2 target, float maxSin, float maxCos, out bool positiveTurn)
    {
        positiveTurn = PerpendicularDotProduct(target, this) > 0F;
        if (DotProduct(this, target) >= maxCos)
        {
            Set(target);
            return true;
        }

        Rotate(positiveTurn ? maxSin : -maxSin, maxCos);
        return false;
    }

    /// <summary>Updates this vector to contain the minimum X and Y components between this vector and another vector.</summary>
    /// <param name="other">The other vector to compare against.</param>
    public void UpdateMin([NotNull] Vector2 other)
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

    /// <summary>Updates this vector to contain the maximum X and Y components between this vector and another vector.</summary>
    /// <param name="other">The other vector to compare against.</param>
    public void UpdateMax([NotNull] Vector2 other)
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

    /// <summary>Scales this vector by multiplying each component by the corresponding scale factors.</summary>
    /// <param name="x">The scale factor for the X component.</param>
    /// <param name="y">The scale factor for the Y component.</param>
    public void Scale(float x, float y)
    {
        X *= x;
        Y *= y;
    }

    /// <summary>Scales this vector by multiplying each component by the corresponding components of the scale vector.</summary>
    /// <param name="scale">The vector containing scale factors for X and Y components.</param>
    public void Scale([NotNull] Vector2 scale)
    {
        X *= scale.X;
        Y *= scale.Y;
    }

    /// <summary>Determines whether the specified object is equal to this vector within floating-point epsilon tolerance.</summary>
    /// <param name="obj">The object to compare with this vector.</param>
    /// <returns><see langword="true"/> if the specified object is a Vector2 and is equal to this vector; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is Vector2 other && Equals(this, other);

    /// <summary>Determines whether two vectors are equal within floating-point epsilon tolerance.</summary>
    /// <param name="x">The first vector to compare.</param>
    /// <param name="y">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are equal or both are null; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Vector2? x, Vector2? y) =>
        ReferenceEquals(x, y)
        || (
            x is not null
            && y is not null
            && x.GetType() == y.GetType()
            && float.Abs(x.X - y.X) < float.Epsilon
            && float.Abs(x.Y - y.Y) < float.Epsilon
        );

    /// <summary>Returns a hash code for this vector.</summary>
    /// <returns>A hash code based on the X and Y components of the vector.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns a hash code for the specified vector.</summary>
    /// <param name="obj">The vector to get a hash code for.</param>
    /// <returns>A hash code based on the X and Y components of the vector.</returns>
    public int GetHashCode([NotNull] Vector2 obj) => HashCode.Combine(obj.X, obj.Y);

    /// <summary>Returns a string representation of the <see cref="Vector2"/> instance.</summary>
    /// <returns>A string in the format "(X, Y)" where X and Y are the components of the vector.</returns>
    public override string ToString() => $"({X}, {Y})";

    /// <summary>Returns a string representation of the current <see cref="Vector2"/> object.</summary>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string that represents the current object in the format "(X, Y)".</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({X.ToString(formatProvider)}, {Y.ToString(formatProvider)})";

    /// <summary>Returns a string representation of the <see cref="Vector2"/> instance.</summary>
    /// <param name="format">A format string that specifies how the X and Y components are formatted.</param>
    /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
    /// <returns>A string formatted as (X, Y), representing the vector's components.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)})";
}
