// -----------------------------------------------------------------------
// <copyright file="CastSurfaceType.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Math;

/// <summary>Defines surface type identifiers for collision detection and material classification in casting operations.</summary>
/// <remarks>This enumeration serves as a base type that can be extended or cast from custom surface type enumerations to identify different material properties, surface characteristics, or collision behaviors. Use explicit casting to convert from domain-specific surface types: <c>SurfaceType = (CastSurfaceType)customType</c>.</remarks>
public enum CastSurfaceType
{
    /// <summary>Represents no specific surface type or an uninitialized surface classification.</summary>
    None,
}
