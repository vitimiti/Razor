// -----------------------------------------------------------------------
// <copyright file="SafeContextDataNode`2.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Vegas.Core.ListNodes;

/// <summary>
/// A lightweight, null-safe convenience subclass of <see cref="ContextDataNode{T,U}"/>
/// that provides a primary-constructor shorthand for initializing the context and
/// data values.
/// </summary>
/// <typeparam name="TContext">Type of the optional context associated with the node.</typeparam>
/// <typeparam name="TData">Type of the payload/data carried by the node.</typeparam>
/// <param name="context">An optional context value to initialize the node with.</param>
/// <param name="data">An optional payload value to initialize the node with.</param>
[SuppressMessage(
    "ReadabilityRules",
    "SA1106:Code should not contain empty statements",
    Justification = "This is a shorthand for a constructor call."
)]
public class SafeContextDataNode<TContext, TData>(TContext context, TData data)
    : ContextDataNode<TContext, TData>(context, data);
