// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal static int InflatePrime(ZStream strm, int bits, int value)
    {
        if (InflateStateCheck(strm))
            return Z_STREAM_ERROR;

        InflateState state = strm.inflateState;
        if (bits < 0)
        {
            state.hold = 0;
            state.bits = 0;
            return Z_OK;
        }
        if (bits > 16 || state.bits + (uint)bits > 32)
            return Z_STREAM_ERROR;
        value &= (1 << bits) - 1;
        state.hold += (uint)(value << (int)state.bits);
        state.bits += (uint)bits;
        return Z_OK;
    }
}