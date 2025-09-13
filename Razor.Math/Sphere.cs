// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Sphere : IEqualityComparer<Sphere>
{
    public Vector3 Center { get; set; } = new();
    public float Radius { get; set; }

    public float Volume => (4F / 3F) * float.Pi * float.Pow(Radius, 3);

    public Sphere() { }

    public Sphere(Sphere other)
    {
        Initialize(other.Center, other.Radius);
    }

    public Sphere(Vector3 center, float radius)
    {
        Initialize(center, radius);
    }

    public Sphere(Matrix3D matrix, Vector3 position, float radius)
    {
        Initialize(matrix, position, radius);
    }

    public Sphere(Vector3 center, Sphere s0)
    {
        var dist = (s0.Center - center).Length;
        Center = center;
        Radius = s0.Radius + dist;
    }

    public Sphere(Vector3[] positions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(positions.Length, 1);

        Vector3 xMin = new(positions[0].X, positions[0].Y, positions[0].Z);
        Vector3 xMax = new(positions[0].X, positions[0].Y, positions[0].Z);
        Vector3 yMin = new(positions[0].X, positions[0].Y, positions[0].Z);
        Vector3 yMax = new(positions[0].X, positions[0].Y, positions[0].Z);
        Vector3 zMin = new(positions[0].X, positions[0].Y, positions[0].Z);
        Vector3 zMax = new(positions[0].X, positions[0].Y, positions[0].Z);

        SphereInitializationWithVectorArrayFirstPass(
            positions,
            ref xMin,
            ref xMax,
            ref yMin,
            ref yMax,
            ref zMin,
            ref zMax
        );

        var dx = xMax.X - xMin.X;
        var dy = yMax.Y - yMin.Y;
        var dz = zMax.Z - zMin.Z;
        var xSpan = double.Pow(dx, 2) + double.Pow(dy, 2) + double.Pow(dz, 2);

        dx = yMax.X - yMin.X;
        dy = zMax.Y - zMin.Y;
        dz = xMax.Z - xMin.Z;
        var ySpan = double.Pow(dx, 2) + double.Pow(dy, 2) + double.Pow(dz, 2);

        dx = zMax.X - zMin.X;
        dy = xMax.Y - xMin.Y;
        dz = yMax.Z - yMin.Z;
        var zSpan = double.Pow(dx, 2) + double.Pow(dy, 2) + double.Pow(dz, 2);

        var dia1 = xMin;
        var dia2 = xMax;
        var maxSpan = xSpan;

        if (ySpan > maxSpan)
        {
            maxSpan = ySpan;
            dia1 = yMin;
            dia2 = yMax;
        }

        if (zSpan > maxSpan)
        {
            dia1 = zMin;
            dia2 = zMax;
        }

        Vector3 center = new(
            (dia1.X + dia2.X) / 2F,
            (dia1.Y + dia2.Y) / 2F,
            (dia1.Z + dia2.Z) / 2F
        );

        dx = dia2.X - center.X;
        dy = dia2.Y - center.Y;
        dz = dia2.Z - center.Z;

        var radSqr = double.Pow(dx, 2) + double.Pow(dy, 2) + double.Pow(dz, 2);
        var radius = double.Sqrt(radSqr);

        SphereInitializationWithVectorArraySecondPass(
            positions,
            ref center,
            ref radius,
            ref radSqr,
            ref dx,
            ref dy,
            ref dz
        );

        Center = center;
        Radius = (float)radius;
    }

    public static bool Intersects(Sphere left, Sphere right)
    {
        var delta = left.Center - right.Center;
        var dist2 = Vector3.DotProduct(delta, delta);
        return dist2 < (left.Radius + right.Radius) * (left.Radius + right.Radius);
    }

    public static Sphere Add(Sphere left, Sphere right)
    {
        if (float.Abs(left.Radius) < float.Epsilon) // Basically 0F
        {
            return right;
        }

        Sphere result = new(left);
        result.AddSphere(right);
        return result;
    }

    public static Sphere Transform(Matrix3D matrix, Sphere sphere)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(matrix.IsOrthogonal, true);
        return new Sphere(matrix, sphere.Center, sphere.Radius);
    }

    public void Initialize(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public void Initialize(Matrix3D matrix, Vector3 position, float radius)
    {
        Center = matrix * position;
        Radius = radius;
    }

    public void Recenter(Vector3 center)
    {
        var dist = (Center - center).Length;
        Center = center;
        Radius += dist;
    }

    public void AddSphere(Sphere sphere)
    {
        if (float.Abs(sphere.Radius - Radius) < float.Epsilon) // Basically 0F
        {
            return;
        }

        var dist = (sphere.Center - Center).Length;
        if (float.Abs(dist) < float.Epsilon) // Basically 0F
        {
            Radius = (Radius > sphere.Radius) ? Radius : sphere.Radius;
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
            var center = (sphere.Center - Center) * lerp + Center;
            Initialize(center, rNew);
        }
    }

    public void Transform(Matrix3D matrix)
    {
        Center *= matrix;
    }

    public override bool Equals(object? obj)
    {
        return obj is Sphere other && Equals(this, other);
    }

    public bool Equals(Sphere? x, Sphere? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null)
        {
            return false;
        }

        if (y is null)
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.Center == y.Center && System.Math.Abs(x.Radius - y.Radius) < float.Epsilon;
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Sphere obj)
    {
        return HashCode.Combine(obj.Center, obj.Radius);
    }

    public override string ToString()
    {
        return $"({Center}, {Radius})";
    }

    public string ToString(string? format)
    {
        return $"({Center.ToString(format)}, {Radius.ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({Center.ToString(format, formatProvider)}, {Radius.ToString(format, formatProvider)})";
    }

    public (Vector3 Center, float Radius) Deconstruct()
    {
        return (Center, Radius);
    }

    private static void SphereInitializationWithVectorArrayFirstPass(
        Vector3[] positions,
        ref Vector3 xMin,
        ref Vector3 xMax,
        ref Vector3 yMin,
        ref Vector3 yMax,
        ref Vector3 zMin,
        ref Vector3 zMax
    )
    {
        for (var i = 1; i < positions.Length; i++)
        {
            if (positions[i].X < xMin.X)
            {
                xMin.X = positions[i].X;
                xMin.Y = positions[i].Y;
                xMin.Z = positions[i].Z;
            }

            if (positions[i].X > xMax.X)
            {
                xMax.X = positions[i].X;
                xMax.Y = positions[i].Y;
                xMax.Z = positions[i].Z;
            }

            if (positions[i].Y < yMin.Y)
            {
                yMin.Y = positions[i].Y;
                yMin.X = positions[i].X;
                yMin.Z = positions[i].Z;
            }

            if (positions[i].Y > yMax.Y)
            {
                yMax.Y = positions[i].Y;
                yMax.X = positions[i].X;
                yMax.Z = positions[i].Z;
            }

            if (positions[i].Z < zMin.Z)
            {
                zMin.Z = positions[i].Z;
                zMin.X = positions[i].X;
                zMin.Y = positions[i].Y;
            }

            if (positions[i].Z > zMax.Z)
            {
                zMax.Z = positions[i].Z;
                zMax.X = positions[i].X;
                zMax.Y = positions[i].Y;
            }
        }
    }

    private static void SphereInitializationWithVectorArraySecondPass(
        Vector3[] positions,
        ref Vector3 center,
        ref double radius,
        ref double radSqr,
        ref float dx,
        ref float dy,
        ref float dz
    )
    {
        foreach (var position in positions)
        {
            dx = position.X - center.X;
            dy = position.Y - center.Y;
            dz = position.Z - center.Z;

            var testRad2 = double.Pow(dx, 2) + double.Pow(dy, 2) + double.Pow(dz, 2);
            if (testRad2 <= radSqr)
            {
                continue;
            }

            var testRad = double.Sqrt(testRad2);

            radius = (radius + testRad) / 2.0;
            radSqr = radius * radius;

            var oldToNew = testRad - radius;
            center.X = (float)((radius * center.X + oldToNew * position.X) / testRad);
            center.Y = (float)((radius * center.Y + oldToNew * position.Y) / testRad);
            center.Z = (float)((radius * center.Z + oldToNew * position.Z) / testRad);
        }
    }

    public static Sphere operator +(Sphere x, Sphere y)
    {
        return Add(x, y);
    }

    public static Sphere operator *(Sphere sphere, Matrix3D matrix)
    {
        Sphere result = new(sphere);
        result.Initialize(matrix, sphere.Center, sphere.Radius);
        return result;
    }

    public static Sphere operator *(Matrix3D matrix, Sphere sphere)
    {
        return Transform(matrix, sphere);
    }

    public static bool operator ==(Sphere x, Sphere y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(Sphere x, Sphere y)
    {
        return !x.Equals(y);
    }
}
