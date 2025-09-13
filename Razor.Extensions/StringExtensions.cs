// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Razor.Extensions;

/// <summary>Provides extension methods for string manipulation.</summary>
public static class StringExtensions
{
    /// <summary>Normalizes a file path by replacing backslashes with forward slashes and converting to uppercase.</summary>
    /// <param name="path">The file path to normalize.</param>
    /// <returns>The normalized file path.</returns>
    public static string PathNormalized([NotNull] this string path) => path.Replace('\\', '/').ToUpperInvariant();
}
