// -----------------------------------------------------------------------
// <copyright file="Sphere.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a 3D sphere defined by a center point and radius.</summary>
[SuppressMessage(
    "csharpsquid",
    "S4050:Operators should be overloaded consistently",
    Justification = "While this is probably correct, the original code does NOT permit equality comparisons between spheres."
)]
public class Sphere
{
    /// <summary>Initializes a new instance of the <see cref="Sphere"/> class with default values.</summary>
    public Sphere() { }

    /// <summary>Initializes a new instance of the <see cref="Sphere"/> class with the specified center and radius.</summary>
    /// <param name="center">The center point of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    public Sphere(Vector3 center, float radius) => Initialize(center, radius);

    /// <summary>Initializes a new instance of the <see cref="Sphere"/> class with the specified transformation matrix, center, and radius.</summary>
    /// <param name="matrix">The transformation matrix to apply to the center point.</param>
    /// <param name="center">The center point of the sphere before transformation.</param>
    /// <param name="radius">The radius of the sphere.</param>
    public Sphere(Matrix3D matrix, Vector3 center, float radius) => Initialize(matrix, center, radius);

    /// <summary>Initializes a new instance of the <see cref="Sphere"/> class that encompasses another sphere from a given center point.</summary>
    /// <param name="center">The new center point of the sphere.</param>
    /// <param name="s0">The sphere to encompass.</param>
    public Sphere([NotNull] Vector3 center, [NotNull] Sphere s0)
    {
        var dist = (s0.Center - center).Length;
        Center = center;
        Radius = s0.Radius + dist;
    }

    /// <summary>Gets or sets the center point of the sphere.</summary>
    /// <value>A <see cref="Vector3"/> representing the center coordinates.</value>
    public Vector3 Center { get; set; } = new();

    /// <summary>Gets or sets the radius of the sphere.</summary>
    /// <value>A <see cref="float"/> representing the sphere's radius.</value>
    public float Radius { get; set; }

    /// <summary>Gets the volume of the sphere.</summary>
    /// <value>The volume calculated as (4/3) * π * r³.</value>
    public float Volume => 4F / 3F * float.Pi * float.Pow(Radius, 3);

    /// <summary>Adds two spheres together, creating a sphere that encompasses both.</summary>
    /// <param name="left">The first sphere.</param>
    /// <param name="right">The second sphere.</param>
    /// <returns>A new <see cref="Sphere"/> that encompasses both input spheres.</returns>
    public static Sphere operator +(Sphere left, Sphere right) => Add(left, right);

    /// <summary>Multiplies a sphere by a transformation matrix.</summary>
    /// <param name="sphere">The sphere to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>A new <see cref="Sphere"/> with the transformed center point.</returns>
    public static Sphere operator *(Sphere sphere, Matrix3D matrix) => Multiply(sphere, matrix);

    /// <summary>Creates a sphere that encompasses two input spheres.</summary>
    /// <param name="left">The first sphere.</param>
    /// <param name="right">The second sphere.</param>
    /// <returns>A new <see cref="Sphere"/> that encompasses both input spheres.</returns>
    public static Sphere Add([NotNull] Sphere left, Sphere right)
    {
        if (float.Abs(left.Radius) < float.Epsilon)
        {
            return right;
        }

        Sphere temp = left;
        temp.AddSphere(right);
        return temp;
    }

    /// <summary>Creates a new sphere by applying a transformation matrix to the specified sphere.</summary>
    /// <param name="sphere">The sphere to transform.</param>
    /// <param name="matrix">The transformation matrix to apply.</param>
    /// <returns>A new <see cref="Sphere"/> with the transformed center point.</returns>
    public static Sphere Multiply([NotNull] Sphere sphere, Matrix3D matrix)
    {
        Sphere temp = sphere;
        temp.Initialize(matrix, sphere.Center, sphere.Radius);
        return temp;
    }

    /// <summary>Creates a new sphere by applying a transformation matrix to the specified sphere.</summary>
    /// <param name="matrix">The transformation matrix to apply.</param>
    /// <param name="sphere">The sphere to transform.</param>
    /// <returns>A new <see cref="Sphere"/> with the transformed center point.</returns>
    public static Sphere Transform(Matrix3D matrix, [NotNull] Sphere sphere) =>
        new(matrix, sphere.Center, sphere.Radius);

    /// <summary>Determines whether two spheres intersect.</summary>
    /// <param name="left">The first sphere.</param>
    /// <param name="right">The second sphere.</param>
    /// <returns><c>true</c> if the spheres intersect; otherwise, <c>false</c>.</returns>
    public static bool Intersects([NotNull] Sphere left, [NotNull] Sphere right)
    {
        Vector3 delta = left.Center - right.Center;
        var dist2 = Vector3.DotProduct(delta, delta);
        return dist2 < (left.Radius + right.Radius) * (left.Radius + right.Radius);
    }

    /// <summary>Initializes the sphere with the specified center and radius.</summary>
    /// <param name="center">The center point of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    public void Initialize(Vector3 center, float radius) => (Center, Radius) = (center, radius);

    /// <summary>Initializes the sphere with the specified transformation matrix, center, and radius.</summary>
    /// <param name="matrix">The transformation matrix to apply to the center point.</param>
    /// <param name="position">The center point before transformation.</param>
    /// <param name="radius">The radius of the sphere.</param>
    public void Initialize([NotNull] Matrix3D matrix, Vector3 position, float radius)
    {
        Center = matrix * position;
        Radius = radius;
    }

    /// <summary>Re-centers the sphere to a new position and adjusts the radius to encompass the original sphere.</summary>
    /// <param name="center">The new center point.</param>
    public void ReCenter(Vector3 center)
    {
        var dist = (Center - center).Length;
        Center = center;
        Radius += dist;
    }

    /// <summary>Expands this sphere to encompass another sphere.</summary>
    /// <param name="sphere">The sphere to add to this sphere.</param>
    public void AddSphere([NotNull] Sphere sphere)
    {
        if (float.Abs(sphere.Radius) < float.Epsilon)
        {
            return;
        }

        var dist = (sphere.Center - Center).Length;
        if (float.Abs(dist) < float.Epsilon)
        {
            Radius = Radius > sphere.Radius ? Radius : sphere.Radius;
            return;
        }

        var rNew = (dist + Radius + sphere.Radius) / 2F;

        if (rNew < Radius)
        {
            return;
        }

        if (rNew < sphere.Radius)
        {
            Initialize(sphere.Center, sphere.Radius);
        }
        else
        {
            var lerp = (rNew - Radius) / dist;
            Vector3 center = ((sphere.Center - Center) * lerp) + Center;
            Initialize(center, rNew);
        }
    }

    /// <summary>Applies a transformation matrix to the sphere's center point.</summary>
    /// <param name="matrix">The transformation matrix to apply.</param>
    public void Transform(Matrix3D matrix) => Center = matrix * Center;
}
