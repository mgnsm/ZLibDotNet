// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Buffers;

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    private const int DefaultMemLevel = 8;
    private static readonly ObjectPool<DeflateState> s_objectPool = new();

    internal static int DeflateInit(ZStream strm, int level) =>
        DeflateInit(strm, level, Z_DEFLATED, MaxWindowBits, DefaultMemLevel, Z_DEFAULT_STRATEGY);

    internal static int DeflateInit(ZStream strm, int level, int method, int windowBits, int memLevel, int strategy)
    {
        const int MaxMemLevel = 9;
        const int MinMatch = 3;

        if (strm == null)
            return Z_STREAM_ERROR;

        strm.msg = null;

        if (level == Z_DEFAULT_COMPRESSION)
            level = 6;

        int wrap = 1;
        if (windowBits < 0) // suppress zlib wrapper
        {
            wrap = 0;
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
            s.strm = strm;
            s.status = InitState; // to pass state test in DeflateReset()

            s.wrap = wrap;
            s.w_bits = windowBits;
            s.w_size = 1 << windowBits;
            s.w_mask = s.w_size - 1;

            int hash_bits = memLevel + 7;
            s.hash_bits = hash_bits;
            s.hash_size = 1 << hash_bits;
            s.hash_mask = s.hash_size - 1;
            s.hash_shift = (s.hash_bits + MinMatch - 1) / MinMatch;

            int w_size = s.w_size;
            s.window = ArrayPool<byte>.Shared.Rent(w_size * 2);
            s.prev = ArrayPool<ushort>.Shared.Rent(w_size);
            s.head = ArrayPool<ushort>.Shared.Rent(s.hash_size);

            s.high_water = 0; // nothing written to s.window yet

            s.lit_bufsize = 1 << (memLevel + 6); // 16K elements by default

            s.pending_buf_size = s.lit_bufsize * 4;
            s.pending_buf = ArrayPool<byte>.Shared.Rent(s.pending_buf_size);
        }
        catch (OutOfMemoryException)
        {
            if (s != default)
                s.status = FinishState;
            strm.msg = s_z_errmsg[Z_NEED_DICT - Z_MEM_ERROR];
            _ = DeflateEnd(strm);
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

        return DeflateReset(strm);
    }
}