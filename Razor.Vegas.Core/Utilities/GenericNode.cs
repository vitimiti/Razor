// -----------------------------------------------------------------------
// <copyright file="GenericNode.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Vegas.Core.Utilities;

/// <summary>
/// Represents a node in a doubly-linked list. Intended to be derived from by
/// objects that will be linked into a <see cref="GenericList"/>.
/// </summary>
public class GenericNode : IDisposable
{
    private bool _disposedValue;

    /// <summary>
    /// Gets the <see cref="GenericList"/> that contains this node. The
    /// property walks from the current node to the head and tail sentinels and
    /// returns a list accessor built from those sentinels.
    /// </summary>
    public GenericList MainList
    {
        get
        {
            // Walk to the head sentinel (the node whose PreviousNode is null)
            GenericNode node = this;
            while (node.PreviousNode is not null)
            {
                node = node.PreviousNode;
            }

            // Now walk to the tail sentinel from the head sentinel
            GenericNode tail = node;
            while (tail.NextNode is not null)
            {
                tail = tail.NextNode;
            }

            return new GenericList(node, tail);
        }
    }

    /// <summary>
    /// Gets the next node reference (may be null for sentinels/isolated nodes).
    /// </summary>
    public GenericNode? Next => NextNode;

    /// <summary>
    /// Gets the next node only if that next node itself has a next node (a
    /// "valid" next element according to the original semantics).
    /// </summary>
    public GenericNode? NextValid => NextNode?.NextNode is not null ? NextNode : null;

    /// <summary>
    /// Gets the previous node reference (may be null for sentinels/isolated nodes).
    /// </summary>
    public GenericNode? Previous => PreviousNode;

    /// <summary>
    /// Gets the previous node only if that previous node itself has a previous
    /// node (a "valid" previous element according to the original semantics).
    /// </summary>
    public GenericNode? PreviousValid => PreviousNode?.PreviousNode is not null ? PreviousNode : null;

    /// <summary>
    /// Gets a value indicating whether this node is a valid member of a list (both neighbours are set).
    /// </summary>
    public bool IsValid => NextNode is not null && PreviousNode is not null;

    /// <summary>
    /// Gets or sets the internal storage for the next pointer. Protected so derived types can
    /// access linkage directly if necessary.
    /// </summary>
    protected GenericNode? NextNode { get; set; }

    /// <summary>
    /// Gets or sets the internal storage for the previous pointer. Protected so derived types can
    /// access linkage directly if necessary.
    /// </summary>
    protected GenericNode? PreviousNode { get; set; }

    /// <summary>
    /// Link <paramref name="node"/> directly after this node. The node will
    /// first be unlinked from any previous list it belonged to.
    /// </summary>
    /// <param name="node">The node to insert after this node.</param>
    public void Link([NotNull] GenericNode node)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        node.Unlink();
        node.NextNode = NextNode;
        node.PreviousNode = this;
        if (NextNode is not null)
        {
            NextNode.PreviousNode = node;
        }

        NextNode = node;
    }

    /// <summary>
    /// Unlink this node from its containing list. If the node is not a valid
    /// list member this method is a no-op.
    /// </summary>
    public void Unlink()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);

        if (!IsValid)
        {
            return;
        }

        PreviousNode!.NextNode = NextNode;
        NextNode!.PreviousNode = PreviousNode;
        PreviousNode = null;
        NextNode = null;
    }

    /// <summary>
    /// Dispose pattern: unlinks the node from any list when disposing.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose implementation. When <paramref name="disposing"/>
    /// is true this will unlink the node.
    /// </summary>
    /// <param name="disposing">True when called from Dispose(), false from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            Unlink();
        }

        _disposedValue = true;
    }
}
