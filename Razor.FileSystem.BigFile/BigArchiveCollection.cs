// -----------------------------------------------------------------------
// <copyright file="BigArchiveCollection.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Razor.Extensions;

namespace Razor.FileSystem.BigFile;

/// <summary>Represents a collection of big archive files, providing methods to manage and access entries across multiple archives.</summary>
public sealed class BigArchiveCollection
    : IDisposable,
        IReadOnlyDictionary<string, (BigArchiveStream Archive, BigArchiveEntry Entry)>
{
    private readonly List<BigArchiveStream> _archives = [];
    private readonly Dictionary<string, (BigArchiveStream Archive, BigArchiveEntry Entry)> _index = new(
        StringComparer.OrdinalIgnoreCase
    );

    private bool _disposed;

    private BigArchiveCollection() { }

    /// <summary>Gets a read-only dictionary containing entries in the big archive collection, where the keys represent normalized paths to entries and the values are tuples containing the associated archive stream and the specific entry details.</summary>
    /// <value>A dictionary where each key is a case-insensitive string representing the normalized path to an archive entry, and each value is a tuple consisting of a <see cref="BigArchiveStream"/> representing the archive and a <see cref="BigArchiveEntry"/> representing the entry's details (such as size and offset).</value>
    /// <remarks>This property can be utilized to enumerate or query entries available in the collection. The dictionary provides efficient access through case-insensitive keys, ensuring compatibility across archive systems with varied case sensitivity.</remarks>
    public IReadOnlyDictionary<string, (BigArchiveStream Archive, BigArchiveEntry Entry)> Entries => _index;

    /// <summary>Gets the total number of entries currently available in the big archive collection.</summary>
    /// <value>An integer representing the total count of entries indexed within the collection.</value>
    /// <remarks>This property provides a quick way to determine the size of the archive collection. Attempting to access it after the collection has been disposed will throw an <see cref="ObjectDisposedException"/>.</remarks>
    public int Count
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _index.Count;
        }
    }

    /// <summary>Gets an enumerable collection of keys representing the normalized paths to archive entries in the big archive collection.</summary>
    /// <value>A collection of case-insensitive strings where each key represents the normalized path to an entry in the collection.</value>
    /// <remarks>This property provides access to the keys of the underlying dictionary that maps paths to their associated archive entries, ensuring efficient traversal and lookup operations.</remarks>
    public IEnumerable<string> Keys
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _index.Keys;
        }
    }

    /// <summary>Gets an enumerable collection containing the values from the big archive collection, where each value represents a tuple of an archive stream and its associated entry details.</summary>
    /// <value>A collection of tuples, each consisting of a <see cref="BigArchiveStream"/> representing the archive and a <see cref="BigArchiveEntry"/> containing the details of an entry (such as size and offset).</value>
    /// <remarks>This property allows enumeration of all entries available within the archive collection, providing direct access to the stored data without the need for keys.</remarks>
    public IEnumerable<(BigArchiveStream Archive, BigArchiveEntry Entry)> Values
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _index.Values;
        }
    }

    /// <summary>Gets the archive and entry associated with the specified key in the collection.</summary>
    /// <param name="key">The normalized key to locate in the collection.</param>
    /// <returns>A tuple containing the associated <see cref="BigArchiveStream"/> and <see cref="BigArchiveEntry"/> for the specified key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the specified key does not exist in the collection.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public (BigArchiveStream Archive, BigArchiveEntry Entry) this[string key]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _index[key.PathNormalized()];
        }
    }

    /// <summary>Creates a new instance of <see cref="BigArchiveCollection"/> by opening and indexing the specified archive paths in the provided order.</summary>
    /// <param name="archivePathsInLoadOrder">A collection of archive file paths, loaded in the specified order. Paths loaded later overwrite entries with the same name from earlier paths.</param>
    /// <returns>A new <see cref="BigArchiveCollection"/> containing the combined indexed entries of the specified archive files.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="archivePathsInLoadOrder"/> is empty.</exception>
    /// <remarks>This method opens and indexes each specified archive file, and returns a new <see cref="BigArchiveCollection"/> containing the combined entries of all opened archives.</remarks>
    public static BigArchiveCollection Open([NotNull] IReadOnlyCollection<string> archivePathsInLoadOrder)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(archivePathsInLoadOrder.Count, 1);

        var collection = new BigArchiveCollection();
        foreach (var path in archivePathsInLoadOrder)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var archive = BigArchiveStream.Open(path);
            collection._archives.Add(archive);

            // Important: later archives should clobber earlier ones.
            foreach (KeyValuePair<string, BigArchiveEntry> kvp in archive.Entries)
            {
                var normalized = kvp.Key.PathNormalized();
                collection._index[normalized] = (archive, kvp.Value);
            }
        }

        return collection;
    }

    /// <summary>Creates a new instance of <see cref="BigArchiveCollection"/> by opening and indexing the specified archive paths in the provided order.</summary>
    /// <param name="archivePathsInLoadOrder">An ordered collection of archive file paths. Later paths overwrite entries with the same name from earlier paths.</param>
    /// <returns>A new <see cref="BigArchiveCollection"/> instance containing the combined indexed entries from the specified archive files.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="archivePathsInLoadOrder"/> is empty.</exception>
    /// <remarks>This method processes and combines entries from all specified archive files into a single collection, ensuring their uniqueness.</remarks>
    public static BigArchiveCollection Open(params string[] archivePathsInLoadOrder) =>
        Open(archivePathsInLoadOrder.ToImmutableArray());

    /// <summary>Determines whether an entry with the specified path exists in the collection.</summary>
    /// <param name="pathInArchive">The normalized path of the entry to locate within the archives.</param>
    /// <returns>True if the entry exists; otherwise, false.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public bool ContainsEntry(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _index.ContainsKey(pathInArchive.PathNormalized());
    }

    /// <summary>Attempts to retrieve the size of an entry within the collection.</summary>
    /// <param name="pathInArchive">The normalized path of the entry within the archive.</param>
    /// <param name="size">When this method returns, contains the size of the entry if found; otherwise, zero.</param>
    /// <returns>true if the entry size was successfully retrieved; otherwise, false.</returns>
    /// <remarks>This method attempts to retrieve the size of the specified entry within the collection. If the entry is not found, the method returns false and the <paramref name="size"/> parameter is set to zero.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public bool TryGetEntrySize(string pathInArchive, out uint size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (
            _index.TryGetValue(
                pathInArchive.PathNormalized(),
                out (BigArchiveStream Archive, BigArchiveEntry Entry) pair
            )
        )
        {
            size = pair.Entry.Size;
            return true;
        }

        size = 0;
        return false;
    }

    /// <summary>Retrieves the size of an archive entry at the specified path.</summary>
    /// <param name="pathInArchive">The normalized path of the entry whose size is to be retrieved.</param>
    /// <returns>The size of the archive entry in bytes.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the BigArchiveCollection has been disposed.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the specified entry is not found in the archives.</exception>
    public uint GetEntrySize(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        pathInArchive = pathInArchive.PathNormalized();
        return !_index.TryGetValue(pathInArchive, out (BigArchiveStream Archive, BigArchiveEntry Entry) pair)
            ? throw new FileNotFoundException($"Entry not found: {pathInArchive}.", pathInArchive)
            : pair.Entry.Size;
    }

    /// <summary>Opens a stream for reading the contents of a specified entry in the archive.</summary>
    /// <param name="pathInArchive">The normalized path to the entry inside the archive.</param>
    /// <returns>A <see cref="BigFileStream"/> that provides access to the contents of the specified entry.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the entry with the specified path is not found in the layered archives.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the <see cref="BigArchiveCollection"/> has been disposed.</exception>
    public BigFileStream OpenEntry(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        pathInArchive = pathInArchive.PathNormalized();
        return !_index.TryGetValue(pathInArchive, out (BigArchiveStream Archive, BigArchiveEntry Entry) pair)
            ? throw new FileNotFoundException($"The entry '{pathInArchive}' was not found in the layered archives.")
            : pair.Archive.OpenEntry(pathInArchive); // Delegate to the owning archive; it will return a BigFileStream over the correct slice.
    }

    /// <summary>Determines whether the collection contains an entry with the specified key.</summary>
    /// <param name="key">The key to locate in the collection. The key is normalized before the search.</param>
    /// <returns><c>true</c> if the collection contains an entry with the specified key; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _index.ContainsKey(key.PathNormalized());
    }

    /// <summary>Attempts to retrieve the value associated with the specified key in the collection.</summary>
    /// <param name="key">The key whose associated value is to be retrieved.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key if it exists in the collection; otherwise, the default value.</param>
    /// <returns>True if the key exists in the collection; otherwise, false.</returns>
    public bool TryGetValue(string key, out (BigArchiveStream Archive, BigArchiveEntry Entry) value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _index.TryGetValue(key.PathNormalized(), out value);
    }

    /// <summary>Returns an enumerator that iterates through the collection of big archive entries.</summary>
    /// <remarks>Throws ObjectDisposedException if the collection has been disposed.</remarks>
    /// <returns>An enumerator for the collection of key-value pairs representing archive entries.</returns>
    public IEnumerator<KeyValuePair<string, (BigArchiveStream Archive, BigArchiveEntry Entry)>> GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _index.GetEnumerator();
    }

    /// <summary>Releases all resources used by the BigArchiveCollection, including its opened archive streams and internal indexes.</summary>
    /// <remarks>Ensures all opened archives are properly disposed and clears all internal collections. Once disposed, accessing any members of the object may throw an ObjectDisposedException.</remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Dispose all opened archives.
        foreach (BigArchiveStream archive in _archives)
        {
            archive.Dispose();
        }

        _archives.Clear();
        _index.Clear();
        _disposed = true;
    }

    /// <summary>Returns an enumerator that iterates through the collection of big archive entries.</summary>
    /// <remarks>Throws ObjectDisposedException if the collection has been disposed.</remarks>
    /// <returns>An enumerator for the collection of key-value pairs representing archive entries.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
