// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public class Matrix4X4
{
    protected Vector4[] Rows { get; } = new Vector4[4];

    public Vector4 this[int index]
    {
        get =>
            index switch
            {
                0 => Rows[0],
                1 => Rows[1],
                2 => Rows[2],
                3 => Rows[3],
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
                    Rows[0] = value;
                    break;
                case 1:
                    Rows[1] = value;
                    break;
                case 2:
                    Rows[2] = value;
                    break;
                case 3:
                    Rows[3] = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        index,
                        "The index must be between 0 and 3."
                    );
            }
        }
    }
}
