// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Browser.Abstractions;

/// <summary>Represents a browser instance interface that provides properties and methods to interact with a specific browser instance.</summary>
public interface IBrowserInstance
{
    /// <summary>Gets the version of the engine used by the browser instance.</summary>
    /// <value>A string representing the engine version.</value>
    string EngineVersion { get; }

    /// <summary>Gets an interface that facilitates interaction with game-related browser extensions or components.</summary>
    /// <value>An object representing the game interface, or <c>null</c> if unavailable.</value>
    object? GameInterface { get; }

    /// <summary>Gets the command-line arguments used to launch the browser instance.</summary>
    /// <value>An array of strings representing the command-line arguments.</value>
    ICollection<string> CommandLineArgs { get; }

    /// <summary>Indicates whether the current build is a release build.</summary>
    /// <value>A boolean where <c>true</c> represents a release build and <c>false</c> represents a non-release (e.g., debug) build.</value>
    bool IsReleaseBuild { get; }

    /// <summary>Gets the folder path where the browser instance is installed.</summary>
    /// <value>A string representing the installation folder path.</value>
    string InstallFolder { get; }

    /// <summary>Gets the native window handle associated with the browser instance.</summary>
    /// <value>An <see cref="IntPtr"/> representing the native window handle.</value>
    nint WindowHandle { get; }

    /// <summary>Gets or sets the update rate of the browser instance.</summary>
    /// <value>An integer representing the frequency, in milliseconds, at which the browser instance updates.</value>
    int UpdateRate { get; set; }

    /// <summary>Gets the name of the browser instance.</summary>
    /// <value>A string representing the browser name.</value>
    string Name { get; }

    /// <summary>Gets the operating system of the system on which the browser instance is running.</summary>
    /// <value>A string representing the system operating system.</value>
    string SystemOs { get; }

    /// <summary>Gets the amount of system RAM available on the machine running the browser.</summary>
    /// <value>An integer representing the total amount of system RAM in megabytes.</value>
    int SystemRam { get; }

    /// <summary>Gets the type of CPU in the system where the browser instance is running.</summary>
    /// <value>A <see cref="TimeSpan"/> representing the system's CPU type.</value>
    TimeSpan SystemCpuType { get; }

    /// <summary>Gets the speed of the system's CPU in megahertz (MHz).</summary>
    /// <value>An integer representing the CPU speed in MHz.</value>
    int SystemCpuSpeed { get; }

    /// <summary>Gets the screen resolution of the system where the browser instance is running.</summary>
    /// <value>A tuple representing the width (X) and height (Y) of the screen in pixels.</value>
    (int X, int Y) SystemScreenResolution { get; }

    /// <summary>Gets the name of the video adapter on the system where the browser instance is running.</summary>
    /// <value>A string representing the system video adapter.</value>
    string SystemVideoAdapter { get; }

    /// <summary>Navigates the browser instance to the specified URL asynchronously.</summary>
    /// <param name="url">The URL to navigate to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NavigateAsync(Uri url);

    /// <summary>Performs an HTTP request of the specified type to the given URL asynchronously.</summary>
    /// <param name="requestType">The type of HTTP request to be performed (e.g., GET, POST, PUT, DELETE).</param>
    /// <param name="url">The URL to which the request is sent.</param>
    /// <param name="formData">The form data to include in the request body.</param>
    /// <param name="extraData">Additional data or parameters to include in the request.</param>
    /// <param name="callback">An optional callback function to process the response of the request.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the request.</returns>
    Task<object> RequestUrlAsync(
        RequestType requestType,
        Uri url,
        string formData,
        string extraData,
        Func<object, Task<object>>? callback
    );

    /// <summary>Closes the browser instance asynchronously.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CloseBrowserAsync();
}
