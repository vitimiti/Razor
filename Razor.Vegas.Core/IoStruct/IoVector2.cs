// -----------------------------------------------------------------------
// <copyright file="IoVector2.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.IoStruct;

/// <summary>
/// Represents a two-dimensional vector used by the I/O layer of <c>Razor.Vegas.Core</c>.
/// This is a lightweight value type (a record struct) that provides
/// value-based equality, deconstruction support, and simple storage of
/// X/Y components for serialization or interop scenarios.
/// </summary>
/// <param name="X">The X component (horizontal coordinate).</param>
/// <param name="Y">The Y component (vertical coordinate).</param>
/// <remarks>
/// Instances are intended to be immutable data carriers for passing
/// coordinate pairs between systems; the generated record semantics
/// provide sensible equality and printing behavior.
/// </remarks>
public record struct IoVector2(float X, float Y);
