// Original code and comments Copyright (C) 1995-2023 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

using System;
using System.Buffers;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    private const int DefaultMemLevel = 8;
    private static readonly ObjectPool<DeflateState> s_objectPool = new();

    internal static int DeflateInit(ref ZStream strm, int level) =>
        DeflateInit(ref strm, level, Z_DEFLATED, MaxWindowBits, DefaultMemLevel, Z_DEFAULT_STRATEGY);

    internal static int DeflateInit(ref ZStream strm, int level, int method, int windowBits, int memLevel, int strategy)
    {
        const int MaxMemLevel = 9;
        const int MinMatch = 3;

        strm.msg = null;

        if (level == Z_DEFAULT_COMPRESSION)
            level = 6;

        int wrap = 1;
        if (windowBits < 0) // suppress zlib wrapper
        {
            wrap = 0;
            if (windowBits < -15)
                return Z_STREAM_ERROR;
            windowBits = -windowBits;
        }

        if (memLevel < 1 || memLevel > MaxMemLevel
            || method != Z_DEFLATED
            || windowBits < 8 || windowBits > 15
            || level < 0 || level > 9
            || strategy < 0 || strategy > Z_FIXED
            || windowBits == 8 && wrap != 1)
            return Z_STREAM_ERROR;

        if (windowBits == 8)
            windowBits = 9;

        DeflateState s = default;
        try
        {
            s = s_objectPool.Get();
            strm.deflateState = s;
#if NET7_0_OR_GREATER
            strm.deflateRefs = new();
#endif
            s.status = InitState; // to pass state test in DeflateReset()

            s.wrap = wrap;
            s.w_bits = (uint)windowBits;
            s.w_size = 1U << windowBits;
            s.w_mask = s.w_size - 1;

            int hash_bits = memLevel + 7;
            s.hash_bits = (uint)hash_bits;
            s.hash_size = 1U << hash_bits;
            s.hash_mask = s.hash_size - 1;
            s.hash_shift = (hash_bits + MinMatch - 1) / MinMatch;

            int w_size = (int)s.w_size;
            s.window = ArrayPool<byte>.Shared.Rent(w_size * 2);
            s.prev = ArrayPool<ushort>.Shared.Rent(w_size);
            s.head = ArrayPool<ushort>.Shared.Rent((int)s.hash_size);

            s.high_water = 0; // nothing written to s.window yet

            s.lit_bufsize = 1U << (memLevel + 6); // 16K elements by default

            s.pending_buf_size = s.lit_bufsize * 4;
            s.pending_buf = ArrayPool<byte>.Shared.Rent((int)s.pending_buf_size);
#if NET7_0_OR_GREATER
            ref DeflateRefs refs = ref strm.deflateRefs;
            refs.head = ref MemoryMarshal.GetReference<ushort>(s.head);
            refs.pending_buf = ref MemoryMarshal.GetReference<byte>(s.pending_buf);
#endif
        }
        catch (OutOfMemoryException)
        {
            if (s != default)
                s.status = FinishState;
            strm.msg = s_z_errmsg[Z_NEED_DICT - Z_MEM_ERROR];
            _ = DeflateEnd(ref strm);
            return Z_MEM_ERROR;
        }
        catch (Exception)
        {
            if (s != default)
            {
                if (s.window != default)
                    ArrayPool<byte>.Shared.Return(s.window);
                if (s.prev != default)
                    ArrayPool<ushort>.Shared.Return(s.prev);
                if (s.head != default)
                    ArrayPool<ushort>.Shared.Return(s.head);
                if (s.pending_buf != default)
                    ArrayPool<byte>.Shared.Return(s.pending_buf);

                s_objectPool.Return(s);
            }

            throw;
        }

        s.level = level;
        s.strategy = strategy;
        s.method = (byte)method;

        return DeflateReset(ref strm);
    }
}