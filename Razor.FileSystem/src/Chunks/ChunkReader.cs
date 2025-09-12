// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using JetBrains.Annotations;

namespace Razor.FileSystem.Chunks;

[PublicAPI]
public sealed class ChunkReader(Stream stream, bool leaveOpen = false) : IDisposable
{
    private readonly Stack<ChunkState> _chunkStack = new();
    private MicroChunkState? _currentMicroChunk;
    private bool _disposed;

    public uint CurrentChunkId => _chunkStack.Count > 0 ? _chunkStack.Peek().Header.ChunkType : 0;

    public uint CurrentChunkLength =>
        _chunkStack.Count > 0 ? _chunkStack.Peek().Header.ChunkSize : 0;

    public byte CurrentMicroChunkId => _currentMicroChunk?.Header.ChunkType ?? 0;
    public byte CurrentMicroChunkLength => (byte?)_currentMicroChunk?.Header.ChunkSize ?? 0;
    public bool CurrentChunkContainsSubChunks =>
        _chunkStack.Count > 0 && _chunkStack.Peek().Header.ContainsSubChunks;

    public int CurrentChunkDepth => _chunkStack.Count;
    public Stream BaseStream => stream;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!leaveOpen)
        {
            stream.Dispose();
        }

        _disposed = true;
    }

    public void ReadChunks(Action<ChunkInfo, ChunkReader> readAction)
    {
        foreach (var chunk in EnumerateChunks())
        {
            readAction(chunk, this);
        }
    }

    public void ReadMicroChunks(Action<MicroChunkInfo, ChunkReader> readAction)
    {
        foreach (var microChunk in EnumerateMicroChunks())
        {
            readAction(microChunk, this);
        }
    }

    public IEnumerable<ChunkInfo> EnumerateChunks()
    {
        while (OpenChunk())
        {
            try
            {
                yield return new ChunkInfo(
                    CurrentChunkId,
                    CurrentChunkLength,
                    CurrentChunkContainsSubChunks
                );
            }
            finally
            {
                CloseChunk();
            }
        }
    }

    public IEnumerable<MicroChunkInfo> EnumerateMicroChunks()
    {
        while (OpenMicroChunk())
        {
            try
            {
                yield return new MicroChunkInfo(CurrentMicroChunkId, CurrentMicroChunkLength);
            }
            finally
            {
                CloseMicroChunk();
            }
        }
    }

    public int Read(Span<byte> buffer)
    {
        if (_chunkStack.Count == 0)
        {
            throw new InvalidOperationException("No chunk opened");
        }

        var maxBytes = GetMaxReadableBytes();
        if (maxBytes == 0)
        {
            return 0;
        }

        var bytesToRead = int.Min(buffer.Length, maxBytes);
        var actualBuffer = buffer[..bytesToRead];
        var bytesRead = stream.Read(actualBuffer);

        UpdateBytesRead(bytesRead);
        return bytesRead;
    }

    public T Read<T>()
        where T : unmanaged
    {
        unsafe
        {
            T value;
            var span = new Span<byte>(&value, sizeof(T));
            var bytesRead = Read(span);

            return bytesRead != sizeof(T)
                ? throw new EndOfStreamException($"Expected {sizeof(T)} bytes, got {bytesRead}")
                : value;
        }
    }

    public string ReadString(Encoding? encoding = null, int maxLength = 1024)
    {
        encoding ??= Encoding.UTF8;
        var buffer = new byte[maxLength];
        var length = 0;

        while (length < maxLength)
        {
            var span = buffer.AsSpan(length, 1);
            if (Read(span) == 0)
            {
                break;
            }

            if (buffer[length] == 0) // null terminator
            {
                break;
            }

            length++;
        }

        return encoding.GetString(buffer, 0, length);
    }

    public byte[] ReadBytes(int count)
    {
        var buffer = new byte[count];
        var bytesRead = Read(buffer);
        if (bytesRead != count)
        {
            Array.Resize(ref buffer, bytesRead);
        }

        return buffer;
    }

    public int ReadInt32()
    {
        return Read<int>();
    }

    public uint ReadUInt32()
    {
        return Read<uint>();
    }

    public float ReadFloat()
    {
        return Read<float>();
    }

    public byte ReadByte()
    {
        return Read<byte>();
    }

    private uint ReadUInt32Direct()
    {
        Span<byte> buffer = stackalloc byte[4];
        return stream.Read(buffer) != 4
            ? throw new EndOfStreamException()
            : BitConverter.ToUInt32(buffer);
    }

    private byte ReadByteDirect()
    {
        var b = stream.ReadByte();
        return b == -1 ? throw new EndOfStreamException() : (byte)b;
    }

    private int GetMaxReadableBytes()
    {
        if (_chunkStack.Count == 0)
        {
            return 0;
        }

        var chunk = _chunkStack.Peek();
        var chunkRemaining = (int)(chunk.Header.ChunkSize - chunk.BytesRead);

        if (!_currentMicroChunk.HasValue)
        {
            return chunkRemaining;
        }

        var microChunk = _currentMicroChunk.Value;
        var microRemaining = microChunk.Header.ChunkSize - microChunk.BytesRead;
        return int.Min(chunkRemaining, (int)microRemaining);
    }

    private bool CanRead(int bytes)
    {
        return GetMaxReadableBytes() >= bytes;
    }

    private void UpdateBytesRead(int bytes)
    {
        if (_chunkStack.Count > 0)
        {
            var chunk = _chunkStack.Pop();
            chunk.BytesRead += bytes;
            _chunkStack.Push(chunk);
        }

        if (!_currentMicroChunk.HasValue)
        {
            return;
        }

        var microChunk = _currentMicroChunk.Value;
        microChunk.BytesRead += bytes;
        _currentMicroChunk = microChunk;
    }

    private bool OpenChunk()
    {
        if (_currentMicroChunk.HasValue)
        {
            throw new InvalidOperationException("Cannot open chunk while in micro chunk.");
        }

        // Check if parent chunk has more data
        if (_chunkStack.Count > 0)
        {
            var parent = _chunkStack.Peek();
            if (parent.BytesRead >= parent.Header.ChunkSize)
            {
                return false;
            }
        }

        if (stream.Length - stream.Position < sizeof(uint) * 2)
            return false;

        var header = new ChunkHeader
        {
            ChunkType = ReadUInt32Direct(),
            RawChunkSize = ReadUInt32Direct(),
        };

        var chunkState = new ChunkState
        {
            Header = header,
            StartPosition = stream.Position,
            BytesRead = 0,
        };

        _chunkStack.Push(chunkState);
        return true;
    }

    private void CloseChunk()
    {
        if (_currentMicroChunk.HasValue)
        {
            throw new InvalidOperationException("Must close micro chunk first.");
        }

        if (_chunkStack.Count == 0)
        {
            throw new InvalidOperationException("No chunk to close.");
        }

        var chunk = _chunkStack.Pop();

        // Seek to end of chunk if not fully read
        if (chunk.BytesRead < chunk.Header.ChunkSize)
        {
            var remainingBytes = chunk.Header.ChunkSize - chunk.BytesRead;
            stream.Seek(remainingBytes, SeekOrigin.Current);
        }

        if (_chunkStack.Count <= 0)
        {
            return;
        }

        // Update parent chunk's bytes read
        var parent = _chunkStack.Pop();
        parent.BytesRead += chunk.Header.ChunkSize + 8; // +8 for header
        _chunkStack.Push(parent);
    }

    private bool OpenMicroChunk()
    {
        if (_currentMicroChunk.HasValue)
        {
            throw new InvalidOperationException("Already in micro chunk.");
        }

        if (!CanRead(2))
        {
            return false;
        }

        var header = new MicroChunkHeader
        {
            ChunkType = ReadByteDirect(),
            ChunkSize = ReadByteDirect(),
        };

        _currentMicroChunk = new MicroChunkState { Header = header, BytesRead = 0 };

        // Update parent chunk bytes read for the header
        UpdateBytesRead(2);

        return true;
    }

    private void CloseMicroChunk()
    {
        if (!_currentMicroChunk.HasValue)
        {
            throw new InvalidOperationException("Not in micro chunk.");
        }

        var microChunk = _currentMicroChunk.Value;

        // Skip any unread bytes
        if (microChunk.BytesRead < microChunk.Header.ChunkSize)
        {
            var remainingBytes = microChunk.Header.ChunkSize - microChunk.BytesRead;
            stream.Seek(remainingBytes, SeekOrigin.Current);
            UpdateBytesRead((int)remainingBytes);
        }

        _currentMicroChunk = null;
    }

    private struct ChunkState
    {
        public ChunkHeader Header { get; init; }
        public long StartPosition { get; set; }
        public long BytesRead { get; set; }
    }

    private struct MicroChunkState
    {
        public MicroChunkHeader Header { get; init; }
        public int BytesRead { get; set; }
    }
}
