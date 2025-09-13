// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Razor.Extensions;

/// <summary>Provides extension methods and properties related to text encoding.</summary>
public static class EncodingExtensions
{
    /// <summary>Gets the ANSI encoding (Code Page 1252).</summary>
    /// <remarks>This property provides a static reference to the ANSI encoding, commonly used for legacy systems or specific text processing scenarios.</remarks>
    public static Encoding Ansi => Encoding.GetEncoding(1252);

    // Register the code page provider to get additional encodings.
    static EncodingExtensions() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
}
