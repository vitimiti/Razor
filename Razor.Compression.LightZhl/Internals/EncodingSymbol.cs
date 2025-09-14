// -----------------------------------------------------------------------
// <copyright file="EncodingSymbol.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal struct EncodingSymbol
{
    public short NumberOfBits { get; set; }

    public ushort Code { get; set; }
}
