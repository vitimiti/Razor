// -----------------------------------------------------------------------
// <copyright file="DataNode`1.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.ListNodes;

/// <summary>
/// A typed data node that wraps a value of type <typeparamref name="T"/> and
/// participates in the sentinel-based doubly-linked list represented by
/// <see cref="GenericList"/> / <see cref="GenericNode"/>.
/// </summary>
/// <typeparam name="T">The payload type stored in this node.</typeparam>
public class DataNode<T> : GenericNode
{
    /// <summary>
    /// Gets or sets the payload value stored in this node.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets the next node in the list cast to <see cref="DataNode{T}"/>, or <c>null</c>.
    /// </summary>
    public new DataNode<T>? Next => NextNode as DataNode<T>;

    /// <summary>
    /// Gets the next valid node in the list cast to <see cref="DataNode{T}"/>, or <c>null</c>.
    /// </summary>
    public new DataNode<T>? NextValid => base.NextValid as DataNode<T>;

    /// <summary>
    /// Gets the previous node in the list cast to <see cref="DataNode{T}"/>, or <c>null</c>.
    /// </summary>
    public new DataNode<T>? Previous => PreviousNode as DataNode<T>;

    /// <summary>
    /// Gets the previous valid node in the list cast to <see cref="DataNode{T}"/>, or <c>null</c>.
    /// </summary>
    public new DataNode<T>? PreviousValid => base.PreviousValid as DataNode<T>;
}
