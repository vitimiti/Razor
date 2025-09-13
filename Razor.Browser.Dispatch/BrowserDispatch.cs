// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Razor.Browser.Abstractions;

namespace Razor.Browser.Dispatch;

[PublicAPI]
public class BrowserDispatch : IBrowserDispatch
{
    public async Task<int> TestMethodAsync(int num1)
    {
        await Task.Delay(1); // Simulate async work
        return num1 * 2;
    }
}
