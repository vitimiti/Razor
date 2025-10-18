// -----------------------------------------------------------------------
// <copyright file="IGameInterface.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.BrowserEngine;

/// <summary>
/// Interface for game-browser communication.
/// </summary>
public interface IGameInterface
{
    /// <summary>
    /// Called when browser navigation starts.
    /// </summary>
    /// <param name="url">URL being navigated to.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task OnNavigationStartedAsync(Uri url);

    /// <summary>
    /// Called when browser navigation completes.
    /// </summary>
    /// <param name="url">Final URL.</param>
    /// <param name="success">Whether navigation was successful.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task OnNavigationCompletedAsync(Uri url, bool success);

    /// <summary>
    /// Called when browser receives a JavaScript message.
    /// </summary>
    /// <param name="message">Message from JavaScript.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task OnJavaScriptMessageAsync(string message);

    /// <summary>
    /// Called when browser window is closed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task OnBrowserClosedAsync();
}
