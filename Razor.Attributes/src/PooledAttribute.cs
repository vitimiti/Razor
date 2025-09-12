// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Attributes;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PooledAttribute : Attribute
{
    public bool CallResetOnReturn { get; set; } = true;
}
