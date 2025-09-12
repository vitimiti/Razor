// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using JetBrains.Annotations;
using Razor.FileSystem.Io;

namespace Razor.FileSystem.Chunks;

[PublicAPI]
public sealed class ChunkWriter(Stream stream, bool leaveOpen = false) : IDisposable
{
    private readonly Stack<(long Position, ChunkHeader Header)> _chunkStack = new();

    private MicroChunkInfo? _currentMicroChunk;
    private bool _disposed;

    public Stream BaseStream => stream;

    public int CurrentChunkDepth => _chunkStack.Count;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_currentMicroChunk.HasValue)
        {
            // Force close micro chunk
            EndMicroChunk();
        }

        if (_chunkStack.Count > 0)
        {
            // Force close chunk
            EndChunk();
        }

        if (!leaveOpen)
        {
            stream.Dispose();
        }

        _disposed = true;
    }

    public void WriteChunk(uint chunkId, Action<ChunkWriter> writeAction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        BeginChunk(chunkId);
        try
        {
            writeAction(this);
        }
        finally
        {
            EndChunk();
        }
    }

    public void WriteMicroChunk(byte chunkId, Action<ChunkWriter> writeAction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        BeginMicroChunk(chunkId);
        try
        {
            writeAction(this);
        }
        finally
        {
            EndMicroChunk();
        }
    }

    public void Write<T>(T value)
        where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        unsafe
        {
            var span = new ReadOnlySpan<byte>(&value, sizeof(T));
            Write(span);
        }
    }

    public void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_chunkStack.Count == 0)
        {
            throw new InvalidOperationException("No chunk is currently open.");
        }

        stream.Write(buffer);
        UpdateChunkSizes(buffer.Length);
    }

    public void Write(string text, Encoding? encoding = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(text);
        Write(bytes);
        Write(stackalloc byte[1]); // null terminator
    }

    public void Write(byte[] data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write(new Span<byte>(data));
    }

    public void Write(int value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write<int>(value);
    }

    public void Write(uint value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write<uint>(value);
    }

    public void Write(float value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write<float>(value);
    }

    public void Write(byte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write<byte>(value);
    }

    public void Write(IoVector2 value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write<IoVector2>(value);
    }

    public void Write(IoVector3 value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write<IoVector3>(value);
    }

    public void Write(IoVector4 value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write<IoVector4>(value);
    }

    public void Write(IoQuaternion value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write(value.Q[0]);
        Write(value.Q[1]);
        Write(value.Q[2]);
        Write(value.Q[3]);
    }

    private void WriteHeader(ChunkHeader header)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write(header.ChunkTypeFlags);
        Write(header.RawChuckSize);
    }

    private void WriteMicroHeader(MicroChunkHeader header)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Write(header.ChunkType);
        Write(header.ChunkSize);
    }

    private void BeginChunk(uint chunkId)
    {
        if (_currentMicroChunk.HasValue)
        {
            throw new InvalidOperationException("Cannot begin chunk while in micro chunk.");
        }

        if (_chunkStack.Count > 0)
        {
            var (pos, header) = _chunkStack.Pop();
            header.ContainsSubChunks = true;
            _chunkStack.Push((pos, header));
        }

        ChunkHeader chunkHeader = new(chunkId, 0);
        var position = stream.Position;

        WriteHeader(chunkHeader);
        _chunkStack.Push((position, chunkHeader));
    }

    private void EndChunk()
    {
        if (_currentMicroChunk.HasValue)
        {
            throw new InvalidOperationException("Must close micro chunk first.");
        }

        if (_chunkStack.Count == 0)
        {
            throw new InvalidOperationException("No chunk is currently open.");
        }

        var currentPos = stream.Position;
        var (headerPos, header) = _chunkStack.Pop();

        header.ChunkSize = (uint)(currentPos - headerPos - (sizeof(uint) * 2));

        stream.Seek(headerPos, SeekOrigin.Begin);
        WriteHeader(header);
        stream.Seek(currentPos, SeekOrigin.Begin);

        if (_chunkStack.Count <= 0)
        {
            return;
        }

        var (parentPos, parentHeader) = _chunkStack.Pop();
        parentHeader.ChunkSize += header.ChunkSize + (sizeof(uint) * 2);
        _chunkStack.Push((parentPos, parentHeader));
    }

    private void BeginMicroChunk(byte chunkId)
    {
        if (_currentMicroChunk.HasValue)
        {
            throw new InvalidOperationException("Already in micro chunk.");
        }

        var microHeader = new MicroChunkHeader(chunkId, 0);
        var position = stream.Position;

        WriteMicroHeader(microHeader);
        _currentMicroChunk = new MicroChunkInfo { Position = position, Header = microHeader };
    }

    private void EndMicroChunk()
    {
        if (!_currentMicroChunk.HasValue)
        {
            throw new InvalidOperationException("Not in micro chunk.");
        }

        var microChunk = _currentMicroChunk.Value;
        var currentPos = stream.Position;

        // Calculate micro chunk size
        var microChunkHeader = microChunk.Header;
        microChunkHeader.ChunkSize = (byte)(currentPos - microChunk.Position - 2);
        if (microChunk.Header.ChunkSize > 255)
        {
            throw new InvalidOperationException("Micro chunk too large (> 255 bytes).");
        }

        // Write final micro header
        stream.Seek(microChunk.Position, SeekOrigin.Begin);
        WriteMicroHeader(microChunk.Header);
        stream.Seek(currentPos, SeekOrigin.Begin);

        _currentMicroChunk = null;
    }

    private void UpdateChunkSizes(int bytesWritten)
    {
        // Update current chunk size
        if (_chunkStack.Count > 0)
        {
            var (pos, header) = _chunkStack.Pop();
            header.ChunkSize += (uint)bytesWritten;
            _chunkStack.Push((pos, header));
        }

        if (!_currentMicroChunk.HasValue)
        {
            return;
        }

        // Update micro chunk size
        var microChunk = _currentMicroChunk.Value;
        var microChunkHeader = microChunk.Header;
        microChunkHeader.ChunkSize += (byte)bytesWritten;
        _currentMicroChunk = microChunk;
    }

    private struct MicroChunkInfo
    {
        public long Position { get; init; }
        public MicroChunkHeader Header { get; init; }
    }
}
