// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Razor.Extensions;

[PublicAPI]
public static class UInt32Extensions
{
    public static uint FindPowerOfTwoLog2(this uint value)
    {
        var recordedPosition = 0;
        var recordedCount = 0;

        // Walk through the value shifting off bits and record the
        // position of the highest bit, and whether we have found
        // more than one bit.
        var val = value;
        var lp = 0;
        for (; val != 0; val >>= 1)
        {
            if ((val & 1) != 0)
            {
                recordedPosition = lp;
                recordedCount++;
            }

            lp++;
        }

        // If we have not found more than one bit then the number
        // was the power of two so return it.
        if (recordedCount < 2)
        {
            return (uint)recordedPosition;
        }

        // If we found more than one bit, then the number needs to
        // be rounded up to the next highest power of 2.
        return (uint)(recordedPosition + 1);
    }
}
