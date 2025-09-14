// -----------------------------------------------------------------------
// <copyright file="PooledAttribute.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Attributes;

/// <summary>Marks a class as utilizing a pooling mechanism, enabling instances to be reused to optimize memory allocation and performance.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PooledAttribute : Attribute
{
    /// <summary>Gets or sets a value indicating whether the instance should be reset after it is returned to the pool.</summary>
    /// <remarks>This is useful for classes that have state that should be reset to a default state after each use.</remarks>
    public bool CallResetOnReturn { get; set; } = true;
}
