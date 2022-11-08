﻿// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static int InflateSync(ZStream strm)
    {
        if (InflateStateCheck(strm))
            return Z_STREAM_ERROR;
        InflateState state = strm.inflateState;
        if (strm.avail_in == 0 && state.bits < 8)
            return Z_BUF_ERROR;

        int len = 0;
        // if first time, start search in bit buffer
        if (state.mode != InflateMode.Sync)
        {
            state.mode = InflateMode.Sync;
            uint temp = state.bits & 7;
            state.hold <<= (int)temp;
            state.bits -= temp;
            Span<byte> span = stackalloc byte[4];
            ref byte buf = ref MemoryMarshal.GetReference(span);
            while (state.bits >= 8)
            {
                Unsafe.Add(ref buf, len) = (byte)state.hold;
                len++;
                state.hold >>= 8;
                state.bits -= 8;
            }
            state.have = 0;
            _ = SyncSearch(ref state.have, ref buf, len);
        }

        // search available input
        uint @in = SyncSearch(ref state.have, ref MemoryMarshal.GetReference(strm._input.AsSpan(strm.next_in)), (int)strm.avail_in);
        strm.avail_in -= @in;
        strm.next_in += (int)@in;
        strm.total_in += @in;

        // return no joy or set up to restart Inflate on a new block
        if (state.have != 4)
            return Z_DATA_ERROR;

        int flags; // temporary to save header status
        if (state.flags == -1)
            state.wrap = 0;     // if no header yet, treat as raw
        else
            state.wrap &= ~4;   // no point in computing a check value now */
        flags = state.flags;

        uint @out; // temporary to total_out
        @in = strm.total_in;
        @out = strm.total_out;
        _ = InflateReset(strm);
        strm.total_in = @in;
        strm.total_out = @out;
        state.flags = flags;
        state.mode = InflateMode.Type;
        return Z_OK;
    }

    private static uint SyncSearch(ref uint have, ref byte buf, int len)
    {
        uint got = have;
        int next = 0;
        while (next < len && got < 4)
        {
            byte b = Unsafe.Add(ref buf, next);
            if (b == (got < 2 ? 0 : 0xff))
                got++;
            else if (b != 0)
                got = 0;
            else
                got = 4 - got;
            next++;
        }
        have = got;
        return (uint)next;
    }
}