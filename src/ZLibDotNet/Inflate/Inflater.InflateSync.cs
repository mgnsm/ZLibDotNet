// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static unsafe int InflateSync(Unsafe.ZStream strm)
    {
        if (InflateStateCheck(strm))
            return Z_STREAM_ERROR;
        InflateState state = strm.inflateState;
        if (strm.avail_in == 0 && state.bits < 8)
            return Z_BUF_ERROR;

        uint len = 0;
        // if first time, start search in bit buffer
        if (state.mode != InflateMode.Sync)
        {
            state.mode = InflateMode.Sync;
            uint temp = state.bits & 7;
            state.hold <<= (int)temp;
            state.bits -= temp;
            byte* buf = stackalloc byte[4];
            while (state.bits >= 8)
            {
                buf[len++] = (byte)state.hold;
                state.hold >>= 8;
                state.bits -= 8;
            }
            state.have = 0;
            _ = SyncSearch(ref state.have, buf, len);
        }

        // search available input
        len = SyncSearch(ref state.have, strm.next_in, strm.avail_in);
        strm.avail_in -= len;
        strm.next_in += len;
        strm.total_in += len;

        // return no joy or set up to restart Inflate on a new block
        if (state.have != 4)
            return Z_DATA_ERROR;

        int flags; // temporary to save header status
        if (state.flags == -1)
            state.wrap = 0;     // if no header yet, treat as raw
        else
            state.wrap &= ~4;   // no point in computing a check value now */
        flags = state.flags;

        uint @in, @out; // temporary to save total_in and total_out
        @in = strm.total_in;
        @out = strm.total_out;
        _ = InflateReset(strm);
        strm.total_in = @in;
        strm.total_out = @out;
        state.flags = flags;
        state.mode = InflateMode.Type;
        return Z_OK;
    }

    private static unsafe uint SyncSearch(ref uint have, byte* buf, uint len)
    {
        uint got = have;
        uint next = 0;
        while (next < len && got < 4)
        {
            if (buf[next] == (got < 2 ? 0 : 0xff))
                got++;
            else if (buf[next] != 0)
                got = 0;
            else
                got = 4 - got;
            next++;
        }
        have = got;
        return next;
    }
}