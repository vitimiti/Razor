// -----------------------------------------------------------------------
// <copyright file="IoQuaternion.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Buffers.Binary;

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
    /// Gets the size of the <see cref="IoQuaternion"/> in bytes.
    /// </summary>
    /// <remarks>
    /// This value is 16.
    /// </remarks>
    public static int ByteSize => sizeof(float) * 4;

    /// <summary>
    /// Gets a span over the quaternion components (X, Y, Z, W).
    /// </summary>
    /// <remarks>
    /// The span references an underlying float array of length 4 created by
    /// this instance. Consumers must respect span/ref struct lifetime rules
    /// and avoid storing the span beyond the lifetime of this ref struct.
    /// </remarks>
    public Span<float> Q { get; } = new float[4];

    /// <summary>
    /// Creates a new <see cref="IoQuaternion"/> from the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer containing the quaternion data.</param>
    /// <returns>A new <see cref="IoQuaternion"/> instance.</returns>
    /// <remarks>
    /// The buffer must contain at least 16 bytes (4 bytes for each component).
    /// The values are read in little-endian format.
    /// </remarks>
    public static IoQuaternion FromBuffer(ReadOnlySpan<byte> buffer)
    {
        IoQuaternion quaternion = new();
        quaternion.Q[0] = BinaryPrimitives.ReadSingleLittleEndian(buffer);
        quaternion.Q[1] = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]);
        quaternion.Q[2] = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]);
        quaternion.Q[3] = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]);
        return quaternion;
    }

    /// <summary>
    /// Converts the current instance to a byte array.
    /// </summary>
    /// <returns>A byte array containing the quaternion data.</returns>
    /// <remarks>
    /// The returned array will contain 16 bytes.
    /// The values are written in little-endian format.
    /// </remarks>
    public Span<byte> ToBuffer()
    {
        Span<byte> buffer = stackalloc byte[ByteSize];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, Q[0]);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Q[1]);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Q[2]);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], Q[3]);
        return buffer.ToArray();
    }
}
