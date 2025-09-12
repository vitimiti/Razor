// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Quaternion
{
    private static int[] s_next = [1, 2, 0];

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public Quaternion() { }

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
            if (s is < float.Epsilon or > float.Epsilon)
            {
                s = .5F / s;
            }

            result[3] = (matrix[k][j] - matrix[j][k]) * s;
            result[j] = (matrix[j][i] + matrix[i][j]) * s;
            result[k] = (matrix[k][i] + matrix[i][k]) * s;
        }

        return result;
    }

    public static Quaternion Slerp(Quaternion p, Quaternion q, float alpha)
    {
        Quaternion result = new();

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

        result.X = beta * p.X + alpha * q.X;
        result.Y = beta * p.Y + alpha * q.Y;
        result.Z = beta * p.Z + alpha * q.Z;
        result.W = beta * p.W + alpha * q.W;

        return result;
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
}
