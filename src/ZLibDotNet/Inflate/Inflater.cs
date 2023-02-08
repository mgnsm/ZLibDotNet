// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    private static readonly Code[] s_lenfix = new Code[512]
    {
        new Code(96,7,0), new Code(0,8,80), new Code(0,8,16), new Code(20,8,115), new Code(18,7,31), new Code(0,8,112), new Code(0,8,48),
        new Code(0,9,192), new Code(16,7,10), new Code(0,8,96), new Code(0,8,32), new Code(0,9,160), new Code(0,8,0), new Code(0,8,128),
        new Code(0,8,64), new Code(0,9,224), new Code(16,7,6), new Code(0,8,88), new Code(0,8,24), new Code(0,9,144), new Code(19,7,59),
        new Code(0,8,120), new Code(0,8,56), new Code(0,9,208), new Code(17,7,17), new Code(0,8,104), new Code(0,8,40), new Code(0,9,176),
        new Code(0,8,8), new Code(0,8,136), new Code(0,8,72), new Code(0,9,240), new Code(16,7,4), new Code(0,8,84), new Code(0,8,20),
        new Code(21,8,227), new Code(19,7,43), new Code(0,8,116), new Code(0,8,52), new Code(0,9,200), new Code(17,7,13), new Code(0,8,100),
        new Code(0,8,36), new Code(0,9,168), new Code(0,8,4), new Code(0,8,132), new Code(0,8,68), new Code(0,9,232), new Code(16,7,8),
        new Code(0,8,92), new Code(0,8,28), new Code(0,9,152), new Code(20,7,83), new Code(0,8,124), new Code(0,8,60), new Code(0,9,216),
        new Code(18,7,23), new Code(0,8,108), new Code(0,8,44), new Code(0,9,184), new Code(0,8,12), new Code(0,8,140), new Code(0,8,76),
        new Code(0,9,248), new Code(16,7,3), new Code(0,8,82), new Code(0,8,18), new Code(21,8,163), new Code(19,7,35), new Code(0,8,114),
        new Code(0,8,50), new Code(0,9,196), new Code(17,7,11), new Code(0,8,98), new Code(0,8,34), new Code(0,9,164), new Code(0,8,2),
        new Code(0,8,130), new Code(0,8,66), new Code(0,9,228), new Code(16,7,7), new Code(0,8,90), new Code(0,8,26), new Code(0,9,148),
        new Code(20,7,67), new Code(0,8,122), new Code(0,8,58), new Code(0,9,212), new Code(18,7,19), new Code(0,8,106), new Code(0,8,42),
        new Code(0,9,180), new Code(0,8,10), new Code(0,8,138), new Code(0,8,74), new Code(0,9,244), new Code(16,7,5), new Code(0,8,86),
        new Code(0,8,22), new Code(64,8,0), new Code(19,7,51), new Code(0,8,118), new Code(0,8,54), new Code(0,9,204), new Code(17,7,15),
        new Code(0,8,102), new Code(0,8,38), new Code(0,9,172), new Code(0,8,6), new Code(0,8,134), new Code(0,8,70), new Code(0,9,236),
        new Code(16,7,9), new Code(0,8,94), new Code(0,8,30), new Code(0,9,156), new Code(20,7,99), new Code(0,8,126), new Code(0,8,62),
        new Code(0,9,220), new Code(18,7,27), new Code(0,8,110), new Code(0,8,46), new Code(0,9,188), new Code(0,8,14), new Code(0,8,142),
        new Code(0,8,78), new Code(0,9,252), new Code(96,7,0), new Code(0,8,81), new Code(0,8,17), new Code(21,8,131), new Code(18,7,31),
        new Code(0,8,113), new Code(0,8,49), new Code(0,9,194), new Code(16,7,10), new Code(0,8,97), new Code(0,8,33), new Code(0,9,162),
        new Code(0,8,1), new Code(0,8,129), new Code(0,8,65), new Code(0,9,226), new Code(16,7,6), new Code(0,8,89), new Code(0,8,25),
        new Code(0,9,146), new Code(19,7,59), new Code(0,8,121), new Code(0,8,57), new Code(0,9,210), new Code(17,7,17), new Code(0,8,105),
        new Code(0,8,41), new Code(0,9,178), new Code(0,8,9), new Code(0,8,137), new Code(0,8,73), new Code(0,9,242), new Code(16,7,4),
        new Code(0,8,85), new Code(0,8,21), new Code(16,8,258), new Code(19,7,43), new Code(0,8,117), new Code(0,8,53), new Code(0,9,202),
        new Code(17,7,13), new Code(0,8,101), new Code(0,8,37), new Code(0,9,170), new Code(0,8,5), new Code(0,8,133), new Code(0,8,69),
        new Code(0,9,234), new Code(16,7,8), new Code(0,8,93), new Code(0,8,29), new Code(0,9,154), new Code(20,7,83), new Code(0,8,125),
        new Code(0,8,61), new Code(0,9,218), new Code(18,7,23), new Code(0,8,109), new Code(0,8,45), new Code(0,9,186), new Code(0,8,13),
        new Code(0,8,141), new Code(0,8,77), new Code(0,9,250), new Code(16,7,3), new Code(0,8,83), new Code(0,8,19), new Code(21,8,195),
        new Code(19,7,35), new Code(0,8,115), new Code(0,8,51), new Code(0,9,198), new Code(17,7,11), new Code(0,8,99), new Code(0,8,35),
        new Code(0,9,166), new Code(0,8,3), new Code(0,8,131), new Code(0,8,67), new Code(0,9,230), new Code(16,7,7), new Code(0,8,91),
        new Code(0,8,27), new Code(0,9,150), new Code(20,7,67), new Code(0,8,123), new Code(0,8,59), new Code(0,9,214), new Code(18,7,19),
        new Code(0,8,107), new Code(0,8,43), new Code(0,9,182), new Code(0,8,11), new Code(0,8,139), new Code(0,8,75), new Code(0,9,246),
        new Code(16,7,5), new Code(0,8,87), new Code(0,8,23), new Code(64,8,0), new Code(19,7,51), new Code(0,8,119), new Code(0,8,55),
        new Code(0,9,206), new Code(17,7,15), new Code(0,8,103), new Code(0,8,39), new Code(0,9,174), new Code(0,8,7), new Code(0,8,135),
        new Code(0,8,71), new Code(0,9,238), new Code(16,7,9), new Code(0,8,95), new Code(0,8,31), new Code(0,9,158), new Code(20,7,99),
        new Code(0,8,127), new Code(0,8,63), new Code(0,9,222), new Code(18,7,27), new Code(0,8,111), new Code(0,8,47), new Code(0,9,190),
        new Code(0,8,15), new Code(0,8,143), new Code(0,8,79), new Code(0,9,254), new Code(96,7,0), new Code(0,8,80), new Code(0,8,16),
        new Code(20,8,115), new Code(18,7,31), new Code(0,8,112), new Code(0,8,48), new Code(0,9,193), new Code(16,7,10), new Code(0,8,96),
        new Code(0,8,32), new Code(0,9,161), new Code(0,8,0), new Code(0,8,128), new Code(0,8,64), new Code(0,9,225), new Code(16,7,6),
        new Code(0,8,88), new Code(0,8,24), new Code(0,9,145), new Code(19,7,59), new Code(0,8,120), new Code(0,8,56), new Code(0,9,209),
        new Code(17,7,17), new Code(0,8,104), new Code(0,8,40), new Code(0,9,177), new Code(0,8,8), new Code(0,8,136), new Code(0,8,72),
        new Code(0,9,241), new Code(16,7,4), new Code(0,8,84), new Code(0,8,20), new Code(21,8,227), new Code(19,7,43), new Code(0,8,116),
        new Code(0,8,52), new Code(0,9,201), new Code(17,7,13), new Code(0,8,100), new Code(0,8,36), new Code(0,9,169), new Code(0,8,4),
        new Code(0,8,132), new Code(0,8,68), new Code(0,9,233), new Code(16,7,8), new Code(0,8,92), new Code(0,8,28), new Code(0,9,153),
        new Code(20,7,83), new Code(0,8,124), new Code(0,8,60), new Code(0,9,217), new Code(18,7,23), new Code(0,8,108), new Code(0,8,44),
        new Code(0,9,185), new Code(0,8,12), new Code(0,8,140), new Code(0,8,76), new Code(0,9,249), new Code(16,7,3), new Code(0,8,82),
        new Code(0,8,18), new Code(21,8,163), new Code(19,7,35), new Code(0,8,114), new Code(0,8,50), new Code(0,9,197), new Code(17,7,11),
        new Code(0,8,98), new Code(0,8,34), new Code(0,9,165), new Code(0,8,2), new Code(0,8,130), new Code(0,8,66), new Code(0,9,229),
        new Code(16,7,7), new Code(0,8,90), new Code(0,8,26), new Code(0,9,149), new Code(20,7,67), new Code(0,8,122), new Code(0,8,58),
        new Code(0,9,213), new Code(18,7,19), new Code(0,8,106), new Code(0,8,42), new Code(0,9,181), new Code(0,8,10), new Code(0,8,138),
        new Code(0,8,74), new Code(0,9,245), new Code(16,7,5), new Code(0,8,86), new Code(0,8,22), new Code(64,8,0), new Code(19,7,51),
        new Code(0,8,118), new Code(0,8,54), new Code(0,9,205), new Code(17,7,15), new Code(0,8,102), new Code(0,8,38), new Code(0,9,173),
        new Code(0,8,6), new Code(0,8,134), new Code(0,8,70), new Code(0,9,237), new Code(16,7,9), new Code(0,8,94), new Code(0,8,30),
        new Code(0,9,157), new Code(20,7,99), new Code(0,8,126), new Code(0,8,62), new Code(0,9,221), new Code(18,7,27), new Code(0,8,110),
        new Code(0,8,46), new Code(0,9,189), new Code(0,8,14), new Code(0,8,142), new Code(0,8,78), new Code(0,9,253), new Code(96,7,0),
        new Code(0,8,81), new Code(0,8,17), new Code(21,8,131), new Code(18,7,31), new Code(0,8,113), new Code(0,8,49), new Code(0,9,195),
        new Code(16,7,10), new Code(0,8,97), new Code(0,8,33), new Code(0,9,163), new Code(0,8,1), new Code(0,8,129), new Code(0,8,65),
        new Code(0,9,227), new Code(16,7,6), new Code(0,8,89), new Code(0,8,25), new Code(0,9,147), new Code(19,7,59), new Code(0,8,121),
        new Code(0,8,57), new Code(0,9,211), new Code(17,7,17), new Code(0,8,105), new Code(0,8,41), new Code(0,9,179), new Code(0,8,9),
        new Code(0,8,137), new Code(0,8,73), new Code(0,9,243), new Code(16,7,4), new Code(0,8,85), new Code(0,8,21), new Code(16,8,258),
        new Code(19,7,43), new Code(0,8,117), new Code(0,8,53), new Code(0,9,203), new Code(17,7,13), new Code(0,8,101), new Code(0,8,37),
        new Code(0,9,171), new Code(0,8,5), new Code(0,8,133), new Code(0,8,69), new Code(0,9,235), new Code(16,7,8), new Code(0,8,93),
        new Code(0,8,29), new Code(0,9,155), new Code(20,7,83), new Code(0,8,125), new Code(0,8,61), new Code(0,9,219), new Code(18,7,23),
        new Code(0,8,109), new Code(0,8,45), new Code(0,9,187), new Code(0,8,13), new Code(0,8,141), new Code(0,8,77), new Code(0,9,251),
        new Code(16,7,3), new Code(0,8,83), new Code(0,8,19), new Code(21,8,195), new Code(19,7,35), new Code(0,8,115), new Code(0,8,51),
        new Code(0,9,199), new Code(17,7,11), new Code(0,8,99), new Code(0,8,35), new Code(0,9,167), new Code(0,8,3), new Code(0,8,131),
        new Code(0,8,67), new Code(0,9,231), new Code(16,7,7), new Code(0,8,91), new Code(0,8,27), new Code(0,9,151), new Code(20,7,67),
        new Code(0,8,123), new Code(0,8,59), new Code(0,9,215), new Code(18,7,19), new Code(0,8,107), new Code(0,8,43), new Code(0,9,183),
        new Code(0,8,11), new Code(0,8,139), new Code(0,8,75), new Code(0,9,247), new Code(16,7,5), new Code(0,8,87), new Code(0,8,23),
        new Code(64,8,0), new Code(19,7,51), new Code(0,8,119), new Code(0,8,55), new Code(0,9,207), new Code(17,7,15), new Code(0,8,103),
        new Code(0,8,39), new Code(0,9,175), new Code(0,8,7), new Code(0,8,135), new Code(0,8,71), new Code(0,9,239), new Code(16,7,9),
        new Code(0,8,95), new Code(0,8,31), new Code(0,9,159), new Code(20,7,99), new Code(0,8,127), new Code(0,8,63), new Code(0,9,223),
        new Code(18,7,27), new Code(0,8,111), new Code(0,8,47), new Code(0,9,191), new Code(0,8,15), new Code(0,8,143), new Code(0,8,79),
        new Code(0,9,255)
    };

    private static readonly Code[] s_distfix = new Code[32]
    {
        new Code(16,5,1), new Code(23,5,257), new Code(19,5,17), new Code(27,5,4097), new Code(17,5,5), new Code(25,5,1025),
        new Code(21,5,65), new Code(29,5,16385), new Code(16,5,3), new Code(24,5,513), new Code(20,5,33), new Code(28,5,8193),
        new Code(18,5,9), new Code(26,5,2049), new Code(22,5,129), new Code(64,5,0), new Code(16,5,2), new Code(23,5,385),
        new Code(19,5,25), new Code(27,5,6145), new Code(17,5,7), new Code(25,5,1537), new Code(21,5,97), new Code(29,5,24577),
        new Code(16,5,4), new Code(24,5,769), new Code(20,5,49), new Code(28,5,12289), new Code(18,5,13), new Code(26,5,3073),
        new Code(22,5,193), new Code(64,5,0)
    };

    // permutation of code lengths
    private static readonly ushort[] s_order = new ushort[19] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

    internal static int Inflate(ZStream strm, int flush)
    {
        if (InflateStateCheck(strm)
            || strm._output == null
            || strm._input == null && strm.avail_in != 0)
            return Z_STREAM_ERROR;

        InflateState state = strm.inflateState;
        if (state.mode == InflateMode.Type) // Skip check
            state.mode = InflateMode.Typedo;

        ref byte next = ref MemoryMarshal.GetReference(strm._input.AsSpan((int)strm.next_in)); // next input
        ref byte put = ref MemoryMarshal.GetReference(strm._output.AsSpan((int)strm.next_out)); // next output
        ref byte from = ref netUnsafe.NullRef<byte>(); // where to copy match bytes from
        ref Code codes = ref MemoryMarshal.GetReference(state.codes.AsSpan());
        ref ushort lens = ref MemoryMarshal.GetReference(state.lens.AsSpan());
        ref ushort work = ref MemoryMarshal.GetReference(state.work.AsSpan());
        ref byte window = ref MemoryMarshal.GetReference(state.window.AsSpan());
        ref Code lencode = ref netUnsafe.NullRef<Code>();
        ref ushort order = ref MemoryMarshal.GetReference(s_order.AsSpan());
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
                        Unsafe.Add(ref lens, (uint)Unsafe.Add(ref order, state.have++)) = (ushort)(hold & ((1U << (3)) - 1));
                        hold >>= 3;
                        bits -= 3;
                    }
                    while (state.have < 19)
                        Unsafe.Add(ref lens, (uint)Unsafe.Add(ref order, state.have++)) = 0;
                    state.next = 0;
                    state.lencode = state.codes;
                    state.lenbits = 7;
                    ret = InflateTable(CodeType.Codes, ref lens, 19, ref codes, ref state.lenbits, ref work, ref state.next);
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
                    lencode = ref MemoryMarshal.GetReference(state.lencode.AsSpan());
                    while (state.have < state.nlen + state.ndist)
                    {
                        for (; ; )
                        {
                            here = Unsafe.Add(ref lencode, hold & ((1U << (state.lenbits)) - 1));
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
                            Unsafe.Add(ref lens, state.have++) = here.val;
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
                                len = Unsafe.Add(ref lens, state.have - 1);
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
                                Unsafe.Add(ref lens, state.have++) = (ushort)len;
                        }
                    }

                    // handle error breaks in while
                    if (state.mode == InflateMode.Bad)
                        break;

                    // check for end-of-block code (better have one)
                    if (Unsafe.Add(ref lens, 256U) == 0)
                    {
                        strm.msg = "invalid code -- missing end-of-block";
                        state.mode = InflateMode.Bad;
                        break;
                    }

                    // build code tables
                    state.next = 0;
                    state.lencode = state.codes;
                    state.lenbits = 9;
                    ret = InflateTable(CodeType.Lens, ref lens, state.nlen, ref codes, ref state.lenbits, ref work, ref state.next);
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
                    ret = InflateTable(CodeType.Dists, ref Unsafe.Add(ref lens, state.nlen), state.ndist, ref codes, ref state.distbits, ref work, ref state.next);
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
                    if (have >= 6 && left >= 258)
                    {
                        strm.next_out = next_out;
                        strm.avail_out = left;
                        strm.next_in = next_in;
                        strm.avail_in = have;
                        strm.inflateState.hold = hold;
                        strm.inflateState.bits = bits;
                        InflateFast(strm, @out);
                        put = ref MemoryMarshal.GetReference(strm._output.AsSpan((int)strm.next_out));
                        next_out = strm.next_out;
                        left = strm.avail_out;
                        next = ref MemoryMarshal.GetReference(strm._input.AsSpan((int)strm.next_in));
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
                    lencode = ref MemoryMarshal.GetReference(state.lencode.AsSpan());
                    for (; ; )
                    {
                        here = Unsafe.Add(ref lencode, hold & ((1U << (state.lenbits)) - 1));
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
                            here = Unsafe.Add(ref lencode, last.val + ((hold & (1U << last.bits + last.op - 1)) >> last.bits));
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
                    ref Code distcode = ref MemoryMarshal.GetReference(state.distcode.AsSpan());
                    for (; ; )
                    {
                        here = Unsafe.Add(ref distcode, state.diststart + (hold & ((1U << (state.distbits)) - 1)));
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
                            here = Unsafe.Add(ref distcode, state.diststart + last.val +
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
                        if (copy > state.wnext)
                        {
                            copy -= state.wnext;
                            from = ref Unsafe.Add(ref window, state.wsize - copy);
                        }
                        else
                            from = ref Unsafe.Add(ref window, state.wnext - copy);
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
                UpdateWindow(strm, ref put, @out - strm.avail_out);
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

    private static bool InflateStateCheck(ZStream strm) =>
        strm == null || strm.inflateState == null || strm.inflateState.strm != strm
            || strm.inflateState.mode < InflateMode.Head || strm.inflateState.mode > InflateMode.Sync;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZSwap32(uint q) => (((q) >> 24) & 0xff) + (((q) >> 8) & 0xff00)
        + (((q) & 0xff00) << 8) + (((q) & 0xff) << 24);
}