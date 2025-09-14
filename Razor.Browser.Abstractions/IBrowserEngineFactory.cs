// -----------------------------------------------------------------------
// <copyright file="IBrowserEngineFactory.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Browser.Abstractions;

/// <summary>Defines a factory interface for creating instances of <see cref="IBrowserEngine"/>.</summary>
public interface IBrowserEngineFactory
{
    /// <summary>Creates and returns a new instance of an <see cref="IBrowserEngine"/>.</summary>
    /// <returns>A new instance of the <see cref="IBrowserEngine"/>.</returns>
    IBrowserEngine CreateEngine();
}
