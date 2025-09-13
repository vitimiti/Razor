// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Razor.Browser.Abstractions;

namespace Razor.Browser.Dispatch;

/// <summary>Provides a factory for creating instances of <see cref="IBrowserDispatch"/>.</summary>
public class BrowserDispatchFactory : IBrowserDispatchFactory
{
    /// <summary>Creates and returns an instance of <see cref="IBrowserDispatch"/>.</summary>
    /// <returns>An instance of <see cref="IBrowserDispatch"/>.</returns>
    public IBrowserDispatch CreateDispatch() => new BrowserDispatch();
}
