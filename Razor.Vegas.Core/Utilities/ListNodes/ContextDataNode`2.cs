// -----------------------------------------------------------------------
// <copyright file="ContextDataNode`2.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Vegas.Core.Utilities.ListNodes;

/// <summary>
/// A data node that carries a payload of type <typeparamref name="TData"/> and an
/// associated context value of type <typeparamref name="TContext"/>. The node is
/// intended to be used within the sentinel-based linked list types
/// (<see cref="GenericList"/> / <see cref="GenericNode"/>).
/// </summary>
/// <typeparam name="TContext">The context type carried by the node.</typeparam>
/// <typeparam name="TData">The payload/data type carried by the node.</typeparam>
public class ContextDataNode<TContext, TData> : DataNode<TData>
{
    /// <summary>
    /// Gets or sets the optional context associated with this node.
    /// </summary>
    public TContext? Context { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextDataNode{T,U}"/> class.
    /// </summary>
    public ContextDataNode() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextDataNode{T,U}"/> class
    /// with the provided context and data value.
    /// </summary>
    /// <param name="context">The context value to associate with the node.</param>
    /// <param name="data">The payload value to store in the node.</param>
    public ContextDataNode(TContext context, TData data)
    {
        Context = context;
        Value = data;
    }
}
