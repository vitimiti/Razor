// -----------------------------------------------------------------------
// <copyright file="Base64Stream.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Razor.Utilities;
using Razor.Vegas.Core.Utilities;

namespace Razor.Vegas.Core.Streams;

/// <summary>
/// A <see cref="Stream"/> wrapper that decodes Base64 text on read and
/// encodes to Base64 on write. The underlying <paramref name="stream"/>
/// is used as the byte transport for encoded data.
/// </summary>
/// <param name="stream">The underlying stream that contains Base64-encoded bytes or bytes to encode to Base64.
/// The stream must support reading and/or writing depending on the intended use.</param>
public class Base64Stream([NotNull] Stream stream) : Stream
{
    /// <summary>
    /// Gets a value indicating whether the underlying stream supports reading.
    /// </summary>
    public override bool CanRead => stream.CanRead;

    /// <summary>
    /// Gets a value indicating whether the underlying stream supports seeking.
    /// </summary>
    public override bool CanSeek => stream.CanSeek;

    /// <summary>
    /// Gets a value indicating whether the underlying stream supports writing.
    /// </summary>
    public override bool CanWrite => stream.CanWrite;

    /// <summary>
    /// Gets the length of the underlying stream.</summary>
    public override long Length => stream.Length;

    /// <summary>
    /// Gets or sets the position within the underlying stream.
    /// </summary>
    public override long Position
    {
        get => stream.Position;
        set => stream.Position = value;
    }

    /// <summary>
    /// Flushes any buffered data in the underlying stream.
    /// </summary>
    public override void Flush() => stream.Flush();

    /// <summary>
    /// Reads Base64-encoded bytes from the underlying stream, decodes them,
    /// and writes the decoded bytes into <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">Destination buffer to receive decoded bytes.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the stream.</param>
    /// <param name="count">The maximum number of decoded bytes to read.</param>
    /// <returns>The number of decoded bytes written into <paramref name="buffer"/>.</returns>
    public override int Read([NotNull] byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        // Decoding mode
        if (buffer.Length == 0 || count == 0)
        {
            return 0;
        }

        // Copy the data to a memory stream
        using BinaryReader reader = new(stream, LegacyEncodings.Ansi, leaveOpen: true);
        using MemoryStream ms = new();
        ms.Write(reader.ReadBytes(count));

        var readBytes = Base64.Decode(ms.ToArray(), buffer[offset..count].AsSpan());
        return readBytes;
    }

    /// <summary>
    /// Sets the position of the underlying stream according to <paramref name="offset"/>
    /// and <paramref name="origin"/>.
    /// </summary>
    /// <param name="offset">A byte offset relative to <paramref name="origin"/>.</param>
    /// <param name="origin">A value of <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the underlying stream.</returns>
    public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);

    /// <summary>
    /// Sets the length of the underlying stream.
    /// </summary>
    /// <param name="value">The desired length in bytes.</param>
    public override void SetLength(long value) => stream.SetLength(value);

    /// <summary>
    /// Encodes the provided bytes to Base64 and writes the encoded bytes to
    /// the underlying stream.
    /// </summary>
    /// <param name="buffer">Source buffer containing raw bytes to encode.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin reading bytes to encode.</param>
    /// <param name="count">The number of bytes to encode from <paramref name="buffer"/>.</param>
    public override void Write([NotNull] byte[] buffer, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, buffer.Length);

        // Encoding mode
        if (buffer.Length == 0 || count == 0)
        {
            return;
        }

        // Copy the stream to a memory stream
        using BinaryReader reader = new(stream, LegacyEncodings.Ansi, leaveOpen: true);
        using MemoryStream ms = new();
        ms.Write(reader.ReadBytes(count));

        // Add 30% padding to the end of the stream as encoding is expected to
        // increase the size by this amount
        ms.Write(new byte[(int)float.Ceiling(ms.Length * .3F)]);

        var destination = ms.ToArray();
        var writtenBytes = Base64.Encode(buffer[offset..count].AsSpan(), destination);
        if (writtenBytes != destination.Length)
        {
            throw new InvalidOperationException("Failed to encode data.");
        }

        stream.Write(destination);
    }
}
