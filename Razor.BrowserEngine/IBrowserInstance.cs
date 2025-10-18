// -----------------------------------------------------------------------
// <copyright file="IBrowserInstance.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.BrowserEngine;

/// <summary>
/// Represents a single browser instance.
/// </summary>
public interface IBrowserInstance : IDisposable
{
    /// <summary>
    /// Navigate to a new URL.
    /// </summary>
    /// <param name="url">URL to navigate to.</param>
    /// <returns>Task representing the navigation operation.</returns>
    Task<bool> NavigateAsync(Uri url);

    /// <summary>
    /// Make an HTTP request.
    /// </summary>
    /// <param name="requestType">HTTP method (GET, POST, etc.)</param>
    /// <param name="url">URL to request.</param>
    /// <param name="formData">Form data for POST requests.</param>
    /// <param name="extraData">Additional request data.</param>
    /// <param name="callback">Callback for response handling.</param>
    /// <returns>Request result.</returns>
    Task<object?> RequestUrlAsync(
        string requestType,
        Uri url,
        string? formData = null,
        string? extraData = null,
        Func<object, Task>? callback = null
    );

    /// <summary>
    /// Close this browser instance.
    /// </summary>
    /// <returns>Task representing the close operation.</returns>
    Task CloseBrowserAsync();

    /// <summary>
    /// Gets the browser engine version.
    /// </summary>
    string EngineVersion { get; }

    /// <summary>
    /// Gets the game interface callback object.
    /// </summary>
    IGameInterface? GameInterface { get; }

    /// <summary>
    /// Gets the command line arguments passed to the application.
    /// </summary>
    IList<string> CommandLineArgs { get; }

    /// <summary>
    /// Gets a value indicating whether this is a release build.
    /// </summary>
    bool IsReleaseBuild { get; }

    /// <summary>
    /// Gets the application installation folder.
    /// </summary>
    string InstallFolder { get; }

    /// <summary>
    /// Gets the platform-specific window handle.
    /// </summary>
    nint WindowHandle { get; }

    /// <summary>
    /// Gets or sets the update rate in FPS.
    /// </summary>
    int UpdateRate { get; set; }

    /// <summary>
    /// Gets the browser instance name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the system information: Operating System.
    /// </summary>
    string SystemInfoOS { get; }

    /// <summary>
    /// Gets the system information: RAM in MB.
    /// </summary>
    int SystemInfoRAM { get; }

    /// <summary>
    /// Gets the system information: CPU Type.
    /// </summary>
    string SystemInfoCPUType { get; }

    /// <summary>
    /// Gets the system information: CPU Speed in MHz.
    /// </summary>
    int SystemInfoCPUSpeed { get; }

    /// <summary>
    /// Gets the system information: Screen X Resolution.
    /// </summary>
    int SystemInfoXRes { get; }

    /// <summary>
    /// Gets the system information: Screen Y Resolution.
    /// </summary>
    int SystemInfoYRes { get; }

    /// <summary>
    /// Gets the system information: Video Adapter name.
    /// </summary>
    string SystemInfoVideoAdapter { get; }
}
