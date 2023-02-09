// Original code and comments Copyright (C) 1995-2005, 2010, 2014, 2016 Jean-loup Gailly, Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System.Buffers;
using ZLibDotNet.Deflate;
using ZLibDotNet.Inflate;

namespace ZLibDotNet;

internal static class Compressor
{
    private const ushort Max = ushort.MaxValue - 1;

    internal static int Compress(byte[] dest, ref int destLen, byte[] source, int sourceLen, int level)
    {
        if (source == null || sourceLen < 0 || sourceLen > source.Length)
            return Z_STREAM_ERROR;

        int left = destLen;
        destLen = 0;

        ZStream stream = new() { _input = source, _output = dest };
        int err = Deflater.DeflateInit(ref stream, level);
        if (err != Z_OK)
            return err;

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
            err = Deflater.Deflate(ref stream, sourceLen != 0 ? Z_NO_FLUSH : Z_FINISH);
        } while (err == Z_OK);

        destLen = (int)stream.total_out;
        _ = Deflater.DeflateEnd(ref stream);

        return err == Z_STREAM_END ? Z_OK : err;
    }

    internal static int Uncompress(byte[] dest, ref int destLen, byte[] source, ref int sourceLen)
    {
        int len = sourceLen;
        int left;
        byte[] buf = default;
        try
        {
            buf = ArrayPool<byte>.Shared.Rent(1);

            if (destLen != 0)
            {
                left = destLen;
                destLen = 0;
            }
            else
            {
                left = 1;
                dest = buf;
            }

            ZStream stream = new() { _input = source, _output = dest };
            int err = Inflater.InflateInit(ref stream, DefaultWindowBits);
            if (err != Z_OK)
                return err;

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
                err = Inflater.Inflate(ref stream, Z_NO_FLUSH);
            } while (err == Z_OK);

            sourceLen -= len + (int)stream.avail_in;
            if (dest != buf)
                destLen = (int)stream.total_out;
            else if (stream.total_out != 0 && err == Z_BUF_ERROR)
                left = 1;

            _ = Inflater.InflateEnd(ref stream);
            return err == Z_STREAM_END ? Z_OK :
                   err == Z_NEED_DICT ? Z_DATA_ERROR :
                   err == Z_BUF_ERROR && left + stream.avail_out != 0 ? Z_DATA_ERROR :
                   err;
        }
        finally
        {
            if (buf != default)
                ArrayPool<byte>.Shared.Return(buf);
        }
    }

    internal static uint CompressBound(uint sourceLen) =>
        sourceLen + (sourceLen >> 12) + (sourceLen >> 14) +
           (sourceLen >> 25) + 13;
}