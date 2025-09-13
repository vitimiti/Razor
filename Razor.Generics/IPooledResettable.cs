// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Generics;

/// <summary>Represents an interface for objects that can be reset and are intended to be utilized within object pooling mechanisms.</summary>
public interface IPooledResettable
{
    /// <summary>Resets the state of the object, typically restoring it to an initial or default configuration.</summary>
    void Reset();
}
