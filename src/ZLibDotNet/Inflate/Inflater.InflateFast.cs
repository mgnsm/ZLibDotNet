// Original code and comments Copyright (C) 1995-2017 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    private static void InflateFast(ZStream strm, uint start)
    {
        InflateState state = strm.inflateState;
        int last = strm.next_in + ((int)strm.avail_in - 5); // have enough input while in < last
        int beg = strm.next_out - (int)(start - strm.avail_out); // inflate()'s initial strm.next_out
        int end = strm.next_out + ((int)strm.avail_out - 257); // while out < end, enough space available
        uint wsize = state.wsize;
        uint whave = state.whave;
        uint wnext = state.wnext;
        uint hold = state.hold;
        uint bits = state.bits;
        uint lmask = (1U << state.lenbits) - 1;
        uint dmask = (1U << state.distbits) - 1;
        uint op;    // code bits, operation, extra bits, or window position, window bytes to copy
        uint len;   // match length, unused bytes
        uint dist;  // match distance

        ref byte @in = ref MemoryMarshal.GetReference(strm._input.AsSpan(strm.next_in));
        ref byte @out = ref MemoryMarshal.GetReference(strm._output.AsSpan(strm.next_out));
        ref byte window = ref MemoryMarshal.GetReference(state.window.AsSpan());
        ref Code lcode = ref MemoryMarshal.GetReference(state.lencode.AsSpan());
        ref Code dcode = ref MemoryMarshal.GetReference(state.distcode.AsSpan(state.diststart));
        ref byte from = ref Unsafe.NullRef<byte>(); // where to copy match from
        ref Code here = ref Unsafe.NullRef<Code>(); // retrieved table entry

        // decode literals and length/distances until end-of-block or not enough  input data or output space            
        do
        {
            if (bits < 15)
            {
                hold += (uint)@in << (int)bits;
                @in = ref Unsafe.Add(ref @in, 1);
                bits += 8;
                hold += (uint)@in << (int)bits;
                @in = ref Unsafe.Add(ref @in, 1);
                bits += 8;
                strm.next_in += 2;
            }
            here = ref Unsafe.Add(ref lcode, (int)(hold & lmask));
        dolen:
            op = here.bits;
            hold >>= (int)op;
            bits -= op;
            op = here.op;
            if (op == 0) // literal
            {
                Trace.Tracevv(here.val >= 0x20 && here.val < 0x7f ?
                    $"inflate:         literal '{Convert.ToChar(here.val)}'\n" :
                    $"inflate:         literal 0x{here.val:X2}\n");
                @out = (byte)here.val;
                @out = ref Unsafe.Add(ref @out, 1);
                strm.next_out++;
            }
            else if ((op & 16) != 0) // length base
            {
                len = here.val;
                op &= 15; // number of extra bits
                if (op != 0)
                {
                    if (bits < op)
                    {
                        hold += (uint)(@in << (int)bits);
                        @in = ref Unsafe.Add(ref @in, 1);
                        bits += 8;
                        strm.next_in++;
                    }
                    len += hold & ((1U << (int)op) - 1);
                    hold >>= (int)op;
                    bits -= op;
                }
                Trace.Tracevv($"inflate:         length {len}\n");
                if (bits < 15)
                {
                    hold += (uint)@in << (int)bits;
                    @in = ref Unsafe.Add(ref @in, 1);
                    bits += 8;
                    hold += (uint)@in << (int)bits;
                    @in = ref Unsafe.Add(ref @in, 1);
                    bits += 8;
                    strm.next_in += 2;
                }
                here = ref Unsafe.Add(ref dcode, (int)(hold & dmask));
            dodist:
                op = here.bits;
                hold >>= (int)op;
                bits -= op;
                op = here.op;
                if ((op & 16) != 0) // distance base
                {
                    dist = here.val;
                    op &= 15; // number of extra bits
                    if (bits < op)
                    {
                        hold += (uint)(@in << (int)bits);
                        @in = ref Unsafe.Add(ref @in, 1);
                        bits += 8;
                        strm.next_in++;
                        if (bits < op)
                        {
                            hold += (uint)(@in << (int)bits);
                            @in = ref Unsafe.Add(ref @in, 1);
                            bits += 8;
                            strm.next_in++;
                        }
                    }
                    dist += hold & ((1U << (int)op) - 1);
                    hold >>= (int)op;
                    bits -= op;
                    Trace.Tracevv($"inflate:         distance {dist}\n");
                    op = (uint)(strm.next_out - beg); // max distance in output
                    if (dist > op)
                    {
                        op = dist - op; // distance back in window
                        if (op > whave)
                        {
                            if (state.sane != 0)
                            {
                                strm.msg = "invalid distance too far back";
                                state.mode = InflateMode.Bad;
                                break;
                            }
                        }
                        from = ref window;
                        if (wnext == 0) // very common case
                        {
                            from = ref Unsafe.Add(ref from, (int)(wsize - op));
                            if (op < len) // some from window
                            {
                                len -= op;
                                do
                                {
                                    @out = from;
                                    @out = ref Unsafe.Add(ref @out, 1);
                                    from = ref Unsafe.Add(ref from, 1);
                                    strm.next_out++;
                                } while (--op != 0);
                                from = ref Unsafe.Subtract(ref @out, (int)dist); // rest from output
                            }
                        }
                        else if (wnext < op) // wrap around window
                        {
                            from = ref Unsafe.Add(ref from, (int)(wsize + wnext - op));
                            op -= wnext;
                            if (op < len) // some from end of window
                            {
                                len -= op;
                                do
                                {
                                    @out = from;
                                    @out = ref Unsafe.Add(ref @out, 1);
                                    from = ref Unsafe.Add(ref from, 1);
                                    strm.next_out++;
                                } while (--op != 0);
                                from = ref window;
                                if (wnext < len) // some from start of window
                                {
                                    op = wnext;
                                    len -= op;
                                    do
                                    {
                                        @out = from;
                                        @out = ref Unsafe.Add(ref @out, 1);
                                        from = ref Unsafe.Add(ref from, 1);
                                        strm.next_out++;
                                    } while (--op != 0);
                                    from = ref Unsafe.Subtract(ref @out, (int)dist); // rest from output
                                }
                            }
                        }
                        else // contiguous in window
                        {
                            from = ref Unsafe.Add(ref from, (int)(wnext - op));
                            if (op < len) // some from window
                            {
                                len -= op;
                                do
                                {
                                    @out = from;
                                    @out = ref Unsafe.Add(ref @out, 1);
                                    from = ref Unsafe.Add(ref from, 1);
                                    strm.next_out++;
                                } while (--op != 0);
                                from = ref Unsafe.Subtract(ref @out, (int)dist); // rest from output
                            }
                        }
                        while (len > 2)
                        {
                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1);
                            from = ref Unsafe.Add(ref from, 1);

                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1);
                            from = ref Unsafe.Add(ref from, 1);

                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1);
                            from = ref Unsafe.Add(ref from, 1);
                            strm.next_out += 3;
                            len -= 3;
                        }
                        if (len != 0)
                        {
                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1);
                            from = ref Unsafe.Add(ref from, 1);
                            strm.next_out++;
                            if (len > 1)
                            {
                                @out = from;
                                @out = ref Unsafe.Add(ref @out, 1);
                                from = ref Unsafe.Add(ref from, 1);
                                strm.next_out++;
                            }
                        }
                    }
                    else
                    {
                        from = ref Unsafe.Subtract(ref @out, (int)dist); // copy direct from output
                        do // minimum length is three
                        {
                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1);
                            from = ref Unsafe.Add(ref from, 1);

                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1);
                            from = ref Unsafe.Add(ref from, 1);

                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1);
                            from = ref Unsafe.Add(ref from, 1);

                            len -= 3;
                            strm.next_out += 3;
                        } while (len > 2);
                        if (len != 0)
                        {
                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1);
                            from = ref Unsafe.Add(ref from, 1);
                            strm.next_out++;
                            if (len > 1)
                            {
                                @out = from;
                                @out = ref Unsafe.Add(ref @out, 1);
                                from = ref Unsafe.Add(ref from, 1);
                                strm.next_out++;
                            }
                        }
                    }
                }
                else if ((op & 64) == 0) // 2nd level distance code
                {
                    here = ref Unsafe.Add(ref dcode, (int)(here.val + (hold & ((1U << (int)op) - 1))));
                    goto dodist;
                }
                else
                {
                    strm.msg = "invalid distance code";
                    state.mode = InflateMode.Bad;
                    break;
                }
            }
            else if ((op & 64) == 0) // 2nd level length code
            {
                here = ref Unsafe.Add(ref lcode, (int)(here.val + (hold & ((1U << (int)op) - 1))));
                goto dolen;
            }
            else if ((op & 32) != 0) // end-of-block
            {
                Trace.Tracevv("inflate:         end of block\n");
                state.mode = InflateMode.Type;
                break;
            }
            else
            {
                strm.msg = "invalid literal/length code";
                state.mode = InflateMode.Bad;
                break;
            }
        } while (strm.next_in < last && strm.next_out < end);

        // return unused bytes (on entry, bits < 8, so in won't go too far back)
        len = bits >> 3;
        @in = ref Unsafe.Subtract(ref @in, (int)len);
        strm.next_in -= (int)len;
        bits -= len << 3;
        hold &= (1U << (int)bits) - 1;

        // update state and return
        strm.avail_in = (uint)(strm.next_in < last ? 5 + (last - strm.next_in) : 5 - (strm.next_in - last));
        strm.avail_out = (uint)(strm.next_out < end ? 257 + (end - strm.next_out) : 257 - (strm.next_out - end));

        state.hold = hold;
        state.bits = bits;
    }
}