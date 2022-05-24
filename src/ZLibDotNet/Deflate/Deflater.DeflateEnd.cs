// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    internal static int DeflateEnd(Unsafe.ZStream strm)
    {
        if (DeflateStateCheck(strm))
            return Z_STREAM_ERROR;

        int status = strm.deflateState.status;

        strm.deflateState = null;

        return status == BusyState ? Z_DATA_ERROR : Z_OK;
    }
}