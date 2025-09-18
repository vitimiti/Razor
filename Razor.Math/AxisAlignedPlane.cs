// -----------------------------------------------------------------------
// <copyright file="AxisAlignedPlane.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a plane aligned with one of the coordinate axes.</summary>
public class AxisAlignedPlane
{
    /// <summary>Initializes a new instance of the <see cref="AxisAlignedPlane"/> class with default values.</summary>
    public AxisAlignedPlane() { }

    /// <summary>Initializes a new instance of the <see cref="AxisAlignedPlane"/> class with the specified normal and distance.</summary>
    /// <param name="normal">The axis normal of the plane.</param>
    /// <param name="distance">The distance from the origin to the plane along the normal axis.</param>
    public AxisAlignedPlane(AxisNormal normal, float distance) => (Normal, Distance) = (normal, distance);

    /// <summary>Gets or sets the axis normal of the plane.</summary>
    /// <value>An <see cref="AxisNormal"/> value indicating which axis the plane is aligned with.</value>
    public AxisNormal Normal { get; set; }

    /// <summary>Gets or sets the distance from the origin to the plane along the normal axis.</summary>
    /// <value>A <see cref="float"/> representing the signed distance from the origin.</value>
    public float Distance { get; set; }

    /// <summary>Sets the normal and distance of the plane.</summary>
    /// <param name="normal">The axis normal of the plane.</param>
    /// <param name="distance">The distance from the origin to the plane along the normal axis.</param>
    public void Set(AxisNormal normal, float distance) => (Normal, Distance) = (normal, distance);

    /// <summary>Gets the normal vector of the plane and stores it in the provided vector reference.</summary>
    /// <param name="normal">A reference to a <see cref="Vector3"/> where the normal vector will be stored.</param>
    public void GetVectorNormal([NotNull] ref Vector3 normal)
    {
        normal.Set(0, 0, 0);
        normal[(int)Normal] = 1F;
    }
}
