// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Razor.Browser.Abstractions;

namespace Razor.Browser.Engine;

[PublicAPI]
public abstract class BrowserEngineBase : IBrowserEngine
{
    protected readonly Dictionary<string, IBrowserInstance> _browsers = new();
    protected bool _initialized = false;

    public string BadPageUrl { get; set; } = string.Empty;
    public string LoadingPageUrl { get; set; } = string.Empty;
    public string MouseFileName { get; set; } = string.Empty;
    public string MouseBusyFileName { get; set; } = string.Empty;

    public abstract Task InitializeAsync(IntPtr d3dDevice);

    public abstract Task ShutdownAsync();

    public abstract Task<string> CreateBrowserAsync(
        string browserName,
        string url,
        IntPtr parentWindow,
        (int Width, int Height) size,
        BrowserOptions options,
        object gameInterface
    );

    public abstract Task GraphicsApiRenderAsync(int backBufferIndex);

    public abstract Task GraphicsApiUpdateAsync();

    public virtual async Task DestroyBrowserAsync(string browserName)
    {
        if (_browsers.TryGetValue(browserName, out var browser))
        {
            await browser.CloseBrowserAsync();
            _browsers.Remove(browserName);
        }
    }

    public virtual async Task NavigateAsync(string browserName, string url)
    {
        if (_browsers.TryGetValue(browserName, out var browser))
        {
            await browser.NavigateAsync(url);
        }
    }

    public virtual async Task<IntPtr> GetWindowHandleAsync(string browserName)
    {
        return await Task.FromResult(
            _browsers.TryGetValue(browserName, out var browser) ? browser.WindowHandle : IntPtr.Zero
        );
    }

    public virtual async Task<bool> IsOpenAsync(string browserName)
    {
        return await Task.FromResult(_browsers.ContainsKey(browserName));
    }

    public virtual async Task<int> GetUpdateRateAsync(string browserName)
    {
        return await Task.FromResult(
            _browsers.TryGetValue(browserName, out var browser) ? browser.UpdateRate : 0
        );
    }

    public virtual Task SetUpdateRateAsync(string browserName, int updateRate)
    {
        if (_browsers.TryGetValue(browserName, out var browser))
        {
            browser.UpdateRate = updateRate;
        }

        return Task.CompletedTask;
    }
}
