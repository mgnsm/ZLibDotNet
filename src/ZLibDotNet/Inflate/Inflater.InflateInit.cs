// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

using System;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    private static readonly ObjectPool<InflateState> s_objectPool = new();

    internal static int InflateInit(ref ZStream strm, int windowBits)
    {
        strm.msg = null;
        InflateState state;
        try
        {
            state = s_objectPool.Get();
        }
        catch (OutOfMemoryException)
        {
            return Z_MEM_ERROR;
        }
        Trace.Tracev("inflate: allocated\n");
        strm.inflateState = state;
        state.mode = InflateMode.Head;

        int ret = InflateReset(ref strm, windowBits);
        if (ret != Z_OK)
        {
            s_objectPool.Return(state);
            strm.inflateState = null;
        }
        return ret;
    }
}