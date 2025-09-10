// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Utilities.Randomization;

[PublicAPI]
public class Random3(int seed1, int seed2) : IRandomClass
{
    // csharpier-ignore
    private static readonly int[] s_mix1 =
    [
        unchecked((int)0xBAA96887), 0x1E17D32C, 0x03BCDC3C, 0x0F33D1B2, 0x76A6491D, unchecked((int)0xc570D85D),
        unchecked((int)0xE382B1E3), 0x78DB4362, 0x7439A9D4, unchecked((int)0x9CEA8AC5), unchecked((int)0x89537C5C),
        0x2588F55D, 0x415B5E1D, 0x216E3D95, unchecked((int)0x85C662E7), 0x5E8AB368, 0x3EA5CC8C,
        unchecked((int)0xD26A0F74), unchecked((int)0xF3A9222B), 0x48AAD7E4
    ];

    // csharpier-ignore
    private static readonly int[] s_mix2 =
    [
        0x4B0F3B58, unchecked((int)0xE874F0C3), 0x6955C5A6, 0x55A7CA46, 0x4D9A9D86, unchecked((int)0xFE28A195),
        unchecked((int)0xB1CA7865), 0x6B235751, unchecked((int)0x9A997A61), unchecked((int)0xAA6E95C8),
        unchecked((int)0xAAA98EE1), 0x5AF9154C, unchecked((int)0xFC8E2263), 0x390f5E8C, 0x58FFD802,
        unchecked((int)0xAC0A5EBA), unchecked((int)0xAC4874F6), unchecked((int)0xA9DF0913), unchecked((int)0x86BE4C74),
        unchecked((int)0xED2C123B)
    ];

    private int _index = seed2;

    public int SignificantBits => 32;

    public int Get()
    {
        var loWord = seed1;
        var hiWord = _index++;
        for (var i = 0; i < 4; i++)
        {
            var hiHold = hiWord;
            var temp = hiHold ^ s_mix1[i];
            var itMpl = temp & 0xFFFF;
            var itMph = temp >> 16;
            temp = itMpl * itMpl + ~(itMph * itMph);
            temp = (temp >> 16) | (temp << 16);
            hiWord = loWord ^ ((temp ^ s_mix2[i]) + itMpl * itMph);
            loWord = hiHold;
        }

        return hiWord;
    }

    public int Get(int minVal, int maxVal)
    {
        return Common.PickRandomNumber(this, minVal, maxVal);
    }
}
