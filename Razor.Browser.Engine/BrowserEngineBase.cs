// -----------------------------------------------------------------------
// <copyright file="BrowserEngineBase.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Razor.Browser.Abstractions;

namespace Razor.Browser.Engine;

/// <summary>Represents the base class for implementing a browser engine, providing core functionalities including browser instance management, initialization, destruction, navigation, and rendering operations.</summary>
public abstract class BrowserEngineBase : IBrowserEngine
{
    /// <summary>Gets or sets the URL to be displayed when a navigation attempt encounters an error.</summary>
    /// <value>A string representing the error page URL. Used to handle navigation errors and display an appropriate fallback page.</value>
    public Uri BadPageUrl { get; set; } = new("about:blank");

    /// <summary>Gets or sets the URL to be displayed when a browser instance is in the process of loading a page.</summary>
    /// <value>A string representing the URL of the loading page. This value is configurable and helps provide a visual cue during navigation or page load operations.</value>
    public Uri LoadingPageUrl { get; set; } = new("about:blank");

    /// <summary>Gets or sets the file path to the mouse input script for the browser engine.</summary>
    /// <value>A string representing the file path to the mouse input configuration or script. This property may be useful for automating or customizing mouse behavior within the browser engine.</value>
    public string MouseFileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the file name used to indicate that the mouse is in a 'busy' state within the browser engine.</summary>
    /// <value>A string representing the path or name of the file used for displaying a busy mouse state. This property is typically utilized internally to manage visual feedback for operations requiring user wait.</value>
    public string MouseBusyFileName { get; set; } = string.Empty;

    /// <summary>Gets a collection of active browser instances managed by the browser engine.</summary>
    /// <value>A dictionary that maps browser instance names (as strings) to their respective <see cref="IBrowserInstance"/> objects. This property is used internally to manage creation, navigation, and destruction of browser instances.</value>
    protected Dictionary<string, IBrowserInstance> Browsers { get; } = [];

    /// <summary>Gets a value indicating whether the browser engine has been successfully initialized.</summary>
    /// <value>A boolean value that is <c>true</c> if the engine has completed initialization, or <c>false</c> otherwise. This property is used internally to track the readiness of the engine for browser-related operations.</value>
    protected bool Initialized { get; }

    /// <summary>Asynchronously initializes the browser engine with the specified graphics API device.</summary>
    /// <param name="graphicsApiDevice">A handle to the graphics API device used for rendering in the browser engine.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task InitializeAsync(IntPtr graphicsApiDevice);

    /// <summary>Asynchronously shuts down the browser engine, releasing all associated resources.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task ShutdownAsync();

    /// <summary>Asynchronously creates a new browser instance with the specified configuration.</summary>
    /// <param name="browserName">The unique name for the browser instance to be created.</param>
    /// <param name="url">The initial URL to navigate the browser instance to.</param>
    /// <param name="parentWindow">A handle to the parent window where the browser will be displayed.</param>
    /// <param name="size">The dimensions (width and height) of the browser window, specified as a tuple.</param>
    /// <param name="options">The configuration options for the browser instance, such as rendering or security settings.</param>
    /// <param name="gameInterface">An additional interface object for game engine integrations or other auxiliary purposes.</param>
    /// <returns>A task that represents the asynchronous operation, returning a string identifier for the created browser instance.</returns>
    public abstract Task<string> CreateBrowserAsync(
        string browserName,
        Uri url,
        IntPtr parentWindow,
        (int Width, int Height) size,
        BrowserOptions options,
        object gameInterface
    );

    /// <summary>Asynchronously performs rendering operations using the specified back buffer index with the graphics API.</summary>
    /// <param name="backBufferIndex">An integer representing the back buffer index to be used for rendering.</param>
    /// <returns>A task that represents the asynchronous rendering operation.</returns>
    public abstract Task GraphicsApiRenderAsync(int backBufferIndex);

    /// <summary>Asynchronously updates the browser engine's internal state using the configured graphics API.</summary>
    /// <returns>A task representing the asynchronous update operation.</returns>
    public abstract Task GraphicsApiUpdateAsync();

    /// <summary>Asynchronously destroys the browser instance with the specified name.</summary>
    /// <param name="browserName">The name of the browser instance to be destroyed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task DestroyBrowserAsync(string browserName)
    {
        if (Browsers.TryGetValue(browserName, out IBrowserInstance? browser))
        {
            await browser.CloseBrowserAsync().ConfigureAwait(true);
            _ = Browsers.Remove(browserName);
        }
    }

    /// <summary>Asynchronously navigates the specified browser instance to the given URL.</summary>
    /// <param name="browserName">The name of the browser instance to navigate.</param>
    /// <param name="url">The URL to navigate the browser instance to.</param>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    public virtual async Task NavigateAsync(string browserName, Uri url)
    {
        if (Browsers.TryGetValue(browserName, out IBrowserInstance? browser))
        {
            await browser.NavigateAsync(url).ConfigureAwait(true);
        }
    }

    /// <summary>Asynchronously retrieves the window handle associated with the specified browser instance.</summary>
    /// <param name="browserName">The name of the browser instance whose window handle is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation, containing the window handle if found; otherwise, <c>IntPtr.Zero</c>.</returns>
    public virtual async Task<IntPtr> GetWindowHandleAsync(string browserName) =>
        await Task.FromResult(
                Browsers.TryGetValue(browserName, out IBrowserInstance? browser) ? browser.WindowHandle : nint.Zero
            )
            .ConfigureAwait(true);

    /// <summary>Asynchronously checks if a browser instance with the specified name is currently open.</summary>
    /// <param name="browserName">The name of the browser instance to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the browser instance is open.</returns>
    public virtual async Task<bool> IsOpenAsync(string browserName) =>
        await Task.FromResult(Browsers.ContainsKey(browserName)).ConfigureAwait(true);

    /// <summary>Asynchronously retrieves the update rate for the specified browser instance.</summary>
    /// <param name="browserName">The name of the browser instance whose update rate is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation, containing the update rate in milliseconds.</returns>
    public virtual async Task<int> GetUpdateRateAsync(string browserName) =>
        await Task.FromResult(Browsers.TryGetValue(browserName, out IBrowserInstance? browser) ? browser.UpdateRate : 0)
            .ConfigureAwait(true);

    /// <summary>Asynchronously sets the update rate for a specified browser instance.</summary>
    /// <param name="browserName">The name of the browser instance for which the update rate is to be set.</param>
    /// <param name="updateRate">The update rate value to be applied to the specified browser instance.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task SetUpdateRateAsync(string browserName, int updateRate)
    {
        if (Browsers.TryGetValue(browserName, out IBrowserInstance? browser))
        {
            browser.UpdateRate = updateRate;
        }

        return Task.CompletedTask;
    }
}
