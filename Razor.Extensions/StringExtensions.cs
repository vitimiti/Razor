// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Extensions;

[PublicAPI]
public static class StringExtensions
{
    public static string PathNormalized(this string path)
    {
        return path.Replace('\\', '/').ToLowerInvariant();
    }
}
