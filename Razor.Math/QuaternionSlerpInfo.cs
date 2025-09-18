// -----------------------------------------------------------------------
// <copyright file="QuaternionSlerpInfo.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Math;

/// <summary>Contains precomputed information for spherical linear interpolation (SLERP) of quaternions to optimize the interpolation calculation.</summary>
/// <param name="SinTheta">The sine of the angle between the two quaternions being interpolated.</param>
/// <param name="Theta">The angle between the two quaternions in radians.</param>
/// <param name="Flip">A value indicating whether the second quaternion should be negated to ensure the shortest rotation path.</param>
/// <param name="Linear">A value indicating whether linear interpolation should be used instead of spherical interpolation when the quaternions are very close to each other.</param>
public record struct QuaternionSlerpInfo(float SinTheta, float Theta, bool Flip, bool Linear);
