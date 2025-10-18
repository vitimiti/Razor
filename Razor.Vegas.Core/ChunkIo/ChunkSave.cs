// -----------------------------------------------------------------------
// <copyright file="ChunkSave.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Razor.Vegas.Core.IoStruct;

namespace Razor.Vegas.Core.ChunkIo;

internal sealed class ChunkSave([NotNull] FileStream file)
{
    private const int MaxStackDepth = 256;

    private readonly int[] _positionStack = new int[MaxStackDepth];
    private readonly ChunkHeader[] _headerStack = new ChunkHeader[MaxStackDepth];

    private int _stackIndex;
    private bool _inMicroChunk;
    private int _microChunkPosition;
    private MicroChunkHeader _microChunkHeader;

    public int CurrentChunkDepth => _stackIndex;

    public void BeginChunk(uint id)
    {
        ChunkHeader chunkHeader = new();

        // If we have a parent chunk, set its sub-chunk flag
        if (_stackIndex > 0)
        {
            _headerStack[_stackIndex - 1].SubChunk = true;
        }

        // Save the current file position and chunk header for the call to `EndChunk`
        chunkHeader.ChunkType = id;
        chunkHeader.Size = 0;
        var filePosition = (int)file.Seek(0, SeekOrigin.Current);

        _positionStack[_stackIndex] = filePosition;
        _headerStack[_stackIndex] = chunkHeader;
        _stackIndex++;

        // Write a temporary chunk header (all zeros)
        file.Write(chunkHeader.ToBuffer());
    }

    public void EndChunk()
    {
        if (_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot end a chunk while in a micro-chunk.");
        }

        var currentPosition = (int)file.Seek(0, SeekOrigin.Current);

        // Pop the position and chunk header off the stacks
        _stackIndex--;
        var chunkPosition = _positionStack[_stackIndex];
        ChunkHeader chunkHeader = _headerStack[_stackIndex];

        // Write the completed header
        _ = file.Seek(chunkPosition, SeekOrigin.Begin);
        file.Write(chunkHeader.ToBuffer());

        // Add the total bytes written to any encompassing chunks
        if (_stackIndex != 0)
        {
            _headerStack[_stackIndex - 1].AddSize((uint)(chunkHeader.Size + ChunkHeader.ByteSize));
        }

        // Go back to the original position
        _ = file.Seek(currentPosition, SeekOrigin.Begin);
    }

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

    public void EndMicroChunk()
    {
        if (!_inMicroChunk)
        {
            throw new InvalidOperationException("Cannot end a micro-chunk while not in a micro-chunk.");
        }

        // Save the current position
        var currentPosition = (int)file.Seek(0, SeekOrigin.Current);

        // Seek back and write the micro chunk header
        _ = file.Seek(_microChunkPosition, SeekOrigin.Begin);
        file.Write(_microChunkHeader.ToBuffer());

        // Go back to the end of the file
        _ = file.Seek(currentPosition, SeekOrigin.Begin);
        _inMicroChunk = false;
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        // If this hits, you mixed data and chunks within the same chunk
        if (_headerStack[_stackIndex - 1].SubChunk)
        {
            throw new InvalidOperationException("Cannot write data while in a sub-chunk.");
        }

        // If this hits, you didn't open any chunks yet
        if (_stackIndex <= 0)
        {
            throw new InvalidOperationException("Cannot write data before opening any chunks.");
        }

        // Write the bytes into the file
        file.Write(bytes);

        // Track them in the wrapping chunk
        _headerStack[_stackIndex - 1].AddSize((uint)bytes.Length);

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

    public void Write(IoVector2 vector) => Write(vector.ToBuffer());

    public void Write(IoVector3 vector) => Write(vector.ToBuffer());

    public void Write(IoVector4 vector) => Write(vector.ToBuffer());

    public void Write(IoQuaternion quaternion) => Write(quaternion.ToBuffer());
}
