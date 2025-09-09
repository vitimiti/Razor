// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Browser.Abstractions;

[PublicAPI]
public interface IBrowserInstance
{
    string EngineVersion { get; }
    object? GameInterface { get; }
    string[] CommandLineArgs { get; }
    bool IsReleaseBuild { get; }
    string InstallFolder { get; }
    IntPtr WindowHandle { get; }
    int UpdateRate { get; set; }
    string Name { get; }
    string SystemOs { get; }
    int SystemRam { get; }
    string SystemCpuType { get; }
    int SystemCpuSpeed { get; }
    (int X, int Y) SystemScreenResolution { get; }
    string SystemVideoAdapter { get; }

    Task NavigateAsync(string url);

    Task<object> RequestUrlAsync(
        RequestType requestType,
        string url,
        string formData,
        string extraData,
        Func<object, Task<object>>? callback
    );

    Task CloseBrowserAsync();
}
