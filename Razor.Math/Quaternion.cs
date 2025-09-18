// -----------------------------------------------------------------------
// <copyright file="Quaternion.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Math;

/// <summary>Represents a quaternion for 3D rotations and provides operations for quaternion mathematics.</summary>
public class Quaternion : IEqualityComparer<Quaternion>
{
    private static readonly int[] Next = [1, 2, 0];

    /// <summary>Initializes a new instance of the <see cref="Quaternion"/> class with default values (0, 0, 0, 1).</summary>
    public Quaternion() { }

    /// <summary>Initializes a new instance of the <see cref="Quaternion"/> class with the specified component values.</summary>
    /// <param name="x">The X component of the quaternion.</param>
    /// <param name="y">The Y component of the quaternion.</param>
    /// <param name="z">The Z component of the quaternion.</param>
    /// <param name="w">The W component of the quaternion.</param>
    public Quaternion(float x, float y, float z, float w) => Set(x, y, z, w);

    /// <summary>Initializes a new instance of the <see cref="Quaternion"/> class from an axis of rotation and an angle.</summary>
    /// <param name="axis">The axis of rotation as a normalized vector.</param>
    /// <param name="angle">The angle of rotation in radians.</param>
    public Quaternion([NotNull] Vector3 axis, float angle)
    {
        var sin = float.Sin(angle * .5F);
        var cos = float.Cos(angle * .5F);
        X = sin * axis.X;
        Y = sin * axis.Y;
        Z = sin * axis.Z;
        W = cos;
    }

    /// <summary>Gets the identity quaternion (0, 0, 0, 1) representing no rotation.</summary>
    /// <value>A quaternion representing the identity rotation.</value>
    public static Quaternion Identity => new(0F, 0F, 0F, 1F);

    /// <summary>Gets or sets the X component of the quaternion.</summary>
    /// <value>The X component value.</value>
    public float X { get; set; }

    /// <summary>Gets or sets the Y component of the quaternion.</summary>
    /// <value>The Y component value.</value>
    public float Y { get; set; }

    /// <summary>Gets or sets the Z component of the quaternion.</summary>
    /// <value>The Z component value.</value>
    public float Z { get; set; }

    /// <summary>Gets or sets the W component of the quaternion.</summary>
    /// <value>The W component value.</value>
    public float W { get; set; }

    /// <summary>Gets the squared length (magnitude) of the quaternion.</summary>
    /// <value>The sum of the squares of all components.</value>
    public float Length2 => float.Pow(X, 2) + float.Pow(Y, 2) + float.Pow(Z, 2) + float.Pow(W, 2);

    /// <summary>Gets the length (magnitude) of the quaternion.</summary>
    /// <value>The square root of the sum of the squares of all components.</value>
    public float Length => float.Sqrt(Length2);

    /// <summary>Gets the inverse of the quaternion.</summary>
    /// <value>A quaternion representing the inverse rotation.</value>
    public Quaternion Inverse => new(-X, -Y, -Z, W);

    /// <summary>Gets the conjugate of the quaternion.</summary>
    /// <value>A quaternion with negated X, Y, Z components and unchanged W component.</value>
    public Quaternion Conjugate => new(-X, -Y, -Z, W);

    /// <summary>Gets a value indicating whether all components of the quaternion are valid (not NaN or infinite).</summary>
    /// <value><see langword="true"/> if all components are valid; otherwise, <see langword="false"/>.</value>
    public bool IsValid => ExtraMath.IsValid(X) && ExtraMath.IsValid(Y) && ExtraMath.IsValid(Z) && ExtraMath.IsValid(W);

    /// <summary>Gets or sets the component at the specified index.</summary>
    /// <param name="index">The zero-based index of the component. Valid range: 0-3 (X, Y, Z, W).</param>
    /// <value>The component value at the specified index.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is not between 0 and 3.</exception>
    public float this[int index]
    {
        get =>
            index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => W,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The index must be between 0 and 3."),
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
                    throw new ArgumentOutOfRangeException(nameof(index), index, "The index must be between 0 and 3.");
            }
        }
    }

    /// <summary>Adds two quaternions component-wise.</summary>
    /// <param name="left">The first quaternion.</param>
    /// <param name="right">The second quaternion.</param>
    /// <returns>A new quaternion that is the sum of the two input quaternions.</returns>
    public static Quaternion operator +(Quaternion left, Quaternion right) => Add(left, right);

    /// <summary>Subtracts the second quaternion from the first component-wise.</summary>
    /// <param name="left">The first quaternion.</param>
    /// <param name="right">The second quaternion.</param>
    /// <returns>A new quaternion that is the difference of the two input quaternions.</returns>
    public static Quaternion operator -(Quaternion left, Quaternion right) => Subtract(left, right);

    /// <summary>Multiplies two quaternions using quaternion multiplication.</summary>
    /// <param name="left">The first quaternion.</param>
    /// <param name="right">The second quaternion.</param>
    /// <returns>A new quaternion that is the product of the two input quaternions.</returns>
    public static Quaternion operator *(Quaternion left, Quaternion right) => Multiply(left, right);

    /// <summary>Multiplies a quaternion by a scalar value.</summary>
    /// <param name="quaternion">The quaternion to multiply.</param>
    /// <param name="scale">The scalar value.</param>
    /// <returns>A new quaternion with all components multiplied by the scalar.</returns>
    public static Quaternion operator *(Quaternion quaternion, float scale) => Multiply(quaternion, scale);

    /// <summary>Multiplies a quaternion by a scalar value.</summary>
    /// <param name="scale">The scalar value.</param>
    /// <param name="quaternion">The quaternion to multiply.</param>
    /// <returns>A new quaternion with all components multiplied by the scalar.</returns>
    public static Quaternion operator *(float scale, Quaternion quaternion) => Multiply(quaternion, scale);

    /// <summary>Divides the first quaternion by the second using quaternion division.</summary>
    /// <param name="left">The dividend quaternion.</param>
    /// <param name="right">The divisor quaternion.</param>
    /// <returns>A new quaternion that is the quotient of the division.</returns>
    public static Quaternion operator /(Quaternion left, Quaternion right) => Divide(left, right);

    /// <summary>Negates all components of the quaternion.</summary>
    /// <param name="quaternion">The quaternion to negate.</param>
    /// <returns>A new quaternion with all components negated.</returns>
    public static Quaternion operator -(Quaternion quaternion) => Negate(quaternion);

    /// <summary>Returns the quaternion unchanged (unary plus operator).</summary>
    /// <param name="quaternion">The quaternion.</param>
    /// <returns>The same quaternion instance.</returns>
    public static Quaternion operator +(Quaternion quaternion) => Plus(quaternion);

    /// <summary>Determines whether two quaternions are equal.</summary>
    /// <param name="left">The first quaternion to compare.</param>
    /// <param name="right">The second quaternion to compare.</param>
    /// <returns><see langword="true"/> if the quaternions are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Quaternion left, Quaternion right) => object.Equals(left, right);

    /// <summary>Determines whether two quaternions are not equal.</summary>
    /// <param name="left">The first quaternion to compare.</param>
    /// <param name="right">The second quaternion to compare.</param>
    /// <returns><see langword="true"/> if the quaternions are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Quaternion left, Quaternion right) => !object.Equals(left, right);

    /// <summary>Adds two quaternions component-wise.</summary>
    /// <param name="left">The first quaternion.</param>
    /// <param name="right">The second quaternion.</param>
    /// <returns>A new quaternion that is the sum of the two input quaternions.</returns>
    public static Quaternion Add([NotNull] Quaternion left, [NotNull] Quaternion right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    /// <summary>Subtracts the second quaternion from the first component-wise.</summary>
    /// <param name="left">The first quaternion.</param>
    /// <param name="right">The second quaternion.</param>
    /// <returns>A new quaternion that is the difference of the two input quaternions.</returns>
    public static Quaternion Subtract([NotNull] Quaternion left, [NotNull] Quaternion right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    /// <summary>Multiplies a quaternion by a scalar value.</summary>
    /// <param name="quaternion">The quaternion to multiply.</param>
    /// <param name="scale">The scalar multiplier.</param>
    /// <returns>A new quaternion with all components multiplied by the scalar.</returns>
    public static Quaternion Multiply([NotNull] Quaternion quaternion, float scale) =>
        new(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);

    /// <summary>Multiplies two quaternions using quaternion multiplication.</summary>
    /// <param name="left">The first quaternion.</param>
    /// <param name="right">The second quaternion.</param>
    /// <returns>A new quaternion that is the product of the two input quaternions.</returns>
    public static Quaternion Multiply([NotNull] Quaternion left, [NotNull] Quaternion right) =>
        new(
            (left.W * right.X) + (right.W * left.X) + ((left.Y * right.Z) - (right.Y * left.Z)),
            (left.W * right.Y) + (right.W * left.Y) - ((left.X * right.Z) - (right.X * left.Z)),
            (left.W * right.Z) + (right.W * left.Z) + ((left.X * right.Y) - (right.X * left.Y)),
            (left.W * right.W) - ((left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z))
        );

    /// <summary>Divides the first quaternion by the second using quaternion division.</summary>
    /// <param name="left">The dividend quaternion.</param>
    /// <param name="right">The divisor quaternion.</param>
    /// <returns>A new quaternion that is the quotient of the division.</returns>
    public static Quaternion Divide([NotNull] Quaternion left, [NotNull] Quaternion right) => left * right.Inverse;

    /// <summary>Negates all components of the quaternion.</summary>
    /// <param name="quaternion">The quaternion to negate.</param>
    /// <returns>A new quaternion with all components negated.</returns>
    public static Quaternion Negate([NotNull] Quaternion quaternion) =>
        new(-quaternion.X, -quaternion.Y, -quaternion.Z, -quaternion.W);

    /// <summary>Returns the quaternion unchanged.</summary>
    /// <param name="quaternion">The input quaternion.</param>
    /// <returns>The same quaternion instance.</returns>
    public static Quaternion Plus([NotNull] Quaternion quaternion) => quaternion;

    /// <summary>Creates a quaternion from a 3D rotation matrix.</summary>
    /// <param name="matrix">The 3D transformation matrix containing rotation information.</param>
    /// <returns>A quaternion representing the same rotation as the input matrix.</returns>
    public static Quaternion Build([NotNull] Matrix3D matrix)
    {
        var tr = matrix[0][0] + matrix[1][1] + matrix[2][2];
        float s;
        Quaternion result = new();
        if (tr > 0F)
        {
            s = float.Sqrt(tr + 1);
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

            var j = Next[i];
            var k = Next[j];

            s = float.Sqrt(matrix[i][i] - (matrix[j][j] + matrix[k][k]) + 1F);
            result[i] = s * .5F;
            if (float.Abs(s) >= float.Epsilon)
            {
                s = .5F / s;
            }

            result[3] = (matrix[k][j] - matrix[j][k]) * s;
            result[j] = (matrix[j][i] + matrix[i][j]) * s;
            result[k] = (matrix[k][i] + matrix[i][k]) * s;
        }

        return result;
    }

    /// <summary>Creates a quaternion from a 3x3 rotation matrix.</summary>
    /// <param name="matrix">The 3x3 rotation matrix.</param>
    /// <returns>A quaternion representing the same rotation as the input matrix.</returns>
    public static Quaternion Build([NotNull] Matrix3X3 matrix)
    {
        Quaternion result = new();
        var tr = matrix[0][0] + matrix[1][1] + matrix[2][2];
        float s;
        Quaternion q = new();
        if (tr > 0F)
        {
            s = float.Sqrt(tr + 1);
            q[3] = s * .5F;
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

            var j = Next[i];
            var k = Next[j];

            s = float.Sqrt(matrix[i][i] - (matrix[j][j] + matrix[k][k]) + 1F);
            result[i] = s * .5F;
            if (float.Abs(s) >= float.Epsilon)
            {
                s = .5F / s;
            }

            result[3] = (matrix[k][j] - matrix[j][k]) * s;
            result[j] = (matrix[j][i] + matrix[i][j]) * s;
            result[k] = (matrix[k][i] + matrix[i][k]) * s;
        }

        return result;
    }

    /// <summary>Creates a quaternion from a 4x4 transformation matrix.</summary>
    /// <param name="matrix">The 4x4 transformation matrix containing rotation information.</param>
    /// <returns>A quaternion representing the rotation component of the input matrix.</returns>
    public static Quaternion Build([NotNull] Matrix4X4 matrix)
    {
        var tr = matrix[0][0] + matrix[1][1] + matrix[2][2];
        float s;
        Quaternion result = new();
        if (tr > 0F)
        {
            s = float.Sqrt(tr + 1);
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

            var j = Next[i];
            var k = Next[j];

            s = float.Sqrt(matrix[i][i] - (matrix[j][j] + matrix[k][k]) + 1F);
            result[i] = s * .5F;
            if (float.Abs(s) >= float.Epsilon)
            {
                s = .5F / s;
            }

            result[3] = (matrix[k][j] - matrix[j][k]) * s;
            result[j] = (matrix[j][i] + matrix[i][j]) * s;
            result[k] = (matrix[k][i] + matrix[i][k]) * s;
        }

        return result;
    }

    /// <summary>Converts a quaternion to a 3D transformation matrix.</summary>
    /// <param name="quaternion">The quaternion to convert.</param>
    /// <returns>A 3D transformation matrix representing the same rotation as the input quaternion.</returns>
    public static Matrix3D BuildMatrix3D([NotNull] Quaternion quaternion)
    {
        Matrix3D result = new()
        {
            [0] = { [0] = 1F - (2F * (float.Pow(quaternion[1], 2) + float.Pow(quaternion[2], 2))) },
            [0] = { [1] = 2F * ((quaternion[0] * quaternion[1]) - (quaternion[2] * quaternion[3])) },
            [0] = { [2] = 2F * ((quaternion[2] * quaternion[0]) + (quaternion[1] * quaternion[3])) },
            [1] = { [0] = 2F * ((quaternion[0] * quaternion[1]) + (quaternion[2] * quaternion[3])) },
            [1] = { [1] = 1F - (2.0f * (float.Pow(quaternion[2], 2) + float.Pow(quaternion[0], 2))) },
            [1] = { [2] = 2F * ((quaternion[1] * quaternion[2]) - (quaternion[0] * quaternion[3])) },
            [2] = { [0] = 2F * ((quaternion[2] * quaternion[0]) - (quaternion[1] * quaternion[3])) },
            [2] = { [1] = 2F * ((quaternion[1] * quaternion[2]) + (quaternion[0] * quaternion[3])) },
            [2] = { [2] = 1F - (2F * (float.Pow(quaternion[1], 2) + float.Pow(quaternion[0], 2))) },
        };

        result[0][3] = result[1][3] = result[2][3] = 0.0f;

        return result;
    }

    /// <summary>Converts a quaternion to a 3x3 rotation matrix.</summary>
    /// <param name="quaternion">The quaternion to convert.</param>
    /// <returns>A 3x3 rotation matrix representing the same rotation as the input quaternion.</returns>
    public static Matrix3X3 BuildMatrix3X3([NotNull] Quaternion quaternion) =>
        new(
            new Vector3(
                1F - (2F * (float.Pow(quaternion[1], 2) + float.Pow(quaternion[2], 2))),
                2F * ((quaternion[0] * quaternion[1]) - (quaternion[2] * quaternion[3])),
                2F * ((quaternion[2] * quaternion[0]) + (quaternion[1] * quaternion[3]))
            ),
            new Vector3(
                2F * ((quaternion[0] * quaternion[1]) + (quaternion[2] * quaternion[3])),
                1F - (2.0f * (float.Pow(quaternion[2], 2) + float.Pow(quaternion[0], 2))),
                2F * ((quaternion[1] * quaternion[2]) - (quaternion[0] * quaternion[3]))
            ),
            new Vector3(
                2F * ((quaternion[2] * quaternion[0]) - (quaternion[1] * quaternion[3])),
                2F * ((quaternion[1] * quaternion[2]) + (quaternion[0] * quaternion[3])),
                1F - (2F * (float.Pow(quaternion[1], 2) + float.Pow(quaternion[0], 2)))
            )
        );

    /// <summary>Converts a quaternion to a 4x4 transformation matrix.</summary>
    /// <param name="quaternion">The quaternion to convert.</param>
    /// <returns>A 4x4 transformation matrix with the rotation component from the quaternion and identity translation.</returns>
    public static Matrix4X4 BuildMatrix4X4([NotNull] Quaternion quaternion)
    {
        Matrix4X4 result = new()
        {
            [0] = { [0] = 1F - (2F * ((quaternion[1] * quaternion[1]) + (quaternion[2] * quaternion[2]))) },
            [0] = { [1] = 2F * ((quaternion[0] * quaternion[1]) - (quaternion[2] * quaternion[3])) },
            [0] = { [2] = 2F * ((quaternion[2] * quaternion[0]) + (quaternion[1] * quaternion[3])) },
            [1] = { [0] = 2F * ((quaternion[0] * quaternion[1]) + (quaternion[2] * quaternion[3])) },
            [1] = { [1] = 1F - (2F * ((quaternion[2] * quaternion[2]) + (quaternion[0] * quaternion[0]))) },
            [1] = { [2] = 2F * ((quaternion[1] * quaternion[2]) - (quaternion[0] * quaternion[3])) },
            [2] = { [0] = 2F * ((quaternion[2] * quaternion[0]) - (quaternion[1] * quaternion[3])) },
            [2] = { [1] = 2F * ((quaternion[1] * quaternion[2]) + (quaternion[0] * quaternion[3])) },
            [2] = { [2] = 1F - (2F * ((quaternion[1] * quaternion[1]) + (quaternion[0] * quaternion[0]))) },
        };

        result[0][3] = result[1][3] = result[2][3] = 0F;

        result[3][0] = result[3][1] = result[3][2] = 0F;
        result[3][3] = 1F;

        return result;
    }

    /// <summary>Performs spherical linear interpolation between two quaternions.</summary>
    /// <param name="p">The start quaternion.</param>
    /// <param name="q">The end quaternion.</param>
    /// <param name="alpha">The interpolation parameter between 0.0 and 1.0.</param>
    /// <returns>A quaternion that is the spherical linear interpolation between <paramref name="p"/> and <paramref name="q"/>.</returns>
    public static Quaternion Slerp([NotNull] Quaternion p, [NotNull] Quaternion q, float alpha)
    {
        var cosTheta = (p.X * q.X) + (p.Y * q.Y) + (p.Z * q.Z) + (p.W * q.W);
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
        if (1F - cosTheta < float.Pow(float.Epsilon, 2))
        {
            beta = 1F - alpha;
        }
        else
        {
            var theta = float.Acos(cosTheta);
            var sinTheta = float.Sin(theta);
            var oSinTheta = 1F / sinTheta;
            beta = float.Sin(theta - (alpha * theta)) * oSinTheta;
            alpha = float.Sin(alpha * theta) * oSinTheta;
        }

        if (qFlip)
        {
            alpha = -alpha;
        }

        return new Quaternion(
            (beta * p.X) + (alpha * q.X),
            (beta * p.Y) + (alpha * q.Y),
            (beta * p.Z) + (alpha * q.Z),
            (beta * p.W) + (alpha * q.W)
        );
    }

    /// <summary>Performs fast spherical linear interpolation between two quaternions using approximated trigonometric functions.</summary>
    /// <param name="p">The start quaternion.</param>
    /// <param name="q">The end quaternion.</param>
    /// <param name="alpha">The interpolation parameter between 0.0 and 1.0.</param>
    /// <returns>A quaternion that is the fast spherical linear interpolation between <paramref name="p"/> and <paramref name="q"/>.</returns>
    public static Quaternion FastSlerp([NotNull] Quaternion p, [NotNull] Quaternion q, float alpha)
    {
        var cosTheta = (p.X * q.X) + (p.Y * q.Y) + (p.Z * q.Z) + (p.W * q.W);
        var qFlip = true;
        if (cosTheta < 0F)
        {
            cosTheta = -cosTheta;
            qFlip = true;
        }

        float beta;
        if (1F - cosTheta < float.Pow(float.Epsilon, 2))
        {
            beta = 1F - alpha;
        }
        else
        {
            var theta = ExtraMath.FastAcos(cosTheta);
            var sinTheta = ExtraMath.FastSin(theta);

            var oSinTheta = 1F / sinTheta;
            beta = ExtraMath.FastSin(theta - (alpha * theta)) * oSinTheta;
            alpha = ExtraMath.FastSin(alpha * theta) * oSinTheta;
        }

        if (qFlip)
        {
            alpha = -alpha;
        }

        return new Quaternion(
            (beta * p.X) + (alpha * q.X),
            (beta * p.Y) + (alpha * q.Y),
            (beta * p.Z) + (alpha * q.Z),
            (beta * p.W) + (alpha * q.W)
        );
    }

    /// <summary>Prepares interpolation information for efficient repeated spherical linear interpolation between two quaternions.</summary>
    /// <param name="p">The start quaternion.</param>
    /// <param name="q">The end quaternion.</param>
    /// <returns>A <see cref="QuaternionSlerpInfo"/> structure containing precomputed values for cached interpolation.</returns>
    public static QuaternionSlerpInfo SlerpSetup([NotNull] Quaternion p, [NotNull] Quaternion q)
    {
        var cosTheta = (p.X * q.X) + (p.Y * q.Y) + (p.Z * q.Z) + (p.W * q.W);
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

        bool linear;
        float theta;
        float sinTheta;
        if (1F - cosTheta < float.Epsilon)
        {
            linear = true;
            theta = 0F;
            sinTheta = 0F;
        }
        else
        {
            linear = false;
            theta = float.Acos(cosTheta);
            sinTheta = float.Sin(theta);
        }

        return new QuaternionSlerpInfo(sinTheta, theta, qFlip, linear);
    }

    /// <summary>Performs spherical linear interpolation using precomputed interpolation information.</summary>
    /// <param name="p">The start quaternion.</param>
    /// <param name="q">The end quaternion.</param>
    /// <param name="alpha">The interpolation parameter between 0.0 and 1.0.</param>
    /// <param name="info">Precomputed interpolation information from <see cref="SlerpSetup"/>.</param>
    /// <returns>A quaternion that is the spherical linear interpolation between <paramref name="p"/> and <paramref name="q"/>.</returns>
    public static Quaternion CachedSlerp(
        [NotNull] Quaternion p,
        [NotNull] Quaternion q,
        float alpha,
        [NotNull] QuaternionSlerpInfo info
    )
    {
        float beta;
        if (info.Linear)
        {
            beta = 1F - alpha;
        }
        else
        {
            var oSinTheta = 1F / info.SinTheta;
            beta = float.Sin(info.Theta - (alpha * info.Theta)) * oSinTheta;
            alpha = float.Sin(alpha * info.Theta) * oSinTheta;
        }

        if (info.Flip)
        {
            alpha = -alpha;
        }

        return new Quaternion(
            (beta * p.X) + (alpha * q.X),
            (beta * p.Y) + (alpha * q.Y),
            (beta * p.Z) + (alpha * q.Z),
            (beta * p.W) + (alpha * q.W)
        );
    }

    /// <summary>Creates a quaternion representing trackball rotation based on mouse movement.</summary>
    /// <param name="x0">The initial X coordinate.</param>
    /// <param name="y0">The initial Y coordinate.</param>
    /// <param name="x1">The final X coordinate.</param>
    /// <param name="y1">The final Y coordinate.</param>
    /// <param name="sphereSize">The radius of the virtual trackball sphere.</param>
    /// <returns>A quaternion representing the rotation from the initial position to the final position.</returns>
    public static Quaternion Trackball(float x0, float y0, float x1, float y1, float sphereSize)
    {
        if (float.Abs(x0 - x1) < float.Epsilon && float.Abs(y0 - y1) < float.Epsilon)
        {
            return new Quaternion(0, 0, 0, 1);
        }

        Vector3 p1 = new()
        {
            [0] = x0,
            [1] = y0,
            [2] = ProjectToSphere(sphereSize, x0, y0),
        };

        Vector3 p2 = new()
        {
            [0] = x1,
            [1] = y1,
            [2] = ProjectToSphere(sphereSize, x1, y1),
        };

        var a = Vector3.CrossProduct(p2, p1);

        Vector3 d = p1 - p2;
        var t = d.Length / (2F * sphereSize);

        // Avoid problems with out-of-control values
        t = float.Clamp(t, -1F, 1F);
        var phi = 2F * float.Asin(t);

        return AxisToQuat(a, phi);
    }

    /// <summary>Creates a quaternion from an axis of rotation and an angle.</summary>
    /// <param name="axis">The axis of rotation (will be normalized internally).</param>
    /// <param name="phi">The angle of rotation in radians.</param>
    /// <returns>A quaternion representing rotation around the specified axis by the specified angle.</returns>
    public static Quaternion AxisToQuat([NotNull] Vector3 axis, float phi)
    {
        Vector3 temp = axis;
        temp.Normalize();

        Quaternion q = new()
        {
            [0] = temp[0],
            [1] = temp[1],
            [2] = temp[2],
        };

        q.Scale(float.Sin(phi / 2F));
        q[3] = float.Cos(phi / 2F);

        return q;
    }

    /// <summary>Sets the components of the quaternion to the specified values.</summary>
    /// <param name="x">The X component value. Default is 0.</param>
    /// <param name="y">The Y component value. Default is 0.</param>
    /// <param name="z">The Z component value. Default is 0.</param>
    /// <param name="w">The W component value. Default is 1.</param>
    public void Set(float x = 0F, float y = 0F, float z = 0F, float w = 1F) => (X, Y, Z, W) = (x, y, z, w);

    /// <summary>Sets the quaternion to the identity quaternion (0, 0, 0, 1).</summary>
    public void MakeIdentity() => Set();

    /// <summary>Scales all components of the quaternion by the specified factor.</summary>
    /// <param name="scale">The scaling factor to apply to all components.</param>
    public void Scale(float scale)
    {
        X *= scale;
        Y *= scale;
        Z *= scale;
        W *= scale;
    }

    /// <summary>Normalizes the quaternion to unit length, making it suitable for representing rotations.</summary>
    /// <remarks>If the quaternion has zero length, this method does nothing to avoid division by zero.</remarks>
    public void Normalize()
    {
        if (float.Abs(Length) < float.Epsilon)
        {
            return;
        }

        var invMag = ExtraMath.InvSqrt(Length2);
        X *= invMag;
        Y *= invMag;
        Z *= invMag;
        W *= invMag;
    }

    /// <summary>Rotates a vector by this quaternion.</summary>
    /// <param name="vector">The vector to rotate.</param>
    /// <returns>A new vector that is the result of rotating the input vector by this quaternion.</returns>
    public Vector3 RotateVector([NotNull] Vector3 vector)
    {
        var x = (W * vector.X) + ((Y * vector.Z) - (vector.Y * Z));
        var y = (W * vector.Y) - ((X * vector.Z) - (vector.X * Z));
        var z = (W * vector.Z) + ((X * vector.Y) - (vector.X * Y));
        var w = -((X * vector.X) + (Y * vector.Y) + (Z * vector.Z));

        return new Vector3(
            (w * (-X)) + (W * x) + ((y * (-Z)) - ((-Y) * z)),
            (w * (-Y)) + (W * y) - ((x * (-Z)) - ((-X) * z)),
            (w * (-Z)) + (W * z) + ((x * (-Y)) - ((-X) * y))
        );
    }

    /// <summary>Adjusts this quaternion to be in the same hemisphere as another quaternion to ensure the shortest interpolation path.</summary>
    /// <param name="other">The reference quaternion to compare against.</param>
    /// <remarks>If the dot product with the other quaternion is negative, all components are negated to ensure the shortest rotation path during interpolation.</remarks>
    public void MakeClosest([NotNull] Quaternion other)
    {
        var cosTheta = (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);
        if (cosTheta >= 0F)
        {
            return;
        }

        X = -X;
        Y = -Y;
        Z = -Z;
        W = -W;
    }

    /// <summary>Rotates this quaternion around the X-axis by the specified angle.</summary>
    /// <param name="theta">The angle of rotation in radians.</param>
    public void RotateX(float theta)
    {
        // TODO: (as per original code) optimize this
        Quaternion temp = this;
        temp *= new Quaternion(new Vector3(1, 0, 0), theta);

        Set(temp.X, temp.Y, temp.Z, temp.W);
    }

    /// <summary>Rotates this quaternion around the Y-axis by the specified angle.</summary>
    /// <param name="theta">The angle of rotation in radians.</param>
    public void RotateY(float theta)
    {
        // TODO: (as per original code) optimize this
        Quaternion temp = this;
        temp *= new Quaternion(new Vector3(0, 1, 0), theta);

        Set(temp.X, temp.Y, temp.Z, temp.W);
    }

    /// <summary>Rotates this quaternion around the Z-axis by the specified angle.</summary>
    /// <param name="theta">The angle of rotation in radians.</param>
    public void RotateZ(float theta)
    {
        // TODO: (as per original code) optimize this
        Quaternion temp = this;
        temp *= new Quaternion(new Vector3(0, 0, 1), theta);

        Set(temp.X, temp.Y, temp.Z, temp.W);
    }

    /// <summary>Sets all components of the quaternion to random values between 0 and 1.</summary>
    /// <remarks>The resulting quaternion will not be normalized and may not represent a valid rotation until normalized.</remarks>
    public void Randomize()
    {
        X = (ExtraMath.SysRand() & 0xFFFF) / 65536F;
        Y = (ExtraMath.SysRand() & 0xFFFF) / 65536F;
        Z = (ExtraMath.SysRand() & 0xFFFF) / 65536F;
        W = (ExtraMath.SysRand() & 0xFFFF) / 65536F;
    }

    /// <summary>Determines whether the specified object is equal to the current quaternion.</summary>
    /// <param name="obj">The object to compare with the current quaternion.</param>
    /// <returns><see langword="true"/> if the specified object is equal to the current quaternion; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is Quaternion quaternion && Equals(this, quaternion);

    /// <summary>Determines whether two specified quaternions have the same value.</summary>
    /// <param name="x">The first quaternion to compare, or <see langword="null"/>.</param>
    /// <param name="y">The second quaternion to compare, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the value of <paramref name="x"/> is the same as the value of <paramref name="y"/>; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Quaternion? x, Quaternion? y) =>
        ReferenceEquals(x, y)
        || (
            x is not null
            && y is not null
            && x.GetType() == y.GetType()
            && float.Abs(x.X - y.X) < float.Epsilon
            && float.Abs(x.Y - y.Y) < float.Epsilon
            && float.Abs(x.Z - y.Z) < float.Epsilon
            && float.Abs(x.W - y.W) < float.Epsilon
        );

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() => GetHashCode(this);

    /// <summary>Returns a hash code for the specified quaternion.</summary>
    /// <param name="obj">The quaternion for which to get a hash code.</param>
    /// <returns>A hash code for the specified quaternion.</returns>
    public int GetHashCode([NotNull] Quaternion obj) => HashCode.Combine(obj.X, obj.Y, obj.Z, obj.W);

    private static float ProjectToSphere(float r, float x, float y)
    {
        var d = float.Sqrt(float.Pow(x, 2) + float.Pow(y, 2));
        if (d < r * (ExtraMath.Sqrt2 / 2F))
        {
            // Inside sphere
            return float.Sqrt(float.Pow(r, 2) - float.Pow(d, 2));
        }

        // On hyperbola
        var t = r / ExtraMath.Sqrt2;
        return float.Pow(t, 2) / d;
    }
}
