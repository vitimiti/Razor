// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Attributes;

/// <summary>Marks a class as utilizing a pooling mechanism, enabling instances to be reused to optimize memory allocation and performance.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PooledAttribute : Attribute
{
    /// <summary>Gets or sets a value indicating whether the instance should be reset after it is returned to the pool.</summary>
    /// <remarks>This is useful for classes that have state that should be reset to a default state after each use.</remarks>
    public bool CallResetOnReturn { get; set; } = true;
}
