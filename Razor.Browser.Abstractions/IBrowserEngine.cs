// -----------------------------------------------------------------------
// <copyright file="IBrowserEngine.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Browser.Abstractions;

/// <summary>Represents an abstraction for a browser engine, providing methods and properties to manage browser instances and rendering operations.</summary>
public interface IBrowserEngine
{
    /// <summary>Gets or sets the URL of a page that is displayed when an error or invalid condition occurs in the browser.</summary>
    /// <remarks>This property is typically used to redirect the user to a designated error page when a navigation or rendering issue is encountered.</remarks>
    Uri BadPageUrl { get; set; }

    /// <summary>Gets or sets the URL of a page that is displayed while content is loading in the browser.</summary>
    /// <remarks>This property is typically used to provide visual feedback or placeholder content during navigation or rendering delays.</remarks>
    Uri LoadingPageUrl { get; set; }

    /// <summary>Gets or sets the file name associated with the default mouse cursor used by the browser engine.</summary>
    /// <remarks>This property allows customizing the appearance of the mouse cursor by specifying a file path to the desired cursor asset.</remarks>
    string MouseFileName { get; set; }

    /// <summary>Gets or sets the file name used to display a busy (loading) mouse cursor in the browser.</summary>
    /// <remarks>This property specifies the visual asset that represents a busy cursor, indicating loading or processing activity in the browser interface.</remarks>
    string MouseBusyFileName { get; set; }

    /// <summary>Initializes the browser engine with the specified graphics API device.</summary>
    /// <param name="graphicsApiDevice">An <see cref="IntPtr"/> pointing to the graphics API device used for rendering.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    Task InitializeAsync(IntPtr graphicsApiDevice);

    /// <summary>Shuts down the browser engine and releases any associated resources.</summary>
    /// <returns>A task that represents the asynchronous shutdown operation.</returns>
    Task ShutdownAsync();

    /// <summary>Creates a new browser instance with the specified parameters.</summary>
    /// <param name="browserName">The name of the browser instance to be created.</param>
    /// <param name="url">The initial URL to load in the browser.</param>
    /// <param name="parentWindow">An <see cref="IntPtr"/> representing the handle of the parent window.</param>
    /// <param name="size">A tuple containing the width and height of the browser instance.</param>
    /// <param name="options">The browser configuration options specified as <see cref="BrowserOptions"/>.</param>
    /// <param name="gameInterface">An object representing the game interface to be used with the browser.</param>
    /// <returns>A task that represents the asynchronous browser creation operation. The result is a <see cref="string"/> containing the identifier of the created browser instance.</returns>
    Task<string> CreateBrowserAsync(
        string browserName,
        Uri url,
        IntPtr parentWindow,
        (int Width, int Height) size,
        BrowserOptions options,
        object gameInterface
    );

    /// <summary>Destroys the browser instance with the specified name and releases any associated resources.</summary>
    /// <param name="browserName">The name of the browser instance to be destroyed.</param>
    /// <returns>A task that represents the asynchronous browser destruction operation.</returns>
    Task DestroyBrowserAsync(string browserName);

    /// <summary>Navigates the specified browser to the given URL.</summary>
    /// <param name="browserName">The name of the browser instance to navigate.</param>
    /// <param name="url">The destination URL to navigate to.</param>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    Task NavigateAsync(string browserName, Uri url);

    /// <summary>Gets the window handle for the specified browser instance.</summary>
    /// <param name="browserName">The name of the browser instance whose window handle is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation, containing an <see cref="IntPtr"/> to the window handle if the browser exists, or <see cref="IntPtr.Zero"/> otherwise.</returns>
    Task<IntPtr> GetWindowHandleAsync(string browserName);

    /// <summary>Checks whether the browser instance with the specified name is currently open.</summary>
    /// <param name="browserName">The name of the browser instance to check.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean value indicating whether the browser instance is open.</returns>
    Task<bool> IsOpenAsync(string browserName);

    /// <summary>Renders the browser content to the specified back buffer using the graphics API.</summary>
    /// <param name="backBufferIndex">An integer representing the index of the back buffer to render to.</param>
    /// <returns>A task that represents the asynchronous rendering operation.</returns>
    Task GraphicsApiRenderAsync(int backBufferIndex);

    /// <summary>Updates the browser engine state for the current frame using the graphics API.</summary>
    /// <returns>A task that represents the asynchronous operation for updating the graphics API state.</returns>
    Task GraphicsApiUpdateAsync();

    /// <summary>Retrieves the update rate for the specified browser.</summary>
    /// <param name="browserName">The name of the browser whose update rate is being retrieved.</param>
    /// <returns>A task that represents the asynchronous operation, containing the update rate as an integer.</returns>
    Task<int> GetUpdateRateAsync(string browserName);

    /// <summary>Sets the update rate for the specified browser instance.</summary>
    /// <param name="browserName">The name of the browser instance for which to set the update rate.</param>
    /// <param name="updateRate">The desired update rate, in updates per second.</param>
    /// <returns>A task that represents the asynchronous operation of setting the update rate.</returns>
    Task SetUpdateRateAsync(string browserName, int updateRate);
}
