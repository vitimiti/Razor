// -----------------------------------------------------------------------
// <copyright file="NodeList.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.Utilities.ListNodes;

/// <summary>
/// Generic typed wrapper around <see cref="GenericList"/> that exposes
/// strongly-typed accessors for nodes of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The node type stored in the list; must derive from <see cref="GenericNode"/>.</typeparam>
public class NodeList<T> : GenericList
    where T : GenericNode
{
    /// <summary>
    /// Gets the first node in the list (the node after the head sentinel) cast to <typeparamref name="T"/>.
    /// </summary>
    public new T First => (T)base.First;

    /// <summary>
    /// Gets the first valid node in the list cast to <typeparamref name="T"/>, or <c>null</c> if there is no valid first element.
    /// </summary>
    public new T? FirstValid => (T?)base.FirstValid;

    /// <summary>
    /// Gets the last node in the list (the node before the tail sentinel) cast to <typeparamref name="T"/>.
    /// </summary>
    public new T Last => (T)base.Last;

    /// <summary>
    /// Gets the last valid node in the list cast to <typeparamref name="T"/>, or <c>null</c> if there is no valid last element.
    /// </summary>
    public new T? LastValid => (T?)base.LastValid;

    /// <summary>
    /// Delete (dispose) all valid nodes in the list.
    /// </summary>
    public void Delete()
    {
        while (First.IsValid)
        {
            First.Dispose();
        }
    }
}
