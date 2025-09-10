// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Utilities.Randomization;

internal static class Common
{
    public static int PickRandomNumber(IRandomClass generator, int minVal, int maxVal)
    {
        if (minVal == maxVal)
        {
            return minVal;
        }

        if (minVal > maxVal)
        {
            (minVal, maxVal) = (maxVal, minVal);
        }

        var magnitude = maxVal - minVal;
        var highBit = generator.SignificantBits - 1;
        while ((magnitude & (1 << highBit)) == 0 && highBit > 0)
        {
            highBit--;
        }

        var mask = ~((~0L) << (highBit + 1));
        var pick = magnitude + 1;
        while (pick > magnitude)
        {
            pick = (int)(generator.Get() & mask);
        }

        return pick + minVal;
    }
}
