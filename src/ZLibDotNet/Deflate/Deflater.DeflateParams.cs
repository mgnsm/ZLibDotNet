// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

using System;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    internal static int DeflateParams(ref ZStream strm, int level, int strategy)
    {
        if (DeflateStateCheck(ref strm))
            return Z_STREAM_ERROR;
        DeflateState s = strm.deflateState;

        if (level == Z_DEFAULT_COMPRESSION)
            level = 6;
        if (level < 0 || level > 9 || strategy < 0 || strategy > Z_FIXED)
            return Z_STREAM_ERROR;

        Config.DeflateType deflate_type = s_configuration_table[s.level].deflate_type;
        if ((strategy != s.strategy || deflate_type != s_configuration_table[level].deflate_type)
            && s.last_flush != -2)
        {
            // Flush the last buffer:
            int err = Deflate(ref strm, Z_BLOCK);
            if (err == Z_STREAM_ERROR)
                return err;
            if (strm.avail_in != 0 || s.strstart - s.block_start + s.lookahead != 0)
                return Z_BUF_ERROR;
        }
        if (s.level != level)
        {
            if (s.level == 0 && s.matches != 0)
            {
                if (s.matches == 1)
                    SlideHash(s, ref MemoryMarshal.GetReference(s.head.AsSpan()));
                else
                    Array.Clear(s.head, 0, s.head.Length);
                s.matches = 0;
            }
            s.level = level;
            Config config = s_configuration_table[level];
            s.max_lazy_match = config.max_lazy;
            s.good_match = config.good_length;
            s.nice_match = config.nice_length;
            s.max_chain_length = config.max_chain;
        }
        s.strategy = strategy;
        return Z_OK;
    }
}