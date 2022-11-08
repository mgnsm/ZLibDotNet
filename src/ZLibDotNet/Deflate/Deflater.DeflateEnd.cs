// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System.Buffers;

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    internal static int DeflateEnd(ZStream strm)
    {
        if (DeflateStateCheck(strm))
            return Z_STREAM_ERROR;

        DeflateState s = strm.deflateState;
        int status = s.status;

        if (s.window != default)
            ArrayPool<byte>.Shared.Return(s.window);
        if (s.prev != default)
            ArrayPool<ushort>.Shared.Return(s.prev);
        if (s.head != default)
            ArrayPool<ushort>.Shared.Return(s.head);
        if (s.pending_buf != default)
            ArrayPool<byte>.Shared.Return(s.pending_buf);

        s_objectPool.Return(s);
        strm.deflateState = null;

        return status == BusyState ? Z_DATA_ERROR : Z_OK;
    }
}