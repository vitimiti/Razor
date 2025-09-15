// -----------------------------------------------------------------------
// <copyright file="Tri.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a triangle in 3D space with normal vector and vertex collection for geometric operations and collision detection.</summary>
public class Tri
{
    /// <summary>Gets or sets the normal vector of the triangle.</summary>
    /// <value>The normal vector perpendicular to the triangle surface.</value>
    public Vector3 N { get; set; } = new();

    /// <summary>Gets the collection of triangle vertices.</summary>
    /// <value>A collection containing exactly three Vector3 vertices defining the triangle.</value>
    public Collection<Vector3> V { get; } = [new(), new(), new()];

    /// <summary>Determines whether a point lies inside a triangle using a 2D projection test on the specified axis plane.</summary>
    /// <param name="triPoint0">The first vertex of the triangle.</param>
    /// <param name="triPoint1">The second vertex of the triangle.</param>
    /// <param name="triPoint2">The third vertex of the triangle.</param>
    /// <param name="point">The test point to check for containment.</param>
    /// <param name="axis1">The first axis index to use for 2D projection (0=X, 1=Y, 2=Z).</param>
    /// <param name="axis2">The second axis index to use for 2D projection (0=X, 1=Y, 2=Z).</param>
    /// <param name="flags">Output parameter containing flags indicating special conditions during the test.</param>
    /// <returns><c>true</c> if the point is inside the triangle; otherwise, <c>false</c>.</returns>
    /// <remarks>This method performs a pure 2D point-in-triangle test by projecting both triangle and test point onto the specified axis plane. The triangle vertices are not assumed to be in any particular winding order.</remarks>
    public static bool PointInTriangle2D(
        [NotNull] Vector3 triPoint0,
        [NotNull] Vector3 triPoint1,
        [NotNull] Vector3 triPoint2,
        [NotNull] Vector3 point,
        int axis1,
        int axis2,
        out TriRaycasts flags
    )
    {
        // This function does a pure 2D point-in-triangle test. We pass in 3D points both for the
        // triangle and test points, but we also pass in the two axes to use (the third axis is ignored,
        // so we are actually testing the projection of the four points onto one of the axis planes). The
        // triangle points are not assumed to be in any particular winding order (that is checked for
        // internally). It is used internally by TriClass::Contains_Point(), and may be used elsewhere.
        // If the ray intersects the camera at an edge we also count it as an intersection. We set a bit
        // in 'flags' to true in this case (some users of this function need this extra information and
        // it is very cheap to compute). We do not modify 'flags' if no edges are hit, so it must be
        // initialized outside this function if its value is to be used.
        //
        // The function is based on checking signs of determinants, or in a more visually intuitive
        // sense, checking on which side of a line a point lies. For example, if the points run in
        // counter-clockwise order, the interior of the triangle is the intersection of the three
        // half-planes to the left of the directed infinite lines along P0 to P1, P1 to P2, P2 to P0.
        // Therefore the test point is in the triangle iff it is to the left of all three lines (if
        // the points are in clockwise order, we check if it is to the right of the lines).
        Vector2 p0P1 = new(triPoint1[axis1] - triPoint0[axis1], triPoint1[axis2] - triPoint0[axis2]);
        Vector2 p1P2 = new(triPoint2[axis1] - triPoint1[axis1], triPoint2[axis2] - triPoint1[axis2]);
        Vector2 p2P0 = new(triPoint0[axis1] - triPoint2[axis1], triPoint0[axis2] - triPoint2[axis2]);

        // Check which side P2 is relative to P0P1. The sign of this test must equal the sign of all
        // three tests between the lines and the test point for the test point to be inside the
        // triangle. (this test will also tell us if the three points are colinear - if the triangle
        // is degenerate).
        Vector2 p0P2 = new(triPoint2[axis1] - triPoint0[axis1], triPoint2[axis2] - triPoint0[axis2]);
        var p0P1P2 = Vector2.PerpendicularDotProduct(p0P1, p0P2);

        if (float.Abs(p0P1P2) >= float.Epsilon)
        {
            return HandleNonDegenerateTriangle(
                (triPoint0, triPoint1, triPoint2),
                point,
                (axis1, axis2),
                (p0P1, p1P2, p2P0),
                p0P1P2,
                out flags
            );
        }

        // The triangle is degenerate. This should be a rare case, so it does not matter much if it
        // is a little slower than the non-colinear case.
        return HandleDegenerateTriangle(
            (triPoint0, triPoint1, triPoint2),
            point,
            (axis1, axis2),
            (p0P1, p1P2, p2P0),
            out flags
        );
    }

    /// <summary>Casts a semi-infinite axis-aligned ray against a triangle to determine intersection.</summary>
    /// <param name="triPoints">Tuple containing the three vertices of the triangle (TriPoint0, TriPoint1, TriPoint2).</param>
    /// <param name="triPlane">The plane equation coefficients for the triangle in the form Ax+By+Cz+D=0.</param>
    /// <param name="rayStart">The starting point of the ray.</param>
    /// <param name="axis">Tuple containing axis indices (AxisR, Axis1, Axis2) where AxisR is the ray direction axis and Axis1, Axis2 are the projection plane axes.</param>
    /// <param name="direction">The ray direction: 0 for negative direction, 1 for positive direction along the specified axis.</param>
    /// <param name="flags">Output parameter containing flags indicating special conditions during the raycast.</param>
    /// <returns><c>true</c> if the ray intersects the triangle; otherwise, <c>false</c>.</returns>
    /// <remarks>This method performs a semi-infinite ray cast along an axis-aligned direction, first checking for 2D intersection then validating the 3D intersection using plane equations.</remarks>
    public static bool CastSemiInfiniteAxisAlignedRayToTriangle(
        (Vector3 TriPoint0, Vector3 TriPoint1, Vector3 TriPoint2) triPoints,
        [NotNull] Vector4 triPlane,
        [NotNull] Vector3 rayStart,
        (int AxisR, int Axis1, int Axis2) axis,
        int direction,
        out TriRaycasts flags
    )
    {
        (Vector3 triPoint0, Vector3 triPoint1, Vector3 triPoint2) = triPoints;
        (var axisR, var axis1, var axis2) = axis;
        flags = TriRaycasts.None;
        var retVal = false;

        // First check infinite ray vs. triangle (2D check)
        if (!PointInTriangle2D(triPoint0, triPoint1, triPoint2, rayStart, axis1, axis2, out TriRaycasts flags2D))
        {
            return retVal;
        }

        // NOTE: SR plane equations, unlike WWMath's PlaneClass, use the Ax+By+Cz+D = 0
        // representation. It can also be viewed as C0x+C1y+C2z+C3 = dist where dist is the
        // signed distance from the plane (and therefore the plane is defined as those points
        // where dist = 0). Now that we know that the projection along the ray is inside the
        // triangle, determining whether the ray hits the triangle is a matter of determining
        // whether the start point is on the triangle plane's "anti-rayward side" (the side
        // which the ray points away from).
        // To determine this, we will use C[axis_r] (the plane coefficient of the ray axis).
        // This coefficient is positive if the positive direction of the ray axis points into
        // the planes' positive halfspace and negative if it points into the planes' negative
        // halfspace (it is zero if the ray axis is parallel to the triangle plane). If we
        // multiply this by 'sign' which is defined to be -1.0 if 'direction' equals 0 and
        // 1.0 if 'direction' equals 1, we will get a number which is positive or negative
        // depending on which halfspace the ray itself (as opposed to the rays axis) points
        // towards. If we further multiply this by dist(start point) - the result of plugging
        // the start point into the plane equation - we will get a number which is positive
        // if the start point is on the 'rayward side' (ray does not intersect the triangle)
        // and is negative if the start point is on the 'anti-rayward side' (ray does
        // intersect triangle). In either of these two cases we are done.
        // (see below for what happens when the result is zero - more checks need to be made).
        Span<float> sign = [-1F, 1F];
        var result =
            triPlane[axisR]
            * sign[direction]
            * ((triPlane.X * rayStart.X) + (triPlane.Y * rayStart.Y) + (triPlane.Z * rayStart.Z) + triPlane.W);

        if (result < 0F)
        {
            flags |= flags2D & TriRaycasts.HitEdge;
            retVal = true;
        }
        else
        {
            if (float.Abs(result) >= float.Epsilon)
            {
                return retVal;
            }

            // If the result is 0, this means either the ray is parallel to the triangle
            // plane or the start point is embedded in the triangle plane. Note that since
            // the start point passed the 2D check, then if the ray is parallel the start
            // point must also be embedded in the triangle plane. This leaves us with two
            // cases:
            // A) The ray is not parallel to the plane - in this case the start point is
            // embedded in the triangle. We report an intersection, bitwise OR the edge
            // result from the 2D check into the 'edge hit' flag and set the 'start in tri'
            // flag.
            // B) The ray is parallel to the plane. In this case the result of the 2D test
            // tells us that the infinite line intersects the triangle (actually is
            // embedded in the triangle along part of its length), but we do not know
            // whether the semi-infinite ray also does so. We simplify things by not
            // counting such an 'intersection'. There are four reasons behind this:
            // 1. It differs from a 'normal' intersection (which has one intersecting
            // point) - there are infinitely many intersecting points.
            // 2. Moving the plane by an infinitesimally small amount to either side will
            // cause the ray to no longer touch the plane.
            // 3. This will not affect results for the known uses of this function.
            // 4. By doing so we avoid having to code up a bunch of complex tests.
            // Therefore in case B) we just report no intersection. We still need to find
            // out whether the point is embedded in the triangle (for setting the flag) so
            // we do another simple 2D test on the dominant plane.
            if (float.Abs(triPlane[axisR]) >= float.Epsilon)
            {
                // Case A)
                flags |= flags2D & TriRaycasts.HitEdge;
                flags |= TriRaycasts.StartInTri;
                retVal = true;
            }
            else
            {
                Tri tri = new();
                tri.V[0] = triPoint0;
                tri.V[1] = triPoint1;
                tri.V[2] = triPoint2;
                tri.N = (Vector3)triPlane;
                if (tri.ContainsPoint(rayStart))
                {
                    flags |= TriRaycasts.StartInTri;
                }
            }
        }

        return retVal;
    }

    /// <summary>Computes and sets the normal vector of the triangle based on its vertices using the cross product of two edge vectors.</summary>
    /// <remarks>The normal is calculated as the cross product of (V[1] - V[0]) and (V[2] - V[0]), then normalized to unit length. This assumes counter-clockwise winding for outward-facing normal.</remarks>
    public void ComputeNormal()
    {
        N = Vector3.CrossProduct(V[1] - V[0], V[2] - V[0]);
        N.Normalize();
    }

    /// <summary>Finds the dominant plane for 2D projection by determining which axis has the largest component in the triangle's normal vector.</summary>
    /// <param name="axis1">Output parameter for the first axis index of the dominant projection plane.</param>
    /// <param name="axis2">Output parameter for the second axis index of the dominant projection plane.</param>
    /// <remarks>This method selects the projection plane that will provide the most accurate 2D representation of the triangle by choosing the plane perpendicular to the dominant normal component.</remarks>
    public void FindDominantPlane(out int axis1, out int axis2)
    {
        axis1 = 0;
        axis2 = 0;

        var ni = 0;
        var x = float.Abs(N.X);
        var y = float.Abs(N.Y);
        var z = float.Abs(N.Z);
        var val = x;

        if (y > val)
        {
            ni = 1;
            val = y;
        }

        if (z > val)
        {
            ni = 2;
        }

        switch (ni)
        {
            case 0:
                // Dominant is the X axis
                axis1 = 1;
                axis2 = 2;
                break;
            case 1:
                // Dominant is the Y axis
                axis1 = 0;
                axis2 = 2;
                break;
            case 2:
                // Dominant is the Z axis
                axis1 = 0;
                axis2 = 1;
                break;
            default:
                break;
        }
    }

    /// <summary>Determines whether the specified point lies within the triangle boundaries using 2D projection onto the dominant plane.</summary>
    /// <param name="point">The point to test for containment within the triangle.</param>
    /// <returns><c>true</c> if the point is inside the triangle; otherwise, <c>false</c>.</returns>
    /// <remarks>This method projects the triangle and test point onto the dominant plane determined by the normal vector, then performs a 2D point-in-triangle test using cross products to determine which side of each edge the point lies on.</remarks>
    public bool ContainsPoint([NotNull] Vector3 point)
    {
        FindDominantPlane(this, out var axis1, out var axis2, out _);
        Span<bool> side = stackalloc bool[3];
        Vector2 edge = new();
        Vector2 dp = new();
        for (var i = 0; i < 3; i++)
        {
            var va = i;
            var vb = (i + 1) % 3;
            edge.Set(V[vb][axis1] - V[va][axis1], V[vb][axis2] - V[va][axis2]);
            dp.Set(point[axis1] - V[va][axis1], point[axis2] - V[va][axis2]);
            var cross = (edge.X * dp.Y) - (edge.Y * dp.X);
            side[i] = cross >= 0F;
        }

        return side[0] == side[1] && side[1] == side[2];
    }

    private static void FindDominantPlane(Tri tri, out int axis1, out int axis2, out int axis3)
    {
        axis1 = 0;
        axis2 = 0;
        axis3 = 0;

        var ni = 0;
        var x = float.Abs(tri.N.X);
        var y = float.Abs(tri.N.Y);
        var z = float.Abs(tri.N.Z);
        var val = x;

        if (y > val)
        {
            ni = 1;
            val = y;
        }

        if (z > val)
        {
            ni = 2;
        }

        switch (ni)
        {
            case 0:
                // Dominant is the X axis
                axis1 = 1;
                axis2 = 2;
                axis3 = 0;
                break;
            case 1:
                // Dominant is the Y axis
                axis1 = 0;
                axis2 = 2;
                axis3 = 1;
                break;
            case 2:
                // Dominant is the Z axis
                axis1 = 0;
                axis2 = 1;
                axis3 = 2;
                break;
            default:
                break;
        }
    }

    private static bool HandleNonDegenerateTriangle(
        (Vector3 TriPoint0, Vector3 TriPoint1, Vector3 TriPoint2) triPoints,
        Vector3 point,
        (int Axis1, int Axis2) axis,
        (Vector2 P0P1, Vector2 P1P2, Vector2 P2P0) points,
        float p0P1P2,
        out TriRaycasts flags
    )
    {
        (Vector3 triPoint0, Vector3 triPoint1, Vector3 triPoint2) = triPoints;
        (var axis1, var axis2) = axis;
        (Vector2 p0P1, Vector2 p1P2, Vector2 p2P0) = points;

        flags = TriRaycasts.None;
        var sideFactor = p0P1P2 > 0F ? 1F : -1F;
        var factors = new float[3];

        Vector2 p0Pt = new(point[axis1] - triPoint0[axis1], point[axis2] - triPoint0[axis2]);
        factors[0] = Vector2.PerpendicularDotProduct(p0P1, p0Pt);
        if (factors[0] * sideFactor < 0F)
        {
            return false;
        }

        Vector2 p1Pt = new(point[axis1] - triPoint1[axis1], point[axis2] - triPoint1[axis2]);
        factors[1] = Vector2.PerpendicularDotProduct(p1P2, p1Pt);
        if (factors[1] * sideFactor < 0F)
        {
            return false;
        }

        Vector2 p2Pt = new(point[axis1] - triPoint2[axis1], point[axis2] - triPoint2[axis2]);
        factors[2] = Vector2.PerpendicularDotProduct(p2P0, p2Pt);
        if (factors[2] * sideFactor < 0F)
        {
            return false;
        }

        if ((float.Abs(factors[0]) < 0F) || (float.Abs(factors[1]) < 0F) || (float.Abs(factors[2]) < 0F))
        {
            flags |= TriRaycasts.HitEdge;
        }

        return true;
    }

    private static (Vector2 pSpe, Vector2 pSpt, float maxDist2) FindOuterPoints(
        (Vector3 TriPoint0, Vector3 TriPoint1, Vector3 TriPoint2) triPoints,
        Vector3 point,
        (int Axis1, int Axis2) axis,
        (Vector2 P0P1, Vector2 P1P2, Vector2 P2P0) points,
        (float P0P1Dist2, float P1P2Dist2, float P2P0Dist2) distances
    )
    {
        (Vector3 triPoint0, Vector3 triPoint1, Vector3 triPoint2) = triPoints;
        (var axis1, var axis2) = axis;
        (Vector2 p0P1, Vector2 p1P2, Vector2 p2P0) = points;
        (var p0P1Dist2, var p1P2Dist2, var p2P0Dist2) = distances;

        Vector2 pSpe;
        Vector2 pSpt = new();
        float maxDist2;

        if (p0P1Dist2 > p1P2Dist2)
        {
            if (p0P1Dist2 > p2P0Dist2)
            {
                // points 0 and 1 are the 'outer' points. 0 is 'start' and 1 is 'end'.
                pSpe = p0P1;
                pSpt.Set(point[axis1] - triPoint0[axis1], point[axis2] - triPoint0[axis2]);
                maxDist2 = p0P1Dist2;
            }
            else
            {
                // points 0 and 2 are the 'outer' points. 2 is 'start' and 0 is 'end'.
                pSpe = p2P0;
                pSpt.Set(point[axis1] - triPoint2[axis1], point[axis2] - triPoint2[axis2]);
                maxDist2 = p2P0Dist2;
            }
        }
        else
        {
            if (p1P2Dist2 > p2P0Dist2)
            {
                // points 1 and 2 are the 'outer' points. 1 is 'start' and 2 is 'end'.
                pSpe = p1P2;
                pSpt.Set(point[axis1] - triPoint1[axis1], point[axis2] - triPoint1[axis2]);
                maxDist2 = p1P2Dist2;
            }
            else
            {
                // points 0 and 2 are the 'outer' points. 2 is 'start' and 0 is 'end'.
                pSpe = p2P0;
                pSpt.Set(point[axis1] - triPoint2[axis1], point[axis2] - triPoint2[axis2]);
                maxDist2 = p2P0Dist2;
            }
        }

        return (pSpe, pSpt, maxDist2);
    }

    private static bool HandleLineSegmentCase(Vector2 pSpe, Vector2 pSpt, float maxDist2, out TriRaycasts flags)
    {
        flags = TriRaycasts.None;

        // Triangle is line segment, check if test point is colinear with it
        if (float.Abs(Vector2.PerpendicularDotProduct(pSpe, pSpt)) < float.Epsilon)
        {
            // Not colinear
            return false;
        }

        // Colinear - is test point's distance from start and end <= segment length?
        Vector2 pEpt = pSpt - pSpe;
        if (pSpt.Length2 > maxDist2 || pEpt.Length2 > maxDist2)
        {
            return false;
        }

        flags |= TriRaycasts.HitEdge;
        return true;
    }

    private static bool HandleDegenerateTriangle(
        (Vector3 TriPoint0, Vector3 TriPoint1, Vector3 TriPoint2) triPoints,
        Vector3 point,
        (int Axis1, int Axis2) axis,
        (Vector2 P0P1, Vector2 P1P2, Vector2 P2P0) points,
        out TriRaycasts flags
    )
    {
        (Vector3 triPoint0, Vector3 triPoint1, Vector3 triPoint2) = triPoints;
        (var axis1, var axis2) = axis;
        (Vector2 p0P1, Vector2 p1P2, Vector2 p2P0) = points;

        flags = TriRaycasts.None;

        // Find the two outer points along the triangle's line ('start' and 'end' points)
        var p0P1Dist2 = p0P1.Length2;
        var p1P2Dist2 = p1P2.Length2;
        var p2P0Dist2 = p1P2.Length2; // BUG: I feel this was a bug in the game engine?

        (Vector2 pSpe, Vector2 pSpt, var maxDist2) = FindOuterPoints(
            (triPoint0, triPoint1, triPoint2),
            point,
            (axis1, axis2),
            (p0P1, p1P2, p2P0),
            (p0P1Dist2, p1P2Dist2, p2P0Dist2)
        );

        if (float.Abs(maxDist2) >= float.Epsilon)
        {
            return HandleLineSegmentCase(pSpe, pSpt, maxDist2, out flags);
        }

        // All triangle points coincide, check if test point coincides with them
        if (float.Abs(pSpt.Length2) >= float.Epsilon)
        {
            return false;
        }

        flags |= TriRaycasts.HitEdge;
        return true;
    }
}
