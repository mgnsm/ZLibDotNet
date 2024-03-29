﻿// Original code and comments Copyright (C) 1995-2024 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

using System;
using System.Runtime.InteropServices;
using static ZLibDotNet.Deflate.Constants;

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    internal static int DeflateSetDictionary(ref ZStream strm, ReadOnlySpan<byte> dictionary)
    {
        if (DeflateStateCheck(ref strm))
            return Z_STREAM_ERROR;
        DeflateState s = strm.deflateState;

        int wrap = s.wrap;
        if (wrap == 2 || wrap == 1 && s.status != InitState || s.lookahead != 0)
            return Z_STREAM_ERROR;

        uint dictLength = (uint)dictionary.Length;
        // when using zlib wrappers, compute Adler-32 for provided dictionary
        if (wrap == 1)
            strm.Adler = Adler32.Update(strm.Adler, ref MemoryMarshal.GetReference(dictionary), dictLength);
        s.wrap = 0; // avoid computing Adler-32 in ReadBuf

        uint next_in = 0;
        // if dictionary would fill window, just replace the history
        if (dictLength >= s.w_size)
        {
            if (wrap == 0) // already empty otherwise
            {
                ClearHash(ref strm);
                s.strstart = 0;
                s.block_start = 0;
                s.insert = 0;
            }
            next_in = dictLength - s.w_size; //use the tail 
            dictLength = s.w_size;
        }

        // insert dictionary into window and hash
        uint avail = strm.avail_in;
        uint next = strm.next_in;
        ReadOnlySpan<byte> input = strm._input;
#if NET7_0_OR_GREATER
        ref byte input_ptr = ref strm.input_ptr;
        strm.Input = dictionary;
        ref DeflateRefs refs = ref strm.deflateRefs;
        if (netUnsafe.IsNullRef(ref refs.window))
            refs.window = ref MemoryMarshal.GetReference<byte>(s.window);
        if (netUnsafe.IsNullRef(ref refs.prev))
            refs.prev = ref MemoryMarshal.GetReference<ushort>(s.prev);
#else
        strm.avail_in = dictLength;
        strm._input = dictionary;
#endif
        strm.next_in = next_in;

        ref byte window = ref
#if NET7_0_OR_GREATER
        refs.window;
#else
        MemoryMarshal.GetReference<byte>(s.window);
#endif
        ref ushort prev = ref
#if NET7_0_OR_GREATER
        refs.prev;
#else
        MemoryMarshal.GetReference<ushort>(s.prev);
#endif
        ref ushort head = ref
#if NET7_0_OR_GREATER
        refs.head;
#else
        MemoryMarshal.GetReference<ushort>(s.head);
#endif
        FillWindow(ref strm, ref window, ref prev, ref head);
        while (s.lookahead >= MinMatch)
        {
            uint str = s.strstart;
            uint n = s.lookahead - (MinMatch - 1);
            do
            {
                UpdateHash(s, ref s.ins_h, Unsafe.Add(ref window, str + MinMatch - 1));
                ref ushort temp = ref Unsafe.Add(ref head, s.ins_h);
                Unsafe.Add(ref prev, str & s.w_mask) = temp;
                temp = (ushort)str;
                str++;
            } while (--n != 0);
            s.strstart = str;
            s.lookahead = MinMatch - 1;
            FillWindow(ref strm, ref window, ref prev, ref head);
        }
        s.strstart += s.lookahead;
        s.block_start = (int)s.strstart;
        s.insert = s.lookahead;
        s.lookahead = 0;
        s.match_length = s.prev_length = MinMatch - 1;
        s.match_available = false;
        strm._input = input;
#if NET7_0_OR_GREATER
        strm.input_ptr = ref input_ptr;
#endif
        strm.next_in = next;
        strm.avail_in = avail;
        s.wrap = wrap;
        return Z_OK;
    }
}