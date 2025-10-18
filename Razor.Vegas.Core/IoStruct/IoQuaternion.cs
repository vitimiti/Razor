// -----------------------------------------------------------------------
// <copyright file="IoQuaternion.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.IoStruct;

/// <summary>
/// Represents a quaternion used by the I/O layer of <c>Razor.Vegas.Core</c>.
/// A quaternion encodes rotation using four components (X, Y, Z, W) and is
/// commonly used for 3D orientation and interpolation.
/// </summary>
/// <remarks>
/// This type is a ref struct that exposes a <see cref="Span{T}"/> of four
/// floating-point values. It is intended as a lightweight carrier for
/// serialization/interop scenarios where stack-only lifetime semantics are
/// desirable. The <see cref="Q"/> span contains the components in the
/// order [X, Y, Z, W].
/// </remarks>
public readonly ref struct IoQuaternion()
{
    /// <summary>
    /// Gets a span over the quaternion components (X, Y, Z, W).
    /// </summary>
    /// <remarks>
    /// The span references an underlying float array of length 4 created by
    /// this instance. Consumers must respect span/ref struct lifetime rules
    /// and avoid storing the span beyond the lifetime of this ref struct.
    /// </remarks>
    public Span<float> Q { get; } = new float[4];
}
