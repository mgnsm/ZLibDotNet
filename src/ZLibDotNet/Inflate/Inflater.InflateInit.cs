// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static int InflateInit(Unsafe.ZStream strm, int windowBits)
    {
        if (strm == null)
            return Z_STREAM_ERROR;

        strm.msg = null;
        InflateState state;
        try
        {
            state = new();
        }
        catch (OutOfMemoryException)
        {
            return Z_MEM_ERROR;
        }
        Trace.Tracev("inflate: allocated\n");
        strm.inflateState = state;
        state.strm = strm;
        state.window = null;
        state.mode = InflateMode.Head;

        int ret = InflateReset(strm, windowBits);
        if (ret != Z_OK)
            strm.inflateState = null;
        return ret;
    }
}