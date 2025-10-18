// -----------------------------------------------------------------------
// <copyright file="LegacyEncodings.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;

namespace Razor.Utilities;

/// <summary>
/// Provides access to legacy code page encodings used by older file formats
/// and interop scenarios.
/// </summary>
/// <remarks>
/// This static helper ensures the <see cref="CodePagesEncodingProvider"/>
/// is registered with <see cref="Encoding"/> so legacy encodings
/// (for example, Windows-1252) can be retrieved via
/// <see cref="Encoding.GetEncoding(int)"/>.
/// </remarks>
public static class LegacyEncodings
{
    /// <summary>
    /// Initializes static members of the <see cref="LegacyEncodings"/> class.
    /// </summary>
    /// <remarks>
    /// Registers the <see cref="CodePagesEncodingProvider"/> so legacy code page
    /// encodings are available from <see cref="Encoding"/>.
    /// </remarks>
    static LegacyEncodings() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>
    /// Gets the legacy ANSI encoding (Windows code page 1252).
    /// </summary>
    /// <remarks>
    /// Code page 1252 is commonly used by legacy Windows text files. For
    /// new content prefer Unicode encodings such as UTF-8.
    /// </remarks>
    public static Encoding Ansi => Encoding.GetEncoding(1252);
}
