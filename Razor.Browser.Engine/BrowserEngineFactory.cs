// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Browser.Abstractions;

namespace Razor.Browser.Engine;

/// <summary>Provides a factory for creating instances of <see cref="IBrowserEngine"/>.</summary>
public class BrowserEngineFactory(Func<IBrowserEngine> engineFactory) : IBrowserEngineFactory
{
    /// <summary>Creates an instance of <see cref="IBrowserEngine"/> using the configured factory.</summary>
    /// <returns>An instance of <see cref="IBrowserEngine"/>.</returns>
    public IBrowserEngine CreateEngine() => engineFactory();
}
