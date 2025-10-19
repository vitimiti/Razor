// -----------------------------------------------------------------------
// <copyright file="GenericList.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Vegas.Core.Utilities.ListNodes;

/// <summary>
/// A sentinel-based doubly-linked list container for <see cref="GenericNode"/> instances.
/// The list uses head and tail sentinel nodes; user data nodes are stored between them.
/// </summary>
public class GenericList : IDisposable
{
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericList"/> class and
    /// creates the head and tail sentinels, linking head -> tail.
    /// </summary>
    public GenericList()
    {
        FirstNode = new GenericNode();
        LastNode = new GenericNode();
        FirstNode.Link(LastNode);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericList"/> class.
    /// </summary>
    /// <remarks>
    /// Internal constructor used to create a <see cref="GenericList"/> wrapper
    /// from existing head/tail sentinel nodes discovered by a <see cref="GenericNode"/>.
    /// </remarks>
    /// <param name="firstNode">The head sentinel node.</param>
    /// <param name="lastNode">The tail sentinel node.</param>
    internal GenericList(GenericNode firstNode, GenericNode lastNode) => (FirstNode, LastNode) = (firstNode, lastNode);

    /// <summary>
    /// Gets the first actual node in the list (the node after the head sentinel).
    /// </summary>
    public GenericNode First => FirstNode.Next!;

    /// <summary>
    /// Gets the first valid (non-sentinel) node only if it is a valid list member.
    /// Returns null if there is no valid first element.
    /// </summary>
    public GenericNode? FirstValid
    {
        get
        {
            GenericNode? node = FirstNode.Next;
            return node?.Next is not null ? node : null;
        }
    }

    /// <summary>
    /// Gets the last actual node in the list (the node before the tail sentinel).
    /// </summary>
    public GenericNode Last => LastNode.Previous!;

    /// <summary>
    /// Gets the last valid (non-sentinel) node only if it is a valid list member.
    /// Returns null if there is no valid last element.
    /// </summary>
    public GenericNode? LastValid
    {
        get
        {
            GenericNode? node = LastNode.Previous;
            return node?.Previous is not null ? node : null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the list is empty (no valid nodes between sentinels).
    /// </summary>
    public bool IsEmpty => !First.IsValid;

    /// <summary>
    /// Gets the number of valid nodes in the list.
    /// </summary>
    public int ValidCount
    {
        get
        {
            GenericNode? node = FirstValid;
            var counter = 0;
            while (node is not null)
            {
                counter++;
                node = node.NextValid;
            }

            return counter;
        }
    }

    /// <summary>
    /// Gets or sets the head sentinel node (protected so derived types and nodes in the same assembly
    /// may access linkage state if necessary).
    /// </summary>
    protected GenericNode FirstNode { get; set; }

    /// <summary>
    /// Gets or sets the tail sentinel node (protected so derived types and nodes in the same assembly
    /// may access linkage state if necessary).
    /// </summary>
    protected GenericNode LastNode { get; set; }

    /// <summary>
    /// Insert <paramref name="node"/> immediately after the head sentinel (add to head).
    /// </summary>
    /// <param name="node">The node to insert at the head of the list.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed.</exception>
    public void AddHead([NotNull] GenericNode node)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        FirstNode.Link(node);
    }

    /// <summary>
    /// Insert <paramref name="node"/> immediately before the tail sentinel (add to tail).
    /// </summary>
    /// <param name="node">The node to append to the list.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed.</exception>
    public void AddTail([NotNull] GenericNode node)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        LastNode.Previous!.Link(node);
    }

    /// <summary>
    /// Dispose pattern: unlinks and removes all valid nodes when disposing the list.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose implementation. When <paramref name="disposing"/>
    /// is true this will unlink all valid nodes between the sentinels.
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
            while (First.IsValid)
            {
                First.Unlink();
            }
        }

        _disposedValue = true;
    }
}
