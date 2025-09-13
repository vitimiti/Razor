// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Rect : IEqualityComparer<Rect>
{
    public float Left { get; set; }
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }

    public float Width => Right - Left;
    public float Height => Bottom - Top;

    public Vector2 Center => new((Left + Right) / 2F, (Top + Bottom) / 2F);
    public Vector2 Extent => new((Right - Left) / 2F, (Bottom - Top) / 2F);
    public Vector2 UpperLeft => new(Left, Top);
    public Vector2 LowerRight => new(Right, Bottom);
    public Vector2 UpperRight => new(Right, Top);
    public Vector2 LowerLeft => new(Left, Bottom);

    public Rect() { }

    public Rect(Rect other)
    {
        Set(other);
    }

    public Rect(float left, float top, float right, float bottom)
    {
        Set(left, top, right, bottom);
    }

    public Rect(Vector2 topLeft, Vector2 bottomRight)
    {
        Set(topLeft, bottomRight);
    }

    public void Set(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public void Set(Vector2 topLeft, Vector2 bottomRight)
    {
        Left = topLeft.X;
        Top = topLeft.Y;
        Right = bottomRight.X;
        Bottom = bottomRight.Y;
    }

    public void Set(Rect other)
    {
        Left = other.Left;
        Top = other.Top;
        Right = other.Right;
        Bottom = other.Bottom;
    }

    public Rect ScaleRelativeCenter(float scale)
    {
        var temp = this;
        temp -= Center;
        temp *= scale;

        Left = temp.Left;
        Top = temp.Top;
        Right = temp.Right;
        Bottom = temp.Bottom;

        return temp;
    }

    public Rect Scale(float scale)
    {
        var temp = this;
        temp *= scale;

        Left = temp.Left;
        Top = temp.Top;
        Right = temp.Right;
        Bottom = temp.Bottom;

        return temp;
    }

    public Rect Scale(Vector2 scale)
    {
        var temp = this;

        temp.Left *= scale.X;
        temp.Top *= scale.Y;
        temp.Right *= scale.X;
        temp.Bottom *= scale.Y;

        Left = temp.Left;
        Top = temp.Top;
        Right = temp.Right;
        Bottom = temp.Bottom;

        return temp;
    }

    public Rect InverseScale(Vector2 scale)
    {
        var temp = this;

        temp.Left /= scale.X;
        temp.Top /= scale.Y;
        temp.Right /= scale.X;
        temp.Bottom /= scale.Y;

        Left = temp.Left;
        Top = temp.Top;
        Right = temp.Right;
        Bottom = temp.Bottom;

        return temp;
    }

    public void Inflate(Vector2 vector)
    {
        Left -= vector.X;
        Top -= vector.Y;
        Right += vector.X;
        Bottom += vector.Y;
    }

    public bool Contains(Vector2 position)
    {
        return position.X >= Left
            && position.X <= Right
            && position.Y >= Top
            && position.Y <= Bottom;
    }

    public void SnapToUnits(Vector2 units)
    {
        Left = (int)(Left / units.X + .5F) * units.X;
        Top = (int)(Top / units.Y + .5F) * units.Y;
        Right = (int)(Right / units.X + .5F) * units.X;
        Bottom = (int)(Bottom / units.Y + .5F) * units.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Rect other && Equals(this, other);
    }

    public bool Equals(Rect? x, Rect? y)
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

        return float.Abs(x.Left - y.Left) < float.Epsilon
            && float.Abs(x.Top - y.Top) < float.Epsilon
            && float.Abs(x.Right - y.Right) < float.Epsilon
            && float.Abs(x.Bottom - y.Bottom) < float.Epsilon;
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Rect obj)
    {
        return HashCode.Combine(obj.Left, obj.Top, obj.Right, obj.Bottom);
    }

    public override string ToString()
    {
        return $"({Left}, {Top}, {Right}, {Bottom})";
    }

    public string ToString(string? format)
    {
        return $"({Left.ToString(format)}, {Top.ToString(format)}, {Right.ToString(format)}, {Bottom.ToString(format)})";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"({Left.ToString(format, formatProvider)}, {Top.ToString(format, formatProvider)}, {Right.ToString(format, formatProvider)}, {Bottom.ToString(format, formatProvider)})";
    }

    public (float Left, float Top, float Right, float Bottom) Deconstruct()
    {
        return (Left, Top, Right, Bottom);
    }

    public static Rect operator +(Rect x, Rect y)
    {
        return new Rect(
            float.Min(x.Left, y.Left),
            float.Min(x.Top, y.Top),
            float.Max(x.Right, y.Right),
            float.Max(x.Bottom, y.Bottom)
        );
    }

    public static Rect operator +(Rect rect, Vector2 vector)
    {
        return new Rect(
            rect.Left + vector.X,
            rect.Top + vector.Y,
            rect.Right + vector.X,
            rect.Bottom + vector.Y
        );
    }

    public static Rect operator -(Rect rect, Vector2 vector)
    {
        return new Rect(
            rect.Left - vector.X,
            rect.Top - vector.Y,
            rect.Right - vector.X,
            rect.Bottom - vector.Y
        );
    }

    public static Rect operator *(Rect obj, float scalar)
    {
        return new Rect(
            obj.Left * scalar,
            obj.Top * scalar,
            obj.Right * scalar,
            obj.Bottom * scalar
        );
    }

    public static Rect operator *(float scalar, Rect obj)
    {
        return obj * scalar;
    }

    public static Rect operator /(Rect obj, float scalar)
    {
        var oScalar = 1F / scalar;
        return obj * oScalar;
    }

    public static bool operator ==(Rect x, Rect y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(Rect x, Rect y)
    {
        return !x.Equals(y);
    }
}
