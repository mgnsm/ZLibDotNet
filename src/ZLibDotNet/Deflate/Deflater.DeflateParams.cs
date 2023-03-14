// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

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

        ref Config configuration_table = ref
#if NET7_0_OR_GREATER
            strm.deflateRefs.configuration_table;
#else
            MemoryMarshal.GetReference<Config>(s_configuration_table);
#endif
        Config.DeflateType deflate_type = Unsafe.Add(ref configuration_table, (uint)s.level).deflate_type;
        ref Config config = ref Unsafe.Add(ref configuration_table, (uint)level);
        if ((strategy != s.strategy || deflate_type != config.deflate_type)
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
                {
#if NET7_0_OR_GREATER
                    ref DeflateRefs refs = ref strm.deflateRefs;
                    if (netUnsafe.IsNullRef(ref refs.prev))
                        refs.prev = ref MemoryMarshal.GetReference<ushort>(s.prev);
#endif
                    ref ushort prev = ref
#if NET7_0_OR_GREATER
                    refs.prev;
#else
                    MemoryMarshal.GetReference<ushort>(s.prev);
#endif

                    SlideHash(s, ref prev, ref
#if NET7_0_OR_GREATER
                    refs.head
#else
                    MemoryMarshal.GetReference<ushort>(s.head)
#endif
                    );
                }
                else
                {
                    ClearHash(ref strm);
                }
                s.matches = 0;
            }
            s.level = level;
            s.max_lazy_match = config.max_lazy;
            s.good_match = config.good_length;
            s.nice_match = config.nice_length;
            s.max_chain_length = config.max_chain;
        }
        s.strategy = strategy;
        return Z_OK;
    }
}