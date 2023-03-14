// Original code and comments Copyright (C) 1995-2005, 2010, 2014, 2016 Jean-loup Gailly, Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

using System;
using ZLibDotNet.Deflate;
using ZLibDotNet.Inflate;

namespace ZLibDotNet;

internal static class Compressor
{
    //private const int Max = int.MaxValue;

    internal static int Compress(Span<byte> dest, ref uint destLen, ReadOnlySpan<byte> source, uint sourceLen, int level)
    {
        uint left = destLen;
        destLen = 0;

        ZStream stream = new();
        int err = Deflater.DeflateInit(ref stream, level);
        if (err != Z_OK)
            return err;

        stream.Output = dest;
        stream.avail_out = 0;
        stream.Input = source;
        stream.avail_in = 0;

        do
        {
            if (stream.avail_out == 0)
            {
                stream.avail_out = left; // left > Max ? Max : left;
                left -= stream.avail_out;
            }
            if (stream.avail_in == 0)
            {
                stream.avail_in = sourceLen; //sourceLen > Max ? Max : sourceLen;
                sourceLen -= stream.avail_in;
            }
            err = Deflater.Deflate(ref stream, sourceLen != 0 ? Z_NO_FLUSH : Z_FINISH);
        } while (err == Z_OK);

        destLen = stream.total_out;
        _ = Deflater.DeflateEnd(ref stream);
        return err == Z_STREAM_END ? Z_OK : err;
    }

    internal static int Uncompress(Span<byte> dest, ref uint destLen, ReadOnlySpan<byte> source, ref uint sourceLen)
    {
        uint left;
        uint len = sourceLen;
        byte[] buf = default; // for detection of incomplete stream when destLen == 0
        if (destLen != 0)
        {
            left = destLen;
            destLen = 0;
        }
        else
        {
            left = 1;
            buf = new byte[1];
            dest = buf;
        }

        ZStream stream = new()
        {
            Input = source,
            avail_in = 0
        };

        int err = Inflater.InflateInit(ref stream, DefaultWindowBits);
        if (err != Z_OK)
            return err;

        stream.Output = dest;
        stream.avail_out = 0;

        do
        {
            if (stream.avail_out == 0)
            {
                stream.avail_out = left; // left > Max ? Max : left;
                left -= stream.avail_out;
            }
            if (stream.avail_in == 0)
            {
                stream.avail_in = len; // len > Max ? Max : len;
                len -= stream.avail_in;
            }
            err = Inflater.Inflate(ref stream, Z_NO_FLUSH);
        } while (err == Z_OK);

        sourceLen -= len + stream.avail_in;
        if (dest != buf)
            destLen = stream.total_out;
        else if (stream.total_out != 0 && err == Z_BUF_ERROR)
            left = 1;

        _ = Inflater.InflateEnd(ref stream);
        return err == Z_STREAM_END ? Z_OK :
               err == Z_NEED_DICT ? Z_DATA_ERROR :
               err == Z_BUF_ERROR && left + stream.avail_out != 0 ? Z_DATA_ERROR :
               err;
    }

    internal static uint CompressBound(uint sourceLen) =>
        sourceLen + (sourceLen >> 12) + (sourceLen >> 14) +
           (sourceLen >> 25) + 13;
}