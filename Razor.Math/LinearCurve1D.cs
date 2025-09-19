// -----------------------------------------------------------------------
// <copyright file="LinearCurve1D.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Math;

/// <summary>Provides a linear interpolation implementation for 1D curves, computing values along straight-line segments between keyframes.</summary>
/// <remarks>This class extends <see cref="Curve1D"/> to provide linear interpolation between keyframes. When evaluating the curve at a given time, it returns values along straight lines connecting adjacent keyframes. For non-looping curves, times outside the keyframe range return the first or last keyframe value respectively.</remarks>
public class LinearCurve1D : Curve1D
{
    /// <summary>Evaluates the curve at the specified time using linear interpolation between keyframes.</summary>
    /// <param name="time">The time at which to evaluate the curve.</param>
    /// <returns>The linearly interpolated float value at the specified time.</returns>
    /// <remarks>
    /// <para>For non-looping curves:</para>
    /// <list type="bullet">
    /// <item>If the time is before the first keyframe, returns the first keyframe's point value.</item>
    /// <item>If the time is at or after the last keyframe, returns the last keyframe's point value.</item>
    /// </list>
    /// <para>For times between keyframes, performs linear interpolation using the formula: Value = Start + (t * (End - Start)), where t is the normalized time within the interval.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var curve = new LinearCurve1D();
    /// curve.AddKey(0.0f, TimeSpan.Zero);
    /// curve.AddKey(10.0f, TimeSpan.FromSeconds(1));
    /// var midValue = curve.Evaluate(TimeSpan.FromMilliseconds(500)); // Returns 5.0f
    /// </code>
    /// </example>
    public override float Evaluate(TimeSpan time)
    {
        if (!IsLooping)
        {
            if (time < Keys[0].Time)
            {
                return Keys[0].Point;
            }

            if (time >= Keys[^1].Time)
            {
                return Keys[^1].Point;
            }
        }

        (var i0, var i1, TimeSpan t) = FindInterval(time);
        return Keys[i0].Point + (t.Ticks * (Keys[i1].Point - Keys[i0].Point));
    }
}
