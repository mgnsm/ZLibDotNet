// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Buffers;

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    private static unsafe void UpdateWindow(Unsafe.ZStream strm, byte* end, uint copy)
    {
        InflateState state = strm.inflateState;

        // if it hasn't been done already, allocate space for the window
        if (state.window == null)
            state.window = ArrayPool<byte>.Shared.Rent(1 << (int)state.wbits);

        // if window not in use yet, initialize
        if (state.wsize == 0)
        {
            state.wsize = 1U << (int)state.wbits;
            state.wnext = 0;
            state.whave = 0;
        }

        uint dist;
        fixed (byte* window = state.window)
        {
            // copy state.wsize or less output bytes into the circular window
            if (copy >= state.wsize)
            {
                Buffer.MemoryCopy(end - state.wsize, window, state.wsize, state.wsize);
                state.wnext = 0;
                state.whave = state.wsize;
            }
            else
            {
                dist = state.wsize - state.wnext;
                if (dist > copy)
                    dist = copy;
                Buffer.MemoryCopy(end - copy, window + state.wnext, dist, dist);
                copy -= dist;
                if (copy != 0)
                {
                    Buffer.MemoryCopy(end - copy, window, copy, copy);
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
}