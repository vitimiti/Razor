// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Razor.Extensions;

/// <summary>Extensions to the encoding types.</summary>
public static class EncodingExtensions
{
    /// <summary>Gets the Windows-1252 ASCII encoding.</summary>
    /// <returns>
    ///     A new <see cref="Encoding" /> that supports Windows-1252.
    /// </returns>
    public static Encoding Ansi => Encoding.GetEncoding(1252);

    static EncodingExtensions()
    {
        // Register the code page provider to get additional encodings.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
