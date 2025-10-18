// -----------------------------------------------------------------------
// <copyright file="IoVector4.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Buffers.Binary;

namespace Razor.Vegas.Core.IoStruct;

/// <summary>
/// Represents a four-dimensional vector used by the I/O layer of <c>Razor.Vegas.Core</c>.
/// This is a lightweight value type (a record struct) that provides
/// value-based equality, deconstruction support, and simple storage of
/// X/Y/Z/W components for serialization or interop scenarios.
/// </summary>
/// <param name="X">The X component (horizontal coordinate).</param>
/// <param name="Y">The Y component (vertical coordinate).</param>
/// <param name="Z">The Z component (depth coordinate).</param>
/// <param name="W">The W component (fourth coordinate, often used for homogenous coordinates).</param>
/// <remarks>
/// Instances are intended to be immutable data carriers for passing
/// 4D coordinate data between systems; the generated record semantics
/// provide sensible equality and printing behavior.
/// </remarks>
public record struct IoVector4(float X, float Y, float Z, float W)
{
    /// <summary>
    /// Creates an <see cref="IoVector4"/> instance from a byte buffer.
    /// </summary>
    /// <param name="buffer">The buffer containing the vector data.</param>
    /// <returns>A new <see cref="IoVector4"/> instance.</returns>
    /// <remarks>
    /// The buffer must contain at least 16 bytes (4 bytes for each component).
    /// The values are read in little-endian format.
    /// </remarks>
    public static IoVector4 FromBuffer(ReadOnlySpan<byte> buffer) =>
        new(
            BinaryPrimitives.ReadSingleLittleEndian(buffer),
            BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]),
            BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]),
            BinaryPrimitives.ReadSingleLittleEndian(buffer[12..])
        );

    /// <summary>
    /// Converts the current instance to a byte array.
    /// </summary>
    /// <returns>A byte array containing the vector data.</returns>
    /// <remarks>
    /// The returned array will contain 16 bytes.
    /// The values are written in little-endian format.
    /// </remarks>
    public readonly Span<byte> ToBuffer()
    {
        Span<byte> buffer = stackalloc byte[16];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, X);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Y);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], Z);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], W);
        return buffer.ToArray();
    }
}
