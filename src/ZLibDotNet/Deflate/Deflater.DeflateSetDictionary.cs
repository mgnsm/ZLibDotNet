﻿// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using static ZLibDotNet.Deflate.Constants;

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    internal static unsafe int DeflateSetDictionary(Unsafe.ZStream strm, byte* dictionary, uint dictLength)
    {
        if (DeflateStateCheck(strm) || dictionary == null)
            return Z_STREAM_ERROR;
        DeflateState s = strm.deflateState;

        int wrap = s.wrap;
        if (wrap == 2 || wrap == 1 && s.status != InitState || s.lookahead != 0)
            return Z_STREAM_ERROR;

        // when using zlib wrappers, compute Adler-32 for provided dictionary
        if (wrap == 1)
            strm.Adler = Adler32.Update(strm.Adler, dictionary, dictLength);
        s.wrap = 0; // avoid computing Adler-32 in ReadBuf

        // if dictionary would fill window, just replace the history
        if (dictLength >= s.w_size)
        {
            if (wrap == 0) // already empty otherwise
            {
                Array.Clear(s.head, 0, s.head.Length);
                s.strstart = 0;
                s.block_start = 0;
                s.insert = 0;
            }
            dictionary += dictLength - s.w_size;  //use the tail
            dictLength = s.w_size;
        }

        // insert dictionary into window and hash
        uint avail = strm.avail_in;
        byte* next = strm.next_in;
        strm.avail_in = dictLength;
        strm.next_in = dictionary;

        uint str, n;
        fixed (byte* window = s.window)
        {
            FillWindow(s, window);
            while (s.lookahead >= MinMatch)
            {
                str = s.strstart;
                n = s.lookahead - (MinMatch - 1);
                do
                {
                    UpdateHash(s, ref s.ins_h, window[str + MinMatch - 1]);
                    s.prev[str & s.w_mask] = s.head[s.ins_h];
                    s.head[s.ins_h] = (ushort)str;
                    str++;
                } while (--n != 0);
                s.strstart = str;
                s.lookahead = MinMatch - 1;
                FillWindow(s, window);
            }
        }
        s.strstart += s.lookahead;
        s.block_start = (int)s.strstart;
        s.insert = s.lookahead;
        s.lookahead = 0;
        s.match_length = s.prev_length = MinMatch - 1;
        s.match_available = 0;
        strm.next_in = next;
        strm.avail_in = avail;
        s.wrap = wrap;
        return Z_OK;
    }
}