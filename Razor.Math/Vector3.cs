// -----------------------------------------------------------------------
// <copyright file="Vector3.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a 3D vector with X, Y, and Z components, providing mathematical operations and utility methods for 3D vector calculations.</summary>
public class Vector3 : IEqualityComparer<Vector3>
{
    /// <summary>Initializes a new instance of the <see cref="Vector3"/> class with default values (0, 0, 0).</summary>
    public Vector3() { }

    /// <summary>Initializes a new instance of the <see cref="Vector3"/> class by copying values from another vector.</summary>
    /// <param name="other">The vector to copy values from.</param>
    public Vector3(Vector3 other) => Set(other);

    /// <summary>Initializes a new instance of the <see cref="Vector3"/> class with specified X, Y, and Z components.</summary>
    /// <param name="x">The X component of the vector.</param>
    /// <param name="y">The Y component of the vector.</param>
    /// <param name="z">The Z component of the vector.</param>
    public Vector3(float x, float y, float z) => Set(x, y, z);

    /// <summary>Initializes a new instance of the <see cref="Vector3"/> class from an array of float values.</summary>
    /// <param name="values">An array containing at least 3 float values, where the first three elements represent X, Y, and Z components.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array contains fewer than 3 elements.</exception>
    public Vector3(float[] values) => Set(values);

    /// <summary>Gets or sets the X component of the vector.</summary>
    public float X { get; set; }

    /// <summary>Gets or sets the Y component of the vector.</summary>
    public float Y { get; set; }

    /// <summary>Gets or sets the Z component of the vector.</summary>
    public float Z { get; set; }

    /// <summary>Gets the length (magnitude) of the vector using the standard Euclidean distance formula.</summary>
    public float Length => float.Sqrt(Length2);

    /// <summary>Gets an approximate length of the vector using a fast algorithm that avoids square root calculation.</summary>
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

            return max + (11F / 32F * mid) + (1F / 4F * min);
        }
    }

    /// <summary>Gets the squared length of the vector, which is more efficient to calculate than the actual length.</summary>
    public float Length2 => float.Pow(X, 2) + float.Pow(Y, 2) + float.Pow(Z, 2);

    /// <summary>Gets a value indicating whether the vector contains valid components (not NaN or infinite).</summary>
    public bool IsValid => ExtraMath.IsValid(X) && ExtraMath.IsValid(Y) && ExtraMath.IsValid(Z);

    /// <summary>Gets or sets the vector component at the specified index.</summary>
    /// <param name="index">The zero-based index of the component (0 for X, 1 for Y, 2 for Z).</param>
    /// <returns>The component value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is not 0, 1, or 2.</exception>
    public float this[int index]
    {
        get =>
            index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be between 0 and 2."),
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
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be between 0 and 2.");
            }
        }
    }

    /// <summary>Adds two vectors component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new vector representing the sum of the two input vectors.</returns>
    public static Vector3 operator +(Vector3 left, Vector3 right) => Add(left, right);

    /// <summary>Subtracts the second vector from the first vector component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector to subtract.</param>
    /// <returns>A new vector representing the difference between the two input vectors.</returns>
    public static Vector3 operator -(Vector3 left, Vector3 right) => Subtract(left, right);

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="value">The vector to multiply.</param>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <returns>A new vector with each component multiplied by the scalar.</returns>
    public static Vector3 operator *(Vector3 value, float scale) => Multiply(value, scale);

    /// <summary>Multiplies a scalar value by a vector.</summary>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <param name="value">The vector to multiply.</param>
    /// <returns>A new vector with each component multiplied by the scalar.</returns>
    public static Vector3 operator *(float scale, Vector3 value) => Multiply(scale, value);

    /// <summary>Computes the dot product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public static float operator *(Vector3 left, Vector3 right) => DotProduct(left, right);

    /// <summary>Divides a vector by a scalar value.</summary>
    /// <param name="value">The vector to divide.</param>
    /// <param name="scale">The scalar value to divide by.</param>
    /// <returns>A new vector with each component divided by the scalar.</returns>
    public static Vector3 operator /(Vector3 value, float scale) => Divide(value, scale);

    /// <summary>Negates a vector (multiplies each component by -1).</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A new vector with negated components.</returns>
    public static Vector3 operator -(Vector3 value) => Negate(value);

    /// <summary>Returns the same vector unchanged (unary plus operator).</summary>
    /// <param name="value">The vector to return.</param>
    /// <returns>The same vector instance.</returns>
    public static Vector3 operator +(Vector3 value) => Plus(value);

    /// <summary>Determines whether two vectors are equal within floating-point epsilon tolerance.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Vector3 left, Vector3 right) => object.Equals(left, right);

    /// <summary>Determines whether two vectors are not equal within floating-point epsilon tolerance.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Vector3 left, Vector3 right) => !object.Equals(left, right);

    /// <summary>Adds two vectors component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new vector representing the sum of the two input vectors.</returns>
    public static Vector3 Add([NotNull] Vector3 left, [NotNull] Vector3 right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>Subtracts the second vector from the first vector component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector to subtract.</param>
    /// <returns>A new vector representing the difference between the two input vectors.</returns>
    public static Vector3 Subtract([NotNull] Vector3 left, [NotNull] Vector3 right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <summary>Multiplies a vector by a scalar value.</summary>
    /// <param name="value">The vector to multiply.</param>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <returns>A new vector with each component multiplied by the scalar.</returns>
    public static Vector3 Multiply([NotNull] Vector3 value, float scale) =>
        new(value.X * scale, value.Y * scale, value.Z * scale);

    /// <summary>Multiplies a scalar value by a vector.</summary>
    /// <param name="scale">The scalar value to multiply by.</param>
    /// <param name="value">The vector to multiply.</param>
    /// <returns>A new vector with each component multiplied by the scalar.</returns>
    public static Vector3 Multiply(float scale, [NotNull] Vector3 value) => Multiply(value, scale);

    /// <summary>Divides a vector by a scalar value.</summary>
    /// <param name="value">The vector to divide.</param>
    /// <param name="scale">The scalar value to divide by.</param>
    /// <returns>A new vector with each component divided by the scalar.</returns>
    public static Vector3 Divide([NotNull] Vector3 value, float scale)
    {
        var oScale = 1F / scale;
        return new Vector3(value.X * oScale, value.Y * oScale, value.Z * oScale);
    }

    /// <summary>Negates a vector (multiplies each component by -1).</summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A new vector with negated components.</returns>
    public static Vector3 Negate([NotNull] Vector3 value) => new(-value.X, -value.Y, -value.Z);

    /// <summary>Returns the same vector unchanged.</summary>
    /// <param name="value">The vector to return.</param>
    /// <returns>The same vector instance.</returns>
    public static Vector3 Plus(Vector3 value) => value;

    /// <summary>Computes the dot product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product of the two vectors (left.X * right.X + left.Y * right.Y + left.Z * right.Z).</returns>
    public static float DotProduct([NotNull] Vector3 left, [NotNull] Vector3 right) =>
        (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);

    /// <summary>Computes the cross product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new vector representing the cross product of the two input vectors.</returns>
    public static Vector3 CrossProduct([NotNull] Vector3 left, [NotNull] Vector3 right) =>
        new(
            (left.Y * right.Z) - (left.Z * right.Y),
            (left.Z * right.X) - (left.X * right.Z),
            (left.X * right.Y) - (left.Y * right.X)
        );

    /// <summary>Computes the normalized cross product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A unit vector representing the normalized cross product of the two input vectors.</returns>
    public static Vector3 CrossProductNormalized(Vector3 left, Vector3 right)
    {
        Vector3 result = CrossProduct(left, right);
        result.Normalize();
        return result;
    }

    /// <summary>Computes the X component of the cross product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The X component of the cross product (left.Y * right.Z - left.Z * right.Y).</returns>
    public static float CrossProductX([NotNull] Vector3 left, [NotNull] Vector3 right) =>
        (left.Y * right.Z) - (left.Z * right.Y);

    /// <summary>Computes the Y component of the cross product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The Y component of the cross product (left.Z * right.X - left.X * right.Z).</returns>
    public static float CrossProductY([NotNull] Vector3 left, [NotNull] Vector3 right) =>
        (left.Z * right.X) - (left.X * right.Z);

    /// <summary>Computes the Z component of the cross product of two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The Z component of the cross product (left.X * right.Y - left.Y * right.X).</returns>
    public static float CrossProductZ([NotNull] Vector3 left, [NotNull] Vector3 right) =>
        (left.X * right.Y) - (left.Y * right.X);

    /// <summary>Performs linear interpolation between two vectors.</summary>
    /// <param name="left">The starting vector (when amount is 0).</param>
    /// <param name="right">The ending vector (when amount is 1).</param>
    /// <param name="amount">The interpolation factor between 0 and 1.</param>
    /// <returns>A new vector representing the interpolated result.</returns>
    public static Vector3 Lerp([NotNull] Vector3 left, [NotNull] Vector3 right, float amount) =>
        new(
            left.X + ((right.X - left.X) * amount),
            left.Y + ((right.Y - left.Y) * amount),
            left.Z + ((right.Z - left.Z) * amount)
        );

    /// <summary>Finds the X coordinate on a line between two points at the specified Y coordinate.</summary>
    /// <param name="y">The Y coordinate to find X for.</param>
    /// <param name="p1">The first point defining the line.</param>
    /// <param name="p2">The second point defining the line.</param>
    /// <returns>The X coordinate at the specified Y coordinate on the line.</returns>
    public static float FindXAtY(float y, [NotNull] Vector3 p1, [NotNull] Vector3 p2) =>
        p1.X + ((y - p1.Y) * ((p2.X - p1.X) / (p2.Y - p1.Y)));

    /// <summary>Finds the X coordinate on a line between two points at the specified Z coordinate.</summary>
    /// <param name="z">The Z coordinate to find X for.</param>
    /// <param name="p1">The first point defining the line.</param>
    /// <param name="p2">The second point defining the line.</param>
    /// <returns>The X coordinate at the specified Z coordinate on the line.</returns>
    public static float FindXAtZ(float z, [NotNull] Vector3 p1, [NotNull] Vector3 p2) =>
        p1.X + ((z - p1.Z) * ((p2.X - p1.X) / (p2.Z - p1.Z)));

    /// <summary>Finds the Y coordinate on a line between two points at the specified X coordinate.</summary>
    /// <param name="x">The X coordinate to find Y for.</param>
    /// <param name="p1">The first point defining the line.</param>
    /// <param name="p2">The second point defining the line.</param>
    /// <returns>The Y coordinate at the specified X coordinate on the line.</returns>
    public static float FindYAtX(float x, [NotNull] Vector3 p1, [NotNull] Vector3 p2) =>
        p1.Y + ((x - p1.X) * ((p2.Y - p1.Y) / (p2.X - p1.X)));

    /// <summary>Finds the Y coordinate on a line between two points at the specified Z coordinate.</summary>
    /// <param name="z">The Z coordinate to find Y for.</param>
    /// <param name="p1">The first point defining the line.</param>
    /// <param name="p2">The second point defining the line.</param>
    /// <returns>The Y coordinate at the specified Z coordinate on the line.</returns>
    public static float FindYAtZ(float z, [NotNull] Vector3 p1, [NotNull] Vector3 p2) =>
        p1.Y + ((z - p1.Z) * ((p2.Y - p1.Y) / (p2.Z - p1.Z)));

    /// <summary>Finds the Z coordinate on a line between two points at the specified X coordinate.</summary>
    /// <param name="x">The X coordinate to find Z for.</param>
    /// <param name="p1">The first point defining the line.</param>
    /// <param name="p2">The second point defining the line.</param>
    /// <returns>The Z coordinate at the specified X coordinate on the line.</returns>
    public static float FindZAtX(float x, [NotNull] Vector3 p1, [NotNull] Vector3 p2) =>
        p1.Z + ((x - p1.X) * ((p2.Z - p1.Z) / (p2.X - p1.X)));

    /// <summary>Finds the Z coordinate on a line between two points at the specified Y coordinate.</summary>
    /// <param name="y">The Y coordinate to find Z for.</param>
    /// <param name="p1">The first point defining the line.</param>
    /// <param name="p2">The second point defining the line.</param>
    /// <returns>The Z coordinate at the specified Y coordinate on the line.</returns>
    public static float FindZAtY(float y, [NotNull] Vector3 p1, [NotNull] Vector3 p2) =>
        p1.Z + ((y - p1.Y) * ((p2.Z - p1.Z) / (p2.Y - p1.Y)));

    /// <summary>Computes the Euclidean distance between two vectors.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The distance between the two vectors.</returns>
    public static float Distance([NotNull] Vector3 left, [NotNull] Vector3 right)
    {
        Vector3 temp = new(left - right);
        return temp.Length;
    }

    /// <summary>Computes an approximate distance between two vectors using a fast algorithm that avoids square root calculation.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>An approximate distance between the two vectors.</returns>
    public static float QuickDistance([NotNull] Vector3 left, [NotNull] Vector3 right)
    {
        Vector3 temp = new(left - right);
        return temp.QuickLength;
    }

    /// <summary>Sets the X, Y, and Z components of this vector to match another vector.</summary>
    /// <param name="other">The vector to copy values from.</param>
    public void Set([NotNull] Vector3 other) => (X, Y, Z) = (other.X, other.Y, other.Z);

    /// <summary>Sets the X, Y, and Z components of this vector to the specified values.</summary>
    /// <param name="x">The new X component value.</param>
    /// <param name="y">The new Y component value.</param>
    /// <param name="z">The new Z component value.</param>
    public void Set(float x, float y, float z) => (X, Y, Z) = (x, y, z);

    /// <summary>Sets the X, Y, and Z components of this vector from an array of float values.</summary>
    /// <param name="values">An array containing at least 3 float values.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array contains fewer than 3 elements.</exception>
    public void Set([NotNull] float[] values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, 3);
        (X, Y, Z) = (values[0], values[1], values[2]);
    }

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
        Z *= oLen;
    }

    /// <summary>Updates this vector to contain the minimum X, Y, and Z components between this vector and another vector.</summary>
    /// <param name="min">The other vector to compare against.</param>
    public void UpdateMin([NotNull] Vector3 min)
    {
        if (min.X < X)
        {
            X = min.X;
        }

        if (min.Y < Y)
        {
            Y = min.Y;
        }

        if (min.Z < Z)
        {
            Z = min.Z;
        }
    }

    /// <summary>Updates this vector to contain the maximum X, Y, and Z components between this vector and another vector.</summary>
    /// <param name="max">The other vector to compare against.</param>
    public void UpdateMax([NotNull] Vector3 max)
    {
        if (max.X > X)
        {
            X = max.X;
        }

        if (max.Y > Y)
        {
            Y = max.Y;
        }

        if (max.Z > Z)
        {
            Z = max.Z;
        }
    }

    /// <summary>Caps the absolute value of each component to the corresponding component in the cap vector.</summary>
    /// <param name="cap">The vector containing the maximum absolute values for each component.</param>
    public void CapAbsoluteTo([NotNull] Vector3 cap)
    {
        XCapAbsoluteTo(cap.X);
        YCapAbsolute(cap.Y);
        ZCapAbsolute(cap.Z);
    }

    /// <summary>Scales this vector by multiplying each component by the corresponding components of the scale vector.</summary>
    /// <param name="scale">The vector containing scale factors for X, Y, and Z components.</param>
    public void Scale([NotNull] Vector3 scale) => Set(X * scale.X, Y * scale.Y, Z * scale.Z);

    /// <summary>Rotates this vector around the X-axis by the specified angle.</summary>
    /// <param name="angle">The rotation angle in radians.</param>
    public void RotateX(float angle) => RotateX(float.Sin(angle), float.Cos(angle));

    /// <summary>Rotates this vector around the X-axis using pre-calculated sine and cosine values for better performance.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public void RotateX(float sin, float cos)
    {
        var newY = (cos * Y) - (sin * Z);
        var newZ = (sin * Y) + (cos * Z);
        Y = newY;
        Z = newZ;
    }

    /// <summary>Rotates this vector around the Y-axis by the specified angle.</summary>
    /// <param name="angle">The rotation angle in radians.</param>
    public void RotateY(float angle) => RotateY(float.Sin(angle), float.Cos(angle));

    /// <summary>Rotates this vector around the Y-axis using pre-calculated sine and cosine values for better performance.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public void RotateY(float sin, float cos)
    {
        var newX = (cos * X) + (sin * Z);
        var newZ = (-sin * X) + (cos * Z);
        X = newX;
        Z = newZ;
    }

    /// <summary>Rotates this vector around the Z-axis by the specified angle.</summary>
    /// <param name="angle">The rotation angle in radians.</param>
    public void RotateZ(float angle) => RotateZ(float.Sin(angle), float.Cos(angle));

    /// <summary>Rotates this vector around the Z-axis using pre-calculated sine and cosine values for better performance.</summary>
    /// <param name="sin">The sine of the rotation angle.</param>
    /// <param name="cos">The cosine of the rotation angle.</param>
    public void RotateZ(float sin, float cos)
    {
        var newX = (cos * X) - (sin * Y);
        var newY = (sin * X) + (cos * Y);
        X = newX;
        Y = newY;
    }

    /// <summary>Converts the vector components to an ABGR color value with alpha set to 255.</summary>
    /// <returns>A 32-bit ABGR color value where components are scaled from 0-1 to 0-255.</returns>
    public ulong ToAbgr() => (ulong)((255 << 24) | ((uint)(Z * 255) << 16) | ((uint)(Y * 255) << 8) | (uint)(X * 255));

    /// <summary>Converts the vector components to an ARGB color value with alpha set to 255.</summary>
    /// <returns>A 32-bit ARGB color value where components are scaled from 0-1 to 0-255.</returns>
    public ulong ToArgb() => (ulong)((255 << 24) | ((uint)(X * 255) << 16) | ((uint)(Y * 255) << 8) | (uint)(Z * 255));

    /// <summary>Converts the vector components to an ARGB color value with the specified alpha.</summary>
    /// <param name="alpha">The alpha value (0-255) to use for the color.</param>
    /// <returns>A 32-bit ARGB color value where RGB components are scaled from 0-1 to 0-255.</returns>
    public ulong ToArgb(float alpha) =>
        ((uint)alpha << 24) | ((uint)(X * 255) << 16) | ((uint)(Y * 255) << 8) | (uint)(Z * 255);

    /// <summary>Determines whether the specified object is equal to this vector within floating-point epsilon tolerance.</summary>
    /// <param name="obj">The object to compare with this vector.</param>
    /// <returns><see langword="true"/> if the specified object is a Vector3 and is equal to this vector; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is Vector3 other && Equals(this, other);

    /// <summary>Determines whether two vectors are equal within floating-point epsilon tolerance.</summary>
    /// <param name="x">The first vector to compare.</param>
    /// <param name="y">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are equal or both are null; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Vector3? x, Vector3? y) =>
        ReferenceEquals(x, y)
        || (
            x is not null
            && y is not null
            && x.GetType() == y.GetType()
            && float.Abs(x.X - y.X) < float.Epsilon
            && float.Abs(x.Y - y.Y) < float.Epsilon
            && float.Abs(x.Z - y.Z) < float.Epsilon
        );

    /// <summary>Returns a hash code for this vector.</summary>
    /// <returns>A hash code based on the X, Y, and Z components of the vector.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns a hash code for the specified vector.</summary>
    /// <param name="obj">The vector to get a hash code for.</param>
    /// <returns>A hash code based on the X, Y, and Z components of the vector.</returns>
    public int GetHashCode([NotNull] Vector3 obj) => HashCode.Combine(obj.X, obj.Y, obj.Z);

    /// <summary>Returns a string representation of the <see cref="Vector3"/> instance.</summary>
    /// <returns>A string in the format "(X, Y, Z)" where X, Y, and Z are the components of the vector.</returns>
    public override string ToString() => $"({X}, {Y}, {Z})";

    /// <summary>Returns a string representation of the current <see cref="Vector3"/> object.</summary>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string that represents the current object in the format "(X, Y, Z)".</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({X.ToString(formatProvider)}, {Y.ToString(formatProvider)}, {Z.ToString(formatProvider)})";

    /// <summary>Returns a string representation of the <see cref="Vector3"/> instance.</summary>
    /// <param name="format">A format string that specifies how the X, Y, and Z components are formatted.</param>
    /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
    /// <returns>A string formatted as (X, Y, Z), representing the vector's components.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({X.ToString(format, formatProvider)}, {Y.ToString(format, formatProvider)}, {Z.ToString(format, formatProvider)})";

    private void XCapAbsoluteTo(float x)
    {
        if (X > 0)
        {
            if (x < X)
            {
                X = x;
            }
        }
        else
        {
            if (-x > X)
            {
                X = -x;
            }
        }
    }

    private void YCapAbsolute(float y)
    {
        if (Y > 0)
        {
            if (y < Y)
            {
                Y = y;
            }
        }
        else
        {
            if (-y > Y)
            {
                Y = -y;
            }
        }
    }

    private void ZCapAbsolute(float z)
    {
        if (Z > 0)
        {
            if (z < Z)
            {
                Z = z;
            }
        }
        else
        {
            if (-z > Z)
            {
                Z = -z;
            }
        }
    }
}
