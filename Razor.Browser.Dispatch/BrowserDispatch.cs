// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
