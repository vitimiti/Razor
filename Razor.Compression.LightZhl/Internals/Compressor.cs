// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression.LightZhl.Internals;

internal sealed class Compressor : LzBuffer
{
    private const int Min = 4;
    private const int HashShift = 5;
    private const int TableBits = 15;
    private const int TableSize = 1 << TableBits;
    private const int Match = 5;
    private const int SkipHash = 0x400;

    private readonly EncodingStat _stat = new();
    private readonly ushort[] _table = new ushort[TableSize];

    public Compressor()
    {
        for (var i = 0; i < TableSize; i++)
        {
            _table[i] = 0xFFFF;
        }
    }

    public int Compress(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        var bitWriter = new BitWriter(destination);
        var encoder2 = new EncodingSystem(_stat, bitWriter);

        var size = source.Length;
        var sourceIndex = 0;

        uint hash = 0;
        InitializeStartHash(size, source, ref hash);

        while (sourceIndex < size)
        {
            var srcLeft = size - sourceIndex;
            if (HandleTailIfShort(srcLeft, encoder2, source, ref sourceIndex))
            {
                break;
            }

            ProcessChunk(encoder2, source, size, srcLeft, ref sourceIndex, ref hash);
        }

        return encoder2.Finish();
    }

    private static uint RotateLeft(uint x, int y)
    {
        return (x << y) | (x >> (32 - y));
    }

    private static void UpdateHash(ref uint hash, byte value)
    {
        hash ^= value;
        hash = RotateLeft(hash, HashShift);
    }

    private static void UpdateHashEx(ref uint hash, ReadOnlySpan<byte> source)
    {
        hash ^= RotateLeft(source[0], HashShift * Match);
        hash ^= source[Match];
        hash = RotateLeft(hash, HashShift);
    }

    private static int HashPosition(uint hash)
    {
        return (int)((hash * 0x343FDU + 0x269EC3U) >> (32 - TableBits));
    }

    private static uint CalculateHash(ReadOnlySpan<byte> source)
    {
        uint hash = 0;
        for (var i = 0; i < Match; ++i)
        {
            UpdateHash(ref hash, source[i]);
        }

        return hash;
    }

    private uint UpdateTable(uint hash, ReadOnlySpan<byte> source, int position, int length)
    {
        switch (length)
        {
            case <= 0:
                return 0;
            case > SkipHash:
            {
                var source1 = source[1..];
                hash = 0;
                foreach (var b in source1.Slice(length, Match))
                {
                    UpdateHash(ref hash, b);
                }

                return hash;
            }
        }

        UpdateHashEx(ref hash, source);
        var source2 = source[1..];

        for (var i = 0; i < length; ++i)
        {
            _table[HashPosition(hash)] = (ushort)EncodingUtilities.Wrap(position + i);
            UpdateHashEx(ref hash, source2[i..]);
        }

        return hash;
    }

    private static void InitializeStartHash(int size, ReadOnlySpan<byte> source, ref uint hash)
    {
        if (size < Match)
        {
            return;
        }

        for (var i = 0; i < Match; ++i)
        {
            UpdateHash(ref hash, source[i]);
        }
    }

    private static int ComputeMatchLimit(int wrapBufPos, int hashPos, int srcLeft, int rawCount)
    {
        return int.Min(
            int.Min(EncodingUtilities.Distance(wrapBufPos - hashPos), srcLeft - rawCount),
            Min + EncodingSystem.MaxMatchOver
        );
    }

    private static int ComputeForwardOverlapExtension(
        ReadOnlySpan<byte> source,
        int sourceIndex,
        int rawCount,
        int matchLen,
        int srcLeft
    )
    {
        var extraMatchLimit = int.Min(
            Min + EncodingSystem.MaxMatchOver - matchLen,
            srcLeft - rawCount - matchLen
        );

        var extraMatch = 0;
        for (; extraMatch < extraMatchLimit; ++extraMatch)
        {
            if (
                source[sourceIndex + rawCount + extraMatch]
                != source[sourceIndex + rawCount + extraMatch + matchLen]
            )
            {
                break;
            }
        }

        return extraMatch;
    }

    private bool TryBackwardExtendMatch(
        ReadOnlySpan<byte> sourceFromIndex,
        ref BackwardExtendState state
    )
    {
        var extraMatchLimit = int.Min(
            Min + EncodingSystem.MaxMatchOver - state.MatchLength,
            state.RawCount
        );

        var distance = EncodingUtilities.Distance(state.BufferPosition - state.PositionOfHash);
        extraMatchLimit = int.Min(
            int.Min(extraMatchLimit, distance - state.MatchLength),
            EncodingGlobals.BufferSize - distance
        );

        var extraMatch = 0;
        for (; extraMatch < extraMatchLimit; ++extraMatch)
        {
            if (
                Buffer[EncodingUtilities.Wrap(state.PositionOfHash - extraMatch - 1)]
                != sourceFromIndex[state.RawCount - extraMatch - 1]
            )
            {
                break;
            }
        }

        if (extraMatch <= 0)
        {
            return false;
        }

        state.RawCount -= extraMatch;
        state.BufferPosition -= extraMatch;
        state.PositionOfHash -= extraMatch;
        state.MatchLength += extraMatch;
        state.WrapBufferPosition = EncodingUtilities.Wrap(state.BufferPosition);
        state.Hash = CalculateHash(sourceFromIndex[(state.RawCount)..]);

        return true;
    }

    private static void EmitRaw(
        EncodingSystem encoder,
        ReadOnlySpan<byte> source,
        ref int sourceIndex,
        int rawCount
    )
    {
        encoder.PutRaw(source.Slice(sourceIndex, rawCount));
        sourceIndex += rawCount;
    }

    private void EmitMatch(
        EncodingSystem encoder,
        ReadOnlySpan<byte> source,
        ref int sourceIndex,
        ref uint hash,
        in MatchEmitArgs args
    )
    {
        encoder.PutMatch(
            source[args.SourceBaseIndex..],
            args.RawCount,
            args.MatchLength - Min,
            args.Distance
        );

        hash = UpdateTable(
            hash,
            source[(args.SourceBaseIndex + args.RawCount + (args.UpdateSliceOffset - 1))..],
            _bufferPosition + args.TablePositionOffset,
            int.Min(
                args.MatchLength - args.UpdateSliceOffset,
                args.Size - (args.SourceBaseIndex + args.RawCount + args.UpdateSliceOffset)
            )
        );

        ToBuffer(source.Slice(args.SourceBaseIndex + args.RawCount, args.MatchLength));
        sourceIndex += args.RawCount + args.MatchLength;
    }

    private bool HandleTailIfShort(
        int srcLeft,
        EncodingSystem encoder,
        ReadOnlySpan<byte> source,
        ref int sourceIndex
    )
    {
        switch (srcLeft)
        {
            case >= Match:
                return false;
            case <= 0:
                return true;
        }

        ToBuffer(source.Slice(sourceIndex, srcLeft));
        encoder.PutRaw(source.Slice(sourceIndex, srcLeft));

        return true;
    }

    private int GetMatchLengthIfAny(
        ReadOnlySpan<byte> source,
        int rawCount,
        ref int hashPos,
        ref int wrapBufPos,
        ref uint hash,
        ref bool lazyForceMatch,
        in MatchLengthArgs args
    )
    {
        if (hashPos == 0xFFFF || hashPos == wrapBufPos)
        {
            return 0;
        }

        var matchLimit = ComputeMatchLimit(wrapBufPos, hashPos, args.SrcLeft, rawCount);
        var matchLen = NumberMatch(hashPos, source[(args.SourceIndex + rawCount)..], matchLimit);
        if (EncodingUtilities.Wrap(hashPos + matchLen) == wrapBufPos)
        {
            matchLen += ComputeForwardOverlapExtension(
                source,
                args.SourceIndex,
                rawCount,
                matchLen,
                args.SrcLeft
            );
        }

        if (matchLen < Min - 1)
        {
            return matchLen;
        }

        var state = new BackwardExtendState
        {
            RawCount = rawCount,
            BufferPosition = _bufferPosition,
            PositionOfHash = hashPos,
            MatchLength = matchLen,
            Hash = hash,
        };

        var extended = TryBackwardExtendMatch(source[(args.SourceIndex)..], ref state);
        if (!extended)
        {
            return matchLen;
        }

        // propagate updated values back to caller
        _bufferPosition = state.BufferPosition;
        hashPos = state.PositionOfHash;
        matchLen = state.MatchLength;
        wrapBufPos = state.WrapBufferPosition;
        hash = state.Hash;

        lazyForceMatch = true;

        return matchLen;
    }

    private void EmitBetterOrLazy(
        EncodingSystem encoder2,
        ReadOnlySpan<byte> source,
        int size,
        ref int sourceIndex,
        ref uint hash,
        in CurrentMatchArgs current,
        in LazyState lazy
    )
    {
        if (current.MatchLen > lazy.LazyMatchLen)
        {
            EmitMatch(
                encoder2,
                source,
                ref sourceIndex,
                ref hash,
                new MatchEmitArgs(
                    sourceIndex,
                    current.RawCount,
                    current.MatchLen,
                    EncodingUtilities.Distance(current.WrapBufPos - current.HashPos),
                    TablePositionOffset: 1,
                    UpdateSliceOffset: 1,
                    Size: size
                )
            );

            return;
        }

        // restore lazy state
        _bufferPosition = lazy.LazyMatchBufPos;

        hash = lazy.LazyMatchHash;
        UpdateHashEx(ref hash, source[(sourceIndex + lazy.LazyMatchNRaw)..]);

        EmitMatch(
            encoder2,
            source,
            ref sourceIndex,
            ref hash,
            new MatchEmitArgs(
                sourceIndex,
                lazy.LazyMatchNRaw,
                lazy.LazyMatchLen,
                EncodingUtilities.Distance(_bufferPosition - lazy.LazyMatchHashPos),
                TablePositionOffset: 2,
                UpdateSliceOffset: 2, // one raw byte already accounted + one step
                Size: size
            )
        );
    }

    private bool HandleImmediateMatchOrDefer(
        EncodingSystem encoder2,
        ReadOnlySpan<byte> source,
        ref int sourceIndex,
        ref uint hash,
        in CurrentMatchArgs current,
        bool lazyForceMatch,
        ref LazyStateMutable lazy
    )
    {
        if (!lazyForceMatch)
        {
            // store lazy candidate
            lazy.Length = current.MatchLen;
            lazy.PositionForHash = current.HashPos;
            lazy.RawCount = current.RawCount;
            lazy.BufferPosition = _bufferPosition;
            lazy.Hash = hash;
            return false;
        }

        EmitMatch(
            encoder2,
            source,
            ref sourceIndex,
            ref hash,
            new MatchEmitArgs(
                sourceIndex,
                current.RawCount,
                current.MatchLen,
                EncodingUtilities.Distance(current.WrapBufPos - current.HashPos),
                TablePositionOffset: 1,
                UpdateSliceOffset: 1,
                Size: source.Length
            )
        );

        return true;
    }

    private bool HandleRawLimitAndMaybeEmit(
        EncodingSystem encoder,
        ReadOnlySpan<byte> source,
        ref int sourceIndex,
        ref uint hash,
        ref int rawCount,
        in RawLimitArgs args
    )
    {
        if (rawCount + 1 <= args.MaxRaw)
        {
            return false;
        }

        if (args.LazyMatchLen >= Min)
        {
            EmitMatch(
                encoder,
                source,
                ref sourceIndex,
                ref hash,
                new MatchEmitArgs(
                    sourceIndex,
                    rawCount,
                    args.LazyMatchLen,
                    EncodingUtilities.Distance(_bufferPosition - args.LazyMatchHashPos),
                    TablePositionOffset: 1,
                    UpdateSliceOffset: 1,
                    Size: args.Size
                )
            );

            return true;
        }

        if (rawCount + Match >= args.SrcLeft && args.SrcLeft <= EncodingSystem.MaxRaw)
        {
            ToBuffer(source.Slice(sourceIndex + rawCount, args.SrcLeft - rawCount));
            rawCount = args.SrcLeft;
        }

        EmitRaw(encoder, source, ref sourceIndex, rawCount);
        return true;
    }

    private void AdvanceOneRawByte(
        ReadOnlySpan<byte> source,
        int sourceIndex,
        ref int rawCount,
        ref uint hash
    )
    {
        UpdateHashEx(ref hash, source[(sourceIndex + rawCount)..]);
        ToBuffer(source[sourceIndex + rawCount]);
        rawCount++;
    }

    private void ProcessChunk(
        EncodingSystem encoder,
        ReadOnlySpan<byte> source,
        int size,
        int srcLeft,
        ref int sourceIndex,
        ref uint hash
    )
    {
        var rawCount = 0;
        var maxRaw = int.Min(srcLeft - Match, EncodingSystem.MaxRaw);

        var lazy = new LazyStateMutable();
        var lazyForceMatch = false;

        while (true)
        {
            var hash2 = (uint)HashPosition(hash);

            int hashPos = _table[hash2];
            var wrapBufPos = EncodingUtilities.Wrap(_bufferPosition);
            _table[hash2] = (ushort)wrapBufPos;

            var matchLen = GetMatchLengthIfAny(
                source,
                rawCount,
                ref hashPos,
                ref wrapBufPos,
                ref hash,
                ref lazyForceMatch,
                new MatchLengthArgs(sourceIndex, srcLeft)
            );

            if (lazy.Length >= Min)
            {
                EmitBetterOrLazy(
                    encoder,
                    source,
                    size,
                    ref sourceIndex,
                    ref hash,
                    new CurrentMatchArgs(rawCount, matchLen, wrapBufPos, hashPos),
                    new LazyState(
                        lazy.Length,
                        lazy.PositionForHash,
                        lazy.RawCount,
                        lazy.BufferPosition,
                        lazy.Hash
                    )
                );

                break;
            }

            if (
                matchLen >= Min
                && HandleImmediateMatchOrDefer(
                    encoder,
                    source,
                    ref sourceIndex,
                    ref hash,
                    new CurrentMatchArgs(rawCount, matchLen, wrapBufPos, hashPos),
                    lazyForceMatch,
                    ref lazy
                )
            )
            {
                break;
            }

            if (
                HandleRawLimitAndMaybeEmit(
                    encoder,
                    source,
                    ref sourceIndex,
                    ref hash,
                    ref rawCount,
                    new RawLimitArgs(size, srcLeft, maxRaw, lazy.Length, lazy.PositionForHash)
                )
            )
            {
                break;
            }

            AdvanceOneRawByte(source, sourceIndex, ref rawCount, ref hash);
        }
    }

    private readonly record struct RawLimitArgs(
        int Size,
        int SrcLeft,
        int MaxRaw,
        int LazyMatchLen,
        int LazyMatchHashPos
    );

    private readonly record struct MatchLengthArgs(int SourceIndex, int SrcLeft);

    private readonly record struct CurrentMatchArgs(
        int RawCount,
        int MatchLen,
        int WrapBufPos,
        int HashPos
    );

    private readonly record struct LazyState(
        int LazyMatchLen,
        int LazyMatchHashPos,
        int LazyMatchNRaw,
        int LazyMatchBufPos,
        uint LazyMatchHash
    );

    private struct LazyStateMutable
    {
        public int Length { get; set; }
        public int PositionForHash { get; set; }
        public int RawCount { get; set; }
        public int BufferPosition { get; set; }
        public uint Hash { get; set; }
    }

    private struct BackwardExtendState
    {
        public int RawCount { get; set; }
        public int BufferPosition { get; set; }
        public int PositionOfHash { get; set; }
        public int MatchLength { get; set; }
        public int WrapBufferPosition { get; set; }
        public uint Hash { get; set; }
    }

    private readonly record struct MatchEmitArgs(
        int SourceBaseIndex,
        int RawCount,
        int MatchLength,
        int Distance,
        int TablePositionOffset,
        int UpdateSliceOffset,
        int Size
    );
}
