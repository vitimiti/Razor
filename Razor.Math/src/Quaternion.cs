// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Quaternion : IEqualityComparer<Quaternion>
{
    private static int[] s_next = [1, 2, 0];

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public bool IsValid =>
        ExtraMath.IsValid(X)
        && ExtraMath.IsValid(Y)
        && ExtraMath.IsValid(Z)
        && ExtraMath.IsValid(W);

    public float Length2 => float.Pow(X, 2) + float.Pow(Y, 2) + float.Pow(Z, 2) + float.Pow(W, 2);

    public float Length => float.Sqrt(Length2);

    public Quaternion Normalized
    {
        get
        {
            var mag = Length;
            if (float.Abs(mag) < float.Epsilon) // Basically 0F
            {
                return this;
            }

            var oMag = 1F / mag;
            return new Quaternion(X * oMag, Y * oMag, Z * oMag, W * oMag);
        }
    }

    public float this[int index]
    {
        get =>
            index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => W,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Index must be between 0 and 3."
                ),
            };
        set
        {
            switch (index)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                case 2:
                    Z = value;
                    break;
                case 3:
                    W = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        index,
                        "Index must be between 0 and 3."
                    );
            }
        }
    }

    public Quaternion() { }

    public Quaternion(float x, float y, float z, float w)
    {
        Set(x, y, z, w);
    }

    public Quaternion(Vector3 axis, float angle)
    {
        var sin = float.Sin(angle / 2F);
        var cos = float.Cos(angle / 2F);
        X = sin * axis.X;
        Y = sin * axis.Y;
        Z = sin * axis.Z;
        W = cos;
    }

    public static Quaternion Build(Matrix3D matrix)
    {
        Quaternion result = new();

        var tr = matrix[0][0] + matrix[1][1] + matrix[2][2];
        if (tr > 0F)
        {
            var s = float.Sqrt(tr + 1F);
            result[3] = s * .5F;
            s = .5F / s;

            result[0] = (matrix[2][1] - matrix[1][2]) * s;
            result[1] = (matrix[0][2] - matrix[2][0]) * s;
            result[2] = (matrix[1][0] - matrix[0][1]) * s;
        }
        else
        {
            var i = 0;
            if (matrix[1][1] > matrix[0][0])
            {
                i = 1;
            }

            if (matrix[2][2] > matrix[i][i])
            {
                i = 2;
            }

            var j = s_next[i];
            var k = s_next[j];

            var s = float.Sqrt(matrix[i][i] - matrix[j][j] - matrix[k][k] + 1F);
            result[i] = s * .5F;
            // Essentially s != 0F
            if (float.Abs(s) > float.Epsilon)
            {
                s = .5F / s;
            }

            result[3] = (matrix[k][j] - matrix[j][k]) * s;
            result[j] = (matrix[j][i] + matrix[i][j]) * s;
            result[k] = (matrix[k][i] + matrix[i][k]) * s;
        }

        return result;
    }

    public static Quaternion Build(Matrix3X3 matrix)
    {
        Quaternion result = new();

        var tr = matrix[0][0] + matrix[1][1] + matrix[2][2];
        if (tr > 0F)
        {
            var s = float.Sqrt(tr + 1F);
            result[3] = s * .5F;
            s = .5F / s;

            result[0] = (matrix[2][1] - matrix[1][2]) * s;
            result[1] = (matrix[0][2] - matrix[2][0]) * s;
            result[2] = (matrix[1][0] - matrix[0][1]) * s;
        }
        else
        {
            var i = 0;
            if (matrix[1][1] > matrix[0][0])
            {
                i = 1;
            }

            if (matrix[2][2] > matrix[i][i])
            {
                i = 2;
            }

            var j = s_next[i];
            var k = s_next[j];

            var s = float.Sqrt(matrix[i][i] - matrix[j][j] - matrix[k][k] + 1F);
            result[i] = s * .5F;

            // Essentially s != 0F
            if (float.Abs(s) > float.Epsilon)
            {
                s = .5F / s;
            }

            result[3] = (matrix[k][j] - matrix[j][k]) * s;
            result[j] = (matrix[j][i] + matrix[i][j]) * s;
            result[k] = (matrix[k][i] + matrix[i][k]) * s;
        }

        return result;
    }

    public static Quaternion Build(Matrix4X4 matrix)
    {
        Quaternion result = new();

        var tr = matrix[0][0] + matrix[1][1] + matrix[2][2];
        if (tr > 0F)
        {
            var s = float.Sqrt(tr + 1F);
            result[3] = s * .5F;
            s = .5F / s;

            result[0] = (matrix[2][1] - matrix[1][2]) * s;
            result[1] = (matrix[0][2] - matrix[2][0]) * s;
            result[2] = (matrix[1][0] - matrix[0][1]) * s;
        }
        else
        {
            var i = 0;
            if (matrix[1][1] > matrix[0][0])
            {
                i = 1;
            }

            if (matrix[2][2] > matrix[i][i])
            {
                i = 2;
            }

            var j = s_next[i];
            var k = s_next[j];

            var s = float.Sqrt(matrix[i][i] - matrix[j][j] - matrix[k][k] + 1F);
            result[i] = s * .5F;

            // Essentially s != 0F
            if (float.Abs(s) > float.Epsilon)
            {
                s = .5F / s;
            }

            result[3] = (matrix[k][j] - matrix[j][k]) * s;
            result[j] = (matrix[j][i] + matrix[i][j]) * s;
            result[k] = (matrix[k][i] + matrix[i][k]) * s;
        }

        return result;
    }

    public static Matrix3D BuildMatrix3D(Quaternion quaternion)
    {
        return new Matrix3D(
            [
                1F - 2F * (quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2]),
                2F * (quaternion[0] * quaternion[1] - quaternion[2] * quaternion[3]),
                2F * (quaternion[2] * quaternion[0] + quaternion[1] * quaternion[3]),
                0F,
                2F * (quaternion[0] * quaternion[1] + quaternion[2] * quaternion[3]),
                1F - 2.0f * (quaternion[2] * quaternion[2] + quaternion[0] * quaternion[0]),
                2F * (quaternion[1] * quaternion[2] - quaternion[0] * quaternion[3]),
                0F,
                2F * (quaternion[2] * quaternion[0] - quaternion[1] * quaternion[3]),
                2F * (quaternion[1] * quaternion[2] + quaternion[0] * quaternion[3]),
                1F - 2F * (quaternion[1] * quaternion[1] + quaternion[0] * quaternion[0]),
                0F,
            ]
        );
    }

    public static Matrix3X3 BuildMatrix3X3(Quaternion quaternion)
    {
        return new Matrix3X3(
            (
                1F - 2F * (quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2]),
                2F * (quaternion[0] * quaternion[1] - quaternion[2] * quaternion[3]),
                2F * (quaternion[2] * quaternion[0] + quaternion[1] * quaternion[3])
            ),
            (
                2F * (quaternion[0] * quaternion[1] + quaternion[2] * quaternion[3]),
                1F - 2.0f * (quaternion[2] * quaternion[2] + quaternion[0] * quaternion[0]),
                2F * (quaternion[1] * quaternion[2] - quaternion[0] * quaternion[3])
            ),
            (
                2F * (quaternion[2] * quaternion[0] - quaternion[1] * quaternion[3]),
                2F * (quaternion[1] * quaternion[2] + quaternion[0] * quaternion[3]),
                1F - 2F * (quaternion[1] * quaternion[1] + quaternion[0] * quaternion[0])
            )
        );
    }

    public static Matrix4X4 BuildMatrix4X4(Quaternion quaternion)
    {
        return new Matrix4X4(
            (
                1F - 2F * (quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2]),
                2F * (quaternion[0] * quaternion[1] - quaternion[2] * quaternion[3]),
                2F * (quaternion[2] * quaternion[0] + quaternion[1] * quaternion[3]),
                0F
            ),
            (
                2F * (quaternion[0] * quaternion[1] + quaternion[2] * quaternion[3]),
                1F - 2.0f * (quaternion[2] * quaternion[2] + quaternion[0] * quaternion[0]),
                2F * (quaternion[1] * quaternion[2] - quaternion[0] * quaternion[3]),
                0F
            ),
            (
                2F * (quaternion[2] * quaternion[0] - quaternion[1] * quaternion[3]),
                2F * (quaternion[1] * quaternion[2] + quaternion[0] * quaternion[3]),
                1F - 2F * (quaternion[1] * quaternion[1] + quaternion[0] * quaternion[0]),
                0F
            ),
            (0F, 0F, 0F, 1F)
        );
    }

    public static Quaternion Inverse(Quaternion quaternion)
    {
        return -quaternion;
    }

    public static Quaternion Conjugate(Quaternion quaternion)
    {
        return -quaternion;
    }

    public static Quaternion Slerp(Quaternion p, Quaternion q, float alpha)
    {
        var cosTheta =
            float.Pow(p.X, 2) + float.Pow(p.Y, 2) + float.Pow(p.Z, 2) + float.Pow(p.W, 2);

        bool qFlip;
        if (cosTheta < 0F)
        {
            cosTheta = -cosTheta;
            qFlip = true;
        }
        else
        {
            qFlip = false;
        }

        float beta;
        if (1F - cosTheta > float.Pow(float.Epsilon, 2))
        {
            beta = 1F - alpha;
        }
        else
        {
            var theta = float.Acos(cosTheta);
            var sinTheta = float.Sin(theta);
            var oOSinTheta = 1F / sinTheta;
            beta = float.Sin(theta - alpha * theta) * oOSinTheta;
            alpha = float.Sin(alpha * theta) * oOSinTheta;
        }

        if (qFlip)
        {
            alpha = -alpha;
        }

        return new Quaternion(
            beta * p.X + alpha * q.X,
            beta * p.Y + alpha * q.Y,
            beta * p.Z + alpha * q.Z,
            beta * p.W + alpha * q.W
        );
    }

    public static Quaternion FastSlerp(Quaternion p, Quaternion q, float alpha)
    {
        var cosT = float.Pow(p.X, 2) + float.Pow(p.Y, 2) + float.Pow(p.Z, 2) + float.Pow(p.W, 2);

        bool qFlip;
        if (cosT < 0F)
        {
            cosT = -cosT;
            qFlip = true;
        }
        else
        {
            qFlip = false;
        }

        float beta;
        if (1F - cosT < float.Pow(float.Epsilon, 2))
        {
            beta = 1F - alpha;
        }
        else
        {
            var theta = ExtraMath.FastAcos(cosT);
            var sinT = ExtraMath.FastSin(theta);
            var oSinT = 1F / sinT;
            beta = ExtraMath.FastSin(theta - alpha * theta) * oSinT;
            alpha = ExtraMath.FastSin(alpha * theta) * oSinT;
        }

        if (qFlip)
        {
            alpha = -alpha;
        }

        return new Quaternion(
            beta * p.X + alpha * q.X,
            beta * p.Y + alpha * q.Y,
            beta * p.Z + alpha * q.Z,
            beta * p.W + alpha * q.W
        );
    }

    public static void SlerpSetup(Quaternion p, Quaternion q, out QuaternionSlerpInfo slerpInfo)
    {
        slerpInfo = new QuaternionSlerpInfo(0, 0, false, false);
        var cosT = float.Pow(p.X, 2) + float.Pow(p.Y, 2) + float.Pow(p.Z, 2) + float.Pow(p.W, 2);

        if (cosT < 0F)
        {
            cosT = -cosT;
            slerpInfo.Flip = true;
        }
        else
        {
            slerpInfo.Flip = false;
        }

        if (float.Abs(1F - cosT) < float.Epsilon)
        {
            slerpInfo.Linear = true;
            slerpInfo.Theta = 0F;
            slerpInfo.SinT = 0F;
        }
        else
        {
            slerpInfo.Linear = false;
            slerpInfo.Theta = float.Acos(cosT);
            slerpInfo.SinT = float.Sin(slerpInfo.Theta);
        }
    }

    public static Quaternion CachedSlerp(
        Quaternion p,
        Quaternion q,
        float alpha,
        QuaternionSlerpInfo slerpInfo
    )
    {
        float beta;
        float oSinT;
        if (slerpInfo.Linear)
        {
            beta = 1F - alpha;
        }
        else
        {
            oSinT = 1F / slerpInfo.Theta;
            beta = float.Sin(slerpInfo.Theta - alpha * slerpInfo.Theta) * oSinT;
            alpha = float.Sin(alpha * slerpInfo.Theta) * oSinT;
        }

        if (slerpInfo.Flip)
        {
            alpha = -alpha;
        }

        return new Quaternion(
            beta * p.X + alpha * q.X,
            beta * p.Y + alpha * q.Y,
            beta * p.Z + alpha * q.Z,
            beta * p.W + alpha * q.W
        );
    }

    public static Quaternion Trackball(float x0, float y0, float x1, float y1, float sphSize)
    {
        if (float.Abs(x0 - x1) < float.Epsilon && float.Abs(y0 - y1) < float.Epsilon)
        {
            return new Quaternion(0F, 0F, 0F, 1F);
        }

        Vector3 p1 = new();
        Vector3 p2 = new();

        p1[0] = x0;
        p1[1] = y0;
        p1[2] = ProjectToSphere(sphSize, x0, y0);

        p2[0] = x1;
        p2[1] = y1;
        p2[2] = ProjectToSphere(sphSize, x1, y1);

        var a = Vector3.CrossProduct(p2, p1);

        var d = p1 - p2;
        var t = d.Length / (2F * sphSize);
        t = float.Clamp(t, -1F, 1F);
        var phi = 2F * float.Asin(t);

        return AxisToQuat(a, phi);
    }

    public static Quaternion AxisToQuat(Vector3 axis, float phi)
    {
        Quaternion quaternion = new();
        var tmp = axis;

        tmp.Normalize();
        quaternion[0] = tmp[0];
        quaternion[1] = tmp[1];
        quaternion[2] = tmp[2];

        quaternion.Scale(float.Sin(phi / 2F));
        quaternion[3] = float.Cos(phi / 2F);

        return quaternion;
    }

    public void Set(float a = 0F, float b = 0F, float c = 0F, float d = 0F)
    {
        X = a;
        Y = b;
        Z = c;
        W = d;
    }

    public void Normalize()
    {
        var len2 = float.Pow(X, 2) + float.Pow(Y, 2) + float.Pow(Z, 2) + float.Pow(W, 2);
        if (float.Abs(len2) < float.Epsilon) // Basically 0F
        {
            return;
        }

        var invMag = ExtraMath.InvSqrt(len2);
        X *= invMag;
        Y *= invMag;
        Z *= invMag;
        W *= invMag;
    }

    public void MakeIdentity()
    {
        Set();
    }

    public Quaternion MakeClosest(Quaternion quaternion)
    {
        var cosT =
            float.Pow(quaternion.X, 2)
            + float.Pow(quaternion.Y, 2)
            + float.Pow(quaternion.Z, 2)
            + float.Pow(quaternion.W, 2);

        if (cosT >= 0F)
        {
            return this;
        }

        X = -X;
        Y = -Y;
        Z = -Z;
        W = -W;

        return this;
    }

    public void Scale(float scale)
    {
        X *= scale;
        Y *= scale;
        Z *= scale;
        W *= scale;
    }

    public Vector3 RotateVector(Vector3 vector)
    {
        var x = W * vector.X + (Y * vector.Z - vector.Y * Z);
        var y = W * vector.Y - (X * vector.Z - vector.X * Z);
        var z = W * vector.Z + (X * vector.Y - vector.X * Y);
        var w = -(X * vector.X + Y * vector.Y + Z * vector.Z);

        return new Vector3(
            w * (-X) + W * x + (y * (-Z) - (-Y) * z),
            w * (-Y) + W * y - (x * (-Z) - (-X) * z),
            w * (-Z) + W * z + (x * (-Y) - (-X) * y)
        );
    }

    public void RotateX(float theta)
    {
        var temp = new Quaternion(new Vector3(1, 0, 0), theta);
        X = temp.X;
        Y = temp.Y;
        Z = temp.Z;
        W = temp.W;
    }

    public void RotateY(float theta)
    {
        var temp = new Quaternion(new Vector3(0, 1, 0), theta);
        X = temp.X;
        Y = temp.Y;
        Z = temp.Z;
        W = temp.W;
    }

    public void RotateZ(float theta)
    {
        var temp = new Quaternion(new Vector3(0, 0, 1), theta);
        X = temp.X;
        Y = temp.Y;
        Z = temp.Z;
        W = temp.W;
    }

    public void Randomize()
    {
        X = (ExtraMath.SystemRand() & 0xFFFF) / 65536F;
        Y = (ExtraMath.SystemRand() & 0xFFFF) / 65536F;
        Z = (ExtraMath.SystemRand() & 0xFFFF) / 65536F;
        W = (ExtraMath.SystemRand() & 0xFFFF) / 65536F;

        Normalize();
    }

    private static float ProjectToSphere(float r, float x, float y)
    {
        var d = float.Sqrt(float.Pow(x, 2) + float.Pow(y, 2));

        float z;
        if (d < r * (ExtraMath.Sqrt2 / 2F))
        {
            z = float.Sqrt(float.Pow(r, 2) - float.Pow(d, 2));
        }
        else
        {
            var t = r / ExtraMath.Sqrt2;
            z = float.Pow(t, 2) / d;
        }

        return z;
    }

    public override bool Equals(object? obj)
    {
        return obj is Quaternion other && Equals(this, other);
    }

    public bool Equals(Quaternion? x, Quaternion? y)
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

        return float.Abs(x.X - y.X) < float.Epsilon
            && float.Abs(x.Y - y.Y) < float.Epsilon
            && float.Abs(x.Z - y.Z) < float.Epsilon
            && float.Abs(x.W - y.W) < float.Epsilon;
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode(Quaternion obj)
    {
        return HashCode.Combine(obj.X, obj.Y, obj.Z, obj.W);
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z}, {W})";
    }

    public string ToString(string? format)
    {
        return $"({X.ToString(format)}, {Y.ToString(format)}, {Z.ToString(format)}, {W.ToString(format)})";
    }

    public string ToString(IFormatProvider? formatProvider)
    {
        return $"({X.ToString(formatProvider)}, {Y.ToString(formatProvider)}, {Z.ToString(formatProvider)}, {W.ToString(formatProvider)})";
    }

    public (float X, float Y, float Z, float W) Deconstruct()
    {
        return (X, Y, Z, W);
    }

    public static Quaternion operator +(Quaternion x, Quaternion y)
    {
        return new Quaternion(x.X + y.X, x.Y + y.Y, x.Z + y.Z, x.W + y.W);
    }

    public static Quaternion operator -(Quaternion x, Quaternion y)
    {
        return new Quaternion(x.X - y.X, x.Y - y.Y, x.Z - y.Z, x.W - y.W);
    }

    public static Quaternion operator *(Quaternion x, Quaternion y)
    {
        return new Quaternion(
            x.W * y.X + y.W * x.X + (x.Y * y.Z - y.Y * x.Z),
            x.W * y.Y + y.W * x.Y - (x.X * y.Z - y.X * x.Z),
            x.W * y.Z + y.W * x.Z + (x.X * y.Y - y.X * x.Y),
            x.W * y.W - (x.X * y.X + x.Y * y.Y + x.Z * y.Z)
        );
    }

    public static Quaternion operator *(Quaternion obj, float scalar)
    {
        return new Quaternion(obj[0] * scalar, obj[1] * scalar, obj[2] * scalar, obj[3] * scalar);
    }

    public static Quaternion operator *(float scalar, Quaternion obj)
    {
        return obj * scalar;
    }

    public static Quaternion operator /(Quaternion x, Quaternion y)
    {
        return x * Inverse(y);
    }

    public static Quaternion operator -(Quaternion obj)
    {
        return new Quaternion(-obj.X, -obj.Y, -obj.Z, -obj.W);
    }

    public static Quaternion operator +(Quaternion obj)
    {
        return obj;
    }

    public static bool operator ==(Quaternion x, Quaternion y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(Quaternion x, Quaternion y)
    {
        return !x.Equals(y);
    }
}
