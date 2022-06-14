// Original code and comments Copyright (C) 1995 - 2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Buffers;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static int InflateCopy(Unsafe.ZStream dest, Unsafe.ZStream source)
    {
        // check input
        if (InflateStateCheck(source) || dest == null)
            return Z_STREAM_ERROR;
        InflateState state = source.inflateState;

        // allocate space
        InflateState copy;
        try
        {
            copy = s_objectPool.Get();
            if (copy == null)
                return Z_MEM_ERROR;
        }
        catch (OutOfMemoryException)
        {
            return Z_MEM_ERROR;
        }

        byte[] window = default;
        int wsize = default;
        if (state.window != null)
        {
            try
            {
                wsize = 1 << (int)state.wbits;
                window = ArrayPool<byte>.Shared.Rent(wsize);
                if (window == null)
                    return Z_MEM_ERROR;
            }
            catch (OutOfMemoryException)
            {
                return Z_MEM_ERROR;
            }
        }

        // copy state
        dest.avail_in = source.avail_in;
        dest.total_in = source.total_in;
        dest.avail_out = source.avail_out;
        dest.total_out = source.total_out;
        dest.msg = source.msg;
        dest.inflateState = copy;
        dest.deflateState = default;
        unsafe
        {
            dest.next_in = source.next_in;
            dest.next_out = source.next_out;
        }

        copy.strm = dest;
        copy.mode = state.mode;
        copy.last = state.last;
        copy.wrap = state.wrap;
        copy.havedict = state.havedict;
        copy.flags = state.flags;
        copy.dmax = state.dmax;
        copy.check = state.check;
        copy.total = state.total;
        copy.wbits = state.wbits;
        copy.wsize = state.wsize;
        copy.whave = state.whave;
        copy.wnext = state.wnext;
        copy.hold = state.hold;
        copy.bits = state.bits;
        copy.length = state.length;
        copy.offset = state.offset;
        copy.extra = state.extra;
        copy.lenbits = state.lenbits;
        copy.distbits = state.distbits;
        copy.ncode = state.ncode;
        copy.nlen = state.nlen;
        copy.ndist = state.ndist;
        copy.have = state.have;
        copy.sane = state.sane;
        copy.back = state.back;
        copy.was = state.was;
        unsafe
        {
            fixed (ushort* lensSrc = state.lens, lensDest = copy.lens, workSrc = state.work, workDest = copy.work)
            {
                Buffer.MemoryCopy(lensSrc, lensDest, state.lens.Length, state.lens.Length);
                Buffer.MemoryCopy(workSrc, workDest, state.work.Length, state.work.Length);
            }
            fixed (Code* src = state.codes, destination = copy.codes)
            {
                Buffer.MemoryCopy(src, destination, state.codes.Length, state.codes.Length);

                if (state.lencode >= src && state.lencode <= src + InflateState.Enough - 1)
                {
                    copy.lencode = destination + (state.lencode - src);
                    copy.distcode = destination + (state.distcode - src);
                }
                copy.next = destination + (state.next - src);
            }
            if (window != default)
            {
                fixed (byte* src = state.window, destination = window)
                    Buffer.MemoryCopy(src, destination, wsize, wsize);
            }
        }
        copy.window = window;
        return Z_OK;
    }
}
