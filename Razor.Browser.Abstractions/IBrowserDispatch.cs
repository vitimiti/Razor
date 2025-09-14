// -----------------------------------------------------------------------
// <copyright file="IBrowserDispatch.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Browser.Abstractions;

/// <summary>Defines methods for browser-related dispatch operations.</summary>
public interface IBrowserDispatch
{
    /// <summary>Asynchronously processes the input number and returns the result.</summary>
    /// <param name="num1">The number to be processed.</param>
    /// <returns>A task representing the asynchronous operation, containing the processed result.</returns>
    Task<int> TestMethodAsync(int num1);
}
