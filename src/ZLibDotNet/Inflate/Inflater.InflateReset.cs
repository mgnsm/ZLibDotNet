// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System.Buffers;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static unsafe int InflateReset(Unsafe.ZStream strm)
    {
        if (InflateStateCheck(strm))
            return Z_STREAM_ERROR;

        InflateState state = strm.inflateState;
        state.wsize = 0;
        state.whave = 0;
        state.wnext = 0;
        return InflateResetKeep(strm);
    }

    internal static int InflateReset(Unsafe.ZStream strm, int windowBits)
    {
        if (InflateStateCheck(strm))
            return Z_STREAM_ERROR;

        InflateState state = strm.inflateState;
        int wrap;
        // extract wrap request from windowBits parameter
        if (windowBits < 0)
        {
            wrap = 0;
            windowBits = -windowBits;
        }
        else
        {
            wrap = (windowBits >> 4) + 5;
        }

        // set number of window bits, free window if different
        if (windowBits != 0 && (windowBits < 8 || windowBits > 15))
            return Z_STREAM_ERROR;
        if (state.window != null && state.wbits != (uint)windowBits)
        {
            ArrayPool<byte>.Shared.Return(state.window);
            state.window = null;
        }

        // update state and reset the rest of it
        state.wrap = wrap;
        state.wbits = (uint)windowBits;
        return InflateReset(strm);
    }

    internal static unsafe int InflateResetKeep(Unsafe.ZStream strm)
    {
        if (InflateStateCheck(strm))
            return Z_STREAM_ERROR;

        InflateState state = strm.inflateState;
        strm.total_in = strm.total_out = state.total = 0;
        strm.msg = null;
        if (state.wrap != 0)
            strm.Adler = (uint)(state.wrap & 1);
        state.mode = InflateMode.Head;
        state.last = 0;
        state.havedict = 0;
        state.flags = -1;
        state.dmax = 32768U;
        state.hold = 0;
        state.bits = 0;
        state.lencode = state.distcode = state.codes;
        state.next = default;
        state.diststart = default;
        state.sane = 1;
        state.back = -1;
        Trace.Tracev("inflate: reset\n");
        return Z_OK;
    }
}