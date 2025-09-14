// -----------------------------------------------------------------------
// <copyright file="CastResult.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Math;

/// <summary>Represents the result of a volume or ray cast operation, containing information about collision detection and intersection points.</summary>
/// <remarks>This class stores the outcome of collision detection operations including ray casting and volume sweeping. For performance optimization, avoid enabling <see cref="ComputeContactPoint"/> when possible, as computing intersection points outside the collision detection code is often more efficient.</remarks>
public class CastResult
{
    /// <summary>Initializes a new instance of the <see cref="CastResult"/> class and resets all properties to their default values.</summary>
    public CastResult() => Reset();

    /// <summary>Gets or sets a value indicating whether the initial configuration was interpenetrating with geometry.</summary>
    /// <value><c>true</c> if the starting position was already intersecting with collision geometry; otherwise, <c>false</c>.</value>
    public bool StartBad { get; set; }

    /// <summary>Gets or sets the fraction of the movement that occurred before collision was detected.</summary>
    /// <value>A float value between 0.0 and 1.0 representing the proportion of the intended movement completed before hitting an obstacle, where 1.0 indicates no collision occurred.</value>
    public float Fraction { get; set; }

    /// <summary>Gets or sets the surface normal vector at the collision point.</summary>
    /// <value>A <see cref="Vector3"/> representing the normalized surface normal at the point of collision.</value>
    public Vector3 Normal { get; set; } = new();

    /// <summary>Gets or sets the surface type identifier of the polygon at the collision point.</summary>
    /// <value>A <see cref="CastSurfaceType"/> value that can be cast from custom surface type enumerations to identify material or surface properties.</value>
    /// <remarks>This property allows for material-specific collision handling by casting custom surface type enumerations using the pattern: <c>SurfaceType = (CastSurfaceType)myType.ThisType</c>.</remarks>
    public CastSurfaceType SurfaceType { get; set; }

    /// <summary>Gets or sets a value indicating whether the collision detection system should compute the exact contact point.</summary>
    /// <value><c>true</c> to enable computation of <see cref="ContactPoint"/>; otherwise, <c>false</c>. The default is <c>false</c> for performance optimization.</value>
    /// <remarks>Enable this property only when the exact collision point is required, as it adds computational overhead. For ray casting, it's often more efficient to calculate the contact point using the <see cref="Fraction"/> value.</remarks>
    public bool ComputeContactPoint { get; set; }

    /// <summary>Gets or sets the exact point of collision in world coordinates.</summary>
    /// <value>A <see cref="Vector3"/> representing the collision point, valid only when <see cref="ComputeContactPoint"/> is <c>true</c>.</value>
    /// <remarks>This property is only populated when <see cref="ComputeContactPoint"/> is enabled during the collision detection operation.</remarks>
    public Vector3 ContactPoint { get; set; } = new();

    /// <summary>Resets all properties to their default values for reuse of the <see cref="CastResult"/> instance.</summary>
    /// <remarks>This method sets: <see cref="StartBad"/> to <c>false</c>, <see cref="Fraction"/> to 1.0, <see cref="Normal"/> to zero vector, <see cref="SurfaceType"/> to <see cref="CastSurfaceType.None"/>, <see cref="ComputeContactPoint"/> to <c>false</c>, and <see cref="ContactPoint"/> to zero vector.</remarks>
    public void Reset()
    {
        StartBad = false;
        Fraction = 1F;
        Normal.Set(0, 0, 0);
        SurfaceType = CastSurfaceType.None;
        ComputeContactPoint = false;
        ContactPoint.Set(0, 0, 0);
    }
}
