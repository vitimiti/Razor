// -----------------------------------------------------------------------
// <copyright file="Vector2I.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a 2D vector with integer I and J components, providing mathematical operations and utility methods for 2D integer vector calculations.</summary>
public class Vector2I : IEqualityComparer<Vector2I>
{
    /// <summary>Initializes a new instance of the <see cref="Vector2I"/> class with default values (0, 0).</summary>
    public Vector2I() { }

    /// <summary>Initializes a new instance of the <see cref="Vector2I"/> class with specified I and J components.</summary>
    /// <param name="i">The I component of the vector.</param>
    /// <param name="j">The J component of the vector.</param>
    public Vector2I(int i, int j) => Set(i, j);

    /// <summary>Gets or sets the I component of the vector.</summary>
    public int I { get; set; }

    /// <summary>Gets or sets the J component of the vector.</summary>
    public int J { get; set; }

    /// <summary>Adds two vectors component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new vector representing the sum of the two input vectors.</returns>
    public static Vector2I operator +(Vector2I left, Vector2I right) => Add(left, right);

    /// <summary>Subtracts the second vector from the first vector component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector to subtract.</param>
    /// <returns>A new vector representing the difference between the two input vectors.</returns>
    public static Vector2I operator -(Vector2I left, Vector2I right) => Subtract(left, right);

    /// <summary>Determines whether two vectors are equal.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Vector2I left, Vector2I right) => object.Equals(left, right);

    /// <summary>Determines whether two vectors are not equal.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Vector2I left, Vector2I right) => !object.Equals(left, right);

    /// <summary>Adds two vectors component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new vector representing the sum of the two input vectors.</returns>
    public static Vector2I Add([NotNull] Vector2I left, [NotNull] Vector2I right) =>
        new(left.I + right.I, left.J + right.J);

    /// <summary>Subtracts the second vector from the first vector component-wise.</summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector to subtract.</param>
    /// <returns>A new vector representing the difference between the two input vectors.</returns>
    public static Vector2I Subtract([NotNull] Vector2I left, [NotNull] Vector2I right) =>
        new(left.I - right.I, left.J - right.J);

    /// <summary>Sets the I and J components of this vector to the specified values.</summary>
    /// <param name="i">The new I component value.</param>
    /// <param name="j">The new J component value.</param>
    public void Set(int i, int j) => (I, J) = (i, j);

    /// <summary>Determines whether the specified object is equal to this vector.</summary>
    /// <param name="obj">The object to compare with this vector.</param>
    /// <returns><see langword="true"/> if the specified object is a Vector2I and is equal to this vector; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is Vector2I other && Equals(this, other);

    /// <summary>Determines whether two vectors are equal.</summary>
    /// <param name="x">The first vector to compare.</param>
    /// <param name="y">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are equal or both are null; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Vector2I? x, Vector2I? y) =>
        ReferenceEquals(x, y)
        || (x is not null && y is not null && x.GetType() == y.GetType() && x.I == y.I && x.J == y.J);

    /// <summary>Returns a hash code for this vector.</summary>
    /// <returns>A hash code based on the I and J components of the vector.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns a hash code for the specified vector.</summary>
    /// <param name="obj">The vector to get a hash code for.</param>
    /// <returns>A hash code based on the I and J components of the vector.</returns>
    public int GetHashCode([NotNull] Vector2I obj) => HashCode.Combine(obj.I, obj.J);

    /// <summary>Returns a string representation of the <see cref="Vector2I"/> instance.</summary>
    /// <returns>A string in the format "(I, J)" where I and J are the components of the vector.</returns>
    public override string ToString() => $"({I}, {J})";

    /// <summary>Returns a string representation of the current <see cref="Vector2I"/> object.</summary>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string that represents the current object in the format "(I, J)".</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({I.ToString(formatProvider)}, {J.ToString(formatProvider)})";

    /// <summary>Returns a string representation of the <see cref="Vector2I"/> instance.</summary>
    /// <param name="format">A format string that specifies how the I and J components are formatted.</param>
    /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
    /// <returns>A string formatted as (I, J), representing the vector's components.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({I.ToString(format, formatProvider)}, {J.ToString(format, formatProvider)})";
}
