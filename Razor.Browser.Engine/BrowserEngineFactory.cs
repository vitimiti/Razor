// -----------------------------------------------------------------------
// <copyright file="BrowserEngineFactory.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Razor.Browser.Abstractions;

namespace Razor.Browser.Engine;

/// <summary>Provides a factory for creating instances of <see cref="IBrowserEngine"/>.</summary>
public class BrowserEngineFactory(Func<IBrowserEngine> engineFactory) : IBrowserEngineFactory
{
    /// <summary>Creates an instance of <see cref="IBrowserEngine"/> using the configured factory.</summary>
    /// <returns>An instance of <see cref="IBrowserEngine"/>.</returns>
    public IBrowserEngine CreateEngine() => engineFactory();
}
