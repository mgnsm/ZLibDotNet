// Original code and comments Copyright (C) 1995-2024 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    internal static int DeflateReset(ref ZStream strm)
    {
        int ret = DeflateResetKeep(ref strm);
        if (ret == Z_OK)
            LongestMatchInit(ref strm);
        return ret;
    }

    private static int DeflateResetKeep(ref ZStream strm)
    {
        const int Z_UNKNOWN = 2;

        if (DeflateStateCheck(ref strm))
            return Z_STREAM_ERROR;

        strm.total_in = strm.total_out = 0;
        strm.msg = null;
        strm.data_type = Z_UNKNOWN;

        DeflateState s = strm.deflateState;
        s.pending = 0;
        s.pending_out = s.pending_buf;
#if NET7_0_OR_GREATER
        ref DeflateRefs refs = ref strm.deflateRefs;
        refs.pending_out = ref refs.pending_buf;
#endif

        if (s.wrap < 0)
            s.wrap = -s.wrap; // was made negative by deflate(..., Z_FINISH);

        s.status = InitState;

        strm.Adler = Adler32.Update(0, ref netUnsafe.NullRef<byte>(), 0);

        s.last_flush = -2;

        Tree.Init(ref strm);

        return Z_OK;
    }
}