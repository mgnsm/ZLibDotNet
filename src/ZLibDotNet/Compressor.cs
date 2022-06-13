// Original code and comments Copyright (C) 1995-2005, 2010, 2014, 2016 Jean-loup Gailly, Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using ZLibDotNet.Deflate;
using ZLibDotNet.Inflate;

namespace ZLibDotNet;

internal static class Compressor
{
    private const ushort Max = ushort.MaxValue - 1;

    internal static unsafe int Compress(byte* dest, uint* destLen, byte* source, uint sourceLen, int level)
    {
        uint left = *destLen;
        *destLen = 0;

        Unsafe.ZStream stream = new();
        int err = Deflater.DeflateInit(stream, level);
        if (err != Z_OK)
            return err;

        stream.next_out = dest;
        stream.avail_out = 0;
        stream.next_in = source;
        stream.avail_in = 0;

        do
        {
            if (stream.avail_out == 0)
            {
                stream.avail_out = left > Max ? Max : left;
                left -= stream.avail_out;
            }
            if (stream.avail_in == 0)
            {
                stream.avail_in = sourceLen > Max ? Max : sourceLen;
                sourceLen -= stream.avail_in;
            }
            err = Deflater.Deflate(stream, sourceLen != 0 ? Z_NO_FLUSH : Z_FINISH);
        } while (err == Z_OK);

        *destLen = stream.total_out;
        _ = Deflater.DeflateEnd(stream);
        return err == Z_STREAM_END ? Z_OK : err;
    }

    internal static unsafe int Uncompress(byte* dest, uint* destLen, byte* source, uint* sourceLen)
    {
        uint len, left;
        byte* buf = stackalloc byte[1]; // for detection of incomplete stream when *destLen == 0

        len = *sourceLen;
        if (*destLen != 0)
        {
            left = *destLen;
            *destLen = 0;
        }
        else
        {
            left = 1;
            dest = buf;
        }

        Unsafe.ZStream stream = new()
        {
            next_in = source,
            avail_in = 0
        };

        int err = Inflater.InflateInit(stream, DefaultWindowBits);
        if (err != Z_OK)
            return err;

        stream.next_out = dest;
        stream.avail_out = 0;

        do
        {
            if (stream.avail_out == 0)
            {
                stream.avail_out = left > Max ? Max : left;
                left -= stream.avail_out;
            }
            if (stream.avail_in == 0)
            {
                stream.avail_in = len > Max ? Max : len;
                len -= stream.avail_in;
            }
            err = Inflater.Inflate(stream, Z_NO_FLUSH);
        } while (err == Z_OK);

        *sourceLen -= len + stream.avail_in;
        if (dest != buf)
            *destLen = stream.total_out;
        else if (stream.total_out != 0 && err == Z_BUF_ERROR)
            left = 1;

        _ = Inflater.InflateEnd(stream);
        return err == Z_STREAM_END ? Z_OK :
               err == Z_NEED_DICT ? Z_DATA_ERROR :
               err == Z_BUF_ERROR && left + stream.avail_out != 0 ? Z_DATA_ERROR :
               err;
    }
}