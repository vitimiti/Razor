// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Razor.Extensions;

public static class EncodingExtensions
{
    public static Encoding Ansi => Encoding.GetEncoding(1252);

    static EncodingExtensions()
    {
        // Register the code page provider to get additional encodings.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
