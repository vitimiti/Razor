// -----------------------------------------------------------------------
// <copyright file="LinearCurve3D.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Math;

/// <summary>Provides a linear interpolation implementation for 3D curves, computing points along straight-line segments between keyframes.</summary>
/// <remarks>This class extends <see cref="Curve3D"/> to provide linear interpolation between keyframes. When evaluating the curve at a given time, it returns points along straight lines connecting adjacent keyframes. For times outside the keyframe range, it returns the first or last keyframe point respectively.</remarks>
public class LinearCurve3D : Curve3D
{
    /// <summary>Evaluates the curve at the specified time using linear interpolation between keyframes.</summary>
    /// <param name="time">The time at which to evaluate the curve.</param>
    /// <returns>The linearly interpolated <see cref="Vector3"/> point at the specified time.</returns>
    /// <remarks>
    /// <para>If the time is before the first keyframe, returns the first keyframe's point.</para>
    /// <para>If the time is at or after the last keyframe, returns the last keyframe's point.</para>
    /// <para>For times between keyframes, performs linear interpolation using the formula: Point = Start + (t * (End - Start)), where t is the normalized time within the interval.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var curve = new LinearCurve3D();
    /// curve.AddKey(new Vector3(0, 0, 0), TimeSpan.Zero);
    /// curve.AddKey(new Vector3(10, 10, 10), TimeSpan.FromSeconds(1));
    /// var midPoint = curve.Evaluate(TimeSpan.FromMilliseconds(500)); // Returns (5, 5, 5)
    /// </code>
    /// </example>
    public override Vector3 Evaluate(TimeSpan time)
    {
        if (time < Keys[0].Time)
        {
            return Keys[0].Point;
        }

        if (time >= Keys[^1].Time)
        {
            return Keys[^1].Point;
        }

        (var i0, var i1, TimeSpan t) = FindInterval(time);
        return Keys[i0].Point + (t.Ticks * (Keys[i1].Point - Keys[i0].Point));
    }
}
