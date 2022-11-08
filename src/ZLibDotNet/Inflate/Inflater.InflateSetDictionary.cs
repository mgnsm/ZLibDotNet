// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Runtime.CompilerServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static int InflateSetDictionary(ZStream strm, ref byte dictionary, uint dictLength)
    {
        if (InflateStateCheck(strm) || Unsafe.IsNullRef(ref dictionary))
            return Z_STREAM_ERROR;

        InflateState state = strm.inflateState;
        if (state.wrap != 0 && state.mode != InflateMode.Dict)
            return Z_STREAM_ERROR;

        // check for correct dictionary identifier
        if (state.mode == InflateMode.Dict)
        {
            uint dictid = Adler32.Update(0, ref Unsafe.NullRef<byte>(), 0);
            dictid = Adler32.Update(dictid, ref dictionary, dictLength);
            if (dictid != state.check)
                return Z_DATA_ERROR;
        }

        // copy dictionary to window using updatewindow(), which will amend the existing dictionary if appropriate
        try
        {
            UpdateWindow(strm, ref Unsafe.Add(ref dictionary, (int)dictLength), dictLength);
        }
        catch (OutOfMemoryException)
        {
            state.mode = InflateMode.Mem;
            return Z_MEM_ERROR;
        }

        state.havedict = 1;
        Trace.Tracev("inflate:   dictionary set\n");
        return Z_OK;
    }
}