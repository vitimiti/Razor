// -----------------------------------------------------------------------
// <copyright file="BrowserDispatch.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Razor.Browser.Abstractions;

namespace Razor.Browser.Dispatch;

/// <summary>Represents an implementation of the <see cref="IBrowserDispatch"/> interface that provides functionality for browser-related dispatch operations.</summary>
public class BrowserDispatch : IBrowserDispatch
{
    /// <summary>Asynchronously processes the input number and returns the result.</summary>
    /// <param name="num1">The number to be processed.</param>
    /// <returns>A task representing the asynchronous operation, containing the processed result.</returns>
    public async Task<int> TestMethodAsync(int num1)
    {
        await Task.Delay(1).ConfigureAwait(false); // Simulate async work
        return num1 * 2;
    }
}
