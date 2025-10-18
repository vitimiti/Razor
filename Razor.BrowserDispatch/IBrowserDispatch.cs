// -----------------------------------------------------------------------
// <copyright file="IBrowserDispatch.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.BrowserDispatch;

/// <summary>
/// Browser dispatch interface for handling browser-related operations in the game.
/// </summary>
public interface IBrowserDispatch : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the dispatch system is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Test method for verifying browser dispatch functionality.
    /// </summary>
    /// <param name="num1">Test integer parameter.</param>
    /// <returns>Task representing the asynchronous operation result.</returns>
    Task<bool> TestMethodAsync(int num1);

    /// <summary>
    /// Initialize the browser dispatch system.
    /// </summary>
    /// <returns><see langword="true"/> if initialization was successful.</returns>
    Task<bool> InitializeAsync();

    /// <summary>
    /// Shutdown the browser dispatch system.
    /// </summary>
    /// <returns>Task representing the shutdown operation.</returns>
    Task ShutdownAsync();
}
