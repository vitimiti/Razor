// -----------------------------------------------------------------------
// <copyright file="IBrowserEngine.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.BrowserEngine;

/// <summary>
/// Cross-platform browser engine interface for embedded web content.
/// </summary>
public interface IBrowserEngine : IDisposable
{
    /// <summary>
    /// Initialize the browser engine with optional graphics device.
    /// </summary>
    /// <param name="graphicsDevice">Platform-specific graphics device (optional.)</param>
    /// <returns>Task representing the initialization operation.</returns>
    Task<bool> InitializeAsync(IntPtr graphicsDevice = default);

    /// <summary>
    /// Shutdown the browser engine.
    /// </summary>
    /// <returns>Task representing the shutdown operation.</returns>
    Task ShutdownAsync();

    /// <summary>
    /// Create a new browser instance.
    /// </summary>
    /// <param name="browserName">Unique name for the browser instance.</param>
    /// <param name="url">Initial URL to navigate to.</param>
    /// <param name="parentWindow">Parent window handle (platform-specific).</param>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="options">Browser options flags.</param>
    /// <param name="gameInterface">Game interface callback object.</param>
    /// <returns>Browser instance or null if creation failed.</returns>
    Task<IBrowserInstance?> CreateBrowserAsync(
        string browserName,
        Uri url,
        IntPtr parentWindow,
        int x,
        int y,
        int width,
        int height,
        BrowserOptions options,
        IGameInterface? gameInterface = null
    );

    /// <summary>
    /// Destroy a browser instance by name.
    /// </summary>
    /// <param name="browserName">Name of the browser to destroy.</param>
    /// <returns>True if successfully destroyed.</returns>
    Task<bool> DestroyBrowserAsync(string browserName);

    /// <summary>
    /// Navigate a browser to a new URL.
    /// </summary>
    /// <param name="browserName">Name of the browser.</param>
    /// <param name="url">URL to navigate to.</param>
    /// <returns>True if navigation started successfully.</returns>
    Task<bool> NavigateAsync(string browserName, Uri url);

    /// <summary>
    /// Get the platform-specific window handle for a browser.
    /// </summary>
    /// <param name="browserName">Name of the browser.</param>
    /// <returns>Window handle or IntPtr.Zero if not found.</returns>
    IntPtr GetWindowHandle(string browserName);

    /// <summary>
    /// Check if a browser is open.
    /// </summary>
    /// <param name="browserName">Name of the browser.</param>
    /// <returns>True if browser is open.</returns>
    bool IsOpen(string browserName);

    /// <summary>
    /// Render browsers to graphics device (for in-game rendering.)
    /// </summary>
    /// <param name="backBufferIndex">Back buffer index for rendering.</param>
    /// <returns>True if render was successful.</returns>
    bool Render(int backBufferIndex = 0);

    /// <summary>
    /// Update all browser instances.
    /// </summary>
    /// <returns>True if update was successful.</returns>
    bool Update();

    /// <summary>
    /// Get update rate for a specific browser.
    /// </summary>
    /// <param name="browserName">Name of the browser.</param>
    /// <returns>Update rate in FPS.</returns>
    int GetUpdateRate(string browserName);

    /// <summary>
    /// Set update rate for a specific browser.
    /// </summary>
    /// <param name="browserName">Name of the browser.</param>
    /// <param name="rate">Update rate in FPS.</param>
    void SetUpdateRate(string browserName, int rate);

    /// <summary>
    /// Gets or sets the URL to display for bad/error pages.
    /// </summary>
    Uri BadPageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL to display while pages are loading.
    /// </summary>
    Uri LoadingPageUrl { get; set; }

    /// <summary>
    /// Gets or sets the mouse cursor filename for normal state.
    /// </summary>
    string MouseFileName { get; set; }

    /// <summary>
    /// Gets or sets the mouse cursor filename for busy state.
    /// </summary>
    string MouseBusyFileName { get; set; }
}
