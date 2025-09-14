// -----------------------------------------------------------------------
// <copyright file="DecodingStepOutcome.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Compression.LightZhl.Internals;

internal enum DecodingStepOutcome
{
    Continue,
    Finished,
    Failed,
}
