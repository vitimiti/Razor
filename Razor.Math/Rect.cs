// -----------------------------------------------------------------------
// <copyright file="Rect.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a rectangle defined by left, top, right, and bottom coordinates.</summary>
public class Rect : IEqualityComparer<Rect>
{
    /// <summary>Initializes a new instance of the <see cref="Rect"/> class with default values.</summary>
    public Rect() { }

    /// <summary>Initializes a new instance of the <see cref="Rect"/> class by copying another rectangle.</summary>
    /// <param name="other">The rectangle to copy from.</param>
    public Rect(Rect other) => Set(other);

    /// <summary>Initializes a new instance of the <see cref="Rect"/> class with specified coordinates.</summary>
    /// <param name="left">The left coordinate.</param>
    /// <param name="top">The top coordinate.</param>
    /// <param name="right">The right coordinate.</param>
    /// <param name="bottom">The bottom coordinate.</param>
    public Rect(float left, float top, float right, float bottom) => Set(left, top, right, bottom);

    /// <summary>Initializes a new instance of the <see cref="Rect"/> class with top-left and bottom-right points.</summary>
    /// <param name="topLeft">The top-left point.</param>
    /// <param name="bottomRight">The bottom-right point.</param>
    public Rect(Vector2 topLeft, Vector2 bottomRight) => Set(topLeft, bottomRight);

    /// <summary>Gets or sets the left coordinate of the rectangle.</summary>
    /// <value>The left coordinate.</value>
    public float Left { get; set; }

    /// <summary>Gets or sets the top coordinate of the rectangle.</summary>
    /// <value>The top coordinate.</value>
    public float Top { get; set; }

    /// <summary>Gets or sets the right coordinate of the rectangle.</summary>
    /// <value>The right coordinate.</value>
    public float Right { get; set; }

    /// <summary>Gets or sets the bottom coordinate of the rectangle.</summary>
    /// <value>The bottom coordinate.</value>
    public float Bottom { get; set; }

    /// <summary>Gets the width of the rectangle.</summary>
    /// <value>The width calculated as Right - Left.</value>
    public float Width => Right - Left;

    /// <summary>Gets the height of the rectangle.</summary>
    /// <value>The height calculated as Bottom - Top.</value>
    public float Height => Bottom - Top;

    /// <summary>Gets the center point of the rectangle.</summary>
    /// <value>A <see cref="Vector2"/> representing the center point.</value>
    public Vector2 Center => new((Left + Right) / 2F, (Top + Bottom) / 2F);

    /// <summary>Gets the upper-left corner of the rectangle.</summary>
    /// <value>A <see cref="Vector2"/> representing the upper-left corner.</value>
    public Vector2 UpperLeft => new(Left, Top);

    /// <summary>Gets the lower-right corner of the rectangle.</summary>
    /// <value>A <see cref="Vector2"/> representing the lower-right corner.</value>
    public Vector2 LowerRight => new(Right, Bottom);

    /// <summary>Gets the upper-right corner of the rectangle.</summary>
    /// <value>A <see cref="Vector2"/> representing the upper-right corner.</value>
    public Vector2 UpperRight => new(Right, Top);

    /// <summary>Gets the lower-left corner of the rectangle.</summary>
    /// <value>A <see cref="Vector2"/> representing the lower-left corner.</value>
    public Vector2 LowerLeft => new(Left, Bottom);

    /// <summary>Adds a vector to a rectangle, translating its position.</summary>
    /// <param name="rect">The rectangle to translate.</param>
    /// <param name="vector">The vector to add.</param>
    /// <returns>A new rectangle translated by the specified vector.</returns>
    public static Rect operator +(Rect rect, Vector2 vector) => Add(rect, vector);

    /// <summary>Subtracts a vector from a rectangle, translating its position.</summary>
    /// <param name="rect">The rectangle to translate.</param>
    /// <param name="vector">The vector to subtract.</param>
    /// <returns>A new rectangle translated by the negative of the specified vector.</returns>
    public static Rect operator -(Rect rect, Vector2 vector) => Subtract(rect, vector);

    /// <summary>Multiplies a rectangle by a scalar value, scaling all coordinates.</summary>
    /// <param name="rect">The rectangle to scale.</param>
    /// <param name="scale">The scale factor.</param>
    /// <returns>A new rectangle with all coordinates scaled.</returns>
    public static Rect operator *(Rect rect, float scale) => Multiply(rect, scale);

    /// <summary>Multiplies a rectangle by a scalar value, scaling all coordinates.</summary>
    /// <param name="scale">The scale factor.</param>
    /// <param name="rect">The rectangle to scale.</param>
    /// <returns>A new rectangle with all coordinates scaled.</returns>
    public static Rect operator *(float scale, Rect rect) => Multiply(rect, scale);

    /// <summary>Divides a rectangle by a scalar value, scaling all coordinates.</summary>
    /// <param name="rect">The rectangle to scale.</param>
    /// <param name="scale">The scale divisor.</param>
    /// <returns>A new rectangle with all coordinates divided by the scale factor.</returns>
    public static Rect operator /(Rect rect, float scale) => Divide(rect, scale);

    /// <summary>Determines whether two rectangles are equal.</summary>
    /// <param name="left">The first rectangle to compare.</param>
    /// <param name="right">The second rectangle to compare.</param>
    /// <returns><c>true</c> if the rectangles are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Rect left, Rect right) => object.Equals(left, right);

    /// <summary>Determines whether two rectangles are not equal.</summary>
    /// <param name="left">The first rectangle to compare.</param>
    /// <param name="right">The second rectangle to compare.</param>
    /// <returns><c>true</c> if the rectangles are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Rect left, Rect right) => !object.Equals(left, right);

    /// <summary>Adds a vector to a rectangle, translating its position.</summary>
    /// <param name="rect">The rectangle to translate.</param>
    /// <param name="vector">The vector to add.</param>
    /// <returns>A new rectangle translated by the specified vector.</returns>
    public static Rect Add([NotNull] Rect rect, [NotNull] Vector2 vector) =>
        new(rect.Left + vector.X, rect.Top + vector.Y, rect.Right + vector.X, rect.Bottom + vector.Y);

    /// <summary>Subtracts a vector from a rectangle, translating its position.</summary>
    /// <param name="rect">The rectangle to translate.</param>
    /// <param name="vector">The vector to subtract.</param>
    /// <returns>A new rectangle translated by the negative of the specified vector.</returns>
    public static Rect Subtract([NotNull] Rect rect, [NotNull] Vector2 vector) =>
        new(rect.Left - vector.X, rect.Top - vector.Y, rect.Right - vector.X, rect.Bottom - vector.Y);

    /// <summary>Multiplies a rectangle by a scalar value, scaling all coordinates.</summary>
    /// <param name="rect">The rectangle to scale.</param>
    /// <param name="scale">The scale factor.</param>
    /// <returns>A new rectangle with all coordinates scaled by the factor.</returns>
    public static Rect Multiply([NotNull] Rect rect, float scale) =>
        new(rect.Left * scale, rect.Top * scale, rect.Right * scale, rect.Bottom * scale);

    /// <summary>Multiplies a rectangle by a scalar value, scaling all coordinates.</summary>
    /// <param name="scale">The scale factor.</param>
    /// <param name="rect">The rectangle to scale.</param>
    /// <returns>A new rectangle with all coordinates scaled by the factor.</returns>
    public static Rect Multiply(float scale, [NotNull] Rect rect) => Multiply(rect, scale);

    /// <summary>Divides a rectangle by a scalar value, scaling all coordinates.</summary>
    /// <param name="rect">The rectangle to scale.</param>
    /// <param name="scale">The scale divisor.</param>
    /// <returns>A new rectangle with all coordinates divided by the scale factor.</returns>
    public static Rect Divide([NotNull] Rect rect, float scale)
    {
        var oScale = 1F / scale;
        return new Rect(rect.Left * oScale, rect.Top * oScale, rect.Right * oScale, rect.Bottom * oScale);
    }

    /// <summary>Sets the coordinates of the rectangle.</summary>
    /// <param name="left">The left coordinate.</param>
    /// <param name="top">The top coordinate.</param>
    /// <param name="right">The right coordinate.</param>
    /// <param name="bottom">The bottom coordinate.</param>
    public void Set(float left, float top, float right, float bottom) =>
        (Left, Top, Right, Bottom) = (left, top, right, bottom);

    /// <summary>Sets the coordinates of the rectangle using two points.</summary>
    /// <param name="topLeft">The top-left point.</param>
    /// <param name="bottomRight">The bottom-right point.</param>
    public void Set([NotNull] Vector2 topLeft, [NotNull] Vector2 bottomRight) =>
        Set(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);

    /// <summary>Sets the coordinates of the rectangle by copying from another rectangle.</summary>
    /// <param name="other">The rectangle to copy from.</param>
    public void Set([NotNull] Rect other) => Set(other.Left, other.Top, other.Right, other.Bottom);

    /// <summary>Scales all coordinates of the rectangle by a uniform factor.</summary>
    /// <param name="scale">The scale factor to apply to all coordinates.</param>
    public void Scale(float scale) => Set(Left * scale, Top * scale, Right * scale, Bottom * scale);

    /// <summary>Scales the rectangle coordinates using different factors for X and Y axes.</summary>
    /// <param name="scale">The scale vector containing X and Y scale factors.</param>
    public void Scale([NotNull] Vector2 scale) => Set(Left * scale.X, Top * scale.Y, Right * scale.X, Bottom * scale.Y);

    /// <summary>Scales the rectangle coordinates using the inverse of the provided scale factors.</summary>
    /// <param name="scale">The scale vector to divide coordinates by.</param>
    public void InverseScale([NotNull] Vector2 scale) =>
        Set(Left / scale.X, Top / scale.Y, Right / scale.X, Bottom / scale.Y);

    /// <summary>Inflates the rectangle by the specified amount on all sides.</summary>
    /// <param name="amount">The amount to inflate. X component affects left/right, Y component affects top/bottom.</param>
    public void Inflate([NotNull] Vector2 amount) =>
        Set(Left - amount.X, Top - amount.Y, Right + amount.X, Bottom + amount.Y);

    /// <summary>Determines whether the rectangle contains the specified point.</summary>
    /// <param name="point">The point to test for containment.</param>
    /// <returns><c>true</c> if the point is contained within or on the boundary of the rectangle; otherwise, <c>false</c>.</returns>
    public bool Contains([NotNull] Vector2 point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    /// <summary>Snaps the rectangle coordinates to the nearest unit grid.</summary>
    /// <param name="units">The unit grid size for X and Y axes.</param>
    public void SnapToUnits([NotNull] Vector2 units) =>
        Set(
            (int)(Left / units.X) + (.5F * units.X),
            (int)(Top / units.Y) + (.5F * units.Y),
            (int)(Right / units.X) + (.5F * units.X),
            (int)(Bottom / units.Y) + (.5F * units.Y)
        );

    /// <summary>Determines whether the specified object is equal to the current rectangle.</summary>
    /// <param name="obj">The object to compare with the current rectangle.</param>
    /// <returns><c>true</c> if the specified object is equal to the current rectangle; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is Rect other && Equals(this, other);

    /// <summary>Determines whether the specified rectangles are equal.</summary>
    /// <param name="x">The first rectangle to compare.</param>
    /// <param name="y">The second rectangle to compare.</param>
    /// <returns><c>true</c> if the specified rectangles are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(Rect? x, Rect? y) =>
        ReferenceEquals(x, y)
        || (
            x is not null
            && y is not null
            && x.GetType() == y.GetType()
            && float.Abs(x.Left - y.Left) < float.Epsilon
            && float.Abs(x.Top - y.Top) < float.Epsilon
            && float.Abs(x.Right - y.Right) < float.Epsilon
            && float.Abs(x.Bottom - y.Bottom) < float.Epsilon
        );

    /// <summary>Returns the hash code for this rectangle.</summary>
    /// <returns>A 32-bit signed integer that is the hash code for this rectangle.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns the hash code for the specified rectangle.</summary>
    /// <param name="obj">The rectangle for which to get the hash code.</param>
    /// <returns>A 32-bit signed integer that is the hash code for the specified rectangle.</returns>
    public int GetHashCode([NotNull] Rect obj) => HashCode.Combine(obj.Left, obj.Top, obj.Right, obj.Bottom);

    /// <summary>Returns a string that represents the current rectangle.</summary>
    /// <returns>A string containing the values of the Left, Top, Right, and Bottom properties.</returns>
    public override string ToString() => $"({Left}, {Top}, {Right}, {Bottom})";

    /// <summary>Returns a string representation of the <see cref="Rect"/> object in the specified format.</summary>
    /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
    /// <returns>A string that represents the <see cref="Rect"/> in the specified format.</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({Left.ToString(formatProvider)}, {Top.ToString(formatProvider)}, {Right.ToString(formatProvider)}, {Bottom.ToString(formatProvider)})";

    /// <summary>Returns a string representation of the <see cref="Rect"/> instance.</summary>
    /// <param name="format">A format string that specifies the format to apply to each numeric value.</param>
    /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
    /// <returns>A string representation of the <see cref="Rect"/> instance.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({Left.ToString(format, formatProvider)}, {Top.ToString(format, formatProvider)}, {Right.ToString(format, formatProvider)}, {Bottom.ToString(format, formatProvider)})";
}
