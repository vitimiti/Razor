// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Binary;
using Razor.Extensions;

namespace Razor.FileSystem.BigFile;

/// <summary>Represents a stream for accessing entries in large archive files.</summary>
public sealed class BigArchiveStream : Stream
{
    private readonly MemoryStream _archive;
    private readonly Dictionary<string, BigArchiveEntry> _entries;

    private bool _disposed;

    /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
    /// <value><c>true</c> if the stream supports reading and has not been disposed; otherwise, <c>false</c>.</value>
    /// <remarks>This property returns <c>false</c> if the stream is disposed or if the underlying archive stream does not support reading.</remarks>
    public override bool CanRead => !_disposed && _archive.CanRead;

    /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
    /// <value><c>true</c> if the stream supports seeking and has not been disposed; otherwise, <c>false</c>.</value>
    /// <remarks>This property returns <c>false</c> if the stream is disposed or if the underlying archive stream does not support seeking.</remarks>
    public override bool CanSeek => !_disposed && _archive.CanSeek;

    /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
    /// <value>Always returns <c>false</c> as writing is not supported for this stream.</value>
    /// <remarks>This property indicates that the stream is read-only and does not allow writing operations.</remarks>
    public override bool CanWrite => false;

    /// <summary>Gets the length of the underlying archive stream in bytes.</summary>
    /// <value>The total number of bytes in the archive stream.</value>
    /// <remarks>The property retrieves the length of the archive only if the stream has not been disposed. This property is read-only.</remarks>
    public override long Length => _archive.Length;

    /// <summary>Gets or sets the current position within the stream.</summary>
    /// <value>The position within the stream.</value>
    /// <remarks>Setting this property adjusts the position of the underlying archive stream. Throws <see cref="ObjectDisposedException"/> if the stream has been disposed.</remarks>
    public override long Position
    {
        get => _archive.Position;
        set => _archive.Position = value;
    }

    /// <summary>Gets the file path of the archive associated with this stream.</summary>
    /// <value>The full file path of the archive.</value>
    /// <remarks>This property is set when the archive is opened and remains read-only during the lifetime of the stream.</remarks>
    public string ArchivePath { get; }

    /// <summary>Gets a read-only dictionary containing the entries of the archive, where keys are entry paths and values are corresponding archive entries.</summary>
    /// <value>An <see cref="IReadOnlyDictionary{TKey, TValue}" /> with keys as entry paths and values as <see cref="BigArchiveEntry" /> objects, representing the archive contents.</value>
    /// <remarks>The dictionary provides access to all entries within the archive. Operations on this property are designed to ensure thread safety. Accessing this property after the stream is disposed will result in undefined behavior.</remarks>
    public IReadOnlyDictionary<string, BigArchiveEntry> Entries => _entries;

    private BigArchiveStream(string archivePath, MemoryStream archive, Dictionary<string, BigArchiveEntry> entries)
    {
        ArchivePath = archivePath;
        _archive = archive;
        _entries = entries;
    }

    /// <summary>Releases the unmanaged resources used by the instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
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

    /// <summary>Opens a specified archive file and returns a stream for accessing its entries.</summary>
    /// <param name="path">The path to the archive file to be opened.</param>
    /// <returns>A <see cref="BigArchiveStream"/> instance to interact with the archive file.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified path is null, empty, or whitespace.</exception>
    public static BigArchiveStream Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The path cannot be null or whitespace.", nameof(path));
        }

        var bytes = File.ReadAllBytes(path);
        MemoryStream archive = new(bytes, writable: false);
        Dictionary<string, BigArchiveEntry> entries = ParseTableOfContents(archive);
        return new BigArchiveStream(path, archive, entries);
    }

    /// <summary>Clears any internal buffers for this stream and writes any buffered data to the underlying device.</summary>
    /// <remarks>This method propagates the flush operation to the underlying stream if the stream is writable.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the stream is not in a writable state.</exception>
    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _archive.Flush();
    }

    /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
    /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _archive.Read(buffer, offset, count);
    }

    /// <summary>Sets the position within the current stream.</summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
    /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the current stream.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _archive.Seek(offset, origin);
    }

    /// <summary>Throws an exception as setting the length is not supported.</summary>
    /// <param name="value">The desired length of the stream, which is not supported.</param>
    /// <exception cref="NotSupportedException">Always thrown to indicate that setting the length is not supported.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException("Setting the length is not supported.");

    /// <summary>Writes data to the stream. This operation is not supported.</summary>
    /// <param name="buffer">The buffer containing data to be written to the stream.</param>
    /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the stream.</param>
    /// <param name="count">The number of bytes to be written to the stream.</param>
    /// <exception cref="NotSupportedException">Thrown always as writing is not supported.</exception>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Writing is not supported.");

    /// <summary>Opens an entry within the archive and provides a readable stream to its contents.</summary>
    /// <param name="pathInArchive">The path of the entry within the archive to open.</param>
    /// <returns>A <see cref="BigFileStream"/> that allows reading the contents of the entry.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified entry is not found in the archive.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the entry in the archive is incomplete or exceeds the stream's bounds.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
    public BigFileStream OpenEntry(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        pathInArchive = pathInArchive.PathNormalized();
        if (!_entries.TryGetValue(pathInArchive, out BigArchiveEntry entry))
        {
            throw new FileNotFoundException($"The entry '{pathInArchive}' was not found in the archive.");
        }

        if (!_archive.TryGetBuffer(out ArraySegment<byte> buffer))
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
                        throw new EndOfStreamException("Unexpected end of stream while reading archive.");
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

        var arr = buffer.Array ?? throw new EndOfStreamException("Unexpected end of stream while reading archive.");
        var start = buffer.Offset + checked((int)entry.Offset);
        var length = checked((int)entry.Size);
        return BigFileStream.FromSharedBuffer(arr, start, length);
    }

    /// <summary>Tries to retrieve an entry from the archive by its path.</summary>
    /// <param name="pathInArchive">The normalized path of the entry in the archive.</param>
    /// <param name="entry">The resulting entry if found; otherwise, null.</param>
    /// <returns>True if the entry is found; otherwise, false.</returns>
    /// <remarks>This method attempts to retrieve the entry with the specified path from the archive. If the entry is not found, the method returns false and the <paramref name="entry"/> parameter is set to null.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
    public bool TryGetEntry(string pathInArchive, out BigArchiveEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _entries.TryGetValue(pathInArchive.PathNormalized(), out entry);
    }

    /// <summary>Determines whether the archive contains an entry at the specified path.</summary>
    /// <param name="pathInArchive">The path of the entry within the archive to search for.</param>
    /// <returns>True if the entry exists in the archive; otherwise, false.</returns>
    /// <remarks>This method searches the archive for the specified entry. If the entry is not found, the method returns false.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
    public bool ContainsEntry(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _entries.ContainsKey(pathInArchive.PathNormalized());
    }

    /// <summary>Retrieves the size of a specified entry within the archive.</summary>
    /// <param name="pathInArchive">The path of the entry within the archive, normalized for comparison.</param>
    /// <returns>The size of the entry in bytes.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the archive stream has been disposed.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the specified entry is not found in the archive.</exception>
    public uint GetEntrySize(string pathInArchive)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        pathInArchive = pathInArchive.PathNormalized();
        return !_entries.TryGetValue(pathInArchive, out BigArchiveEntry entry)
            ? throw new FileNotFoundException($"Entry not found: {pathInArchive}", pathInArchive)
            : entry.Size;
    }

    /// <summary>Attempts to retrieve the size of an archive entry by its path.</summary>
    /// <param name="pathInArchive">The normalized path of the entry within the archive.</param>
    /// <param name="size">When this method returns, contains the size of the entry if found, or 0 if not.</param>
    /// <returns>True if the entry is found; otherwise, false.</returns>
    /// <remarks>This method attempts to retrieve the size of the specified entry within the archive. If the entry is not found, the method returns false and the <paramref name="size"/> parameter is set to zero.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
    public bool TryGetEntrySize(string pathInArchive, out uint size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        pathInArchive = pathInArchive.PathNormalized();
        if (_entries.TryGetValue(pathInArchive, out BigArchiveEntry entry))
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
        _ = archive.Seek(0x10, SeekOrigin.Begin);

        // Read TOC entries
        for (var i = 0; i < count; i++)
        {
            ReadExactly(reader, fileCount);
            var offset = BinaryPrimitives.ReadUInt32BigEndian(fileCount);

            ReadExactly(reader, fileCount);
            var size = BinaryPrimitives.ReadUInt32BigEndian(fileCount);

            // Zero-terminated ANSI path
            var nameBytes = ReadString(reader);
            var normalized = EncodingExtensions.Ansi.GetString(nameBytes).PathNormalized();

            entries[normalized] = new BigArchiveEntry(offset, size);
        }

        // Reset position for archive-level stream semantics
        archive.Position = 0;
        return entries;
    }
}
