// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl;

internal readonly struct DecodingMatchOverItem(int numberOfExtraBits, int @base)
{
    public readonly int NumberOfExtraBits = numberOfExtraBits;
    public readonly int Base = @base;
}
