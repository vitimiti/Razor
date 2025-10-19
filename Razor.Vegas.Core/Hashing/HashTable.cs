// -----------------------------------------------------------------------
// <copyright file="HashTable.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Razor.Vegas.Core.Utilities;

namespace Razor.Vegas.Core.Hashing;

/// <summary>
/// Represents a simple hash table that stores <see cref="Hashable"/> entries keyed by a case-insensitive string.
/// </summary>
/// <remarks>
/// The table size must be a power of two. The class provides basic operations to add, remove and find
/// entries. Enumeration traverses buckets in index order and follows the linked list in each bucket.
/// </remarks>
public class HashTable : IEnumerable<Hashable>
{
    private readonly int _hashTableSize;
    private readonly Hashable?[] _hashTable;

    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashTable"/> class with the specified backing size.
    /// </summary>
    /// <param name="size">The size of the underlying table. Must be a positive power of two.</param>
    public HashTable(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        ArgumentOutOfRangeException.ThrowIfEqual(size, 0);

        if ((size & (size - 1)) != 0)
        {
            throw new ArgumentException("Size must be a power of 2.");
        }

        _hashTableSize = size;
        _hashTable = new Hashable[size];
        Reset();
    }

    /// <summary>
    /// Clears the table of all entries.
    /// </summary>
    public void Reset()
    {
        Array.Fill(_hashTable, null);
        _version++;
    }

    /// <summary>
    /// Adds an entry to the table. The entry's <see cref="Hashable.Next"/> must be <c>null</c> before adding.
    /// </summary>
    /// <param name="entry">The entry to add to the table.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="entry"/> already has a non-null <c>Next</c> pointer.</exception>
    public void Add([NotNull] Hashable entry)
    {
        var index = Hash(entry.Key);
        if (entry.Next is not null)
        {
            throw new InvalidOperationException("Cannot add an entry with a next pointer.");
        }

        entry.Next = _hashTable[index];
        _hashTable[index] = entry;
        _version++;
    }

    /// <summary>
    /// Attempts to remove a previously added entry from the table.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    /// <returns><c>true</c> when the entry was found and removed; otherwise <c>false</c>.</returns>
    public bool TryRemove([NotNull] Hashable entry)
    {
        var key = entry.Key;
        var index = Hash(key);

        if (_hashTable[index] is null)
        {
            return false;
        }

        // Special check for first entry
        if (ReferenceEquals(_hashTable[index], entry))
        {
            _hashTable[index] = entry.Next;
            _version++;
            return true;
        }

        // Seach the list for the entry, and remove it
        Hashable? node = _hashTable[index];
        while (node?.Next is not null)
        {
            if (ReferenceEquals(node.Next, entry))
            {
                node.Next = entry.Next;
                _version++;
                return true;
            }

            node = node.Next;
        }

        return false;
    }

    /// <summary>
    /// Finds an entry by its case-insensitive key.
    /// </summary>
    /// <param name="key">The case-insensitive key to search for.</param>
    /// <returns>The matching <see cref="Hashable"/> if found; otherwise <c>null</c>.</returns>
    public Hashable? Find([NotNull] string key)
    {
        var index = Hash(key);
        for (Hashable? node = _hashTable[index]; node is not null; node = node.Next)
        {
            if (string.Equals(node.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a generic enumerator that iterates the entries in the table.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{Hashable}"/> for the table.</returns>
    public IEnumerator<Hashable> GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Returns a non-generic enumerator that iterates the entries in the table.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> that iterates the entries in the table.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int Hash([NotNull] string key) =>
        (int)(CyclicRedundancyCheck.CaseInsensitiveString(key) & (_hashTableSize - 1));

    private sealed class Enumerator : IEnumerator<Hashable>
    {
        private readonly HashTable _table;
        private readonly int _initialVersion;
        private int _bucketIndex;
        private Hashable? _current;
        private Hashable? _next;

        internal Enumerator(HashTable table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _initialVersion = table._version;
            _bucketIndex = 0;
            _current = null;
            _next = table._hashTable.Length > 0 ? table._hashTable[0] : null;

            // Advance to first valid element so MoveNext behaves like .NET enumerators:
            // leave Current undefined until MoveNext is called.
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <exception cref="InvalidOperationException">Enumeration has not started. Call MoveNext.</exception>
        public Hashable Current =>
            _current ?? throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");

        object IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next element;
        /// <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public bool MoveNext()
        {
            if (_initialVersion != _table._version)
            {
                throw new InvalidOperationException("Collection was modified during enumeration.");
            }

            // If next is null, advance to next bucket that has an element
            while (_next is null)
            {
                _bucketIndex++;
                if (_bucketIndex >= _table._hashTable.Length)
                {
                    // end of table
                    _current = null;
                    return false;
                }

                _next = _table._hashTable[_bucketIndex];
            }

            // Accept _next as the current, and advance _next to its .Next
            _current = _next;
            _next = _next.Next;
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public void Reset()
        {
            if (_initialVersion != _table._version)
            {
                throw new InvalidOperationException("Collection was modified during enumeration.");
            }

            _bucketIndex = 0;
            _current = null;
            _next = _table._hashTable.Length > 0 ? _table._hashTable[0] : null;
        }

        /// <summary>
        /// Releases the resources used by the current instance of the enumerator.
        /// </summary>
        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
