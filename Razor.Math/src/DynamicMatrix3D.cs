// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Razor.Attributes;
using Razor.Generics;

namespace Razor.Math;

[PublicAPI]
[Pooled]
public partial class DynamicMatrix3D : IPooledResettable
{
    private DynamicMatrix3D() { }

    public Matrix3D Matrix { get; set; } = new();

    public void Reset()
    {
        Matrix.MakeIdentity();
    }
}
