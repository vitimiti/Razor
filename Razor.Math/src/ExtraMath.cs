// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Math;

[PublicAPI]
public static class ExtraMath
{
    public static float InvSqrt(float value)
    {
        return 1F / float.Sqrt(value);
    }

    public static bool IsValid(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
