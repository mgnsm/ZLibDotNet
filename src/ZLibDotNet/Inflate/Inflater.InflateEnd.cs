// Original code and comments Copyright (C) 1995-2024 Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static int InflateEnd(ref ZStream strm)
    {
        if (InflateStateCheck(ref strm))
            return Z_STREAM_ERROR;

        s_objectPool.Return(strm.inflateState);
        strm.inflateState = null;
        Trace.Tracev("inflate: end\n");
        return Z_OK;
    }
}