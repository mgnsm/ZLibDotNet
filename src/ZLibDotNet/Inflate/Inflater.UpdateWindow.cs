// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

using System.Buffers;
using System.Runtime.InteropServices;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    private static void UpdateWindow(ref ZStream strm, ref byte end, uint copy)
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

        ref byte window = ref MemoryMarshal.GetReference<byte>(state.window);
        // copy state.wsize or less output bytes into the circular window
        if (copy >= state.wsize)
        {
            netUnsafe.CopyBlockUnaligned(ref window, ref Unsafe.Subtract(ref end, state.wsize), state.wsize);
            state.wnext = 0;
            state.whave = state.wsize;
        }
        else
        {
            uint dist = state.wsize - state.wnext;
            if (dist > copy)
                dist = copy;
            netUnsafe.CopyBlockUnaligned(ref Unsafe.Add(ref window, state.wnext), ref Unsafe.Subtract(ref end, copy), dist);
            copy -= dist;
            if (copy != 0)
            {
                netUnsafe.CopyBlockUnaligned(ref window, ref Unsafe.Subtract(ref end, copy), copy);
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