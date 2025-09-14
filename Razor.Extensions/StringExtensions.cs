// -----------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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
