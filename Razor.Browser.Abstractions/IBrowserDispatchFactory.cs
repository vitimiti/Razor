// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Browser.Abstractions;

/// <summary>Represents a factory interface for creating instances of <see cref="IBrowserDispatch"/>.</summary>
public interface IBrowserDispatchFactory
{
    /// <summary>Creates and returns an instance of <see cref="IBrowserDispatch"/>.</summary>
    /// <returns>An instance of <see cref="IBrowserDispatch"/>.</returns>
    IBrowserDispatch CreateDispatch();
}
