// -----------------------------------------------------------------------
// <copyright file="IBrowserDispatchFactory.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Browser.Abstractions;

/// <summary>Represents a factory interface for creating instances of <see cref="IBrowserDispatch"/>.</summary>
public interface IBrowserDispatchFactory
{
    /// <summary>Creates and returns an instance of <see cref="IBrowserDispatch"/>.</summary>
    /// <returns>An instance of <see cref="IBrowserDispatch"/>.</returns>
    IBrowserDispatch CreateDispatch();
}
