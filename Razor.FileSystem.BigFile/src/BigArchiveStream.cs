// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Binary;
using JetBrains.Annotations;
using Razor.Extensions;

namespace Razor.FileSystem.BigFile;

[PublicAPI]
public sealed class BigArchiveStream : Stream
{
    private readonly MemoryStream _archive;
    private readonly Dictionary<string, BigArchiveEntry> _entries;

    private bool _disposed;

    public override bool CanRead => !_disposed && _archive.CanRead;
    public override bool CanSeek => !_disposed && _archive.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _archive.Length;

    public override long Position
    {
        get => _archive.Position;
        set => _archive.Position = value;
    }

    public string ArchivePath { get; }
    public IReadOnlyDictionary<string, BigArchiveEntry> Entries => _entries;

    private BigArchiveStream(
        string archivePath,
        MemoryStream archive,
        Dictionary<string, BigArchiveEntry> entries
    )
    {
        ArchivePath = archivePath;
        _archive = archive;
        _entries = entries;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _archive.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    public static BigArchiveStream Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The path cannot be null or whitespace.", nameof(path));
        }

        var bytes = File.ReadAllBytes(path);
        MemoryStream archive = new(bytes, writable: false);
        var entries = ParseTableOfContents(archive);
        return new BigArchiveStream(path, archive, entries);
    }

    public override void Flush()
    {
        _archive.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _archive.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _archive.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Setting the length is not supported.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Writing is not supported.");
    }

    public BigFileStream OpenEntry(string pathInArchive)
    {
        pathInArchive = Normalize(pathInArchive);
        if (!_entries.TryGetValue(pathInArchive, out var entry))
        {
            throw new FileNotFoundException(
                $"The entry '{pathInArchive}' was not found in the archive."
            );
        }

        if (!_archive.TryGetBuffer(out var buffer))
        {
            var temp = new byte[entry.Size];
            var saved = _archive.Position;
            try
            {
                _archive.Position = entry.Offset;
                var remaining = (int)entry.Size;
                var total = 0;
                while (remaining > 0)
                {
                    var read = _archive.Read(temp, total, remaining);
                    if (read <= 0)
                    {
                        throw new EndOfStreamException(
                            "Unexpected end of stream while reading archive."
                        );
                    }

                    total += read;
                    remaining -= read;
                }

                return BigFileStream.FromOwnedBuffer(temp);
            }
            finally
            {
                _archive.Position = saved;
            }
        }

        var arr = buffer.Array;
        ArgumentNullException.ThrowIfNull(arr);

        var start = buffer.Offset + checked((int)entry.Offset);
        var length = checked((int)entry.Size);
        return BigFileStream.FromSharedBuffer(arr, start, length);
    }

    public bool TryGetEntry(string pathInArchive, out BigArchiveEntry entry)
    {
        return _entries.TryGetValue(Normalize(pathInArchive), out entry);
    }

    public bool ContainsEntry(string pathInArchive)
    {
        return _entries.ContainsKey(Normalize(pathInArchive));
    }

    public uint GetEntrySize(string pathInArchive)
    {
        pathInArchive = Normalize(pathInArchive);
        return !_entries.TryGetValue(pathInArchive, out var entry)
            ? throw new FileNotFoundException($"Entry not found: {pathInArchive}", pathInArchive)
            : entry.Size;
    }

    public bool TryGetEntrySize(string pathInArchive, out uint size)
    {
        pathInArchive = Normalize(pathInArchive);
        if (_entries.TryGetValue(pathInArchive, out var entry))
        {
            size = entry.Size;
            return true;
        }

        size = 0;
        return false;
    }

    private static byte[] ReadString(BinaryReader reader)
    {
        var buffer = new ArrayBufferWriter<byte>(64);
        while (true)
        {
            var b = reader.ReadByte();
            if (b == 0)
            {
                break;
            }

            buffer.Write(new ReadOnlySpan<byte>(in b));
        }

        return buffer.WrittenMemory.ToArray();
    }

    private static void ReadExactly(BinaryReader reader, Span<byte> buffer)
    {
        var n = reader.Read(buffer);
        if (n != buffer.Length)
        {
            throw new EndOfStreamException("Unexpected end of stream while reading archive.");
        }
    }

    private static string Normalize(string path) => path.Replace('\\', '/').ToLowerInvariant();

    private static Dictionary<string, BigArchiveEntry> ParseTableOfContents(MemoryStream archive)
    {
        var entries = new Dictionary<string, BigArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        using BinaryReader reader = new(archive, EncodingExtensions.Ansi, leaveOpen: true);

        Span<byte> magic = stackalloc byte[4];
        ReadExactly(reader, magic);
        if (!magic.SequenceEqual("BIGF"u8))
        {
            throw new InvalidDataException("The archive is not a BigFile archive.");
        }

        // Archive size (unused by parser except for compatibility)
        _ = reader.ReadUInt32();

        // File count (big-endian)
        Span<byte> fileCount = stackalloc byte[4];
        ReadExactly(reader, fileCount);
        var count = BinaryPrimitives.ReadUInt32BigEndian(fileCount);

        // TOC starts at 0x10
        archive.Seek(0x10, SeekOrigin.Begin);

        // Read TOC entries
        for (var i = 0; i < count; i++)
        {
            ReadExactly(reader, fileCount);
            var offset = BinaryPrimitives.ReadUInt32BigEndian(fileCount);

            ReadExactly(reader, fileCount);
            var size = BinaryPrimitives.ReadUInt32BigEndian(fileCount);

            // Zero-terminated ANSI path
            var nameBytes = ReadString(reader);
            var normalized = Normalize(EncodingExtensions.Ansi.GetString(nameBytes));

            entries[normalized] = new BigArchiveEntry(offset, size);
        }

        // Reset position for archive-level stream semantics
        archive.Position = 0;
        return entries;
    }
}
