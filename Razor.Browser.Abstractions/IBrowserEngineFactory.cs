// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Browser.Abstractions;

/// <summary>Defines a factory interface for creating instances of <see cref="IBrowserEngine"/>.</summary>
public interface IBrowserEngineFactory
{
    /// <summary>Creates and returns a new instance of an <see cref="IBrowserEngine"/>.</summary>
    /// <returns>A new instance of the <see cref="IBrowserEngine"/>.</returns>
    IBrowserEngine CreateEngine();
}
