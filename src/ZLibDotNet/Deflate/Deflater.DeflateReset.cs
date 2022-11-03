// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    internal static int DeflateReset(Unsafe.ZStream strm)
    {
        int ret = DeflateResetKeep(strm);
        if (ret == Z_OK)
            LongestMatchInit(strm.deflateState);
        return ret;
    }

    private static int DeflateResetKeep(Unsafe.ZStream strm)
    {
        const int Z_UNKNOWN = 2;

        if (DeflateStateCheck(strm))
            return Z_STREAM_ERROR;

        strm.total_in = strm.total_out = 0;
        strm.msg = null;
        strm.data_type = Z_UNKNOWN;

        DeflateState s = strm.deflateState;
        s.pending = 0;
        s.pending_out = s.pending_buf;

        if (s.wrap < 0)
            s.wrap = -s.wrap; // was made negative by deflate(..., Z_FINISH);

        s.status = InitState;

        strm.Adler = Adler32.Update(0, ref netUnsafe.NullRef<byte>(), 0);

        s.last_flush = -2;

        Tree.Init(s);

        return Z_OK;
    }
}