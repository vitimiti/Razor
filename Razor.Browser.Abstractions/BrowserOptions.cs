// -----------------------------------------------------------------------
// <copyright file="BrowserOptions.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Browser.Abstractions;

/// <summary>Represents configuration options for a web browser instance.</summary>
/// <remarks>This enumeration supports the use of flags, allowing multiple options to be combined.</remarks>
[Flags]
public enum BrowserOptions : long
{
    /// <summary>Represents no options.</summary>
    None = 0,
}
