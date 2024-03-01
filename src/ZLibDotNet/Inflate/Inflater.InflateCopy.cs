// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static int InflateCopy(ref ZStream dest, ref ZStream source)
    {
        // check input
        if (InflateStateCheck(ref source))
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
        dest.data_type = source.data_type;
        dest.Adler = source.Adler;

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

#if NET7_0_OR_GREATER
        ref InflateRefs sourceRefs = ref source.inflateRefs;
        ref InflateRefs destRefs = ref dest.inflateRefs;
        InitRefFields(state, ref sourceRefs);
        InitRefFields(copy, ref destRefs);
#endif

        ref ushort sourceLens = ref
#if NET7_0_OR_GREATER
        sourceRefs.lens;
#else
        MemoryMarshal.GetReference<ushort>(state.lens);
#endif
        ref ushort sourceWork = ref
#if NET7_0_OR_GREATER
        sourceRefs.work;
#else
        MemoryMarshal.GetReference<ushort>(state.work);
#endif
        ref Code sourceCodes = ref
#if NET7_0_OR_GREATER
        sourceRefs.codes;
#else
        MemoryMarshal.GetReference<Code>(state.codes);
#endif

        ref ushort destLens = ref
#if NET7_0_OR_GREATER
        destRefs.lens;
#else
        MemoryMarshal.GetReference<ushort>(copy.lens);
#endif
        ref ushort destWork = ref
#if NET7_0_OR_GREATER
        destRefs.work;
#else
        MemoryMarshal.GetReference<ushort>(copy.work);
#endif
        ref Code destCodes = ref
#if NET7_0_OR_GREATER
        destRefs.codes;
#else
        MemoryMarshal.GetReference<Code>(copy.codes);
#endif

        netUnsafe.CopyBlock(ref netUnsafe.As<ushort, byte>(ref destLens),
            ref netUnsafe.As<ushort, byte>(ref sourceLens), (uint)(state.lens.Length * sizeof(ushort)));

        netUnsafe.CopyBlock(ref netUnsafe.As<ushort, byte>(ref destWork),
            ref netUnsafe.As<ushort, byte>(ref sourceWork), (uint)(state.work.Length * sizeof(ushort)));

        netUnsafe.CopyBlock(ref netUnsafe.As<Code, byte>(ref destCodes),
            ref netUnsafe.As<Code, byte>(ref sourceCodes), (uint)(state.codes.Length * Code.Size));

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
            netUnsafe.CopyBlock(ref MemoryMarshal.GetReference<byte>(window),
                ref MemoryMarshal.GetReference<byte>(state.window), (uint)wsize);

        copy.window = window;
        return Z_OK;
    }

#if NET7_0_OR_GREATER
    private static void InitRefFields(InflateState s, ref InflateRefs refs)
    {
        if (netUnsafe.IsNullRef(ref refs.lens))
            refs.lens = ref MemoryMarshal.GetReference<ushort>(s.lens);
        if (netUnsafe.IsNullRef(ref refs.codes))
        {
            refs.codes = ref MemoryMarshal.GetReference<Code>(s.codes);
            refs.work = ref MemoryMarshal.GetReference<ushort>(s.work);
        }
    }
#endif
}
