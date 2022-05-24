// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    internal static int DeflateInit(Unsafe.ZStream strm, int level, int windowBits)
    {
        const int ZDeflated = 8; // The deflate compression method (the only one supported)
        const int DefMemLevel = 8;
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

        const int Method = ZDeflated;
        const int MemLevel = DefMemLevel;
        const int Strategy = Z_DEFAULT_STRATEGY;
        if (MemLevel < 1 || MemLevel > MaxMemLevel || windowBits < 8
            || windowBits > 15 || level < 0 || level > 9 || Strategy < 0 || Strategy > Z_FIXED
            || windowBits == 8 && wrap != 1)
            return Z_STREAM_ERROR;

        if (windowBits == 8)
            windowBits = 9;

        DeflateState s = new();
        strm.deflateState = s;
        s.strm = strm;
        s.status = InitState; // to pass state test in DeflateReset()

        s.wrap = wrap;
        s.w_bits = (uint)windowBits;
        s.w_size = (uint)(1 << windowBits);
        s.w_mask = s.w_size - 1;

        int hash_bits = MemLevel + 7;
        s.hash_bits = (uint)hash_bits;
        s.hash_size = (uint)(1 << hash_bits);
        s.hash_mask = s.hash_size - 1;
        s.hash_shift = (s.hash_bits + MinMatch - 1) / MinMatch;

        try
        {
            s.window = new byte[s.w_size * 2];
            s.prev = new ushort[s.w_size];
            s.head = new ushort[s.hash_size];

            s.high_water = 0; // nothing written to s.window yet

            s.lit_bufsize = 1 << (MemLevel + 6); // 16K elements by default

            s.pending_buf_size = s.lit_bufsize * 4;
            s.pendingManagedBuffer = new byte[s.pending_buf_size];
        }
        catch (OutOfMemoryException)
        {
            s.status = FinishState;
            strm.msg = s_z_errmsg[Z_NEED_DICT - Z_MEM_ERROR];
            _ = DeflateEnd(strm);
            return Z_MEM_ERROR;
        }

        s.level = level;
        s.strategy = Strategy;
        s.method = Method;

        return DeflateReset(strm);
    }
}