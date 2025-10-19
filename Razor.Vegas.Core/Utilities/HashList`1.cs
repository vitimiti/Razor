// -----------------------------------------------------------------------
// <copyright file="HashList`1.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Razor.Vegas.Core.ListNodes;

namespace Razor.Vegas.Core.Utilities;

/// <summary>
/// A combined hash-table and doubly-linked list container that stores nodes
/// in insertion order while providing fast lookup by a string key.
/// </summary>
/// <typeparam name="TNode">The node type stored in the collection. Must derive from
/// <see cref="GenericNode"/> and implement <see cref="IHashListEntry"/> which
/// exposes the <c>Key</c> used for hashing.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item>Nodes are kept in a doubly-linked list to allow iteration in insertion order.</item>
/// <item>Nodes are also linked into per-bucket chains for O(1) average lookup by key.</item>
/// <item>Keys are compared case-insensitively (the implementation uses a CRC-based hash).</item>
/// <item>The class does not take ownership of node memory; nodes are expected to be managed by the caller.</item>
/// </list>
/// </remarks>
public sealed class HashList<TNode> : IEnumerable<TNode>, IDisposable
    where TNode : GenericNode, IHashListEntry
{
    private readonly TNode?[] _buckets;
    private readonly int _mask;

    // Maintains insertion order via sentinels in GenericList
    private readonly NodeList<TNode> _list = new();

    // Per-node next pointer for bucket chains (separate from linked-list pointers)
    private readonly Dictionary<TNode, TNode?> _nextMap = [];

    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashList{TNode}"/> class.
    /// </summary>
    /// <param name="size">
    /// The number of hash buckets to allocate. Larger values reduce collisions.
    /// Typical default value is 257.
    /// </param>
    public HashList(int size = 257)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        if ((size & (size - 1)) != 0)
        {
            throw new ArgumentException("size must be a power of two", nameof(size));
        }

        _buckets = new TNode?[size];
        _mask = size - 1;
    }

    /// <summary>
    /// Gets the number of nodes currently stored in the collection.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Removes all nodes from the collection. This clears the hash buckets and
    /// unlinks every node from the internal linked list. Nodes themselves are
    /// not disposed or deleted by this method; the caller is responsible for
    /// node lifetime.
    /// </summary>
    public void Clear()
    {
        for (var i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = null;
        }

        while (_list.First.IsValid)
        {
            _list.First.Dispose();
        }

        Count = 0;
        _version++;
    }

    /// <summary>
    /// Adds the specified <paramref name="node"/> to the hash list.
    /// </summary>
    /// <param name="node">A node that is not currently in any list. The node's
    /// <see cref="IHashListEntry.Key"/> is used for hashing and lookup.</param>
    /// <remarks>
    /// The node is inserted into the appropriate bucket chain and appended to
    /// the end of the insertion-order linked list. The same node reference must
    /// not be added more than once; doing so results in an exception.
    /// </remarks>
    public void Add([NotNull] TNode node)
    {
        var idx = BucketIndex(node.Key);

        // Ensure node isn't already present somewhere by checking node equality in bucket chain
        TNode? head = _buckets[idx];
        for (TNode? cur = head; cur is not null; cur = _nextMap.GetValueOrDefault(cur))
        {
            if (ReferenceEquals(cur, node))
            {
                throw new InvalidOperationException("Node is already present in the hash-list");
            }
        }

        // insert at head of bucket chain (maintain separate chain map)
        _nextMap[node] = head;
        _buckets[idx] = node;

        // link into the doubly-linked list at tail
        _list.AddTail(node);

        Count++;
        _version++;
    }

    /// <summary>
    /// Attempts to remove the specified <paramref name="node"/> from the collection.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    /// <returns><c>true</c> if the node was found and removed; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Removal unlinks the node from its bucket chain and from the insertion-order
    /// list. The node object is not deleted by this method; ownership remains with
    /// the caller.
    /// </remarks>
    public bool TryRemove([NotNull] TNode node)
    {
        var idx = BucketIndex(node.Key);
        TNode? cur = _buckets[idx];
        if (cur is null)
        {
            return false;
        }

        if (ReferenceEquals(cur, node))
        {
            // head of chain -> replace with next
            _buckets[idx] = _nextMap.GetValueOrDefault(cur);
        }
        else
        {
            // find previous node in chain
            TNode prev = cur;
            var found = false;
            cur = _nextMap.TryGetValue(prev, out TNode? nxt) ? nxt : null;
            while (cur is not null)
            {
                if (ReferenceEquals(cur, node))
                {
                    // link prev -> cur.Next
                    TNode? after = _nextMap.GetValueOrDefault(cur);
                    _nextMap[prev] = after;
                    found = true;
                    break;
                }

                prev = cur;
                cur = _nextMap.TryGetValue(cur, out nxt) ? nxt : null;
            }

            if (!found)
            {
                return false;
            }
        }

        // unlink from list
        node.Unlink();

        // clear chain map
        _ = _nextMap.Remove(node);

        Count--;
        _version++;
        return true;
    }

    /// <summary>
    /// Finds the first node with the specified case-insensitive <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to search for (case-insensitive).</param>
    /// <returns>The matching node if found; otherwise <c>null</c>.</returns>
    public TNode? Find([NotNull] string key)
    {
        var idx = BucketIndex(key);
        for (TNode? cur = _buckets[idx]; cur is not null; cur = _nextMap.GetValueOrDefault(cur))
        {
            if (string.Equals(cur.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return cur;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns an enumerator that iterates the collection in insertion order.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{TNode}"/> for the collection.</returns>
    public IEnumerator<TNode> GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Returns an enumerator that iterates the collection in insertion order.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> for the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Disposes the internal list resources. Does not dispose individual nodes.
    /// </summary>
    public void Dispose() => _list.Dispose();

    private static uint ComputeCrc(string s) => CyclicRedundancyCheck.CaseInsensitiveString(s);

    private int BucketIndex(string key) => (int)(ComputeCrc(key) & (uint)_mask);

    private class Enumerator([NotNull] HashList<TNode> owner) : IEnumerator<TNode>
    {
        private readonly int _startVersion = owner._version;
        private GenericNode? _current;
        private bool _started;

        public TNode Current => (TNode)_current!;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            if (_startVersion != owner._version)
            {
                throw new InvalidOperationException("Collection was modified");
            }

            if (!_started)
            {
                _current = owner._list.First;
                _started = true;
            }
            else
            {
                _current = _current?.Next;
            }

            return _current?.IsValid == true;
        }

        public void Reset()
        {
            if (_startVersion != owner._version)
            {
                throw new InvalidOperationException("Collection was modified");
            }

            _current = null;
            _started = false;
        }
    }
}
