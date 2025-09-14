// -----------------------------------------------------------------------
// <copyright file="BrowserDispatchFactory.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Razor.Browser.Abstractions;

namespace Razor.Browser.Dispatch;

/// <summary>Provides a factory for creating instances of <see cref="IBrowserDispatch"/>.</summary>
public class BrowserDispatchFactory : IBrowserDispatchFactory
{
    /// <summary>Creates and returns an instance of <see cref="IBrowserDispatch"/>.</summary>
    /// <returns>An instance of <see cref="IBrowserDispatch"/>.</returns>
    public IBrowserDispatch CreateDispatch() => new BrowserDispatch();
}
