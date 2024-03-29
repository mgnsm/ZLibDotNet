﻿// Original code and comments Copyright (C) 1995-2024 Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

using System;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static readonly Code[] s_lenfix = new Code[512]
    {
        new(96,7,0), new(0,8,80), new(0,8,16), new(20,8,115), new(18,7,31), new(0,8,112), new(0,8,48),
        new(0,9,192), new(16,7,10), new(0,8,96), new(0,8,32), new(0,9,160), new(0,8,0), new(0,8,128),
        new(0,8,64), new(0,9,224), new(16,7,6), new(0,8,88), new(0,8,24), new(0,9,144), new(19,7,59),
        new(0,8,120), new(0,8,56), new(0,9,208), new(17,7,17), new(0,8,104), new(0,8,40), new(0,9,176),
        new(0,8,8), new(0,8,136), new(0,8,72), new(0,9,240), new(16,7,4), new(0,8,84), new(0,8,20),
        new(21,8,227), new(19,7,43), new(0,8,116), new(0,8,52), new(0,9,200), new(17,7,13), new(0,8,100),
        new(0,8,36), new(0,9,168), new(0,8,4), new(0,8,132), new(0,8,68), new(0,9,232), new(16,7,8),
        new(0,8,92), new(0,8,28), new(0,9,152), new(20,7,83), new(0,8,124), new(0,8,60), new(0,9,216),
        new(18,7,23), new(0,8,108), new(0,8,44), new(0,9,184), new(0,8,12), new(0,8,140), new(0,8,76),
        new(0,9,248), new(16,7,3), new(0,8,82), new(0,8,18), new(21,8,163), new(19,7,35), new(0,8,114),
        new(0,8,50), new(0,9,196), new(17,7,11), new(0,8,98), new(0,8,34), new(0,9,164), new(0,8,2),
        new(0,8,130), new(0,8,66), new(0,9,228), new(16,7,7), new(0,8,90), new(0,8,26), new(0,9,148),
        new(20,7,67), new(0,8,122), new(0,8,58), new(0,9,212), new(18,7,19), new(0,8,106), new(0,8,42),
        new(0,9,180), new(0,8,10), new(0,8,138), new(0,8,74), new(0,9,244), new(16,7,5), new(0,8,86),
        new(0,8,22), new(64,8,0), new(19,7,51), new(0,8,118), new(0,8,54), new(0,9,204), new(17,7,15),
        new(0,8,102), new(0,8,38), new(0,9,172), new(0,8,6), new(0,8,134), new(0,8,70), new(0,9,236),
        new(16,7,9), new(0,8,94), new(0,8,30), new(0,9,156), new(20,7,99), new(0,8,126), new(0,8,62),
        new(0,9,220), new(18,7,27), new(0,8,110), new(0,8,46), new(0,9,188), new(0,8,14), new(0,8,142),
        new(0,8,78), new(0,9,252), new(96,7,0), new(0,8,81), new(0,8,17), new(21,8,131), new(18,7,31),
        new(0,8,113), new(0,8,49), new(0,9,194), new(16,7,10), new(0,8,97), new(0,8,33), new(0,9,162),
        new(0,8,1), new(0,8,129), new(0,8,65), new(0,9,226), new(16,7,6), new(0,8,89), new(0,8,25),
        new(0,9,146), new(19,7,59), new(0,8,121), new(0,8,57), new(0,9,210), new(17,7,17), new(0,8,105),
        new(0,8,41), new(0,9,178), new(0,8,9), new(0,8,137), new(0,8,73), new(0,9,242), new(16,7,4),
        new(0,8,85), new(0,8,21), new(16,8,258), new(19,7,43), new(0,8,117), new(0,8,53), new(0,9,202),
        new(17,7,13), new(0,8,101), new(0,8,37), new(0,9,170), new(0,8,5), new(0,8,133), new(0,8,69),
        new(0,9,234), new(16,7,8), new(0,8,93), new(0,8,29), new(0,9,154), new(20,7,83), new(0,8,125),
        new(0,8,61), new(0,9,218), new(18,7,23), new(0,8,109), new(0,8,45), new(0,9,186), new(0,8,13),
        new(0,8,141), new(0,8,77), new(0,9,250), new(16,7,3), new(0,8,83), new(0,8,19), new(21,8,195),
        new(19,7,35), new(0,8,115), new(0,8,51), new(0,9,198), new(17,7,11), new(0,8,99), new(0,8,35),
        new(0,9,166), new(0,8,3), new(0,8,131), new(0,8,67), new(0,9,230), new(16,7,7), new(0,8,91),
        new(0,8,27), new(0,9,150), new(20,7,67), new(0,8,123), new(0,8,59), new(0,9,214), new(18,7,19),
        new(0,8,107), new(0,8,43), new(0,9,182), new(0,8,11), new(0,8,139), new(0,8,75), new(0,9,246),
        new(16,7,5), new(0,8,87), new(0,8,23), new(64,8,0), new(19,7,51), new(0,8,119), new(0,8,55),
        new(0,9,206), new(17,7,15), new(0,8,103), new(0,8,39), new(0,9,174), new(0,8,7), new(0,8,135),
        new(0,8,71), new(0,9,238), new(16,7,9), new(0,8,95), new(0,8,31), new(0,9,158), new(20,7,99),
        new(0,8,127), new(0,8,63), new(0,9,222), new(18,7,27), new(0,8,111), new(0,8,47), new(0,9,190),
        new(0,8,15), new(0,8,143), new(0,8,79), new(0,9,254), new(96,7,0), new(0,8,80), new(0,8,16),
        new(20,8,115), new(18,7,31), new(0,8,112), new(0,8,48), new(0,9,193), new(16,7,10), new(0,8,96),
        new(0,8,32), new(0,9,161), new(0,8,0), new(0,8,128), new(0,8,64), new(0,9,225), new(16,7,6),
        new(0,8,88), new(0,8,24), new(0,9,145), new(19,7,59), new(0,8,120), new(0,8,56), new(0,9,209),
        new(17,7,17), new(0,8,104), new(0,8,40), new(0,9,177), new(0,8,8), new(0,8,136), new(0,8,72),
        new(0,9,241), new(16,7,4), new(0,8,84), new(0,8,20), new(21,8,227), new(19,7,43), new(0,8,116),
        new(0,8,52), new(0,9,201), new(17,7,13), new(0,8,100), new(0,8,36), new(0,9,169), new(0,8,4),
        new(0,8,132), new(0,8,68), new(0,9,233), new(16,7,8), new(0,8,92), new(0,8,28), new(0,9,153),
        new(20,7,83), new(0,8,124), new(0,8,60), new(0,9,217), new(18,7,23), new(0,8,108), new(0,8,44),
        new(0,9,185), new(0,8,12), new(0,8,140), new(0,8,76), new(0,9,249), new(16,7,3), new(0,8,82),
        new(0,8,18), new(21,8,163), new(19,7,35), new(0,8,114), new(0,8,50), new(0,9,197), new(17,7,11),
        new(0,8,98), new(0,8,34), new(0,9,165), new(0,8,2), new(0,8,130), new(0,8,66), new(0,9,229),
        new(16,7,7), new(0,8,90), new(0,8,26), new(0,9,149), new(20,7,67), new(0,8,122), new(0,8,58),
        new(0,9,213), new(18,7,19), new(0,8,106), new(0,8,42), new(0,9,181), new(0,8,10), new(0,8,138),
        new(0,8,74), new(0,9,245), new(16,7,5), new(0,8,86), new(0,8,22), new(64,8,0), new(19,7,51),
        new(0,8,118), new(0,8,54), new(0,9,205), new(17,7,15), new(0,8,102), new(0,8,38), new(0,9,173),
        new(0,8,6), new(0,8,134), new(0,8,70), new(0,9,237), new(16,7,9), new(0,8,94), new(0,8,30),
        new(0,9,157), new(20,7,99), new(0,8,126), new(0,8,62), new(0,9,221), new(18,7,27), new(0,8,110),
        new(0,8,46), new(0,9,189), new(0,8,14), new(0,8,142), new(0,8,78), new(0,9,253), new(96,7,0),
        new(0,8,81), new(0,8,17), new(21,8,131), new(18,7,31), new(0,8,113), new(0,8,49), new(0,9,195),
        new(16,7,10), new(0,8,97), new(0,8,33), new(0,9,163), new(0,8,1), new(0,8,129), new(0,8,65),
        new(0,9,227), new(16,7,6), new(0,8,89), new(0,8,25), new(0,9,147), new(19,7,59), new(0,8,121),
        new(0,8,57), new(0,9,211), new(17,7,17), new(0,8,105), new(0,8,41), new(0,9,179), new(0,8,9),
        new(0,8,137), new(0,8,73), new(0,9,243), new(16,7,4), new(0,8,85), new(0,8,21), new(16,8,258),
        new(19,7,43), new(0,8,117), new(0,8,53), new(0,9,203), new(17,7,13), new(0,8,101), new(0,8,37),
        new(0,9,171), new(0,8,5), new(0,8,133), new(0,8,69), new(0,9,235), new(16,7,8), new(0,8,93),
        new(0,8,29), new(0,9,155), new(20,7,83), new(0,8,125), new(0,8,61), new(0,9,219), new(18,7,23),
        new(0,8,109), new(0,8,45), new(0,9,187), new(0,8,13), new(0,8,141), new(0,8,77), new(0,9,251),
        new(16,7,3), new(0,8,83), new(0,8,19), new(21,8,195), new(19,7,35), new(0,8,115), new(0,8,51),
        new(0,9,199), new(17,7,11), new(0,8,99), new(0,8,35), new(0,9,167), new(0,8,3), new(0,8,131),
        new(0,8,67), new(0,9,231), new(16,7,7), new(0,8,91), new(0,8,27), new(0,9,151), new(20,7,67),
        new(0,8,123), new(0,8,59), new(0,9,215), new(18,7,19), new(0,8,107), new(0,8,43), new(0,9,183),
        new(0,8,11), new(0,8,139), new(0,8,75), new(0,9,247), new(16,7,5), new(0,8,87), new(0,8,23),
        new(64,8,0), new(19,7,51), new(0,8,119), new(0,8,55), new(0,9,207), new(17,7,15), new(0,8,103),
        new(0,8,39), new(0,9,175), new(0,8,7), new(0,8,135), new(0,8,71), new(0,9,239), new(16,7,9),
        new(0,8,95), new(0,8,31), new(0,9,159), new(20,7,99), new(0,8,127), new(0,8,63), new(0,9,223),
        new(18,7,27), new(0,8,111), new(0,8,47), new(0,9,191), new(0,8,15), new(0,8,143), new(0,8,79),
        new(0,9,255)
    };

    internal static readonly Code[] s_distfix = new Code[32]
    {
        new(16,5,1), new(23,5,257), new(19,5,17), new(27,5,4097), new(17,5,5), new(25,5,1025),
        new(21,5,65), new(29,5,16385), new(16,5,3), new(24,5,513), new(20,5,33), new(28,5,8193),
        new(18,5,9), new(26,5,2049), new(22,5,129), new(64,5,0), new(16,5,2), new(23,5,385),
        new(19,5,25), new(27,5,6145), new(17,5,7), new(25,5,1537), new(21,5,97), new(29,5,24577),
        new(16,5,4), new(24,5,769), new(20,5,49), new(28,5,12289), new(18,5,13), new(26,5,3073),
        new(22,5,193), new(64,5,0)
    };

    // permutation of code lengths
    private static readonly ushort[] s_order = new ushort[19] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

    internal static void Init() => s_objectPool.Return(new InflateState());

    internal static int Inflate(ref ZStream strm, int flush)
    {
        if (InflateStateCheck(ref strm)
            || strm._output == null
            || strm._input == null && strm.avail_in != 0)
            return Z_STREAM_ERROR;

        InflateState state = strm.inflateState;
        if (state.mode == InflateMode.Type) // Skip check
            state.mode = InflateMode.Typedo;

        ref byte next = ref // next input
#if NET7_0_OR_GREATER
            Unsafe.Add(ref strm.input_ptr, strm.next_in);
#else
            MemoryMarshal.GetReference(strm._input.Slice((int)strm.next_in));
#endif
        ref byte put = ref // next output
#if NET7_0_OR_GREATER
            Unsafe.Add(ref strm.output_ptr, strm.next_out);
#else
            MemoryMarshal.GetReference(strm._output.Slice((int)strm.next_out));
#endif
        ref byte from = ref netUnsafe.NullRef<byte>(); // where to copy match bytes from
#if NET7_0_OR_GREATER
        ref InflateRefs refs = ref strm.inflateRefs;
        ref Code codes = ref refs.codes;
#else
        ref Code codes = ref netUnsafe.NullRef<Code>();
        ref ushort lens = ref netUnsafe.NullRef<ushort>();
        ref ushort work = ref netUnsafe.NullRef<ushort>();
        ref byte window = ref netUnsafe.NullRef<byte>();
        ref Code lencode = ref netUnsafe.NullRef<Code>();
        ref Code distcode = ref netUnsafe.NullRef<Code>();
        ref ushort order = ref netUnsafe.NullRef<ushort>();
        ref ushort lbase = ref netUnsafe.NullRef<ushort>();
        ref ushort lext = ref netUnsafe.NullRef<ushort>();
        ref ushort dbase = ref netUnsafe.NullRef<ushort>();
        ref ushort dext = ref netUnsafe.NullRef<ushort>();
#endif
        uint have = strm.avail_in;          // available input
        uint left = strm.avail_out;         // ...and output
        uint hold = strm.inflateState.hold; // bit buffer
        uint bits = strm.inflateState.bits; // bits in bit buffer
        uint @in = have;    // save starting available input
        uint @out = left;   // ...and output
        uint copy;          // number of stored or match bytes to copy
        Code here;          // current decoding table entry
        Code last;          // parent table entry
        uint len;           // length to copy for repeats, bits to drop
        uint next_in = strm.next_in;
        uint next_out = strm.next_out;
        int ret = Z_OK;

        for (; ; )
            switch (state.mode)
            {
                case InflateMode.Head:
                    if (state.wrap == 0)
                    {
                        state.mode = InflateMode.Typedo;
                        break;
                    }
                    while (bits < 16)
                    {
                        if (have == 0)
                            goto inf_leave;
                        have--;
                        hold += (uint)next << (int)bits;
                        next = ref Unsafe.Add(ref next, 1U);
                        next_in++;
                        bits += 8;
                    }
                    if ((((hold & ((1U << (8)) - 1)) << 8) + (hold >> 8)) % 31 != 0)
                    {
                        strm.msg = "incorrect header check";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    if ((hold & ((1U << (4)) - 1)) != Z_DEFLATED)
                    {
                        strm.msg = "unknown compression method";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    hold >>= 4;
                    bits -= 4;
                    len = (hold & ((1U << (4)) - 1)) + 8;
                    if (state.wbits == 0)
                        state.wbits = len;
                    if (len > 15 || len > state.wbits)
                    {
                        strm.msg = "invalid window size";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    state.dmax = (uint)(1 << (int)len);
                    state.flags = 0; // indicate zlib header
                    Trace.Tracev("inflate:   zlib header ok\n");
                    strm.Adler = state.check = Adler32.Update(0, ref netUnsafe.NullRef<byte>(), 0);
                    state.mode = (hold & 0x200) != 0 ? InflateMode.DictId : InflateMode.Type;
                    hold = 0;
                    bits = 0;
                    break;
                case InflateMode.DictId:
                    while (bits < 32)
                    {
                        if (have == 0)
                            goto inf_leave;
                        have--;
                        hold += (uint)next << (int)bits;
                        next = ref Unsafe.Add(ref next, 1U);
                        next_in++;
                        bits += 8;
                    }
                    strm.Adler = state.check = ZSwap32(hold);
                    hold = 0;
                    bits = 0;
                    state.mode = InflateMode.Dict;
                    goto case InflateMode.Dict;
                case InflateMode.Dict:
                    if (state.havedict == 0)
                    {
                        strm.next_out = next_out;
                        strm.avail_out = left;
                        strm.next_in = next_in;
                        strm.avail_in = have;
                        strm.inflateState.hold = hold;
                        strm.inflateState.bits = bits;
                        return Z_NEED_DICT;
                    }
                    strm.Adler = state.check = Adler32.Update(0, ref netUnsafe.NullRef<byte>(), 0);
                    state.mode = InflateMode.Type;
                    goto case InflateMode.Type;
                case InflateMode.Type:
                    if (flush == Z_BLOCK || flush == Z_TREES)
                        goto inf_leave;
                    goto case InflateMode.Typedo;
                case InflateMode.Typedo:
                    if (state.last != 0)
                    {
                        hold >>= (int)(bits & 7);
                        bits -= bits & 7;
                        state.mode = InflateMode.Check;
                        break;
                    }
                    while (bits < 3)
                    {
                        if (have == 0)
                            goto inf_leave;
                        have--;
                        hold += (uint)next << (int)bits;
                        next = ref Unsafe.Add(ref next, 1U);
                        next_in++;
                        bits += 8;
                    }
                    state.last = (int)(hold & ((1U << (1)) - 1));
                    hold >>= 1;
                    bits--;
                    switch (hold & ((1U << (2)) - 1))
                    {
                        case 0: // stored block
                            Trace.Tracev($"inflate:     stored block{(state.last != 0 ? " (last)" : "")}\n");
                            state.mode = InflateMode.Stored;
                            break;
                        case 1: // fixed block
                            state.lencode = s_lenfix;
                            state.lenbits = 9;
                            state.diststart = default;
                            state.distcode = s_distfix;
                            state.distbits = 5;
                            Trace.Tracev($"inflate:     fixed codes block{(state.last != 0 ? " (last)" : "")}\n");
                            state.mode = InflateMode.Len_; // decode codes
                            if (flush == Z_TREES)
                            {
                                hold >>= 2;
                                bits -= 2;
                                goto inf_leave;
                            }
                            break;
                        case 2: // dynamic block
                            Trace.Tracev($"inflate:     dynamic codes block{(state.last != 0 ? "(last)" : "")}\n");
                            state.mode = InflateMode.Table;
                            break;
                        case 3:
                            strm.msg = "invalid block type";
                            state.mode = InflateMode.Bad;
                            break;
                    }
                    hold >>= 2;
                    bits -= 2;
                    break;
                case InflateMode.Stored:
                    hold >>= (int)(bits & 7); // go to byte boundary
                    bits -= bits & 7;
                    while (bits < 32)
                    {
                        if (have == 0)
                            goto inf_leave;
                        have--;
                        hold += (uint)next << (int)bits;
                        next = ref Unsafe.Add(ref next, 1U);
                        next_in++;
                        bits += 8;
                    }
                    if ((hold & 0xffff) != ((hold >> 16) ^ 0xffff))
                    {
                        strm.msg = "invalid stored block lengths";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    state.length = hold & 0xffff;
                    Trace.Tracev($"inflate:       stored length {state.length}\n");
                    hold = 0;
                    bits = 0;
                    state.mode = InflateMode.Copy_;
                    if (flush == Z_TREES)
                        goto inf_leave;
                    goto case InflateMode.Copy_;
                case InflateMode.Copy_:
                    state.mode = InflateMode.Copy;
                    goto case InflateMode.Copy;
                case InflateMode.Copy:
                    copy = state.length;
                    if (copy != 0)
                    {
                        if (copy > have)
                            copy = have;
                        if (copy > left)
                            copy = left;
                        if (copy == 0)
                            goto inf_leave;
                        netUnsafe.CopyBlockUnaligned(ref put, ref next, copy);
                        have -= copy;
                        next = ref Unsafe.Add(ref next, copy);
                        next_in += copy;
                        left -= copy;
                        put = ref Unsafe.Add(ref put, copy);
                        next_out += copy;
                        state.length -= copy;
                        break;
                    }
                    Trace.Tracev("inflate:       stored end\n");
                    state.mode = InflateMode.Type;
                    break;
                case InflateMode.Table:
                    while (bits < 14)
                    {
                        if (have == 0)
                            goto inf_leave;
                        have--;
                        hold += (uint)next << (int)bits;
                        next = ref Unsafe.Add(ref next, 1U);
                        next_in++;
                        bits += 8;
                    }
                    state.nlen = (hold & ((1U << (5)) - 1)) + 257;
                    hold >>= 5;
                    bits -= 5;
                    state.ndist = (hold & ((1U << (5)) - 1)) + 1;
                    hold >>= 5;
                    bits -= 5;
                    state.ncode = (hold & ((1U << (4)) - 1)) + 4;
                    hold >>= 4;
                    bits -= 4;
                    if (state.nlen > 286 || state.ndist > 30)
                    {
                        strm.msg = "too many length or distance symbols";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    Trace.Tracev("inflate:       table sizes ok\n");
                    state.have = 0;
                    state.mode = InflateMode.LenLens;
                    goto case InflateMode.LenLens;
                case InflateMode.LenLens:
                    if (netUnsafe.IsNullRef(ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lens))
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lens = ref MemoryMarshal.GetReference<ushort>(state.lens);
                    if (netUnsafe.IsNullRef(ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    order))
#if NET7_0_OR_GREATER
                        refs.
#endif
                        order = ref MemoryMarshal.GetReference<ushort>(s_order);
                    while (state.have < state.ncode)
                    {
                        while (bits < 3)
                        {
                            if (have == 0)
                                goto inf_leave;
                            have--;
                            hold += (uint)next << (int)bits;
                            next = ref Unsafe.Add(ref next, 1U);
                            next_in++;
                            bits += 8;
                        }
                        Unsafe.Add(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lens, (uint)Unsafe.Add(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        order, state.have++)) = (ushort)(hold & ((1U << (3)) - 1));
                        hold >>= 3;
                        bits -= 3;
                    }
                    while (state.have < 19)
                        Unsafe.Add(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lens, (uint)Unsafe.Add(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        order, state.have++)) = 0;
                    state.next = 0;
                    state.lencode = state.codes;
                    state.lenbits = 7;
                    if (netUnsafe.IsNullRef(ref codes))
                    {
                        codes = ref MemoryMarshal.GetReference<Code>(state.codes);
#if NET7_0_OR_GREATER
                        refs.codes = ref codes;
#endif
#if NET7_0_OR_GREATER
                        refs.
#endif
                        work = ref MemoryMarshal.GetReference<ushort>(state.work);
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lbase = ref MemoryMarshal.GetReference<ushort>(s_lbase);
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lext = ref MemoryMarshal.GetReference<ushort>(s_lext);
#if NET7_0_OR_GREATER
                        refs.
#endif
                        dbase = ref MemoryMarshal.GetReference<ushort>(s_dbase);
#if NET7_0_OR_GREATER
                        refs.
#endif
                        dext = ref MemoryMarshal.GetReference<ushort>(s_dext);
                    }
                    ret = InflateTable(CodeType.Codes, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lens, 19, ref codes, ref state.lenbits, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    work, ref state.next, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lbase, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lext, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    dbase, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    dext);
                    if (ret != 0)
                    {
                        strm.msg = "invalid code lengths set";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    Trace.Tracev("inflate:       code lengths ok\n");
                    state.have = 0;
                    state.mode = InflateMode.CodeLens;
                    goto case InflateMode.CodeLens;
                case InflateMode.CodeLens:
                    if (netUnsafe.IsNullRef(ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lencode))
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lencode = ref MemoryMarshal.GetReference<Code>(state.lencode);
                    while (state.have < state.nlen + state.ndist)
                    {
                        for (; ; )
                        {
                            here = Unsafe.Add(ref
#if NET7_0_OR_GREATER
                            refs.
#endif
                            lencode, hold & ((1U << (state.lenbits)) - 1));
                            if (here.bits <= bits)
                                break;
                            if (have == 0)
                                goto inf_leave;
                            have--;
                            hold += (uint)next << (int)bits;
                            next = ref Unsafe.Add(ref next, 1U);
                            next_in++;
                            bits += 8;
                        }
                        if (here.val < 16)
                        {
                            hold >>= here.bits;
                            bits -= here.bits;
                            Unsafe.Add(ref
#if NET7_0_OR_GREATER
                            refs.
#endif
                            lens, state.have++) = here.val;
                        }
                        else
                        {
                            if (here.val == 16)
                            {
                                while (bits < here.bits + 2)
                                {
                                    if (have == 0)
                                        goto inf_leave;
                                    have--;
                                    hold += (uint)next << (int)bits;
                                    next = ref Unsafe.Add(ref next, 1U);
                                    next_in++;
                                    bits += 8;
                                }
                                hold >>= here.bits;
                                bits -= here.bits;
                                if (state.have == 0)
                                {
                                    strm.msg = "invalid bit length repeat";
                                    state.mode = InflateMode.Bad;
                                    break;
                                }
                                len = Unsafe.Add(ref
#if NET7_0_OR_GREATER
                                refs.
#endif
                                lens, state.have - 1);
                                copy = 3 + (hold & ((1U << (2)) - 1));
                                hold >>= 2;
                                bits -= 2;
                            }
                            else if (here.val == 17)
                            {
                                while (bits < here.bits + 3)
                                {
                                    if (have == 0)
                                        goto inf_leave;
                                    have--;
                                    hold += (uint)next << (int)bits;
                                    next = ref Unsafe.Add(ref next, 1U);
                                    next_in++;
                                    bits += 8;
                                }
                                hold >>= here.bits;
                                bits -= here.bits;
                                len = 0;
                                copy = 3 + (hold & ((1U << (3)) - 1));
                                hold >>= 3;
                                bits -= 3;
                            }
                            else
                            {
                                while (bits < here.bits + 7)
                                {
                                    if (have == 0)
                                        goto inf_leave;
                                    have--;
                                    hold += (uint)next << (int)bits;
                                    next = ref Unsafe.Add(ref next, 1U);
                                    next_in++;
                                    bits += 8;
                                }
                                hold >>= here.bits;
                                bits -= here.bits;
                                len = 0;
                                copy = 11 + (hold & ((1U << (7)) - 1));
                                hold >>= 7;
                                bits -= 7;
                            }
                            if (state.have + copy > state.nlen + state.ndist)
                            {
                                strm.msg = "invalid bit length repeat";
                                state.mode = InflateMode.Bad;
                                break;
                            }
                            while (copy-- != 0)
                                Unsafe.Add(ref
#if NET7_0_OR_GREATER
                                refs.
#endif
                                lens, state.have++) = (ushort)len;
                        }
                    }

                    // handle error breaks in while
                    if (state.mode == InflateMode.Bad)
                        break;

                    // check for end-of-block code (better have one)
                    if (Unsafe.Add(ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lens, 256U) == 0)
                    {
                        strm.msg = "invalid code -- missing end-of-block";
                        state.mode = InflateMode.Bad;
                        break;
                    }

                    // build code tables
                    state.next = 0;
                    state.lencode = state.codes;
                    state.lenbits = 9;
                    ret = InflateTable(CodeType.Lens, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lens, state.nlen, ref codes, ref state.lenbits, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    work, ref state.next, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lbase, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lext, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    dbase, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    dext);
                    if (ret != 0)
                    {
                        strm.msg = "invalid literal/lengths set";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    state.distcode = state.codes;
                    state.diststart = state.next;
                    state.distbits = 6;
                    codes = ref Unsafe.Add(ref codes, state.next);
                    ret = InflateTable(CodeType.Dists, ref Unsafe.Add(ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lens, state.nlen), state.ndist, ref codes, ref state.distbits, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    work, ref state.next, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lbase, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lext, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    dbase, ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    dext);
                    if (ret != 0)
                    {
                        strm.msg = "invalid distances set";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    Trace.Tracev("inflate:       codes ok\n");
                    state.mode = InflateMode.Len_;
                    if (flush == Z_TREES)
                        goto inf_leave;
                    goto case InflateMode.Len_;
                case InflateMode.Len_:
                    state.mode = InflateMode.Len;
                    goto case InflateMode.Len;
                case InflateMode.Len:
                    if (netUnsafe.IsNullRef(ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    lencode))
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lencode = ref MemoryMarshal.GetReference<Code>(state.lencode);
                    if (netUnsafe.IsNullRef(ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    distcode))
#if NET7_0_OR_GREATER
                        refs.
#endif
                        distcode = ref MemoryMarshal.GetReference<Code>(state.distcode);
                    if (have >= 6 && left >= 258)
                    {
                        strm.next_out = next_out;
                        strm.avail_out = left;
                        strm.next_in = next_in;
                        strm.avail_in = have;
                        strm.inflateState.hold = hold;
                        strm.inflateState.bits = bits;
                        if (netUnsafe.IsNullRef(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        window))
#if NET7_0_OR_GREATER
                            refs.
#endif
                            window = ref MemoryMarshal.GetReference<byte>(state.window);
                        InflateFast(ref strm, @out, ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        window, ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lencode, ref Unsafe.Add(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        distcode, state.diststart));
                        put = ref
#if NET7_0_OR_GREATER
                        Unsafe.Add(ref strm.output_ptr, strm.next_out);
#else
                        MemoryMarshal.GetReference(strm._output.Slice((int)strm.next_out));
#endif
                        next_out = strm.next_out;
                        left = strm.avail_out;
                        next = ref
#if NET7_0_OR_GREATER
                        Unsafe.Add(ref strm.input_ptr, strm.next_in);
#else
                        MemoryMarshal.GetReference(strm._input.Slice((int)strm.next_in));
#endif
                        next_in = strm.next_in;
                        have = strm.avail_in;
                        hold = strm.inflateState.hold;
                        bits = strm.inflateState.bits;
#pragma warning disable CA1508
                        if (state.mode == InflateMode.Type)
                            state.back = -1;
#pragma warning restore CA1508
                        break;
                    }
                    state.back = 0;
                    for (; ; )
                    {
                        here = Unsafe.Add(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        lencode, hold & ((1U << (state.lenbits)) - 1));
                        if (here.bits <= bits)
                            break;
                        if (have == 0)
                            goto inf_leave;
                        have--;
                        hold += (uint)next << (int)bits;
                        next = ref Unsafe.Add(ref next, 1U);
                        next_in++;
                        bits += 8;
                    }
                    if (here.op > 0 && (here.op & 0xf0) == 0)
                    {
                        last = here;
                        for (; ; )
                        {
                            here = Unsafe.Add(ref
#if NET7_0_OR_GREATER
                            refs.
#endif
                            lencode, last.val + ((hold & (1U << last.bits + last.op - 1)) >> last.bits));
                            if ((uint)(last.bits + here.bits) <= bits)
                                break;
                            if (have == 0)
                                goto inf_leave;
                            have--;
                            hold += (uint)next << (int)bits;
                            next = ref Unsafe.Add(ref next, 1U);
                            next_in++;
                            bits += 8;
                        }
                        hold >>= last.bits;
                        bits -= last.bits;
                        state.back += last.bits;
                    }
                    hold >>= here.bits;
                    bits -= here.bits;
                    state.back += here.bits;
                    state.length = here.val;
                    if (here.op == 0)
                    {
                        Trace.Tracevv(here.val >= 0x20 && here.val < 0x7f ?
                            $"inflate:         literal '{Convert.ToChar(here.val)}'\n" :
                            $"inflate:         literal 0x{here.val:X2}\n");
                        state.mode = InflateMode.Lit;
                        break;
                    }
                    if ((here.op & 32) != 0)
                    {
                        Trace.Tracevv("inflate:         end of block\n");
                        state.back = -1;
                        state.mode = InflateMode.Type;
                        break;
                    }
                    if ((here.op & 64) != 0)
                    {
                        strm.msg = "invalid literal/length code";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    state.extra = (uint)here.op & 15;
                    state.mode = InflateMode.LenExt;
                    goto case InflateMode.LenExt;
                case InflateMode.LenExt:
                    if (state.extra != 0)
                    {
                        while (bits < state.extra)
                        {
                            if (have == 0)
                                goto inf_leave;
                            have--;
                            hold += (uint)next << (int)bits;
                            next = ref Unsafe.Add(ref next, 1U);
                            next_in++;
                            bits += 8;
                        }
                        state.length += hold & ((1U << ((int)state.extra)) - 1);
                        hold >>= (int)state.extra;
                        bits -= state.extra;
                        state.back += (int)state.extra;
                    }
                    Trace.Tracevv($"inflate:         length {state.length}\n");
                    state.was = state.length;
                    state.mode = InflateMode.Dist;
                    goto case InflateMode.Dist;
                case InflateMode.Dist:
                    if (netUnsafe.IsNullRef(ref
#if NET7_0_OR_GREATER
                    refs.
#endif
                    distcode))
#if NET7_0_OR_GREATER
                        refs.
#endif
                        distcode = ref MemoryMarshal.GetReference<Code>(state.distcode);
                    for (; ; )
                    {
                        here = Unsafe.Add(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        distcode, state.diststart + (hold & ((1U << (state.distbits)) - 1)));
                        if (here.bits <= bits)
                            break;
                        if (have == 0)
                            goto inf_leave;
                        have--;
                        hold += (uint)next << (int)bits;
                        next = ref Unsafe.Add(ref next, 1U);
                        next_in++;
                        bits += 8;
                    }
                    if ((here.op & 0xf0) == 0)
                    {
                        last = here;
                        for (; ; )
                        {
                            here = Unsafe.Add(ref
#if NET7_0_OR_GREATER
                            refs.
#endif
                            distcode, state.diststart + last.val +
                                ((hold & ((1U << (last.bits + last.op)) - 1)) >> last.bits));
                            if ((uint)(last.bits + here.bits) <= bits)
                                break;
                            if (have == 0)
                                goto inf_leave;
                            have--;
                            hold += (uint)next << (int)bits;
                            next = ref Unsafe.Add(ref next, 1U);
                            next_in++;
                            bits += 8;
                        }
                        hold >>= last.bits;
                        bits -= last.bits;
                        state.back += last.bits;
                    }
                    hold >>= here.bits;
                    bits -= here.bits;
                    state.back += here.bits;
                    if ((here.op & 64) != 0)
                    {
                        strm.msg = "invalid distance code";
                        state.mode = InflateMode.Bad;
                        break;
                    }
                    state.offset = here.val;
                    state.extra = (uint)here.op & 15;
                    state.mode = InflateMode.DistExt;
                    goto case InflateMode.DistExt;
                case InflateMode.DistExt:
                    if (state.extra != 0)
                    {
                        while (bits < state.extra)
                        {
                            if (have == 0)
                                goto inf_leave;
                            have--;
                            hold += (uint)next << (int)bits;
                            next = ref Unsafe.Add(ref next, 1U);
                            next_in++;
                            bits += 8;
                        }
                        state.offset += hold & ((1U << ((int)state.extra)) - 1);
                        hold >>= (int)state.extra;
                        bits -= state.extra;
                        state.back += (int)state.extra;
                    }
                    Trace.Tracevv($"inflate:         distance {state.offset}\n");
                    state.mode = InflateMode.Match;
                    goto case InflateMode.Match;
                case InflateMode.Match:
                    if (left == 0)
                        goto inf_leave;
                    copy = @out - left;
                    if (state.offset > copy) // copy from window
                    {
                        copy = state.offset - copy;
                        if (copy > state.whave && state.sane != 0)
                        {
                            strm.msg = "invalid distance too far back";
                            state.mode = InflateMode.Bad;
                            break;
                        }
                        if (netUnsafe.IsNullRef(ref
#if NET7_0_OR_GREATER
                        refs.
#endif
                        window))
#if NET7_0_OR_GREATER
                            refs.
#endif
                            window = ref MemoryMarshal.GetReference<byte>(state.window);
                        if (copy > state.wnext)
                        {
                            copy -= state.wnext;
                            from = ref Unsafe.Add(ref
#if NET7_0_OR_GREATER
                            refs.
#endif
                            window, state.wsize - copy);
                        }
                        else
                            from = ref Unsafe.Add(ref
#if NET7_0_OR_GREATER
                            refs.
#endif
                            window, state.wnext - copy);
                        if (copy > state.length)
                            copy = state.length;
                    }
                    else // copy from output
                    {
                        from = ref Unsafe.Subtract(ref put, state.offset);
                        copy = state.length;
                    }
                    if (copy > left)
                        copy = left;
                    left -= copy;
                    state.length -= copy;
                    do
                    {
                        put = from;
                        put = ref Unsafe.Add(ref put, 1U);
                        next_out++;
                        from = ref Unsafe.Add(ref from, 1U);
                    } while (--copy != 0);
                    if (state.length == 0)
                        state.mode = InflateMode.Len;
                    break;
                case InflateMode.Lit:
                    if (left == 0)
                        goto inf_leave;
                    put = (byte)state.length;
                    put = ref Unsafe.Add(ref put, 1U);
                    next_out++;
                    left--;
                    state.mode = InflateMode.Len;
                    break;
                case InflateMode.Check:
                    if (state.wrap != 0)
                    {
                        while (bits < 32)
                        {
                            if (have == 0)
                                goto inf_leave;
                            have--;
                            hold += (uint)next << (int)bits;
                            next = ref Unsafe.Add(ref next, 1U);
                            next_in++;
                            bits += 8;
                        }
                        @out -= left;
                        strm.total_out += @out;
                        state.total += @out;
                        if ((state.wrap & 4) != 0 && @out != 0)
                            strm.Adler = state.check = Adler32.Update(state.check, ref Unsafe.Subtract(ref put, @out), @out);
                        @out = left;
                        if ((state.wrap & 4) != 0 && ZSwap32(hold) != state.check)
                        {
                            strm.msg = "incorrect data check";
                            state.mode = InflateMode.Bad;
                            break;
                        }
                        hold = 0;
                        bits = 0;
                        Trace.Tracev("inflate:   check matches trailer\n");
                    }
                    state.mode = InflateMode.Done;
                    goto case InflateMode.Done;
                case InflateMode.Done:
                    ret = Z_STREAM_END;
                    goto inf_leave;
                case InflateMode.Bad:
                    ret = Z_DATA_ERROR;
                    goto inf_leave;
                case InflateMode.Mem:
                    return Z_MEM_ERROR;
                case InflateMode.Sync:
                default:
                    return Z_STREAM_ERROR;
            }

        inf_leave:
        strm.next_out = next_out;
        strm.avail_out = left;
        strm.next_in = next_in;
        strm.avail_in = have;
        strm.inflateState.hold = hold;
        strm.inflateState.bits = bits;
        if (state.wsize != 0 || @out != strm.avail_out && state.mode < InflateMode.Bad &&
            (state.mode < InflateMode.Check || flush != Z_FINISH))
        {
            try
            {
                UpdateWindow(ref strm, ref put, @out - strm.avail_out, ref
#if NET7_0_OR_GREATER
                refs.
#endif
                window);
            }
            catch (OutOfMemoryException)
            {
                state.mode = InflateMode.Mem;
                return Z_MEM_ERROR;
            }
        }
        @in -= strm.avail_in;
        @out -= strm.avail_out;
        strm.total_in += @in;
        strm.total_out += @out;
        state.total += @out;
        if ((state.wrap & 4) != 0 && @out != 0)
            strm.Adler = state.check = Adler32.Update(state.check, ref Unsafe.Subtract(ref put, @out), @out);
        strm.data_type = (int)state.bits + (state.last != 0 ? 64 : 0) +
            (state.mode == InflateMode.Type ? 128 : 0) +
            (state.mode == InflateMode.Len_ || state.mode == InflateMode.Copy_ ? 256 : 0);
        if ((@in == 0 && @out == 0 || flush == Z_FINISH) && ret == Z_OK)
            ret = Z_BUF_ERROR;

        return ret;
    }

    private static bool InflateStateCheck(ref ZStream strm) =>
        strm.inflateState == null || strm.inflateState.mode < InflateMode.Head
        || strm.inflateState.mode > InflateMode.Sync;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZSwap32(uint q) => (((q) >> 24) & 0xff) + (((q) >> 8) & 0xff00)
        + (((q) & 0xff00) << 8) + (((q) & 0xff) << 24);
}