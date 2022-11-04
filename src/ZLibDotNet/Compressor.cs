// Original code and comments Copyright (C) 1995-2005, 2010, 2014, 2016 Jean-loup Gailly, Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Runtime.InteropServices;
using ZLibDotNet.Deflate;
using ZLibDotNet.Inflate;

namespace ZLibDotNet;

internal static class Compressor
{
    private const ushort Max = ushort.MaxValue - 1;

    internal static unsafe int Compress(ref byte dest, ref int destLen, ref byte source, int sourceLen, int level)
    {
        int left = destLen;
        destLen = 0;

        Unsafe.ZStream stream = new();
        int err = Deflater.DeflateInit(stream, level);
        if (err != Z_OK)
            return err;

        stream.next_out = (byte*)netUnsafe.AsPointer(ref dest);
        stream.avail_out = 0;
        stream.next_in = (byte*)netUnsafe.AsPointer(ref source);
        stream.avail_in = 0;

        do
        {
            if (stream.avail_out == 0)
            {
                stream.avail_out = (uint)(left > Max ? Max : left);
                left -= (int)stream.avail_out;
            }
            if (stream.avail_in == 0)
            {
                stream.avail_in = (uint)(sourceLen > Max ? Max : sourceLen);
                sourceLen -= (int)stream.avail_in;
            }
            err = Deflater.Deflate(stream, sourceLen != 0 ? Z_NO_FLUSH : Z_FINISH);
        } while (err == Z_OK);

        destLen = (int)stream.total_out;
        _ = Deflater.DeflateEnd(stream);
        return err == Z_STREAM_END ? Z_OK : err;
    }

    internal static unsafe int Uncompress(ref byte dest, ref int destLen, ref byte source, ref int sourceLen)
    {
        Span<byte> span = stackalloc byte[1]; // for detection of incomplete stream when destLen == 0
        ref byte buf = ref MemoryMarshal.GetReference(span);
        ref byte destination = ref dest;

        int len = sourceLen;
        int left;
        if (destLen != 0)
        {
            left = destLen;
            destLen = 0;
        }
        else
        {
            left = 1;
            destination = buf;
        }

        Unsafe.ZStream stream = new()
        {
            next_in = (byte*)netUnsafe.AsPointer(ref source),
            avail_in = 0
        };

        int err = Inflater.InflateInit(stream, DefaultWindowBits);
        if (err != Z_OK)
            return err;

        stream.next_out = (byte*)netUnsafe.AsPointer(ref destination);
        stream.avail_out = 0;

        do
        {
            if (stream.avail_out == 0)
            {
                stream.avail_out = (uint)(left > Max ? Max : left);
                left -= (int)stream.avail_out;
            }
            if (stream.avail_in == 0)
            {
                stream.avail_in = (uint)(len > Max ? Max : len);
                len -= (int)stream.avail_in;
            }
            err = Inflater.Inflate(stream, Z_NO_FLUSH);
        } while (err == Z_OK);

        sourceLen -= len + (int)stream.avail_in;
        if (!netUnsafe.AreSame(ref dest, ref buf))
            destLen = (int)stream.total_out;
        else if (stream.total_out != 0 && err == Z_BUF_ERROR)
            left = 1;

        _ = Inflater.InflateEnd(stream);
        return err == Z_STREAM_END ? Z_OK :
               err == Z_NEED_DICT ? Z_DATA_ERROR :
               err == Z_BUF_ERROR && left + stream.avail_out != 0 ? Z_DATA_ERROR :
               err;
    }

    internal static uint CompressBound(uint sourceLen) =>
        sourceLen + (sourceLen >> 12) + (sourceLen >> 14) +
           (sourceLen >> 25) + 13;
}