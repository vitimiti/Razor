// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Browser.Abstractions;

/// <summary>Defines methods for browser-related dispatch operations.</summary>
public interface IBrowserDispatch
{
    /// <summary>Asynchronously processes the input number and returns the result.</summary>
    /// <param name="num1">The number to be processed.</param>
    /// <returns>A task representing the asynchronous operation, containing the processed result.</returns>
    Task<int> TestMethodAsync(int num1);
}
