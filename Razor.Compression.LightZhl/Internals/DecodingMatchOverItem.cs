// -----------------------------------------------------------------------
// <copyright file="DecodingMatchOverItem.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal readonly struct DecodingMatchOverItem(int numberOfExtraBits, int @base)
{
    public readonly int NumberOfExtraBits = numberOfExtraBits;
    public readonly int Base = @base;
}
