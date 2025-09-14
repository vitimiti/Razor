// -----------------------------------------------------------------------
// <copyright file="EncodingExtensions.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;

namespace Razor.Extensions;

/// <summary>Provides extension methods and properties related to text encoding.</summary>
public static class EncodingExtensions
{
    // Register the code page provider to get additional encodings.
    static EncodingExtensions() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>Gets the ANSI encoding (Code Page 1252).</summary>
    /// <remarks>This property provides a static reference to the ANSI encoding, commonly used for legacy systems or specific text processing scenarios.</remarks>
    public static Encoding Ansi => Encoding.GetEncoding(1252);
}
