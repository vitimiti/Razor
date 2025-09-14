// -----------------------------------------------------------------------
// <copyright file="EncodingGlobals.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal static class EncodingGlobals
{
    public const int BufferBits = 16;
    public const int BufferSize = 1 << BufferBits;
    public const int SymbolCount = 256 + 16 + 2;

    // csharpier-ignore
    public static EncodingMatchOverItem[] MatchOverEncodeTable =>
    [
        new(264, 1, 0x00), new(265, 2, 0x00), new(265, 2, 0x02), new(266, 3, 0x00), new(266, 3, 0x02),
        new(266, 3, 0x04), new(266, 3, 0x06), new(267, 4, 0x00), new(267, 4, 0x02), new(267, 4, 0x04),
        new(267, 4, 0x06), new(267, 4, 0x08), new(267, 4, 0x0A), new(267, 4, 0x0C), new(267, 4, 0x0E),
    ];

    // csharpier-ignore
    public static EncodingDisplayItem[] DisplayEncodeTable =>
    [
        new(3, 0x0000), new(3, 0x0001), new(4, 0x0004), new(4, 0x0005), new(5, 0x000C), new(5, 0x000D), new(5, 0x000E),
        new(5, 0x000F), new(6, 0x0020), new(6, 0x0021), new(6, 0x0022), new(6, 0x0023), new(6, 0x0024), new(6, 0x0025),
        new(6, 0x0026), new(6, 0x0027), new(7, 0x0050), new(7, 0x0051), new(7, 0x0052), new(7, 0x0053), new(7, 0x0054),
        new(7, 0x0055), new(7, 0x0056), new(7, 0x0057), new(7, 0x0058), new(7, 0x0059), new(7, 0x005A), new(7, 0x005B),
        new(7, 0x005C), new(7, 0x005D), new(7, 0x005E), new(7, 0x005F), new(8, 0x00C0), new(8, 0x00C1), new(8, 0x00C2),
        new(8, 0x00C3), new(8, 0x00C4), new(8, 0x00C5), new(8, 0x00C6), new(8, 0x00C7), new(8, 0x00C8), new(8, 0x00C9),
        new(8, 0x00CA), new(8, 0x00CB), new(8, 0x00CC), new(8, 0x00CD), new(8, 0x00CE), new(8, 0x00CF), new(8, 0x00D0),
        new(8, 0x00D1), new(8, 0x00D2), new(8, 0x00D3), new(8, 0x00D4), new(8, 0x00D5), new(8, 0x00D6), new(8, 0x00D7),
        new(8, 0x00D8), new(8, 0x00D9), new(8, 0x00DA), new(8, 0x00DB), new(8, 0x00DC), new(8, 0x00DD), new(8, 0x00DE),
        new(8, 0x00DF), new(9, 0x01C0), new(9, 0x01C1), new(9, 0x01C2), new(9, 0x01C3), new(9, 0x01C4), new(9, 0x01C5),
        new(9, 0x01C6), new(9, 0x01C7), new(9, 0x01C8), new(9, 0x01C9), new(9, 0x01CA), new(9, 0x01CB), new(9, 0x01CC),
        new(9, 0x01CD), new(9, 0x01CE), new(9, 0x01CF), new(9, 0x01D0), new(9, 0x01D1), new(9, 0x01D2), new(9, 0x01D3),
        new(9, 0x01D4), new(9, 0x01D5), new(9, 0x01D6), new(9, 0x01D7), new(9, 0x01D8), new(9, 0x01D9), new(9, 0x01DA),
        new(9, 0x01DB), new(9, 0x01DC), new(9, 0x01DD), new(9, 0x01DE), new(9, 0x01DF), new(9, 0x01E0), new(9, 0x01E1),
        new(9, 0x01E2), new(9, 0x01E3), new(9, 0x01E4), new(9, 0x01E5), new(9, 0x01E6), new(9, 0x01E7), new(9, 0x01E8),
        new(9, 0x01E9), new(9, 0x01EA), new(9, 0x01EB), new(9, 0x01EC), new(9, 0x01ED), new(9, 0x01EE), new(9, 0x01EF),
        new(9, 0x01F0), new(9, 0x01F1), new(9, 0x01F2), new(9, 0x01F3), new(9, 0x01F4), new(9, 0x01F5), new(9, 0x01F6),
        new(9, 0x01F7), new(9, 0x01F8), new(9, 0x01F9), new(9, 0x01FA), new(9, 0x01FB), new(9, 0x01FC), new(9, 0x01FD),
        new(9, 0x01FE), new(9, 0x01FF),
    ];

    // csharpier-ignore
    public static EncodingSymbol[] InitialSymbolTable =>
    [
        new() { NumberOfBits = 7, Code = 0x0014 }, new() { NumberOfBits = 8, Code = 0x0030 }, new() { NumberOfBits = 8, Code = 0x0031 },
        new() { NumberOfBits = 8, Code = 0x0032 }, new() { NumberOfBits = 8, Code = 0x0033 }, new() { NumberOfBits = 8, Code = 0x0034 },
        new() { NumberOfBits = 8, Code = 0x0035 }, new() { NumberOfBits = 8, Code = 0x0036 }, new() { NumberOfBits = 8, Code = 0x0037 },
        new() { NumberOfBits = 8, Code = 0x0038 }, new() { NumberOfBits = 8, Code = 0x0039 }, new() { NumberOfBits = 8, Code = 0x003A },
        new() { NumberOfBits = 8, Code = 0x003B }, new() { NumberOfBits = 8, Code = 0x003C }, new() { NumberOfBits = 8, Code = 0x003D },
        new() { NumberOfBits = 8, Code = 0x003E }, new() { NumberOfBits = 8, Code = 0x003F }, new() { NumberOfBits = 8, Code = 0x0040 },
        new() { NumberOfBits = 8, Code = 0x0041 }, new() { NumberOfBits = 8, Code = 0x0042 }, new() { NumberOfBits = 8, Code = 0x0043 },
        new() { NumberOfBits = 8, Code = 0x0044 }, new() { NumberOfBits = 8, Code = 0x0045 }, new() { NumberOfBits = 8, Code = 0x0046 },
        new() { NumberOfBits = 8, Code = 0x0047 }, new() { NumberOfBits = 8, Code = 0x0048 }, new() { NumberOfBits = 8, Code = 0x0049 },
        new() { NumberOfBits = 8, Code = 0x004A }, new() { NumberOfBits = 8, Code = 0x004B }, new() { NumberOfBits = 8, Code = 0x004C },
        new() { NumberOfBits = 8, Code = 0x004D }, new() { NumberOfBits = 8, Code = 0x004E }, new() { NumberOfBits = 7, Code = 0x0015 },
        new() { NumberOfBits = 8, Code = 0x004F }, new() { NumberOfBits = 8, Code = 0x0050 }, new() { NumberOfBits = 8, Code = 0x0051 },
        new() { NumberOfBits = 8, Code = 0x0052 }, new() { NumberOfBits = 8, Code = 0x0053 }, new() { NumberOfBits = 8, Code = 0x0054 },
        new() { NumberOfBits = 8, Code = 0x0055 }, new() { NumberOfBits = 8, Code = 0x0056 }, new() { NumberOfBits = 8, Code = 0x0057 },
        new() { NumberOfBits = 8, Code = 0x0058 }, new() { NumberOfBits = 8, Code = 0x0059 }, new() { NumberOfBits = 8, Code = 0x005A },
        new() { NumberOfBits = 8, Code = 0x005B }, new() { NumberOfBits = 8, Code = 0x005C }, new() { NumberOfBits = 8, Code = 0x005D },
        new() { NumberOfBits = 7, Code = 0x0016 }, new() { NumberOfBits = 8, Code = 0x005E }, new() { NumberOfBits = 8, Code = 0x005F },
        new() { NumberOfBits = 8, Code = 0x0060 }, new() { NumberOfBits = 8, Code = 0x0061 }, new() { NumberOfBits = 8, Code = 0x0062 },
        new() { NumberOfBits = 8, Code = 0x0063 }, new() { NumberOfBits = 8, Code = 0x0064 }, new() { NumberOfBits = 8, Code = 0x0065 },
        new() { NumberOfBits = 8, Code = 0x0066 }, new() { NumberOfBits = 8, Code = 0x0067 }, new() { NumberOfBits = 8, Code = 0x0068 },
        new() { NumberOfBits = 8, Code = 0x0069 }, new() { NumberOfBits = 8, Code = 0x006A }, new() { NumberOfBits = 8, Code = 0x006B },
        new() { NumberOfBits = 8, Code = 0x006C }, new() { NumberOfBits = 8, Code = 0x006D }, new() { NumberOfBits = 8, Code = 0x006E },
        new() { NumberOfBits = 8, Code = 0x006F }, new() { NumberOfBits = 8, Code = 0x0070 }, new() { NumberOfBits = 8, Code = 0x0071 },
        new() { NumberOfBits = 8, Code = 0x0072 }, new() { NumberOfBits = 8, Code = 0x0073 }, new() { NumberOfBits = 8, Code = 0x0074 },
        new() { NumberOfBits = 8, Code = 0x0075 }, new() { NumberOfBits = 8, Code = 0x0076 }, new() { NumberOfBits = 8, Code = 0x0077 },
        new() { NumberOfBits = 8, Code = 0x0078 }, new() { NumberOfBits = 8, Code = 0x0079 }, new() { NumberOfBits = 8, Code = 0x007A },
        new() { NumberOfBits = 8, Code = 0x007B }, new() { NumberOfBits = 8, Code = 0x007C }, new() { NumberOfBits = 8, Code = 0x007D },
        new() { NumberOfBits = 8, Code = 0x007E }, new() { NumberOfBits = 8, Code = 0x007F }, new() { NumberOfBits = 8, Code = 0x0080 },
        new() { NumberOfBits = 8, Code = 0x0081 }, new() { NumberOfBits = 8, Code = 0x0082 }, new() { NumberOfBits = 8, Code = 0x0083 },
        new() { NumberOfBits = 8, Code = 0x0084 }, new() { NumberOfBits = 8, Code = 0x0085 }, new() { NumberOfBits = 8, Code = 0x0086 },
        new() { NumberOfBits = 8, Code = 0x0087 }, new() { NumberOfBits = 8, Code = 0x0088 }, new() { NumberOfBits = 8, Code = 0x0089 },
        new() { NumberOfBits = 8, Code = 0x008A }, new() { NumberOfBits = 8, Code = 0x008B }, new() { NumberOfBits = 8, Code = 0x008C },
        new() { NumberOfBits = 8, Code = 0x008D }, new() { NumberOfBits = 8, Code = 0x008E }, new() { NumberOfBits = 8, Code = 0x008F },
        new() { NumberOfBits = 8, Code = 0x0090 }, new() { NumberOfBits = 8, Code = 0x0091 }, new() { NumberOfBits = 8, Code = 0x0092 },
        new() { NumberOfBits = 8, Code = 0x0093 }, new() { NumberOfBits = 8, Code = 0x0094 }, new() { NumberOfBits = 8, Code = 0x0095 },
        new() { NumberOfBits = 8, Code = 0x0096 }, new() { NumberOfBits = 8, Code = 0x0097 }, new() { NumberOfBits = 8, Code = 0x0098 },
        new() { NumberOfBits = 8, Code = 0x0099 }, new() { NumberOfBits = 8, Code = 0x009A }, new() { NumberOfBits = 8, Code = 0x009B },
        new() { NumberOfBits = 8, Code = 0x009C }, new() { NumberOfBits = 8, Code = 0x009D }, new() { NumberOfBits = 8, Code = 0x009E },
        new() { NumberOfBits = 8, Code = 0x009F }, new() { NumberOfBits = 8, Code = 0x00A0 }, new() { NumberOfBits = 8, Code = 0x00A1 },
        new() { NumberOfBits = 8, Code = 0x00A2 }, new() { NumberOfBits = 8, Code = 0x00A3 }, new() { NumberOfBits = 8, Code = 0x00A4 },
        new() { NumberOfBits = 8, Code = 0x00A5 }, new() { NumberOfBits = 8, Code = 0x00A6 }, new() { NumberOfBits = 8, Code = 0x00A7 },
        new() { NumberOfBits = 8, Code = 0x00A8 }, new() { NumberOfBits = 8, Code = 0x00A9 }, new() { NumberOfBits = 8, Code = 0x00AA },
        new() { NumberOfBits = 8, Code = 0x00AB }, new() { NumberOfBits = 8, Code = 0x00AC }, new() { NumberOfBits = 8, Code = 0x00AD },
        new() { NumberOfBits = 8, Code = 0x00AE }, new() { NumberOfBits = 8, Code = 0x00AF }, new() { NumberOfBits = 8, Code = 0x00B0 },
        new() { NumberOfBits = 8, Code = 0x00B1 }, new() { NumberOfBits = 8, Code = 0x00B2 }, new() { NumberOfBits = 8, Code = 0x00B3 },
        new() { NumberOfBits = 8, Code = 0x00B4 }, new() { NumberOfBits = 8, Code = 0x00B5 }, new() { NumberOfBits = 8, Code = 0x00B6 },
        new() { NumberOfBits = 8, Code = 0x00B7 }, new() { NumberOfBits = 8, Code = 0x00B8 }, new() { NumberOfBits = 8, Code = 0x00B9 },
        new() { NumberOfBits = 8, Code = 0x00BA }, new() { NumberOfBits = 8, Code = 0x00BB }, new() { NumberOfBits = 8, Code = 0x00BC },
        new() { NumberOfBits = 8, Code = 0x00BD }, new() { NumberOfBits = 8, Code = 0x00BE }, new() { NumberOfBits = 8, Code = 0x00BF },
        new() { NumberOfBits = 8, Code = 0x00C0 }, new() { NumberOfBits = 8, Code = 0x00C1 }, new() { NumberOfBits = 8, Code = 0x00C2 },
        new() { NumberOfBits = 8, Code = 0x00C3 }, new() { NumberOfBits = 8, Code = 0x00C4 }, new() { NumberOfBits = 8, Code = 0x00C5 },
        new() { NumberOfBits = 8, Code = 0x00C6 }, new() { NumberOfBits = 8, Code = 0x00C7 }, new() { NumberOfBits = 8, Code = 0x00C8 },
        new() { NumberOfBits = 8, Code = 0x00C9 }, new() { NumberOfBits = 8, Code = 0x00CA }, new() { NumberOfBits = 8, Code = 0x00CB },
        new() { NumberOfBits = 8, Code = 0x00CC }, new() { NumberOfBits = 8, Code = 0x00CD }, new() { NumberOfBits = 8, Code = 0x00CE },
        new() { NumberOfBits = 8, Code = 0x00CF }, new() { NumberOfBits = 9, Code = 0x01A0 }, new() { NumberOfBits = 9, Code = 0x01A1 },
        new() { NumberOfBits = 9, Code = 0x01A2 }, new() { NumberOfBits = 9, Code = 0x01A3 }, new() { NumberOfBits = 9, Code = 0x01A4 },
        new() { NumberOfBits = 9, Code = 0x01A5 }, new() { NumberOfBits = 9, Code = 0x01A6 }, new() { NumberOfBits = 9, Code = 0x01A7 },
        new() { NumberOfBits = 9, Code = 0x01A8 }, new() { NumberOfBits = 9, Code = 0x01A9 }, new() { NumberOfBits = 9, Code = 0x01AA },
        new() { NumberOfBits = 9, Code = 0x01AB }, new() { NumberOfBits = 9, Code = 0x01AC }, new() { NumberOfBits = 9, Code = 0x01AD },
        new() { NumberOfBits = 9, Code = 0x01AE }, new() { NumberOfBits = 9, Code = 0x01AF }, new() { NumberOfBits = 9, Code = 0x01B0 },
        new() { NumberOfBits = 9, Code = 0x01B1 }, new() { NumberOfBits = 9, Code = 0x01B2 }, new() { NumberOfBits = 9, Code = 0x01B3 },
        new() { NumberOfBits = 9, Code = 0x01B4 }, new() { NumberOfBits = 9, Code = 0x01B5 }, new() { NumberOfBits = 9, Code = 0x01B6 },
        new() { NumberOfBits = 9, Code = 0x01B7 }, new() { NumberOfBits = 9, Code = 0x01B8 }, new() { NumberOfBits = 9, Code = 0x01B9 },
        new() { NumberOfBits = 9, Code = 0x01BA }, new() { NumberOfBits = 9, Code = 0x01BB }, new() { NumberOfBits = 9, Code = 0x01BC },
        new() { NumberOfBits = 9, Code = 0x01BD }, new() { NumberOfBits = 9, Code = 0x01BE }, new() { NumberOfBits = 9, Code = 0x01BF },
        new() { NumberOfBits = 9, Code = 0x01C0 }, new() { NumberOfBits = 9, Code = 0x01C1 }, new() { NumberOfBits = 9, Code = 0x01C2 },
        new() { NumberOfBits = 9, Code = 0x01C3 }, new() { NumberOfBits = 9, Code = 0x01C4 }, new() { NumberOfBits = 9, Code = 0x01C5 },
        new() { NumberOfBits = 9, Code = 0x01C6 }, new() { NumberOfBits = 9, Code = 0x01C7 }, new() { NumberOfBits = 9, Code = 0x01C8 },
        new() { NumberOfBits = 9, Code = 0x01C9 }, new() { NumberOfBits = 9, Code = 0x01CA }, new() { NumberOfBits = 9, Code = 0x01CB },
        new() { NumberOfBits = 9, Code = 0x01CC }, new() { NumberOfBits = 9, Code = 0x01CD }, new() { NumberOfBits = 9, Code = 0x01CE },
        new() { NumberOfBits = 9, Code = 0x01CF }, new() { NumberOfBits = 9, Code = 0x01D0 }, new() { NumberOfBits = 9, Code = 0x01D1 },
        new() { NumberOfBits = 9, Code = 0x01D2 }, new() { NumberOfBits = 9, Code = 0x01D3 }, new() { NumberOfBits = 9, Code = 0x01D4 },
        new() { NumberOfBits = 9, Code = 0x01D5 }, new() { NumberOfBits = 9, Code = 0x01D6 }, new() { NumberOfBits = 9, Code = 0x01D7 },
        new() { NumberOfBits = 9, Code = 0x01D8 }, new() { NumberOfBits = 9, Code = 0x01D9 }, new() { NumberOfBits = 9, Code = 0x01DA },
        new() { NumberOfBits = 9, Code = 0x01DB }, new() { NumberOfBits = 9, Code = 0x01DC }, new() { NumberOfBits = 9, Code = 0x01DD },
        new() { NumberOfBits = 9, Code = 0x01DE }, new() { NumberOfBits = 9, Code = 0x01DF }, new() { NumberOfBits = 9, Code = 0x01E0 },
        new() { NumberOfBits = 9, Code = 0x01E1 }, new() { NumberOfBits = 9, Code = 0x01E2 }, new() { NumberOfBits = 9, Code = 0x01E3 },
        new() { NumberOfBits = 9, Code = 0x01E4 }, new() { NumberOfBits = 9, Code = 0x01E5 }, new() { NumberOfBits = 9, Code = 0x01E6 },
        new() { NumberOfBits = 9, Code = 0x01E7 }, new() { NumberOfBits = 9, Code = 0x01E8 }, new() { NumberOfBits = 9, Code = 0x01E9 },
        new() { NumberOfBits = 9, Code = 0x01EA }, new() { NumberOfBits = 9, Code = 0x01EB }, new() { NumberOfBits = 9, Code = 0x01EC },
        new() { NumberOfBits = 9, Code = 0x01ED }, new() { NumberOfBits = 9, Code = 0x01EE }, new() { NumberOfBits = 9, Code = 0x01EF },
        new() { NumberOfBits = 9, Code = 0x01F0 }, new() { NumberOfBits = 9, Code = 0x01F1 }, new() { NumberOfBits = 9, Code = 0x01F2 },
        new() { NumberOfBits = 9, Code = 0x01F3 }, new() { NumberOfBits = 9, Code = 0x01F4 }, new() { NumberOfBits = 9, Code = 0x01F5 },
        new() { NumberOfBits = 9, Code = 0x01F6 }, new() { NumberOfBits = 9, Code = 0x01F7 }, new() { NumberOfBits = 9, Code = 0x01F8 },
        new() { NumberOfBits = 9, Code = 0x01F9 }, new() { NumberOfBits = 9, Code = 0x01FA }, new() { NumberOfBits = 9, Code = 0x01FB },
        new() { NumberOfBits = 7, Code = 0x0017 }, new() { NumberOfBits = 6, Code = 0x0000 }, new() { NumberOfBits = 6, Code = 0x0001 },
        new() { NumberOfBits = 6, Code = 0x0002 }, new() { NumberOfBits = 6, Code = 0x0003 }, new() { NumberOfBits = 7, Code = 0x0008 },
        new() { NumberOfBits = 7, Code = 0x0009 }, new() { NumberOfBits = 7, Code = 0x000A }, new() { NumberOfBits = 7, Code = 0x000B },
        new() { NumberOfBits = 7, Code = 0x000C }, new() { NumberOfBits = 7, Code = 0x000D }, new() { NumberOfBits = 7, Code = 0x000E },
        new() { NumberOfBits = 7, Code = 0x000F }, new() { NumberOfBits = 7, Code = 0x0010 }, new() { NumberOfBits = 7, Code = 0x0011 },
        new() { NumberOfBits = 7, Code = 0x0012 }, new() { NumberOfBits = 7, Code = 0x0013 }, new() { NumberOfBits = 9, Code = 0x01FC },
        new() { NumberOfBits = 9, Code = 0x01FD },
    ];
}
