// -----------------------------------------------------------------------
// <copyright file="ChunkSave.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Razor.Utilities;
using Razor.Vegas.Core.IoStruct;

namespace Razor.Vegas.Core.ChunkIo;

/// <summary>
/// Provides functionality to write chunked data to a <see cref="FileStream"/>.
/// This type manages nested chunk headers, micro-chunks, and the patching of
/// header sizes when chunks are completed.
/// </summary>
/// <param name="file">The <see cref="FileStream"/> to which chunk data will be written. The stream must support writing and seeking.</param>
public sealed class ChunkSave([NotNull] FileStream file)
{
    private const int MaxStackDepth = 256;

    private readonly int[] _positionStack = new int[MaxStackDepth];
    private readonly ChunkHeader[] _headerStack = new ChunkHeader[MaxStackDepth];
    private readonly MicroChunkHeader _microChunkHeader = new();

    private bool _inMicroChunk;
    private int _microChunkPosition;

    /// <summary>
    /// Gets the current nesting depth of open chunks. A value of zero means no chunks are open.
    /// </summary>
    public int CurrentChunkDepth => StackIndex;

    private int StackIndex { get; set; }

    /// <summary>
    /// Begins a new chunk with the specified identifier and writes a placeholder header.
    /// The header is patched with the final size when <see cref="EndChunk"/> is called.
    /// </summary>
    /// <param name="id">The numeric chunk type identifier to store in the header.</param>
    public void BeginChunk(uint id)
    {
        ChunkHeader chunkHeader = new();

        // If we have a parent chunk, set its sub-chunk flag
        if (StackIndex > 0)
        {
            _headerStack[StackIndex - 1].SubChunk = true;
        }

        // Save the current file position and chunk header for the call to `EndChunk`
        chunkHeader.ChunkType = id;
        chunkHeader.Size = 0;
        var filePosition = (int)file.Seek(0, SeekOrigin.Current);

        _positionStack[StackIndex] = filePosition;
        _headerStack[StackIndex] = chunkHeader;
        StackIndex++;

        // Write a temporary chunk header (all zeros)
        file.Write(chunkHeader.ToBuffer());
    }

    /// <summary>
    /// Completes the most recently opened chunk by writing the finalized header
    /// (with the computed size) back into the file and updating any enclosing chunk's size.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no chunk is open.</exception>
    public void EndChunk()
    {
        if (_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot end a chunk while in a micro-chunk.");
        }

        var currentPosition = (int)file.Seek(0, SeekOrigin.Current);

        // Pop the position and chunk header off the stacks
        StackIndex--;
        var chunkPosition = _positionStack[StackIndex];
        ChunkHeader chunkHeader = _headerStack[StackIndex];

        // Write the completed header
        if (file.Seek(chunkPosition, SeekOrigin.Begin) != chunkPosition)
        {
            throw new InvalidOperationException("Failed to seek to chunk header position.");
        }

        file.Write(chunkHeader.ToBuffer());

        // Add the total bytes written to any encompassing chunks
        if (StackIndex != 0)
        {
            _headerStack[StackIndex - 1].AddSize((uint)(chunkHeader.Size + ChunkHeader.ByteSize));
        }

        // Go back to the original position
        if (file.Seek(currentPosition, SeekOrigin.Begin) != currentPosition)
        {
            throw new InvalidOperationException("Failed to seek to original position.");
        }
    }

    /// <summary>
    /// Begins a compact "micro-chunk" with the specified single-byte identifier.
    /// Micro-chunks are small sub-data blocks stored inside normal chunks.
    /// </summary>
    /// <param name="id">The micro-chunk identifier; must be less than 256.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="id"/> is 256 or greater.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a micro-chunk is already active.</exception>
    public void BeginMicroChunk(uint id)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, 256U);

        if (_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot begin a micro-chunk while in a micro-chunk.");
        }

        // Save the current file position and chunk header for the call to `EndMicroChunk`
        _microChunkHeader.ChunkType = (byte)id;
        _microChunkHeader.ChunkSize = 0;
        _microChunkPosition = (int)file.Seek(0, SeekOrigin.Current);

        // Write a temporary micro-chunk header (all zeros)
        // NOTE: We call the `Write` method so that the bytes for this header
        // are tracked in the wrapping chunk. This is because micro-chunks
        // are simply data inside the normal chunks
        Write(_microChunkHeader.ToBuffer());
    }

    /// <summary>
    /// Completes the active micro-chunk by writing its finalized header back
    /// into the file. After calling this method, micro-chunk mode is exited.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no micro-chunk is active.</exception>
    public void EndMicroChunk()
    {
        if (!_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot end a micro-chunk while not in a micro-chunk.");
        }

        // Save the current position
        var currentPosition = (int)file.Seek(0, SeekOrigin.Current);

        // Seek back and write the micro chunk header
        if (file.Seek(_microChunkPosition, SeekOrigin.Begin) != _microChunkPosition)
        {
            throw new InvalidOperationException("Failed to seek to micro-chunk header position.");
        }

        file.Write(_microChunkHeader.ToBuffer());

        // Go back to the end of the file
        if (file.Seek(currentPosition, SeekOrigin.Begin) != currentPosition)
        {
            throw new InvalidOperationException("Failed to seek to original position.");
        }

        _inMicroChunk = false;
    }

    /// <summary>
    /// Writes the provided bytes into the underlying file and updates the
    /// currently open chunk and micro-chunk sizes as appropriate.
    /// </summary>
    /// <param name="bytes">The bytes to write to the file.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if attempting to write while inside a sub-chunk, or if no chunk is open,
    /// or if the micro-chunk size would be exceeded.
    /// </exception>
    public void Write(ReadOnlySpan<byte> bytes)
    {
        // If this hits, you mixed data and chunks within the same chunk
        if (_headerStack[StackIndex - 1].SubChunk)
        {
            throw new InvalidOperationException("Cannot write data while in a sub-chunk.");
        }

        // If this hits, you didn't open any chunks yet
        if (StackIndex <= 0)
        {
            throw new InvalidOperationException("Cannot write data before opening any chunks.");
        }

        // Write the bytes into the file
        file.Write(bytes);

        // Track them in the wrapping chunk
        _headerStack[StackIndex - 1].AddSize((uint)bytes.Length);

        // Track them if you are using a micro-chunk too
        if (!_inMicroChunk)
        {
            return;
        }

        // Micro chunks can only be 255 bytes or fewer
        if (_microChunkHeader.ChunkSize >= 255 - bytes.Length)
        {
            throw new InvalidOperationException("Micro-chunk size exceeded.");
        }

        _microChunkHeader.AddSize((byte)bytes.Length);
    }

    /// <summary>
    /// Writes a two-dimensional vector by serializing it to its buffer representation.
    /// </summary>
    /// <param name="vector">The vector to write.</param>
    public void Write(IoVector2 vector) => Write(vector.ToBuffer());

    /// <summary>
    /// Writes a three-dimensional vector by serializing it to its buffer representation.
    /// </summary>
    /// <param name="vector">The vector to write.</param>
    public void Write(IoVector3 vector) => Write(vector.ToBuffer());

    /// <summary>
    /// Writes a four-dimensional vector by serializing it to its buffer representation.
    /// </summary>
    /// <param name="vector">The vector to write.</param>
    public void Write(IoVector4 vector) => Write(vector.ToBuffer());

    /// <summary>
    /// Writes a quaternion by serializing it to its buffer representation.
    /// </summary>
    /// <param name="quaternion">The quaternion to write.</param>
    public void Write(IoQuaternion quaternion) => Write(quaternion.ToBuffer());

    /// <summary>
    /// Writes a string chunk with the specified identifier and string value.
    /// </summary>
    /// <param name="id">The chunk identifier.</param>
    /// <param name="str">The string to write.</param>
    /// <param name="isAnsi">True to encode the string with <see cref="LegacyEncodings.Ansi"/>, false to encode with <see cref="Encoding.UTF8"/>.</param>
    /// <remarks>
    /// The string is encoded with <see cref="LegacyEncodings.Ansi"/> by default, unless <paramref name="isAnsi"/> is <see langword="false"/>, in which case it is encoded with <see cref="Encoding.UTF8"/>.
    /// </remarks>
    public void WriteStringChunk(uint id, string str, bool isAnsi = true)
    {
        BeginChunk(id);
        Write(isAnsi ? LegacyEncodings.Ansi.GetBytes(str) : Encoding.UTF8.GetBytes(str));
        EndChunk();
    }

    /// <summary>
    /// Writes a complete micro-chunk with the specified identifier and byte data.
    /// This method handles beginning and ending the micro-chunk automatically.
    /// </summary>
    /// <param name="id">The micro-chunk identifier; must be less than 256.</param>
    /// <param name="bytes">The byte data to write inside the micro-chunk.</param>
    public void WriteMicroChunk(uint id, ReadOnlySpan<byte> bytes)
    {
        BeginMicroChunk(id);
        Write(bytes);
        EndMicroChunk();
    }

    /// <summary>
    /// Writes a complete micro-chunk with the specified identifier and string data.
    /// This method handles beginning and ending the micro-chunk automatically.
    /// </summary>
    /// <param name="id">The micro-chunk identifier; must be less than 256.</param>
    /// <param name="str">The string data to write inside the micro-chunk.</param>
    /// <param name="isAnsi">True to encode the string with <see cref="LegacyEncodings.Ansi"/>, false to encode with <see cref="Encoding.UTF8"/>.</param>
    /// <remarks>
    /// The string is encoded with <see cref="LegacyEncodings.Ansi"/> by default, unless <paramref name="isAnsi"/> is <see langword="false"/>, in which case it is encoded with <see cref="Encoding.UTF8"/>.
    /// </remarks>
    public void WriteMicroChunkString(uint id, string str, bool isAnsi = true)
    {
        BeginMicroChunk(id);
        Write(isAnsi ? LegacyEncodings.Ansi.GetBytes(str) : Encoding.UTF8.GetBytes(str));
        EndMicroChunk();
    }
}
