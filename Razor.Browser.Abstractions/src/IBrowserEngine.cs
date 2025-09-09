// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Browser.Abstractions;

[PublicAPI]
public interface IBrowserEngine
{
    string BadPageUrl { get; set; }
    string LoadingPageUrl { get; set; }
    string MouseFileName { get; set; }
    string MouseBusyFileName { get; set; }

    Task InitializeAsync(IntPtr d3dDevice);

    Task ShutdownAsync();

    Task<string> CreateBrowserAsync(
        string browserName,
        string url,
        IntPtr parentWindow,
        (int Width, int Height) size,
        BrowserOptions options,
        object gameInterface
    );

    Task DestroyBrowserAsync(string browserName);

    Task NavigateAsync(string browserName, string url);

    Task<IntPtr> GetHWndAsync(string browserName);

    Task<bool> IsOpenAsync(string browserName);

    Task D3DRenderAsync(int backBufferIndex);

    Task D3DUpdateAsync();

    Task<int> GetUpdateRateAsync(string browserName);

    Task SetUpdateRateAsync(string browserName, int updateRate);
}
