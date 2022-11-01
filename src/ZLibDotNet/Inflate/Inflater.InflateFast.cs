// Original code and comments Copyright (C) 1995-2017 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    private static unsafe void InflateFast(Unsafe.ZStream strm, uint start)
    {
        InflateState state = strm.inflateState; // have enough input while in < last
        byte* @in = strm.next_in;
        byte* last = @in + (strm.avail_in - 5);
        byte* @out = strm.next_out;
        byte* beg = @out - (start - strm.avail_out); // inflate()'s initial strm->next_out
        byte* end = @out + (strm.avail_out - 257); // while out < end, enough space available
        uint wsize = state.wsize;
        uint whave = state.whave;
        uint wnext = state.wnext;
        uint hold = state.hold;
        uint bits = state.bits;
        ref Code lcode = ref MemoryMarshal.GetReference(state.lencode.AsSpan());
        ref Code dcode = ref MemoryMarshal.GetReference(state.distcode.AsSpan(state.diststart));
        uint lmask = (1U << (int)state.lenbits) - 1;
        uint dmask = (1U << (int)state.distbits) - 1;
        uint op;    // code bits, operation, extra bits, or window position, window bytes to copy
        uint len;   // match length, unused bytes
        uint dist;  // match distance
        byte* from; // where to copy match from

        fixed (byte* window_ = state.window)
        {
            byte* window = window_;
            // decode literals and length/distances until end-of-block or not enough  input data or output space            
            do
            {
                if (bits < 15)
                {
                    hold += (uint)*@in++ << (int)bits;
                    bits += 8;
                    hold += (uint)*@in++ << (int)bits;
                    bits += 8;
                }
                ref Code here = ref netUnsafe.Add(ref lcode, (int)(hold & lmask));
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
                    *@out++ = (byte)here.val;
                }
                else if ((op & 16) != 0) // length base
                {
                    len = here.val;
                    op &= 15; // number of extra bits
                    if (op != 0)
                    {
                        if (bits < op)
                        {
                            hold += (uint)(*@in++ << (int)bits);
                            bits += 8;
                        }
                        len += hold & ((1U << (int)op) - 1);
                        hold >>= (int)op;
                        bits -= op;
                    }
                    Trace.Tracevv($"inflate:         length {len}\n");
                    if (bits < 15)
                    {
                        hold += (uint)*@in++ << (int)bits;
                        bits += 8;
                        hold += (uint)*@in++ << (int)bits;
                        bits += 8;
                    }
                    here = ref netUnsafe.Add(ref dcode, (int)(hold & dmask));
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
                            hold += (uint)(*@in++ << (int)bits);
                            bits += 8;
                            if (bits < op)
                            {
                                hold += (uint)(*@in++ << (int)bits);
                                bits += 8;
                            }
                        }
                        dist += hold & ((1U << (int)op) - 1);
                        hold >>= (int)op;
                        bits -= op;
                        Trace.Tracevv($"inflate:         distance {dist}\n");
                        op = (uint)(@out - beg); // max distance in output
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
                            from = window;
                            if (wnext == 0) // very common case
                            {
                                from += wsize - op;
                                if (op < len) // some from window
                                {
                                    len -= op;
                                    do
                                    {
                                        *@out++ = *from++;
                                    } while (--op != 0);
                                    from = @out - dist; // rest from output
                                }
                            }
                            else if (wnext < op) // wrap around window
                            {
                                from += wsize + wnext - op;
                                op -= wnext;
                                if (op < len) // some from end of window
                                {
                                    len -= op;
                                    do
                                    {
                                        *@out++ = *from++;
                                    } while (--op != 0);
                                    from = window;
                                    if (wnext < len) // some from start of window
                                    {
                                        op = wnext;
                                        len -= op;
                                        do
                                        {
                                            *@out++ = *from++;
                                        } while (--op != 0);
                                        from = @out - dist; // rest from output
                                    }
                                }
                            }
                            else // contiguous in window
                            {
                                from += wnext - op;
                                if (op < len) // some from window
                                {
                                    len -= op;
                                    do
                                    {
                                        *@out++ = *from++;
                                    } while (--op != 0);
                                    from = @out - dist;  // rest from output
                                }
                            }
                            while (len > 2)
                            {
                                *@out++ = *from++;
                                *@out++ = *from++;
                                *@out++ = *from++;
                                len -= 3;
                            }
                            if (len != 0)
                            {
                                *@out++ = *from++;
                                if (len > 1)
                                    *@out++ = *from++;
                            }
                        }
                        else
                        {
                            from = @out - dist; // copy direct from output
                            do // minimum length is three
                            {
                                *@out++ = *from++;
                                *@out++ = *from++;
                                *@out++ = *from++;
                                len -= 3;
                            } while (len > 2);
                            if (len != 0)
                            {
                                *@out++ = *from++;
                                if (len > 1)
                                    *@out++ = *from++;
                            }
                        }
                    }
                    else if ((op & 64) == 0) // 2nd level distance code
                    {
                        here = ref netUnsafe.Add(ref dcode, (int)(here.val + (hold & ((1U << (int)op) - 1))));
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
                    here = ref netUnsafe.Add(ref lcode, (int)(here.val + (hold & ((1U << (int)op) - 1))));
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
            } while (@in < last && @out < end);

            // return unused bytes (on entry, bits < 8, so in won't go too far back)
            len = bits >> 3;
            @in -= len;
            bits -= len << 3;
            hold &= (1U << (int)bits) - 1;

            // update state and return
            strm.next_in = @in;
            strm.next_out = @out;
            strm.avail_in = (uint)(@in < last ? 5 + (last - @in) : 5 - (@in - last));
            strm.avail_out = (uint)(@out < end ? 257 + (end - @out) : 257 - (@out - end));

            state.hold = hold;
            state.bits = bits;
        }
    }
}