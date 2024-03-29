﻿// Original code and comments Copyright (C) 1995-2017 Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

using System;
#if !NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    private static void InflateFast(ref ZStream strm, uint start, ref byte window, ref Code lcode, ref Code dcode)
    {
        InflateState state = strm.inflateState;
        uint last = strm.next_in + (strm.avail_in - 5);      // have enough input while in < last
        uint beg = strm.next_out - (start - strm.avail_out); // inflate()'s initial strm.next_out
        uint end = strm.next_out + (strm.avail_out - 257);   // while out < end, enough space available
        uint wsize = state.wsize;
        uint whave = state.whave;
        uint wnext = state.wnext;
        uint hold = state.hold;
        uint bits = state.bits;
        uint lmask = (1U << state.lenbits) - 1;
        uint dmask = (1U << state.distbits) - 1;
        uint len;   // match length, unused bytes

        ref byte @in = ref
#if NET7_0_OR_GREATER
            Unsafe.Add(ref strm.input_ptr, strm.next_in);
#else
            MemoryMarshal.GetReference(strm._input.Slice((int)strm.next_in));
#endif
        ref byte @out = ref
#if NET7_0_OR_GREATER
            Unsafe.Add(ref strm.output_ptr, strm.next_out);
#else
            MemoryMarshal.GetReference(strm._output.Slice((int)strm.next_out));
#endif

        // decode literals and length/distances until end-of-block or not enough input data or output space            
        do
        {
            if (bits < 15)
            {
                hold += (uint)@in << (int)bits;
                @in = ref Unsafe.Add(ref @in, 1U);
                bits += 8;
                hold += (uint)@in << (int)bits;
                @in = ref Unsafe.Add(ref @in, 1U);
                bits += 8;
                strm.next_in += 2;
            }
            ref Code here = ref Unsafe.Add(ref lcode, hold & lmask); // retrieved table entry
        dolen:
            uint op = here.bits; // code bits, operation, extra bits, or window position, window bytes to copy
            hold >>= (int)op;
            bits -= op;
            op = here.op;
            if (op == 0) // literal
            {
                Trace.Tracevv(here.val >= 0x20 && here.val < 0x7f ?
                    $"inflate:         literal '{Convert.ToChar(here.val)}'\n" :
                    $"inflate:         literal 0x{here.val:X2}\n");
                @out = (byte)here.val;
                @out = ref Unsafe.Add(ref @out, 1U);
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
                        @in = ref Unsafe.Add(ref @in, 1U);
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
                    @in = ref Unsafe.Add(ref @in, 1U);
                    bits += 8;
                    hold += (uint)@in << (int)bits;
                    @in = ref Unsafe.Add(ref @in, 1U);
                    bits += 8;
                    strm.next_in += 2;
                }
                here = ref Unsafe.Add(ref dcode, hold & dmask);
            dodist:
                op = here.bits;
                hold >>= (int)op;
                bits -= op;
                op = here.op;
                if ((op & 16) != 0) // distance base
                {
                    uint dist = here.val; // match distance
                    op &= 15; // number of extra bits
                    if (bits < op)
                    {
                        hold += (uint)(@in << (int)bits);
                        @in = ref Unsafe.Add(ref @in, 1U);
                        bits += 8;
                        strm.next_in++;
                        if (bits < op)
                        {
                            hold += (uint)(@in << (int)bits);
                            @in = ref Unsafe.Add(ref @in, 1U);
                            bits += 8;
                            strm.next_in++;
                        }
                    }
                    dist += hold & ((1U << (int)op) - 1);
                    hold >>= (int)op;
                    bits -= op;
                    Trace.Tracevv($"inflate:         distance {dist}\n");
                    op = strm.next_out - beg; // max distance in output
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
                        ref byte from = ref window; // where to copy match from
                        if (wnext == 0) // very common case
                        {
                            from = ref Unsafe.Add(ref from, wsize - op);
                            if (op < len) // some from window
                            {
                                len -= op;
                                do
                                {
                                    @out = from;
                                    @out = ref Unsafe.Add(ref @out, 1U);
                                    from = ref Unsafe.Add(ref from, 1U);
                                    strm.next_out++;
                                } while (--op != 0);
                                from = ref Unsafe.Subtract(ref @out, dist); // rest from output
                            }
                        }
                        else if (wnext < op) // wrap around window
                        {
                            from = ref Unsafe.Add(ref from, wsize + wnext - op);
                            op -= wnext;
                            if (op < len) // some from end of window
                            {
                                len -= op;
                                do
                                {
                                    @out = from;
                                    @out = ref Unsafe.Add(ref @out, 1U);
                                    from = ref Unsafe.Add(ref from, 1U);
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
                                        @out = ref Unsafe.Add(ref @out, 1U);
                                        from = ref Unsafe.Add(ref from, 1U);
                                        strm.next_out++;
                                    } while (--op != 0);
                                    from = ref Unsafe.Subtract(ref @out, dist); // rest from output
                                }
                            }
                        }
                        else // contiguous in window
                        {
                            from = ref Unsafe.Add(ref from, wnext - op);
                            if (op < len) // some from window
                            {
                                len -= op;
                                do
                                {
                                    @out = from;
                                    @out = ref Unsafe.Add(ref @out, 1U);
                                    from = ref Unsafe.Add(ref from, 1U);
                                    strm.next_out++;
                                } while (--op != 0);
                                from = ref Unsafe.Subtract(ref @out, dist); // rest from output
                            }
                        }
                        while (len > 2)
                        {
                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1U);
                            from = ref Unsafe.Add(ref from, 1U);

                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1U);
                            from = ref Unsafe.Add(ref from, 1U);

                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1U);
                            from = ref Unsafe.Add(ref from, 1U);
                            strm.next_out += 3;
                            len -= 3;
                        }
                        if (len != 0)
                        {
                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1U);
                            from = ref Unsafe.Add(ref from, 1U);
                            strm.next_out++;
                            if (len > 1)
                            {
                                @out = from;
                                @out = ref Unsafe.Add(ref @out, 1U);
                                from = ref Unsafe.Add(ref from, 1U);
                                strm.next_out++;
                            }
                        }
                    }
                    else
                    {
                        ref byte from = ref Unsafe.Subtract(ref @out, dist); // copy direct from output
                        do // minimum length is three
                        {
                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1U);
                            from = ref Unsafe.Add(ref from, 1U);

                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1U);
                            from = ref Unsafe.Add(ref from, 1U);

                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1U);
                            from = ref Unsafe.Add(ref from, 1U);

                            len -= 3;
                            strm.next_out += 3;
                        } while (len > 2);
                        if (len != 0)
                        {
                            @out = from;
                            @out = ref Unsafe.Add(ref @out, 1U);
                            from = ref Unsafe.Add(ref from, 1U);
                            strm.next_out++;
                            if (len > 1)
                            {
                                @out = from;
                                @out = ref Unsafe.Add(ref @out, 1U);
                                from = ref Unsafe.Add(ref from, 1U);
                                strm.next_out++;
                            }
                        }
                    }
                }
                else if ((op & 64) == 0) // 2nd level distance code
                {
                    here = ref Unsafe.Add(ref dcode, here.val + (hold & ((1U << (int)op) - 1)));
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
                here = ref Unsafe.Add(ref lcode, here.val + (hold & ((1U << (int)op) - 1)));
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
        @in = ref Unsafe.Subtract(ref @in, len);
        strm.next_in -= len;
        bits -= len << 3;
        hold &= (1U << (int)bits) - 1;

        // update state and return
        strm.avail_in = strm.next_in < last ? 5 + (last - strm.next_in) : 5 - (strm.next_in - last);
        strm.avail_out = strm.next_out < end ? 257 + (end - strm.next_out) : 257 - (strm.next_out - end);

        state.hold = hold;
        state.bits = bits;
    }
}