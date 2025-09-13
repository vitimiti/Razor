// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Generics;

/// <summary>Defines a contract for accessing elements of a matrix in a manner suitable for operations requiring matrix inversion or transformation.</summary>
/// <remarks>This is intended to be used to define what the SAGE engine expects a native graphics API (like D3D or GLM) matrix function will expose for it to be able to interface with it.</remarks>
public interface INativeMatrixInverse
{
    /// <summary>Provides indexed access to elements of the implementing type, allowing retrieval or assignment based on specified parameters.</summary>
    /// <param name="row">The zero-based row index of the element to access.</param>
    /// <param name="column">The zero-based column index of the element to access.</param>
    /// <returns>The value of the element at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the specified row or column index is out of range.</exception>
    float this[int row, int column] { get; }
}
