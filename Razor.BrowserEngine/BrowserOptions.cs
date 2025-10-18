// -----------------------------------------------------------------------
// <copyright file="BrowserOptions.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.BrowserEngine;

/// <summary>
/// Browser creation options.
/// </summary>
[Flags]
public enum BrowserOptions : long
{
    /// <summary>
    /// No special options.
    /// </summary>
    None = 0,

    /// <summary>
    /// The browser window is resizable.
    /// </summary>
    Resizable = 1 << 0,

    /// <summary>
    /// The browser window does not have scrollbars.
    /// </summary>
    NoScrollbars = 1 << 1,

    /// <summary>
    /// The browser window does not have a context menu.
    /// </summary>
    NoContextMenu = 1 << 2,

    /// <summary>
    /// The browser window is transparent.
    /// </summary>
    Transparent = 1 << 3,

    /// <summary>
    /// The browser window uses offscreen rendering.
    /// </summary>
    OffscreenRendering = 1 << 4,

    /// <summary>
    /// Disable JavaScript in the browser.
    /// </summary>
    DisableJavaScript = 1 << 5,

    /// <summary>
    /// Disable plugins in the browser.
    /// </summary>
    DisablePlugins = 1 << 6,

    /// <summary>
    /// Disable images in the browser.
    /// </summary>
    DisableImages = 1 << 7,

    /// <summary>
    /// Disable web security in the browser.
    /// </summary>
    DisableWebSecurity = 1 << 8,
}
