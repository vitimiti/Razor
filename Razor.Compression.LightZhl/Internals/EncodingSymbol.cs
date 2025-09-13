// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl.Internals;

internal struct EncodingSymbol
{
    public short NumberOfBits { get; set; }
    public ushort Code { get; set; }
}
