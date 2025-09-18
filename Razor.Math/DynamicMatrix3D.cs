// -----------------------------------------------------------------------
// <copyright file="DynamicMatrix3D.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Razor.Abstractions;
using Razor.Attributes;

namespace Razor.Math;

/// <summary>Provides a poolable wrapper for <see cref="Matrix3D"/> instances, enabling efficient reuse and memory management in high-performance scenarios.</summary>
/// <remarks>This class implements <see cref="IPooledResettable"/> to support object pooling mechanisms, automatically resetting the contained matrix when returned to the pool.</remarks>
[Pooled]
public partial class DynamicMatrix3D : IPooledResettable
{
    /// <summary>Gets or sets the underlying <see cref="Matrix3D"/> instance used for 3D transformations and matrix operations.</summary>
    /// <value>The <see cref="Matrix3D"/> instance representing a 3x4 transformation matrix. Defaults to a new identity matrix.</value>
    public Matrix3D Matrix { get; set; } = new();

    /// <summary>Resets the <see cref="Matrix"/> property to a new identity matrix, restoring the object to its default state for reuse in object pooling scenarios.</summary>
    /// <remarks>This method is automatically called when the object is returned to the pool if <see cref="PooledAttribute.CallResetOnReturn"/> is <c>true</c>.</remarks>
    public void Reset() => Matrix = new Matrix3D();
}
