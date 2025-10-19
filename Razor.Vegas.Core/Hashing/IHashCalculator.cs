// -----------------------------------------------------------------------
// <copyright file="IHashCalculator.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Hashing;

/// <summary>
/// Calculates and provides hash values for instances of <typeparamref name="T"/>.
/// Implementations compute one or more hash values for an item and expose
/// them through <see cref="GetHashValue(int)"/>. Implementations may also
/// provide an equality check via <see cref="ItemsMatch(T, T)"/>.
/// </summary>
/// <typeparam name="T">The type of item that this calculator can hash.</typeparam>
public interface IHashCalculator<in T>
{
    /// <summary>
    /// Gets the number of bits in each hash value produced by this calculator.
    /// </summary>
    int HashBitCount { get; }

    /// <summary>
    /// Gets the number of hash values produced for each item. Each value can be
    /// retrieved by calling <see cref="GetHashValue(int)"/> with a zero-based index.
    /// </summary>
    int HashValuesCount { get; }

    /// <summary>
    /// Determines whether two items are considered equal by this calculator.
    /// This is a logical comparison used alongside the computed hash values.
    /// </summary>
    /// <param name="left">The first item to compare.</param>
    /// <param name="right">The second item to compare.</param>
    /// <returns><c>true</c> if the items match; otherwise, <c>false</c>.</returns>
    bool ItemsMatch(T left, T right);

    /// <summary>
    /// Computes and caches the hash values for the specified <paramref name="item"/>.
    /// After calling this method, call <see cref="GetHashValue(int)"/> to obtain
    /// individual hash values by index.
    /// </summary>
    /// <param name="item">The item to compute hash values for.</param>
    void ComputeHash(T item);

    /// <summary>
    /// Gets a previously computed hash value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the hash value to retrieve. Must be in the range 0..(<see cref="HashValuesCount"/> - 1).</param>
    /// <returns>The hash value at the given <paramref name="index"/>.</returns>
    int GetHashValue(int index);
}
