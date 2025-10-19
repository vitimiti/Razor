// -----------------------------------------------------------------------
// <copyright file="Node`1.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Utilities.ListNodes;

/// <summary>
/// A typed <see cref="GenericNode"/> which exposes strongly-typed navigation
/// properties for nodes of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The node type; must derive from <see cref="GenericNode"/>.</typeparam>
public class Node<T> : GenericNode
    where T : GenericNode
{
    /// <summary>
    /// Gets the list that contains this node as a <see cref="NodeList{T}"/>.
    /// </summary>
    public new NodeList<T> MainList => (NodeList<T>)base.MainList;

    /// <summary>
    /// Gets the next node in the list cast to <typeparamref name="T"/>, or <c>null</c>.
    /// </summary>
    public new T? Next => (T?)base.Next;

    /// <summary>
    /// Gets the next valid node in the list cast to <typeparamref name="T"/>, or <c>null</c>.
    /// </summary>
    public new T? NextValid => (T?)base.NextValid;

    /// <summary>
    /// Gets the previous node in the list cast to <typeparamref name="T"/>, or <c>null</c>.
    /// </summary>
    public new T? Previous => (T?)base.Previous;

    /// <summary>
    /// Gets the previous valid node in the list cast to <typeparamref name="T"/>, or <c>null</c>.
    /// </summary>
    public new T? PreviousValid => (T?)base.PreviousValid;

    /// <summary>
    /// Gets a value indicating whether this node is a valid member of a list (both neighbours are set).
    /// </summary>
    public new bool IsValid => base.IsValid;
}
