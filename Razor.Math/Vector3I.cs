// -----------------------------------------------------------------------
// <copyright file="Vector3I.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a 3D vector with integer components I, J, and K.</summary>
public class Vector3I : IEqualityComparer<Vector3I>
{
    /// <summary>Initializes a new instance of the <see cref="Vector3I"/> class with all components set to zero.</summary>
    public Vector3I() { }

    /// <summary>Initializes a new instance of the <see cref="Vector3I"/> class with the specified components.</summary>
    /// <param name="i">The I component of the vector.</param>
    /// <param name="j">The J component of the vector.</param>
    /// <param name="k">The K component of the vector.</param>
    public Vector3I(int i, int j, int k) => (I, J, K) = (i, j, k);

    /// <summary>Gets or sets the I component of the vector.</summary>
    /// <value>An integer representing the I component.</value>
    public int I { get; set; }

    /// <summary>Gets or sets the J component of the vector.</summary>
    /// <value>An integer representing the J component.</value>
    public int J { get; set; }

    /// <summary>Gets or sets the K component of the vector.</summary>
    /// <value>An integer representing the K component.</value>
    public int K { get; set; }

    /// <summary>Gets or sets the component of the vector at the specified index.</summary>
    /// <param name="index">The index of the component to access (0 for I, 1 for J, 2 for K).</param>
    /// <returns>The value of the component at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is not between 0 and 2.</exception>
    public int this[int index]
    {
        get =>
            index switch
            {
                0 => I,
                1 => J,
                2 => K,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be between 0 and 2."),
            };
        set
        {
            switch (index)
            {
                case 0:
                    I = value;
                    break;
                case 1:
                    J = value;
                    break;
                case 2:
                    K = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be between 0 and 2.");
            }
        }
    }

    /// <summary>Adds two vectors using component-wise addition.</summary>
    /// <param name="left">The first vector to add.</param>
    /// <param name="right">The second vector to add.</param>
    /// <returns>A new <see cref="Vector3I"/> representing the sum of the two vectors.</returns>
    public static Vector3I operator +(Vector3I left, Vector3I right) => Add(left, right);

    /// <summary>Subtracts one vector from another using component-wise subtraction.</summary>
    /// <param name="left">The vector to subtract from.</param>
    /// <param name="right">The vector to subtract.</param>
    /// <returns>A new <see cref="Vector3I"/> representing the difference of the two vectors.</returns>
    public static Vector3I operator -(Vector3I left, Vector3I right) => Subtract(left, right);

    /// <summary>Determines whether two vectors are equal.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Vector3I left, Vector3I right) => object.Equals(left, right);

    /// <summary>Determines whether two vectors are not equal.</summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns><see langword="true"/> if the vectors are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Vector3I left, Vector3I right) => !object.Equals(left, right);

    /// <summary>Adds two vectors using component-wise addition.</summary>
    /// <param name="left">The first vector to add.</param>
    /// <param name="right">The second vector to add.</param>
    /// <returns>A new <see cref="Vector3I"/> with components (left.I + right.I, left.J + right.J, left.K + right.K).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.</exception>
    public static Vector3I Add([NotNull] Vector3I left, [NotNull] Vector3I right) =>
        new(left.I + right.I, left.J + right.J, left.K + right.K);

    /// <summary>Subtracts one vector from another using component-wise subtraction.</summary>
    /// <param name="left">The vector to subtract from.</param>
    /// <param name="right">The vector to subtract.</param>
    /// <returns>A new <see cref="Vector3I"/> with components (left.I - right.I, left.J - right.J, left.K - right.K).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.</exception>
    public static Vector3I Subtract([NotNull] Vector3I left, [NotNull] Vector3I right) =>
        new(left.I - right.I, left.J - right.J, left.K - right.K);

    /// <summary>Determines whether the specified object is equal to the current vector.</summary>
    /// <param name="obj">The object to compare with the current vector.</param>
    /// <returns><see langword="true"/> if the specified object is a <see cref="Vector3I"/> with the same component values; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is Vector3I other && Equals(this, other);

    /// <summary>Determines whether two vectors are equal by comparing their components.</summary>
    /// <param name="x">The first vector to compare.</param>
    /// <param name="y">The second vector to compare.</param>
    /// <returns><see langword="true"/> if both vectors are <see langword="null"/> or have identical I, J, and K components; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Vector3I? x, Vector3I? y) =>
        ReferenceEquals(x, y)
        || (x is not null && y is not null && x.GetType() == y.GetType() && x.I == y.I && x.J == y.J && x.K == y.K);

    /// <summary>Returns a hash code for the current vector.</summary>
    /// <returns>A hash code based on the I, J, and K components.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns a hash code for the specified vector.</summary>
    /// <param name="obj">The vector for which to get a hash code.</param>
    /// <returns>A hash code based on the vector's I, J, and K components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is <see langword="null"/>.</exception>
    public int GetHashCode([NotNull] Vector3I obj) => HashCode.Combine(obj.I, obj.J, obj.K);

    /// <summary>Returns a string representation of the vector in the format "(I, J, K)".</summary>
    /// <returns>A string containing the vector's components formatted as "(I, J, K)".</returns>
    public override string ToString() => $"({I}, {J}, {K})";

    /// <summary>Returns a string representation of the vector using the specified format provider.</summary>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string containing the vector's components formatted as "(I, J, K)" using the specified format provider.</returns>
    public string ToString(IFormatProvider? formatProvider) =>
        $"({I.ToString(formatProvider)}, {J.ToString(formatProvider)}, {K.ToString(formatProvider)})";

    /// <summary>Returns a string representation of the vector using the specified format string and format provider.</summary>
    /// <param name="format">A numeric format string to apply to each component.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information, or <see langword="null"/> to use the current culture.</param>
    /// <returns>A string containing the vector's components formatted as "(I, J, K)" using the specified format and format provider.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider = null) =>
        $"({I.ToString(format, formatProvider)}, {J.ToString(format, formatProvider)}, {K.ToString(format, formatProvider)})";
}
