// -----------------------------------------------------------------------
// <copyright file="Hashable.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Hashing;

/// <summary>
/// Base class for objects that can be stored in a <see cref="HashTable"/>.
/// </summary>
/// <remarks>
/// Implementations must provide a string key via <see cref="Key"/> used for hashing and lookup.
/// The <see cref="Next"/> property is used internally by the hash table implementation to chain
/// entries that map to the same bucket.
/// </remarks>
public abstract class Hashable
{
    /// <summary>
    /// Gets the key used for hashing and lookup.
    /// </summary>
    public abstract string Key { get; }

    /// <summary>
    /// Gets or sets the next entry in the same hash bucket (used internally by <see cref="HashTable"/>).
    /// </summary>
    internal Hashable? Next { get; set; }
}
