// -----------------------------------------------------------------------
// <copyright file="TriRaycasts.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Math;

/// <summary>Specifies flags for triangle raycast operations to indicate special conditions during ray-triangle intersection tests.</summary>
[Flags]
public enum TriRaycasts
{
    /// <summary>No special conditions occurred during the raycast operation.</summary>
    None = 0x00,

    /// <summary>The ray intersected with an edge of the triangle rather than the interior.</summary>
    HitEdge = 0x01,

    /// <summary>The ray start point is located inside the triangle.</summary>
    StartInTri = 0x02,
}
