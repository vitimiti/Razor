// -----------------------------------------------------------------------
// <copyright file="IoVector2.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Buffers.Binary;

namespace Razor.Vegas.Core.IoStruct;

/// <summary>
/// Represents a two-dimensional vector used by the I/O layer of <c>Razor.Vegas.Core</c>.
/// This is a lightweight value type (a record struct) that provides
/// value-based equality, deconstruction support, and simple storage of
/// X/Y components for serialization or interop scenarios.
/// </summary>
/// <param name="X">The X component (horizontal coordinate).</param>
/// <param name="Y">The Y component (vertical coordinate).</param>
/// <remarks>
/// Instances are intended to be immutable data carriers for passing
/// coordinate pairs between systems; the generated record semantics
/// provide sensible equality and printing behavior.
/// </remarks>
public readonly record struct IoVector2(float X, float Y)
{
    /// <summary>
    /// Gets the size of the <see cref="IoVector2"/> in bytes.
    /// </summary>
    /// <remarks>
    /// This value is 8.
    /// </remarks>
    public static int ByteSize => sizeof(float) * 2;

    /// <summary>
    /// Creates an <see cref="IoVector2"/> instance from a byte buffer.
    /// </summary>
    /// <param name="buffer">The buffer containing the vector data.</param>
    /// <returns>A new <see cref="IoVector2"/> instance.</returns>
    /// <remarks>
    /// The buffer must contain at least 8 bytes (4 bytes for X and 4 bytes for Y).
    /// The values are read in little-endian format.
    /// </remarks>
    public static IoVector2 FromBuffer(ReadOnlySpan<byte> buffer) =>
        new(BinaryPrimitives.ReadSingleLittleEndian(buffer), BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]));

    /// <summary>
    /// Converts the current instance to a byte array.
    /// </summary>
    /// <returns>A byte array containing the vector data.</returns>
    /// <remarks>
    /// The returned array will contain 8 bytes.
    /// The values are written in little-endian format.
    /// </remarks>
    public readonly Span<byte> ToBuffer()
    {
        Span<byte> buffer = new byte[ByteSize];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, X);
        BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], Y);
        return buffer;
    }
}
