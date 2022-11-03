// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    private static void UpdateWindow(Unsafe.ZStream strm, ref byte end, uint copy)
    {
        InflateState state = strm.inflateState;

        // if it hasn't been done already, allocate space for the window
        state.window ??= ArrayPool<byte>.Shared.Rent(1 << (int)state.wbits);

        // if window not in use yet, initialize
        if (state.wsize == 0)
        {
            state.wsize = 1U << (int)state.wbits;
            state.wnext = 0;
            state.whave = 0;
        }

        uint dist;
        ref byte window = ref MemoryMarshal.GetReference(state.window.AsSpan());
        // copy state.wsize or less output bytes into the circular window
        if (copy >= state.wsize)
        {
            netUnsafe.CopyBlockUnaligned(ref window, ref netUnsafe.Subtract(ref end, (int)state.wsize), state.wsize);
            state.wnext = 0;
            state.whave = state.wsize;
        }
        else
        {
            dist = state.wsize - state.wnext;
            if (dist > copy)
                dist = copy;
            netUnsafe.CopyBlockUnaligned(ref netUnsafe.Add(ref window, (int)state.wnext), ref netUnsafe.Subtract(ref end, (int)copy), dist);
            copy -= dist;
            if (copy != 0)
            {
                netUnsafe.CopyBlockUnaligned(ref window, ref netUnsafe.Subtract(ref end, (int)copy), copy);
                state.wnext = copy;
                state.whave = state.wsize;
            }
            else
            {
                state.wnext += dist;
                if (state.wnext == state.wsize)
                    state.wnext = 0;
                if (state.whave < state.wsize)
                    state.whave += dist;
            }
        }
    }
}