// -----------------------------------------------------------------------
// <copyright file="ChunkLoad.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Razor.Utilities;
using Razor.Vegas.Core.IoStruct;

namespace Razor.Vegas.Core.ChunkIo;

/// <summary>
/// Reads chunked data from a <see cref="FileStream"/> produced by the chunk I/O
/// format. This type supports nested chunks and compact micro-chunks and
/// exposes helpers to read primitive buffers and small I/O value types.
/// </summary>
/// <param name="file">The <see cref="FileStream"/> to read chunked data from; the stream must support seeking and reading.</param>
public class ChunkLoad(FileStream file)
{
    private const int MaxStackDepth = 256;

    private readonly uint[] _positionStack = new uint[MaxStackDepth];
    private readonly ChunkHeader[] _headerStack = new ChunkHeader[MaxStackDepth];
    private readonly MicroChunkHeader _microChunkHeader = new();

    private bool _inMicroChunk;
    private int _microChunkPosition;

    /// <summary>
    /// Gets the identifier of the currently open chunk.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no chunk is currently open.</exception>
    public uint CurrentChunkId =>
        StackIndex < 1
            ? throw new InvalidOperationException("No chunk is currently open.")
            : _headerStack[StackIndex - 1].ChunkType;

    /// <summary>
    /// Gets the length (size) of the currently open chunk, excluding the header.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no chunk is currently open.</exception>
    public uint CurrentChunkLength =>
        StackIndex < 1
            ? throw new InvalidOperationException("No chunk is currently open.")
            : _headerStack[StackIndex - 1].Size;

    /// <summary>
    /// Gets the current nesting depth of open chunks. Zero means no chunk is open.
    /// </summary>
    public int CurrentChunkDepth => StackIndex;

    /// <summary>
    /// Gets a value indicating whether the currently open chunk contains sub-chunks.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no chunk is currently open.</exception>
    public bool ContainsChunks => _headerStack[StackIndex - 1].SubChunk;

    /// <summary>
    /// Gets the identifier of the active micro-chunk.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when not currently in a micro-chunk.</exception>
    public int CurrentMicroChunkId =>
        _inMicroChunk ? _microChunkHeader.ChunkType : throw new InvalidOperationException("Not in a micro-chunk.");

    /// <summary>
    /// Gets the length of the active micro-chunk.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when not currently in a micro-chunk.</exception>
    public uint CurrentMicroChunkLength =>
        _inMicroChunk ? _microChunkHeader.ChunkSize : throw new InvalidOperationException("Not in a micro-chunk.");

    private int StackIndex { get; set; }

    /// <summary>
    /// Opens the next chunk from the underlying stream and positions the reader
    /// at the start of that chunk's payload.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when called while in a micro-chunk, when the chunk stack overflows, or when the parent chunk has been consumed.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream does not contain a full chunk header.</exception>
    public void OpenChunk()
    {
        // If the user didn't close any micro chunks that he opened
        if (_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot open a chunk while in a micro-chunk.");
        }

        // Check for stack overflow
        if (StackIndex >= MaxStackDepth)
        {
            throw new InvalidOperationException("Stack overflow.");
        }

        // If the parent chunk has been completely eaten, error
        if (StackIndex > 0 && _positionStack[StackIndex - 1] == _headerStack[StackIndex - 1].Size)
        {
            throw new InvalidOperationException("Parent chunk has been completely eaten.");
        }

        // Read the chunk header
        var buffer = new byte[ChunkHeader.ByteSize];
        if (file.Read(buffer) != ChunkHeader.ByteSize)
        {
            throw new EndOfStreamException("Unexpected end of file.");
        }

        _positionStack[StackIndex] = 0;
        StackIndex++;
    }

    /// <summary>
    /// Closes the currently open chunk, advancing the stream to the end of the
    /// chunk payload if necessary and updating parent chunk tracking information.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when called while in a micro-chunk or when the chunk stack underflows.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream does not contain a full chunk header.</exception>
    public void CloseChunk()
    {
        // If the user didn't close any micro chunks that he opened
        if (_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot close a chunk while in a micro-chunk.");
        }

        // Check for stack underflow
        if (StackIndex <= 0)
        {
            throw new InvalidOperationException("Stack underflow.");
        }

        var chunkSize = _headerStack[StackIndex - 1].Size;
        var position = _positionStack[StackIndex - 1];

        if (position < chunkSize)
        {
            if (file.Seek(chunkSize - position, SeekOrigin.Current) - position != chunkSize - position)
            {
                throw new EndOfStreamException("Unexpected end of file.");
            }
        }

        StackIndex--;
        if (StackIndex > 0)
        {
            _positionStack[StackIndex - 1] += (uint)(chunkSize + ChunkHeader.ByteSize);
        }
    }

    /// <summary>
    /// Opens a compact micro-chunk inside the current chunk. Micro chunks are
    /// small payloads with a one-byte type and length and are consumed separately
    /// from the enclosing chunk payload.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a micro-chunk is already active.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream does not contain a full micro-chunk header.</exception>
    public void OpenMicroChunk()
    {
        if (_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot open a micro-chunk while in a micro-chunk.");
        }

        // Read the chunk header
        // Calling the `Read` function so that if we exhaust the chunk, the read will fail
        var buffer = new byte[MicroChunkHeader.ByteSize];
        if (Read(buffer) != MicroChunkHeader.ByteSize)
        {
            throw new EndOfStreamException("Unexpected end of file.");
        }

        _inMicroChunk = true;
        _microChunkPosition = 0;
    }

    /// <summary>
    /// Closes the active micro-chunk and advances the stream to the end of the
    /// micro-chunk payload if necessary.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when not currently in a micro-chunk.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream does not contain a full micro-chunk header.</exception>
    public void CloseMicroChunk()
    {
        if (!_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot close a micro-chunk while not in a micro-chunk.");
        }

        _inMicroChunk = false;

        var chunkSize = _microChunkHeader.ChunkSize;
        var position = _microChunkPosition;

        // Seek the file past this micro chunk
        if (position >= chunkSize)
        {
            return;
        }

        if (file.Seek(chunkSize - position, SeekOrigin.Current) - position != chunkSize - position)
        {
            throw new EndOfStreamException("Unexpected end of file.");
        }

        // Update the tracking variables for where we are in the normal chunk
        if (StackIndex > 0)
        {
            _positionStack[StackIndex - 1] += (uint)(chunkSize - position);
        }
    }

    /// <summary>
    /// Advances the stream position by <paramref name="bytesCount"/>, subject
    /// to chunk and micro-chunk bounds. Returns the actual number of bytes
    /// advanced, or zero if the request would exceed the current bounds.
    /// </summary>
    /// <param name="bytesCount">Number of bytes to advance.</param>
    /// <returns>The number of bytes actually advanced (or zero on failure).</returns>
    public uint Seek(uint bytesCount)
    {
        if (StackIndex < 1)
        {
            throw new InvalidOperationException("No chunk is currently open.");
        }

        // Don't seek if we should go past the end of the current chunk
        if (_positionStack[StackIndex - 1] + bytesCount > (int)_headerStack[StackIndex - 1].Size)
        {
            return 0;
        }

        // Don't read if we are in a micro chunk and would go past the end of it
        if (_inMicroChunk && _microChunkPosition + bytesCount > _microChunkHeader.ChunkSize)
        {
            return 0;
        }

        var currentPosition = file.Position;
        if (file.Seek(bytesCount, SeekOrigin.Current) - currentPosition != bytesCount)
        {
            return 0;
        }

        // Update our position in the chunk
        _positionStack[StackIndex - 1] += bytesCount;

        // Update our position in the micro chunk if we are in one
        if (_inMicroChunk)
        {
            _microChunkPosition += (int)bytesCount;
        }

        return bytesCount;
    }

    /// <summary>
    /// Reads up to <paramref name="buffer.Length"/> bytes from the current
    /// chunk into <paramref name="buffer"/>, subject to chunk and micro-chunk bounds.
    /// Returns the number of bytes read, or zero on failure.
    /// </summary>
    /// <param name="buffer">Destination buffer to fill with bytes read.</param>
    /// <returns>Number of bytes read into <paramref name="buffer"/>.</returns>
    public uint Read(Span<byte> buffer)
    {
        if (StackIndex < 1)
        {
            throw new InvalidOperationException("No chunk is currently open.");
        }

        // Don't read if we would go past the end of the current chunk
        if (_positionStack[StackIndex - 1] + buffer.Length > (int)_headerStack[StackIndex - 1].Size)
        {
            return 0;
        }

        // Don't read if we are in a micro chunk and would go past the end of it
        if (_inMicroChunk && _microChunkPosition + buffer.Length > _microChunkHeader.ChunkSize)
        {
            return 0;
        }

        if (file.Read(buffer) != buffer.Length)
        {
            return 0;
        }

        // Update our position in the chunk
        _positionStack[StackIndex - 1] += (uint)buffer.Length;

        // Update our position in the micro chunk if we are in one
        if (_inMicroChunk)
        {
            _microChunkPosition += buffer.Length;
        }

        return (uint)buffer.Length;
    }

    /// <summary>
    /// Reads and deserializes an <see cref="IoVector2"/> from the current chunk.
    /// </summary>
    /// <param name="vector">Outputs the deserialized <see cref="IoVector2"/>.</param>
    /// <returns>The number of bytes read (equals <see cref="IoVector2.ByteSize"/> on success).</returns>
    public uint Read(out IoVector2 vector)
    {
        Span<byte> buffer = stackalloc byte[IoVector2.ByteSize];
        var bytesRead = Read(buffer);
        if (bytesRead != IoVector2.ByteSize)
        {
            throw new EndOfStreamException("Unexpected end of file.");
        }

        vector = IoVector2.FromBuffer(buffer);
        return bytesRead;
    }

    /// <summary>
    /// Reads and deserializes an <see cref="IoVector3"/> from the current chunk.
    /// </summary>
    /// <param name="vector">Outputs the deserialized <see cref="IoVector3"/>.</param>
    /// <returns>The number of bytes read (equals <see cref="IoVector3.ByteSize"/> on success).</returns>
    public uint Read(out IoVector3 vector)
    {
        Span<byte> buffer = stackalloc byte[IoVector3.ByteSize];
        var bytesRead = Read(buffer);
        if (bytesRead != IoVector3.ByteSize)
        {
            throw new EndOfStreamException("Unexpected end of file.");
        }

        vector = IoVector3.FromBuffer(buffer);
        return bytesRead;
    }

    /// <summary>
    /// Reads and deserializes an <see cref="IoVector4"/> from the current chunk.
    /// </summary>
    /// <param name="vector">Outputs the deserialized <see cref="IoVector4"/>.</param>
    /// <returns>The number of bytes read (equals <see cref="IoVector4.ByteSize"/> on success).</returns>
    public uint Read(out IoVector4 vector)
    {
        Span<byte> buffer = stackalloc byte[IoVector4.ByteSize];
        var bytesRead = Read(buffer);
        if (bytesRead != IoVector4.ByteSize)
        {
            throw new EndOfStreamException("Unexpected end of file.");
        }

        vector = IoVector4.FromBuffer(buffer);
        return bytesRead;
    }

    /// <summary>
    /// Reads and deserializes an <see cref="IoQuaternion"/> from the current chunk.
    /// </summary>
    /// <param name="quaternion">Outputs the deserialized <see cref="IoQuaternion"/>.</param>
    /// <returns>The number of bytes read (equals <see cref="IoQuaternion.ByteSize"/> on success).</returns>
    public uint Read(out IoQuaternion quaternion)
    {
        Span<byte> buffer = stackalloc byte[IoQuaternion.ByteSize];
        var bytesRead = Read(buffer);
        if (bytesRead != IoQuaternion.ByteSize)
        {
            throw new EndOfStreamException("Unexpected end of file.");
        }

        // It is a ref struct, so we need to copy the data to a new buffer
        quaternion = IoQuaternion.FromBuffer(buffer.ToArray());
        return bytesRead;
    }

    /// <summary>
    /// Reads a fixed-size string encoded in the legacy ANSI encoding from the current chunk.
    /// </summary>
    /// <param name="strSize">The number of bytes that constitute the string in the stream.</param>
    /// <param name="str">Outputs the decoded string.</param>
    /// <returns>The number of bytes read (equals <paramref name="strSize"/> on success).</returns>
    public uint Read(int strSize, out string str)
    {
        Span<byte> buffer = stackalloc byte[strSize];
        var bytesRead = Read(buffer);
        if (bytesRead != strSize)
        {
            throw new EndOfStreamException("Unexpected end of file.");
        }

        str = LegacyEncodings.Ansi.GetString(buffer);
        return bytesRead;
    }
}
