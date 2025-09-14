// -----------------------------------------------------------------------
// <copyright file="IPooledResettable.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Abstractions;

/// <summary>Represents an interface for objects that can be reset and are intended to be utilized within object pooling mechanisms.</summary>
public interface IPooledResettable
{
    /// <summary>Resets the state of the object, typically restoring it to an initial or default configuration.</summary>
    void Reset();
}
