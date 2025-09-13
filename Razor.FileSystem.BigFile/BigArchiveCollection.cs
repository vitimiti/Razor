// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using JetBrains.Annotations;
using Razor.Extensions;

namespace Razor.FileSystem.BigFile;

[PublicAPI]
public sealed class BigArchiveCollection : IDisposable
{
    private readonly List<BigArchiveStream> _archives = [];
    private readonly Dictionary<string, (BigArchiveStream Archive, BigArchiveEntry Entry)> _index =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _disposed;

    public IReadOnlyDictionary<string, (BigArchiveStream Archive, BigArchiveEntry Entry)> Entries =>
        _index;

    private BigArchiveCollection() { }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Dispose all opened archives.
        foreach (var archive in _archives)
        {
            archive.Dispose();
        }

        _archives.Clear();
        _index.Clear();
        _disposed = true;
    }

    public static BigArchiveCollection Open(IReadOnlyCollection<string> archivePathsInLoadOrder)
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
            foreach (var kvp in archive.Entries)
            {
                var normalized = kvp.Key.PathNormalized();
                collection._index[normalized] = (archive, kvp.Value);
            }
        }

        return collection;
    }

    public static BigArchiveCollection Open(params string[] archivePathsInLoadOrder)
    {
        return Open(archivePathsInLoadOrder.ToImmutableArray());
    }

    public bool ContainsEntry(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _index.ContainsKey(pathInArchive.PathNormalized());
    }

    public bool TryGetEntrySize(string pathInArchive, out uint size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_index.TryGetValue(pathInArchive.PathNormalized(), out var pair))
        {
            size = pair.Entry.Size;
            return true;
        }

        size = 0;
        return false;
    }

    public uint GetEntrySize(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        pathInArchive = pathInArchive.PathNormalized();
        return !_index.TryGetValue(pathInArchive, out var pair)
            ? throw new FileNotFoundException($"Entry not found: {pathInArchive}.", pathInArchive)
            : pair.Entry.Size;
    }

    public BigFileStream OpenEntry(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        pathInArchive = pathInArchive.PathNormalized();
        if (!_index.TryGetValue(pathInArchive, out var pair))
        {
            throw new FileNotFoundException(
                $"The entry '{pathInArchive}' was not found in the layered archives."
            );
        }

        // Delegate to the owning archive; it will return a BigFileStream over the correct slice.
        return pair.Archive.OpenEntry(pathInArchive);
    }
}
