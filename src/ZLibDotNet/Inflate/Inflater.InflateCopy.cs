// Original code and comments Copyright (C) 1995 - 2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static int InflateCopy(ZStream dest, ZStream source)
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
        dest.next_in = source.next_in;
        dest.next_out = source.next_out;

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

        Unsafe.CopyBlock(ref Unsafe.As<ushort, byte>(ref MemoryMarshal.GetReference(copy.lens.AsSpan())),
            ref Unsafe.As<ushort, byte>(ref MemoryMarshal.GetReference(state.lens.AsSpan())), (uint)(state.lens.Length * sizeof(ushort)));

        Unsafe.CopyBlock(ref Unsafe.As<ushort, byte>(ref MemoryMarshal.GetReference(copy.work.AsSpan())),
            ref Unsafe.As<ushort, byte>(ref MemoryMarshal.GetReference(state.work.AsSpan())), (uint)(state.work.Length * sizeof(ushort)));

        Unsafe.CopyBlock(ref Unsafe.As<Code, byte>(ref MemoryMarshal.GetReference(copy.codes.AsSpan())),
            ref Unsafe.As<Code, byte>(ref MemoryMarshal.GetReference(state.codes.AsSpan())), (uint)(state.codes.Length * Code.Size));

        if (state.lencode == s_lenfix)
            copy.lencode = s_lenfix;
        else if (state.lencode == state.codes)
            copy.lencode = copy.codes;

        if (state.distcode == s_distfix)
            copy.distcode = s_distfix;
        else if (state.distcode == state.codes)
            copy.distcode = copy.codes;

        copy.next = state.next;
        copy.diststart = state.diststart;

        if (window != default)
            Unsafe.CopyBlock(ref MemoryMarshal.GetReference(window.AsSpan()),
                ref MemoryMarshal.GetReference(state.window.AsSpan()), (uint)wsize);

        copy.window = window;
        return Z_OK;
    }
}
