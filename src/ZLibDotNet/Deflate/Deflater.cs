// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static ZLibDotNet.Deflate.Constants;

namespace ZLibDotNet.Deflate;

internal static partial class Deflater
{
    private const int InitState = 42;       // zlib header -> BUSY_STATE
    private const int ExtraState = 69;      // gzip extra block -> NAME_STATE
    private const int NameState = 73;       // gzip file name -> COMMENT_STATE
    private const int CommentState = 91;    // gzip comment -> HCRC_STATE
    private const int HcrcState = 103;      // gzip header CRC -> BUSY_STATE
    private const int BusyState = 113;      // deflate -> FINISH_STATE
    private const int FinishState = 666;    // stream complete
    private const int PresetDict = 0x20;    // preset dictionary flag in zlib header
    private const int MaxStored = 65535;    // maximum stored block length in deflate format (not including header)
    private const int DistCodeLen = 512;

    private const uint MinLookAhead = MaxMatch + MinMatch + 1; // Minimum amount of lookahead, except at the end of the input file.
#if DEBUG
    private const ushort Literals = 256; // number of literal bytes 0..255
#endif

    private const uint WinInit = MaxMatch;
    /* Number of bytes after end of data in window to initialize in order to avoid
       memory checker errors from longest match routines */

    private const ushort TooFar = 4096;

    private static readonly string[] s_z_errmsg = new string[10]
    {
        "need dictionary",     /* Z_NEED_DICT       2  */
        "stream end",          /* Z_STREAM_END      1  */
        "",                    /* Z_OK              0  */
        "file error",          /* Z_ERRNO         (-1) */
        "stream error",        /* Z_STREAM_ERROR  (-2) */
        "data error",          /* Z_DATA_ERROR    (-3) */
        "insufficient memory", /* Z_MEM_ERROR     (-4) */
        "buffer error",        /* Z_BUF_ERROR     (-5) */
        "incompatible version",/* Z_VERSION_ERROR (-6) */
        ""
    };

    internal static readonly Config[] s_configuration_table = new Config[10]
    {
        new Config(0, 0, 0, 0, Config.DeflateType.Stored),          // 0: store only
        new Config(4, 4, 8, 4, Config.DeflateType.Fast),            // 1: max speed, no lazy matches
        new Config(4, 5, 16, 8, Config.DeflateType.Fast),           // 2
        new Config(4, 6, 32, 32, Config.DeflateType.Fast),          // 3
        new Config(4, 4, 16, 16, Config.DeflateType.Slow),          // 4: lazy matches
        new Config(8, 16, 32, 32, Config.DeflateType.Slow),         // 5
        new Config(8, 16, 128, 128, Config.DeflateType.Slow),       // 6
        new Config(8, 32, 128, 256, Config.DeflateType.Slow),       // 7
        new Config(32, 128, 258, 1024, Config.DeflateType.Slow),    // 8
        new Config(32, 258, 258, 4096, Config.DeflateType.Slow)     // 9: max compression
    };

    private static readonly int[] s_base_dist = new int[DCodes] {
        0,     1,     2,     3,     4,     6,     8,    12,    16,    24,
        32,    48,    64,    96,   128,   192,   256,   384,   512,   768,
        1024,  1536,  2048,  3072,  4096,  6144,  8192, 12288, 16384, 24576
    };

    private static readonly byte[] s_dist_code = new byte[DistCodeLen] {
        0,  1,  2,  3,  4,  4,  5,  5,  6,  6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  8,
        8,  8,  8,  8,  9,  9,  9,  9,  9,  9,  9,  9, 10, 10, 10, 10, 10, 10, 10, 10,
        10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11,
        11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13,
        13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
        13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,  0,  0, 16, 17,
        18, 18, 19, 19, 20, 20, 20, 20, 21, 21, 21, 21, 22, 22, 22, 22, 22, 22, 22, 22,
        23, 23, 23, 23, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
        26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
        26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 27, 27, 27, 27, 27, 27, 27, 27,
        27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
        27, 27, 27, 27, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
        29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
        29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
        29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29
    };

    private static readonly byte[] s_length_code = new byte[MaxMatch - MinMatch + 1] {
        0,  1,  2,  3,  4,  5,  6,  7,  8,  8,  9,  9, 10, 10, 11, 11, 12, 12, 12, 12,
        13, 13, 13, 13, 14, 14, 14, 14, 15, 15, 15, 15, 16, 16, 16, 16, 16, 16, 16, 16,
        17, 17, 17, 17, 17, 17, 17, 17, 18, 18, 18, 18, 18, 18, 18, 18, 19, 19, 19, 19,
        19, 19, 19, 19, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20,
        21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 22, 22, 22, 22,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23,
        23, 23, 23, 23, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
        25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26,
        26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
        26, 26, 26, 26, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
        27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 28
    };

    private static readonly int[] s_base_length = new int[LenghtCodes] {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56,
        64, 80, 96, 112, 128, 160, 192, 224, 0
    };

    private static readonly ushort[] s_bl_order = // The lengths of the bit length codes are sent in order of decreasing probability, to avoid transmitting the lengths for unused bit length codes.
        new ushort[BlCodes] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

    private static readonly int[] s_extra_dbits = // extra bits for each distance code
        new int[DCodes] { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };

    private static readonly int[] s_extra_lbits = // extra bits for each length code
        new int[LenghtCodes] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };

    private static readonly int[] s_extra_blbits = // extra bits for each bit length code
        new int[BlCodes] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 7 };

    internal static int Deflate(ref ZStream strm, int flush)
    {
        if (DeflateStateCheck(ref strm) || flush > Z_BLOCK || flush < 0)
            return Z_STREAM_ERROR;
        DeflateState s = strm.deflateState;

        if (strm._output == null
            || strm.avail_in != 0 && strm._input == null
            || s.status == FinishState && flush != Z_FINISH)
            return ReturnWithError(ref strm, Z_STREAM_ERROR);
        if (strm.avail_out == 0)
            return ReturnWithError(ref strm, Z_BUF_ERROR);

        int old_flush = s.last_flush; // value of flush param for previous deflate call
        s.last_flush = flush;

        s.sym_end = (s.lit_bufsize - 1) * 3;

#if NET7_0_OR_GREATER
        ref DeflateRefs refs = ref strm.deflateRefs;
#endif
        ref byte pending_buf = ref
#if NET7_0_OR_GREATER
            refs.pending_buf;
#else
            MemoryMarshal.GetReference<byte>(s.pending_buf);
#endif
        ref byte pending_out = ref
#if NET7_0_OR_GREATER
            refs.pending_out;
#else
            MemoryMarshal.GetReference<byte>(s.pending_out);
#endif

        // Flush as much pending output as possible
        if (s.pending != 0)
        {
            FlushPending(ref strm, ref pending_buf, ref pending_out);
            if (strm.avail_out == 0)
            {
                /* Since avail_out is 0, deflate will be called again with
                 * more output space, but possibly with both pending and
                 * avail_in equal to zero. There won't be anything to do,
                 * but this is not an error situation so make sure we
                 * return OK instead of BUF_ERROR at next call of deflate:
                 */
                s.last_flush = -1;
                return Z_OK;
            }

            /* Make sure there is something to do and avoid duplicate consecutive
             * flushes. For repeated and useless calls with Z_FINISH, we keep
             * returning Z_STREAM_END instead of Z_BUF_ERROR.
             */
        }
        else if (strm.avail_in == 0
            && Rank(flush) <= Rank(old_flush)
            && flush != Z_FINISH)
        {
            return ReturnWithError(ref strm, Z_BUF_ERROR);
        }

        // User must not provide more input after the first FINISH:
        if (s.status == FinishState && strm.avail_in != 0)
            return ReturnWithError(ref strm, Z_BUF_ERROR);

        // Write the header
        if (s.status == InitState && s.wrap == 0)
            s.status = BusyState;
        if (s.status == InitState)
        {
            // zlib header
            uint header = (Z_DEFLATED + ((s.w_bits - 8) << 4)) << 8;
            uint level_flags;

            if (s.strategy >= Z_HUFFMAN_ONLY || s.level < 2)
                level_flags = 0;
            else if (s.level < 6)
                level_flags = 1;
            else if (s.level == 6)
                level_flags = 2;
            else
                level_flags = 3;
            header |= level_flags << 6;
            if (s.strstart != 0)
                header |= PresetDict;
            header += 31 - header % 31;

            PutShort(s, header, ref pending_buf);

            // Save the adler32 of the preset dictionary:
            if (s.strstart != 0)
            {
                PutShort(s, strm.Adler >> 16, ref pending_buf);
                PutShort(s, strm.Adler & 0xffff, ref pending_buf);
            }
            strm.Adler = Adler32.Update(0, ref netUnsafe.NullRef<byte>(), 0);
            s.status = BusyState;

            // Compression must start with an empty pending buffer
            FlushPending(ref strm, ref pending_buf, ref pending_out);
            if (s.pending != 0)
            {
                s.last_flush = -1;
                return Z_OK;
            }
        }

        // Start a new block or continue the current one.
        if (strm.avail_in != 0
            || s.lookahead != 0
            || flush != Z_NO_FLUSH && s.status != FinishState)
        {
            BlockState bstate;
            if (s.level == 0)
            {
                bstate = DeflateStored(ref strm, flush, ref pending_buf, ref pending_out);
            }
            else
            {
                switch (s.strategy)
                {
                    case Z_HUFFMAN_ONLY:
                        bstate = DeflateHuff(ref strm, flush, ref pending_buf, ref pending_out);
                        break;
                    case Z_RLE:
                        bstate = DeflateRle(ref strm, flush, ref pending_buf, ref pending_out);
                        break;
                    default:
                        ref Config configuration_table = ref
#if NET7_0_OR_GREATER
                            refs.configuration_table;
#else
                            MemoryMarshal.GetReference<Config>(s_configuration_table);
#endif
                        Config.DeflateType type = Unsafe.Add(ref configuration_table, (uint)s.level).deflate_type;
                        bstate = type switch
                        {
                            Config.DeflateType.Stored => DeflateStored(ref strm, flush, ref pending_buf,
                                ref pending_out),
                            Config.DeflateType.Fast => DeflateFast(ref strm, flush, ref pending_buf,
                                ref pending_out),
                            _ => DeflateSlow(ref strm, flush, ref pending_buf, ref pending_out),
                        };
                        break;
                }
            }

            if (bstate == BlockState.FinishStarted || bstate == BlockState.FinishDone)
            {
                s.status = FinishState;
            }
            if (bstate == BlockState.NeedMore || bstate == BlockState.FinishStarted)
            {
                if (strm.avail_out == 0)
                    s.last_flush = -1; // avoid BUF_ERROR next call, see above
                return Z_OK;
                /* If flush != Z_NO_FLUSH && avail_out == 0, the next call
                 * of deflate should use the same flush parameter to make sure
                 * that the flush is complete. So we don't have to output an
                 * empty block here, this will be done at next call. This also
                 * ensures that for a very small output buffer, we emit at most
                 * one empty block.
                 */
            }
            if (bstate == BlockState.BlockDone)
            {
                if (flush == Z_PARTIAL_FLUSH)
                {
#if NET7_0_OR_GREATER
                    if (netUnsafe.IsNullRef(ref refs.sta_ltree))
                        refs.sta_ltree = ref MemoryMarshal.GetReference<TreeNode>(Tree.s_ltree);
#endif
                    Tree.Align(s, ref pending_buf, ref
#if NET7_0_OR_GREATER
                        refs.sta_ltree
#else
                        MemoryMarshal.GetReference<TreeNode>(Tree.s_ltree)
#endif
                    );
                }
                else if (flush != Z_BLOCK) // FULL_FLUSH or SYNC_FLUSH
                {
                    Tree.StoredBlock(s, ref netUnsafe.NullRef<byte>(), 0, 0, ref pending_buf);
                    /* For a full flush, this empty block will be recognized
                     * as a special marker by InflateSync().
                     */
                    if (flush == Z_FULL_FLUSH)
                    {
                        ClearHash(ref strm);
                        if (s.lookahead == 0)
                        {
                            s.strstart = 0;
                            s.block_start = 0;
                            s.insert = 0;
                        }
                    }
                }
                FlushPending(ref strm, ref pending_buf, ref pending_out);
                if (strm.avail_out == 0)
                {
                    s.last_flush = -1; // avoid BUF_ERROR at next call, see above
                    return Z_OK;
                }
            }
        }

        if (flush != Z_FINISH)
            return Z_OK;
        if (s.wrap <= 0)
            return Z_STREAM_END;

        // Write the trailer
        PutShort(s, strm.Adler >> 16, ref pending_buf);
        PutShort(s, strm.Adler & 0xffff, ref pending_buf);

        FlushPending(ref strm, ref pending_buf, ref pending_out);

        // If avail_out is zero, the application will call deflate again to flush the rest.
        if (s.wrap > 0)
            s.wrap = -s.wrap; // write the trailer only once!

        return s.pending != 0 ? Z_OK : Z_STREAM_END;
    }

    private static bool DeflateStateCheck(ref ZStream strm)
    {
        DeflateState s = strm.deflateState;
        return s == null
            || s.status != InitState
                && s.status != ExtraState
                && s.status != NameState
                && s.status != CommentState
                && s.status != HcrcState
                && s.status != BusyState
                && s.status != FinishState;
    }

    private static void LongestMatchInit(ref ZStream strm)
    {
        const byte MinMatch = 3;

        DeflateState s = strm.deflateState;
        s.window_size = 2 * s.w_size;

        ClearHash(ref strm);

        // set the default configuration parameters
        ref Config configuration_table = ref
#if NET7_0_OR_GREATER
            strm.deflateRefs.configuration_table;
#else
            MemoryMarshal.GetReference<Config>(s_configuration_table);
#endif
        ref Config config = ref Unsafe.Add(ref configuration_table, (uint)s.level);
        s.max_lazy_match = config.max_lazy;
        s.good_match = config.good_length;
        s.nice_match = config.nice_length;
        s.max_chain_length = config.max_chain;

        s.strstart = 0;
        s.block_start = 0;
        s.lookahead = 0;
        s.insert = 0;
        s.match_length = s.prev_length = MinMatch - 1;
        s.match_available = false;
        s.ins_h = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReturnWithError(ref ZStream strm, int err)
    {
        strm.msg = s_z_errmsg[Z_NEED_DICT - err];
        return err;
    }

    private static void FlushPending(ref ZStream strm, ref byte pending_buf, ref byte pending_out)
    {
        DeflateState s = strm.deflateState;
        Tree.FlushBits(s, ref pending_buf);
        uint len = s.pending;
        if (len > strm.avail_out)
            len = strm.avail_out;
        if (len == 0)
            return;

        netUnsafe.CopyBlockUnaligned(ref
#if NET7_0_OR_GREATER
            Unsafe.Add(ref strm.output_ptr, strm.next_out),
#else
            MemoryMarshal.GetReference(strm._output.Slice((int)strm.next_out)),
#endif
            ref Unsafe.Add(ref pending_out, s.pendingOutOffset),
            len);

        strm.next_out += len;
        s.pendingOutOffset += len;
        s.pending -= len;
        if (s.pending == 0)
        {
            //s.pending_out = s.pending_buf;
            s.pendingOutOffset = 0;
        }

        strm.total_out += len;
        strm.avail_out -= len;
    }

    /// <summary>
    /// Rank Z_BLOCK between Z_NO_FLUSH and Z_PARTIAL_FLUSH.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Rank(int f) => f * 2 - (f > 4 ? 9 : 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PutShort(DeflateState s, uint b, ref byte pending_buf)
    {
        Unsafe.Add(ref pending_buf, s.pending++) = (byte)(b >> 8);
        Unsafe.Add(ref pending_buf, s.pending++) = (byte)(b & 0xff);
    }

    private static BlockState DeflateStored(ref ZStream strm, int flush, ref byte pending_buf, ref byte pending_out)
    {
        DeflateState s = strm.deflateState;
        /* Smallest worthy block size when not flushing or finishing. By default
         * this is 32K. This can be as small as 507 bytes for memLevel == 1. For
         * large input and output buffers, the stored block size will be larger.
         */
        uint min_block = Math.Min(s.pending_buf_size - 5, s.w_size);

        /* Copy as many min_block or larger stored blocks directly to next_out as
         * possible. If flushing, copy the remaining available input to next_out as
         * stored blocks, if there is enough space.
         */
        uint len, left, have;
        uint last = 0;
        uint used = strm.avail_in;
#if NET7_0_OR_GREATER
        ref DeflateRefs refs = ref strm.deflateRefs;
        if (netUnsafe.IsNullRef(ref refs.window))
            refs.window = ref MemoryMarshal.GetReference<byte>(s.window);
#endif
        ref byte window = ref
#if NET7_0_OR_GREATER
            refs.window;
#else
            MemoryMarshal.GetReference<byte>(s.window);
#endif
        ref byte next_out = ref
#if NET7_0_OR_GREATER
            strm.output_ptr;
#else
            MemoryMarshal.GetReference(strm._output);
#endif
        do
        {
            /* Set len to the maximum size block that we can copy directly with the
             * available input data and output space. Set left to how much of that
             * would be copied from what's left in the window.
             */
            len = MaxStored; // maximum deflate stored block length
            have = (uint)((s.bi_valid + 42) >> 3); // number of header bytes
            if (strm.avail_out < have) // need room for header
                break;
            // maximum stored block length that will fit in avail_out:
            have = strm.avail_out - have;
            left = (uint)(s.strstart - s.block_start); // bytes left in window
            if (len > left + strm.avail_in)
                len = left + strm.avail_in;  // limit len to the input
            if (len > have)
                len = have; // limit len to the output

            /* If the stored block would be less than min_block in length, or if
             * unable to copy all of the available input when flushing, then try
             * copying to the window and the pending buffer instead. Also don't
             * write an empty block when flushing -- deflate() does that.
             */
            if (len < min_block && (len == 0 && flush != Z_FINISH ||
                                    flush == Z_NO_FLUSH ||
                                    len != left + strm.avail_in))
                break;

            /* Make a dummy stored block in pending to get the header bytes,
             * including any pending bits. This also updates the debugging counts.
             */
            last = flush == Z_FINISH && len == left + strm.avail_in ? 1U : 0U;
            Tree.StoredBlock(s, ref netUnsafe.NullRef<byte>(), 0, last, ref pending_buf);

            // Replace the lengths in the dummy stored block with len.
            Unsafe.Add(ref pending_buf, s.pending - 4) = (byte)len;
            Unsafe.Add(ref pending_buf, s.pending - 3) = (byte)(len >> 8);
            Unsafe.Add(ref pending_buf, s.pending - 2) = (byte)~len;
            Unsafe.Add(ref pending_buf, s.pending - 1) = (byte)(~len >> 8);

            // Write the stored block header bytes.
            FlushPending(ref strm, ref pending_buf, ref pending_out);
#if DEBUG
            // Update debugging counts for the data about to be copied.
            s.compressed_len += len << 3;
            s.bits_sent += len << 3;
#endif
            // Copy uncompressed bytes from the window to next_out.
            if (left != 0)
            {
                if (left > len)
                    left = len;
                netUnsafe.CopyBlockUnaligned(ref Unsafe.Add(ref next_out, strm.next_out),
                    ref Unsafe.Add(ref window, (uint)s.block_start), left);
                strm.next_out += left;
                strm.avail_out -= left;
                strm.total_out += left;
                s.block_start += (int)left;
                len -= left;
            }

            // Copy uncompressed bytes directly from next_in to next_out, updating the check value.
            if (len != 0)
            {
                ReadBuf(ref strm, ref Unsafe.Add(ref next_out, strm.next_out), len);
                strm.next_out += len;
                strm.avail_out -= len;
                strm.total_out += len;
            }
        } while (last == 0);

        /* Update the sliding window with the last s.w_size bytes of the copied
         * data, or append all of the copied data to the existing window if less
         * than s.w_size bytes were copied. Also update the number of bytes to
         * insert in the hash tables, in the event that deflateParams() switches to
         * a non-zero compression level.
         */
        used -= strm.avail_in; // number of input bytes directly copied
        if (used != 0)
        {
            ref byte next_in = ref
#if NET7_0_OR_GREATER
                Unsafe.Add(ref strm.input_ptr, strm.next_in);
#else
                MemoryMarshal.GetReference(strm._input.Slice((int)strm.next_in));
#endif
            /* If any input was used, then no unused input remains in the window,
             * therefore s.block_start == s.strstart.
             */
            if (used >= s.w_size) // supplant the previous history
            {
                s.matches = 2; // clear hash
                netUnsafe.CopyBlockUnaligned(ref window, ref Unsafe.Subtract(ref next_in, s.w_size), s.w_size);

                s.strstart = s.w_size;
                s.insert = s.strstart;
            }
            else
            {
                if (s.window_size - s.strstart <= used)
                {
                    // Slide the window down
                    s.strstart -= s.w_size;
                    netUnsafe.CopyBlockUnaligned(ref window, ref Unsafe.Add(ref window, s.w_size), s.strstart);
                    if (s.matches < 2)
                        s.matches++; // add a pending SlideHash()
                    if (s.insert > s.strstart)
                        s.insert = s.strstart;
                }
                netUnsafe.CopyBlockUnaligned(ref Unsafe.Add(ref window, s.strstart), ref Unsafe.Subtract(ref next_in, used), used);
                s.strstart += used;
                s.insert += Math.Min(used, s.w_size - s.insert);
            }
            s.block_start = (int)s.strstart;
        }

        if (s.high_water < s.strstart)
            s.high_water = s.strstart;

        // If the last block was written to next_out, then done.
        if (last != 0)
            return BlockState.FinishDone;

        // If flushing and all input has been consumed, then done.
        if (flush != Z_NO_FLUSH && flush != Z_FINISH &&
            strm.avail_in == 0 && s.strstart == s.block_start)
            return BlockState.BlockDone;

        // Fill the window with any remaining input.
        have = s.window_size - s.strstart;
        if (strm.avail_in > have && s.block_start >= s.w_size)
        {
            // Slide the window down.
            s.block_start -= (int)s.w_size;
            s.strstart -= s.w_size;
            netUnsafe.CopyBlockUnaligned(ref window, ref Unsafe.Add(ref window, s.w_size), s.strstart);
            if (s.matches < 2)
                s.matches++;    // add a pending SlideHash()
            have += s.w_size;   // more space now
            if (s.insert > s.strstart)
                s.insert = s.strstart;
        }
        if (have > strm.avail_in)
            have = strm.avail_in;
        if (have != 0)
        {
            ReadBuf(ref strm, ref Unsafe.Add(ref window, s.strstart), have);
            s.strstart += have;
            s.insert += Math.Min(have, s.w_size - s.insert);
        }
        if (s.high_water < s.strstart)
            s.high_water = s.strstart;

        /* There was not enough avail_out to write a complete worthy or flushed
         * stored block to next_out. Write a stored block to pending instead, if we
         * have enough input for a worthy block, or if flushing and there is enough
         * room for the remaining input as a stored block in the pending buffer.
         */
        have = (uint)((s.bi_valid + 42) >> 3); // number of header bytes
        // maximum stored block length that will fit in pending:
        have = Math.Min(s.pending_buf_size - have, MaxStored);
        min_block = Math.Min(have, s.w_size);
        left = (uint)(s.strstart - s.block_start);
        if (left >= min_block ||
            (left != 0 || flush == Z_FINISH) && flush != Z_NO_FLUSH &&
             strm.avail_in == 0 && left <= have)
        {
            len = Math.Min(left, have);
            last = flush == Z_FINISH && strm.avail_in == 0 && len == left ? 1U : 0U;
            Tree.StoredBlock(s, ref Unsafe.Add(ref window, (uint)s.block_start), len, last, ref pending_buf);
            s.block_start += (int)len;
            FlushPending(ref strm, ref pending_buf, ref pending_out);
        }

        // We've done all we can with the available input and output.
        return last != 0 ? BlockState.FinishStarted : BlockState.NeedMore;
    }

    private static uint ReadBuf(ref ZStream strm, ref byte buf, uint size)
    {
        uint len = strm.avail_in;

        if (len > size)
            len = size;
        if (len == 0)
            return 0;

        strm.avail_in -= len;

        netUnsafe.CopyBlockUnaligned(ref buf, ref
#if NET7_0_OR_GREATER
            Unsafe.Add(ref strm.input_ptr, strm.next_in),
#else
            MemoryMarshal.GetReference(strm._input.Slice((int)strm.next_in)),
#endif
                len);
        if (strm.deflateState.wrap == 1)
            strm.Adler = Adler32.Update(strm.Adler, ref buf, len);

        strm.next_in += len;
        strm.total_in += len;

        return len;
    }

    private static BlockState DeflateHuff(ref ZStream strm, int flush, ref byte pending_buf, ref byte pending_out)
    {
        DeflateState s = strm.deflateState;
#if NET7_0_OR_GREATER
        ref DeflateRefs refs = ref strm.deflateRefs;
        InitRefFields(s, ref refs);
#endif
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
        ref ushort bl_count = ref
#if NET7_0_OR_GREATER
            refs.bl_count;
#else
            MemoryMarshal.GetReference<ushort>(s.bl_count);
#endif
        ref int heap = ref
#if NET7_0_OR_GREATER
            refs.heap;
#else
            MemoryMarshal.GetReference<int>(s.heap);
#endif
        ref byte depth = ref
#if NET7_0_OR_GREATER
            refs.depth;
#else
            MemoryMarshal.GetReference<byte>(s.depth);
#endif

        ref TreeNode sta_ltree = ref
#if NET7_0_OR_GREATER
            refs.sta_ltree;
#else
            MemoryMarshal.GetReference<TreeNode>(Tree.s_ltree);
#endif
        ref TreeNode sta_dtree = ref
#if NET7_0_OR_GREATER
            refs.sta_dtree;
#else
            MemoryMarshal.GetReference<TreeNode>(Tree.s_dtree);
#endif
        ref TreeNode dyn_ltree = ref
#if NET7_0_OR_GREATER
            refs.dyn_ltree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.dyn_ltree);
#endif
        ref TreeNode dyn_dtree = ref
#if NET7_0_OR_GREATER
            refs.dyn_dtree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.dyn_dtree);
#endif
        ref TreeNode bl_tree = ref
#if NET7_0_OR_GREATER
            refs.bl_tree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.bl_tree);
#endif
        ref ushort bl_order = ref
#if NET7_0_OR_GREATER
            refs.bl_order;
#else
            MemoryMarshal.GetReference<ushort>(s_bl_order);
#endif
        ref byte dist_code = ref
#if NET7_0_OR_GREATER
            refs.dist_code;
#else
            MemoryMarshal.GetReference<byte>(s_dist_code);
#endif
        ref byte length_code = ref
#if NET7_0_OR_GREATER
            refs.length_code;
#else
            MemoryMarshal.GetReference<byte>(s_length_code);
#endif
        ref int base_dist = ref
#if NET7_0_OR_GREATER
            refs.base_dist;
#else
            MemoryMarshal.GetReference<int>(s_base_dist);
#endif
        ref int base_length = ref
#if NET7_0_OR_GREATER
            refs.base_length;
#else
            MemoryMarshal.GetReference<int>(s_base_length);
#endif
        ref int extra_dbits = ref
#if NET7_0_OR_GREATER
            refs.extra_dbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_dbits);
#endif
        ref int extra_lbits = ref
#if NET7_0_OR_GREATER
            refs.extra_lbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_lbits);
#endif
        ref int extra_blbits = ref
#if NET7_0_OR_GREATER
            refs.extra_blbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_blbits);
#endif
        BlockState state;
        for (; ; )
        {
            // Make sure that we have a literal to write.
            if (s.lookahead == 0)
            {
                FillWindow(ref strm, ref window, ref prev, ref head);
#pragma warning disable CA1508
                if (s.lookahead == 0)
#pragma warning restore CA1508
                {
                    if (flush == Z_NO_FLUSH)
                        return BlockState.NeedMore;
                    break; // flush the current block
                }
            }

            // Output a literal byte
            s.match_length = 0;
            byte c = Unsafe.Add(ref window, s.strstart);
            Trace.Tracevv($"{Convert.ToChar(c)}");
            TreeTallyLit(s, c, out bool bflush, ref pending_buf, ref dyn_ltree, ref dyn_dtree,
                ref dist_code, ref length_code);
            s.lookahead--;
            s.strstart++;
            if (bflush && FlushBlock(ref strm, 0, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
                return state;
        }

        s.insert = 0;
        if (flush == Z_FINISH)
        {
            if (FlushBlock(ref strm, 1, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
                return state;
            return BlockState.FinishDone;
        }
        if (s.sym_next != 0 && FlushBlock(ref strm, 0, ref window, out state, ref pending_buf, ref pending_out,
            ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
            ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
            ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
            return state;

        return BlockState.BlockDone;
    }

    private static void FillWindow(ref ZStream strm, ref byte window, ref ushort prev, ref ushort head)
    {
        DeflateState s = strm.deflateState;
        uint wsize = s.w_size;

        Debug.Assert(s.lookahead < MinLookAhead, "already enough lookahead");

        do
        {
            uint more = s.window_size - s.lookahead - s.strstart; // Amount of free space at the end of the window.

            /* If the window is almost full and there is insufficient lookahead,
             * move the upper half to the lower one to make room in the upper half.
             */
            if (s.strstart >= wsize + s.w_size - MinLookAhead)
            {
                uint sourceBytesToCopy = wsize - more;
                netUnsafe.CopyBlockUnaligned(ref window, ref Unsafe.Add(ref window, wsize), sourceBytesToCopy);
                s.match_start -= wsize;
                s.strstart -= wsize; // we now have strstart >= MaxDist
                s.block_start -= (int)wsize;
                if (s.insert > s.strstart)
                    s.insert = s.strstart;
                SlideHash(s, ref prev, ref head);
                more += wsize;
            }
            if (strm.avail_in == 0)
                break;

            /* If there was no sliding:
             *    strstart <= WSize+MaxDist-1 && lookahead <= MinLookAhead - 1 &&
             *    more == window_size - lookahead - strstart
             * => more >= window_size - (MinLookAhead-1 + WSize + MaxDist-1)
             * => more >= window_size - 2*WSize + 2
             * In the BIG_MEM or MMAP case (not yet supported),
             *   window_size == input_size + MinLookAhead  &&
             *   strstart + s->lookahead <= input_size => more >= MinLookAhead.
             * Otherwise, window_size == 2*WSize so more >= 2.
             * If there was sliding, more >= WSize. So in all cases, more >= 2.
             */
            Debug.Assert(more >= 2, "more < 2");

            uint n = ReadBuf(ref strm, ref Unsafe.Add(ref window, s.strstart + s.lookahead), more);
            s.lookahead += n;

            // Initialize the hash value now that we have some input:
            if (s.lookahead + s.insert >= MinMatch)
            {
                uint str = s.strstart - s.insert;
                s.ins_h = Unsafe.Add(ref window, str);
                UpdateHash(s, ref s.ins_h, Unsafe.Add(ref window, str + 1));

                while (s.insert != 0)
                {
                    UpdateHash(s, ref s.ins_h, Unsafe.Add(ref window, str + MinMatch - 1));
                    ref ushort temp = ref Unsafe.Add(ref head, s.ins_h);
                    Unsafe.Add(ref prev, str & s.w_mask) = temp;
                    temp = (ushort)str;
                    str++;
                    s.insert--;
                    if (s.lookahead + s.insert < MinMatch)
                        break;
                }
            }
            /* If the whole input has less than MinMatch bytes, ins_h is garbage,
             * but this is not important since only literal bytes will be emitted.
             */
        } while (s.lookahead < MinLookAhead && strm.avail_in != 0);

        /* If the WinInit bytes after the end of the current data have never been
         * written, then zero those bytes in order to avoid memory check reports of
         * the use of uninitialized (or uninitialised as Julian writes) bytes by
         * the longest match routines.  Update the high water mark for the next
         * time through here.  WinInit is set to MaxMatch since the longest match
         * routines allow scanning to strstart + MaxMatch, ignoring lookahead.
         */
        if (s.high_water < s.window_size)
        {
            uint curr = s.strstart + s.lookahead;
            uint init;

            if (s.high_water < curr)
            {
                /* Previous high water mark below current data -- zero WinInit
                 * bytes or up to end of window, whichever is less.
                 */
                init = s.window_size - curr;
                if (init > WinInit)
                    init = WinInit;
                netUnsafe.InitBlockUnaligned(ref Unsafe.Add(ref window, curr), 0, init);
                s.high_water = curr + init;
            }
            else if (s.high_water < curr + WinInit)
            {
                /* High water mark at or above current data, but below current data
                 * plus WinInit -- zero out to current data plus WinInit, or up
                 * to end of window, whichever is less.
                 */
                init = curr + WinInit - s.high_water;
                if (init > s.window_size - s.high_water)
                    init = s.window_size - s.high_water;
                netUnsafe.InitBlockUnaligned(ref Unsafe.Add(ref window, s.high_water), 0, init);
                s.high_water += init;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearHash(ref ZStream strm) =>
        netUnsafe.InitBlock(ref netUnsafe.As<ushort, byte>(ref
#if NET7_0_OR_GREATER
        strm.deflateRefs.head
#else
        MemoryMarshal.GetReference<ushort>(strm.deflateState.head)
#endif
        ), 0, (uint)strm.deflateState.head.Length * sizeof(ushort));

    private static void SlideHash(DeflateState s, ref ushort prev, ref ushort head)
    {
        uint wsize = s.w_size;
        uint n = s.hash_size;
        uint m;

        ref ushort p = ref Unsafe.Add(ref head, n);
        do
        {
            p = ref Unsafe.Subtract(ref p, 1U);
            m = p;
            p = (ushort)(m >= wsize ? m - wsize : 0);
        } while (--n > 0);
        n = wsize;
        p = ref Unsafe.Add(ref prev, n);
        do
        {
            p = ref Unsafe.Subtract(ref p, 1U);
            m = p;
            p = (ushort)(m >= wsize ? m - wsize : 0);
            /* If n is not on any hash chain, prev[n] is garbage but
             * its value will never be used.
             */
        } while (--n > 0);
    }

    /// <summary>
    /// Updates a hash value with the given input byte
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateHash(DeflateState s, ref uint h, byte c) =>
        h = (((h) << s.hash_shift) ^ c) & s.hash_mask;

    private static void FlushBlockOnly(ref ZStream strm, uint last, ref byte pending_buf, ref byte pending_out,
        ref byte window, ref TreeNode sta_ltree, ref TreeNode sta_dtree, ref TreeNode dyn_ltree, ref TreeNode dyn_dtree,
        ref TreeNode bl_tree, ref ushort bl_count, ref int heap, ref byte depth, ref ushort bl_order, ref byte dist_code,
        ref byte length_code, ref int base_dist, ref int base_length, ref int extra_dbits, ref int extra_lbits, ref int extra_blbits)
    {
        DeflateState s = strm.deflateState;
        uint block_start = (uint)s.block_start;
        ref byte buf = ref s.block_start >= 0L ? ref Unsafe.Add(ref window, block_start) : ref netUnsafe.NullRef<byte>();
        Tree.FlushBlock(ref strm, ref buf, s.strstart - block_start, last,
            ref pending_buf, ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count, ref heap, ref depth, ref bl_order,
            ref dist_code, ref length_code, ref base_dist, ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits);
        s.block_start = (int)s.strstart;
        FlushPending(ref strm, ref pending_buf, ref pending_out);
        Trace.Tracev("[FLUSH]");
    }

    private static bool FlushBlock(ref ZStream strm, uint last, ref byte window, out BlockState state,
        ref byte pending_buf, ref byte pending_out, ref TreeNode sta_ltree, ref TreeNode sta_dtree,
        ref TreeNode dyn_ltree, ref TreeNode dyn_dtree, ref TreeNode bl_tree, ref ushort bl_count, ref int heap,
        ref byte depth, ref ushort bl_order, ref byte dist_code, ref byte length_code, ref int base_dist, ref int base_length,
        ref int extra_dbits, ref int extra_lbits, ref int extra_blbits)
    {
        FlushBlockOnly(ref strm, last, ref pending_buf, ref pending_out, ref window, ref sta_ltree, ref sta_dtree,
            ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count, ref heap, ref depth, ref bl_order, ref dist_code,
            ref length_code, ref base_dist, ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits);
        if (strm.avail_out == 0)
        {
            state = (last != 0) ? BlockState.FinishStarted : BlockState.NeedMore;
            return true;
        }

        state = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TreeTallyLit(DeflateState s, byte c, out bool flush,
        ref byte pending_buf, ref TreeNode dyn_ltree, ref TreeNode dyn_dtree,
        ref byte dist_code, ref byte length_code)
#if DEBUG
    {
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = 0;
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = 0;
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = c;
        Unsafe.Add(ref dyn_ltree, c).fc++;
        flush = s.sym_next == s.sym_end;
    }
#else
        => flush = Tree.Tally(s, 0, c, ref pending_buf, ref dyn_ltree, ref dyn_dtree,
            ref dist_code, ref length_code);
#endif

    private static BlockState DeflateRle(ref ZStream strm, int flush, ref byte pending_buf, ref byte pending_out)
    {
        DeflateState s = strm.deflateState;
#if NET7_0_OR_GREATER
        ref DeflateRefs refs = ref strm.deflateRefs;
        InitRefFields(s, ref refs);
#endif
        ref byte window = ref
#if NET7_0_OR_GREATER
            refs.window;
#else
            MemoryMarshal.GetReference<byte>(s.window);
#endif
        ref ushort sprev = ref
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
        ref ushort bl_count = ref
#if NET7_0_OR_GREATER
            refs.bl_count;
#else
            MemoryMarshal.GetReference<ushort>(s.bl_count);
#endif
        ref int heap = ref
#if NET7_0_OR_GREATER
            refs.heap;
#else
            MemoryMarshal.GetReference<int>(s.heap);
#endif
        ref byte depth = ref
#if NET7_0_OR_GREATER
            refs.depth;
#else
            MemoryMarshal.GetReference<byte>(s.depth);
#endif

        ref TreeNode sta_ltree = ref
#if NET7_0_OR_GREATER
            refs.sta_ltree;
#else
            MemoryMarshal.GetReference<TreeNode>(Tree.s_ltree);
#endif
        ref TreeNode sta_dtree = ref
#if NET7_0_OR_GREATER
            refs.sta_dtree;
#else
            MemoryMarshal.GetReference<TreeNode>(Tree.s_dtree);
#endif
        ref TreeNode dyn_ltree = ref
#if NET7_0_OR_GREATER
            refs.dyn_ltree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.dyn_ltree);
#endif
        ref TreeNode dyn_dtree = ref
#if NET7_0_OR_GREATER
            refs.dyn_dtree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.dyn_dtree);
#endif
        ref TreeNode bl_tree = ref
#if NET7_0_OR_GREATER
            refs.bl_tree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.bl_tree);
#endif
        ref ushort bl_order = ref
#if NET7_0_OR_GREATER
            refs.bl_order;
#else
            MemoryMarshal.GetReference<ushort>(s_bl_order);
#endif
        ref byte dist_code = ref
#if NET7_0_OR_GREATER
            refs.dist_code;
#else
            MemoryMarshal.GetReference<byte>(s_dist_code);
#endif
        ref byte length_code = ref
#if NET7_0_OR_GREATER
            refs.length_code;
#else
            MemoryMarshal.GetReference<byte>(s_length_code);
#endif
        ref int base_dist = ref
#if NET7_0_OR_GREATER
            refs.base_dist;
#else
            MemoryMarshal.GetReference<int>(s_base_dist);
#endif
        ref int base_length = ref
#if NET7_0_OR_GREATER
            refs.base_length;
#else
            MemoryMarshal.GetReference<int>(s_base_length);
#endif
        ref int extra_dbits = ref
#if NET7_0_OR_GREATER
            refs.extra_dbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_dbits);
#endif
        ref int extra_lbits = ref
#if NET7_0_OR_GREATER
            refs.extra_lbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_lbits);
#endif
        ref int extra_blbits = ref
#if NET7_0_OR_GREATER
            refs.extra_blbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_blbits);
#endif
        bool bflush; // set if current block must be flushed
        BlockState state;
        for (; ; )
        {
            /* Make sure that we always have enough lookahead, except
             * at the end of the input file. We need MaxMatch bytes
             * for the longest run, plus one for the unrolled loop.
             */
            if (s.lookahead <= MaxMatch)
            {
                FillWindow(ref strm, ref window, ref sprev, ref head);
                if (s.lookahead <= MaxMatch && flush == Z_NO_FLUSH)
                    return BlockState.NeedMore;
                if (s.lookahead == 0)
                    break; // flush the current block
            }

            // See how many times the previous byte repeats
            s.match_length = 0;
            if (s.lookahead >= MinMatch && s.strstart > 0)
            {
                ref byte scan = ref Unsafe.Add(ref window, s.strstart - 1); // scan goes up to strend for length of run
                uint prev = scan; // byte at distance one to match
                if (prev == (scan = ref Unsafe.Add(ref scan, 1U))
                    && prev == (scan = ref Unsafe.Add(ref scan, 1U))
                    && prev == (scan = ref Unsafe.Add(ref scan, 1U)))
                {
                    ref byte strend = ref Unsafe.Add(ref window, s.strstart + MaxMatch);
                    do
                    {
                    } while (prev == (scan = ref Unsafe.Add(ref scan, 1U))
                        && prev == (scan = ref Unsafe.Add(ref scan, 1U))
                        && prev == (scan = ref Unsafe.Add(ref scan, 1U))
                        && prev == (scan = ref Unsafe.Add(ref scan, 1U))
                        && prev == (scan = ref Unsafe.Add(ref scan, 1U))
                        && prev == (scan = ref Unsafe.Add(ref scan, 1U))
                        && prev == (scan = ref Unsafe.Add(ref scan, 1U))
                        && prev == (scan = ref Unsafe.Add(ref scan, 1U))
                        && netUnsafe.IsAddressLessThan(ref scan, ref strend));
                    s.match_length = MaxMatch - (uint)netUnsafe.ByteOffset(ref scan, ref strend);
                    if (s.match_length > s.lookahead)
                        s.match_length = s.lookahead;
                }
                Debug.Assert(netUnsafe.IsAddressGreaterThan(ref Unsafe.Add(ref window, s.window_size - 1), ref scan), "wild scan");
            }

            // Emit match if have run of MinMatch or longer, else emit literal
            if (s.match_length >= MinMatch)
            {
                TreeTallyDist(s, 1, s.match_length - MinMatch, out bflush,
                    ref pending_buf, ref dyn_ltree, ref dyn_dtree, ref dist_code, ref length_code);

                s.lookahead -= s.match_length;
                s.strstart += s.match_length;
                s.match_length = 0;
            }
            else
            {
                // No match, output a literal byte
                byte b = Unsafe.Add(ref window, s.strstart);
                Trace.Tracevv($"{Convert.ToChar(b)}");
                TreeTallyLit(s, b, out bflush, ref pending_buf, ref dyn_ltree, ref dyn_dtree,
                    ref dist_code, ref length_code);
                s.lookahead--;
                s.strstart++;
            }
            if (bflush && FlushBlock(ref strm, 0, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
                return state;
        }

        s.insert = 0;
        if (flush == Z_FINISH)
        {
            if (FlushBlock(ref strm, 1, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
                return state;
            return BlockState.FinishDone;
        }
        if (s.sym_next != 0 && FlushBlock(ref strm, 0, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
            return state;

        return BlockState.BlockDone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TreeTallyDist(DeflateState s, uint distance, uint length, out bool flush,
        ref byte pending_buf, ref TreeNode dyn_ltree, ref TreeNode dyn_dtree, ref byte dist_code,
        ref byte length_code)
#if DEBUG
    {
        byte len = (byte)length;
        ushort dist = (ushort)distance;
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = (byte)dist;
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = (byte)(dist >> 8);
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = len;
        dist--;
        Unsafe.Add(ref dyn_ltree, (uint)(s_length_code[len] + Literals + 1)).fc++;
        Unsafe.Add(ref dyn_dtree, Tree.DCode(dist, ref dist_code)).fc++;
        flush = s.sym_next == s.sym_end;
    }
#else
     => flush = Tree.Tally(s, distance, length, ref pending_buf, ref dyn_ltree, ref dyn_dtree,
         ref dist_code, ref length_code);
#endif

    private static BlockState DeflateFast(ref ZStream strm, int flush, ref byte pending_buf, ref byte pending_out)
    {
        DeflateState s = strm.deflateState;
#if NET7_0_OR_GREATER
        ref DeflateRefs refs = ref strm.deflateRefs;
        InitRefFields(s, ref refs);
#endif
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
        ref ushort bl_count = ref
#if NET7_0_OR_GREATER
            refs.bl_count;
#else
            MemoryMarshal.GetReference<ushort>(s.bl_count);
#endif
        ref int heap = ref
#if NET7_0_OR_GREATER
            refs.heap;
#else
            MemoryMarshal.GetReference<int>(s.heap);
#endif
        ref byte depth = ref
#if NET7_0_OR_GREATER
            refs.depth;
#else
            MemoryMarshal.GetReference<byte>(s.depth);
#endif

        ref TreeNode sta_ltree = ref
#if NET7_0_OR_GREATER
            refs.sta_ltree;
#else
            MemoryMarshal.GetReference<TreeNode>(Tree.s_ltree);
#endif
        ref TreeNode sta_dtree = ref
#if NET7_0_OR_GREATER
            refs.sta_dtree;
#else
            MemoryMarshal.GetReference<TreeNode>(Tree.s_dtree);
#endif
        ref TreeNode dyn_ltree = ref
#if NET7_0_OR_GREATER
            refs.dyn_ltree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.dyn_ltree);
#endif
        ref TreeNode dyn_dtree = ref
#if NET7_0_OR_GREATER
            refs.dyn_dtree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.dyn_dtree);
#endif
        ref TreeNode bl_tree = ref
#if NET7_0_OR_GREATER
            refs.bl_tree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.bl_tree);
#endif
        ref ushort bl_order = ref
#if NET7_0_OR_GREATER
            refs.bl_order;
#else
            MemoryMarshal.GetReference<ushort>(s_bl_order);
#endif
        ref byte dist_code = ref
#if NET7_0_OR_GREATER
            refs.dist_code;
#else
            MemoryMarshal.GetReference<byte>(s_dist_code);
#endif
        ref byte length_code = ref
#if NET7_0_OR_GREATER
            refs.length_code;
#else
            MemoryMarshal.GetReference<byte>(s_length_code);
#endif
        ref int base_dist = ref
#if NET7_0_OR_GREATER
            refs.base_dist;
#else
            MemoryMarshal.GetReference<int>(s_base_dist);
#endif
        ref int base_length = ref
#if NET7_0_OR_GREATER
            refs.base_length;
#else
            MemoryMarshal.GetReference<int>(s_base_length);
#endif
        ref int extra_dbits = ref
#if NET7_0_OR_GREATER
            refs.extra_dbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_dbits);
#endif
        ref int extra_lbits = ref
#if NET7_0_OR_GREATER
            refs.extra_lbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_lbits);
#endif
        ref int extra_blbits = ref
#if NET7_0_OR_GREATER
            refs.extra_blbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_blbits);
#endif
        bool bflush; // set if current block must be flushed
        BlockState state;
        for (; ; )
        {
            /* Make sure that we always have enough lookahead, except
             * at the end of the input file. We need MaxMatch bytes
             * for the next match, plus MinMatch bytes to insert the
             * string following the next match.
             */
            if (s.lookahead < MinLookAhead)
            {
                FillWindow(ref strm, ref window, ref prev, ref head);
                if (s.lookahead < MinLookAhead && flush == Z_NO_FLUSH)
                    return BlockState.NeedMore;
                if (s.lookahead == 0)
                    break; // flush the current block
            }

            /* Insert the string window[strstart .. strstart+2] in the
             * dictionary, and set hash_head to the head of the hash chain:
             */
            uint hash_head = 0; // head of the hash chain
            if (s.lookahead >= MinMatch)
                InsertString(s, s.strstart, ref hash_head, ref window, ref prev, ref head);

            /* Find the longest match, discarding those <= prev_length.
             * At this point we have always match_length < MinMatch
             */
            if (hash_head != 0 && s.strstart - hash_head <= MaxDist(s))
            {
                /* To simplify the code, we prevent matches with the string
                 * of window index 0 (in particular we have to avoid a match
                 * of the string with itself at the start of the input file).
                 */
                s.match_length = LongestMatch(s, hash_head, ref window, ref prev);
                // LongestMatch() sets match_start
            }
            if (s.match_length >= MinMatch)
            {
                TreeTallyDist(s, s.strstart - s.match_start, s.match_length - MinMatch, out bflush, ref pending_buf,
                    ref dyn_ltree, ref dyn_dtree, ref dist_code, ref length_code);

                s.lookahead -= s.match_length;

                /* Insert new strings in the hash table only if the match length
                 * is not too large. This saves time but degrades compression.
                 */
                if (s.match_length <= s.max_lazy_match &&
                    s.lookahead >= MinMatch)
                {
                    s.match_length--; // string at strstart already in table
                    do
                    {
                        s.strstart++;
                        InsertString(s, s.strstart, ref hash_head, ref window, ref prev, ref head);
                        /* strstart never exceeds WSize-MaxMatch, so there are
                         * always MinMatch bytes ahead.
                         */
                    } while (--s.match_length != 0);
                    s.strstart++;
                }
                else
                {
                    s.strstart += s.match_length;
                    s.match_length = 0;
                    s.ins_h = Unsafe.Add(ref window, s.strstart);
                    UpdateHash(s, ref s.ins_h, Unsafe.Add(ref window, s.strstart + 1));

                    /* If lookahead < MinMatch, ins_h is garbage, but it does not
                     * matter since it will be recomputed at next deflate call.
                     */
                }
            }
            else
            {
                // No match, output a literal byte
                byte b = Unsafe.Add(ref window, s.strstart);
                Trace.Tracevv($"{Convert.ToChar(b)}");
                TreeTallyLit(s, b, out bflush, ref pending_buf, ref dyn_ltree, ref dyn_dtree,
                    ref dist_code, ref length_code);
                s.lookahead--;
                s.strstart++;
            }
            if (bflush && FlushBlock(ref strm, 0, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
                return state;
        }
        s.insert = s.strstart < MinMatch - 1 ? s.strstart : MinMatch - 1;
        if (flush == Z_FINISH)
        {
            if (FlushBlock(ref strm, 1, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
                return state;
            return BlockState.FinishDone;
        }
        if (s.sym_next != 0 && FlushBlock(ref strm, 0, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
            return state;
        return BlockState.BlockDone;
    }

    private static void InsertString(DeflateState s, uint str, ref uint match_head,
        ref byte window, ref ushort prev, ref ushort head)
    {
        UpdateHash(s, ref s.ins_h, Unsafe.Add(ref window, str + (MinMatch - 1)));
        ref ushort temp = ref Unsafe.Add(ref head, s.ins_h);
        match_head = Unsafe.Add(ref prev, (str) & s.w_mask) = temp;
        temp = (ushort)str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint MaxDist(DeflateState s) => s.w_size - MinLookAhead;

    private static uint LongestMatch(DeflateState s, uint cur_match, ref byte window, ref ushort prev)
    {
        uint chain_length = s.max_chain_length; // max hash chain length
        ref byte scan = ref Unsafe.Add(ref window, s.strstart); // current string
        uint len;                               // length of current match
        uint best_len = s.prev_length;          // best match length so far
        int nice_match = s.nice_match;          // stop if match long enough
        uint limit = s.strstart > MaxDist(s) ? s.strstart - MaxDist(s) : 0;
        /* Stop when cur_match becomes <= limit. To simplify the code,
         * we prevent matches with the string of window index 0.
         */
        uint wmask = s.w_mask;
        ref byte strend = ref Unsafe.Add(ref window, s.strstart + MaxMatch);
        byte scan_end1 = Unsafe.Add(ref scan, best_len - 1);
        byte scan_end = Unsafe.Add(ref scan, best_len);

        /* The code is optimized for HASH_BITS >= 8 and MaxMatch-2 multiple of 16.
         * It is easy to get rid of this optimization if necessary.
         */
        Debug.Assert(s.hash_bits >= 8, "Code too clever");

        // Do not waste too much time if we already have a good match:
        if (s.prev_length >= s.good_match)
            chain_length >>= 2;

        /* Do not look for matches beyond the end of the input. This is necessary
         * to make deflate deterministic.
         */
        if (nice_match > s.lookahead)
            nice_match = (int)s.lookahead;

        Debug.Assert(s.strstart <= s.window_size - MinLookAhead, "need lookahead");

        do
        {
            Debug.Assert(cur_match < s.strstart, "no future");
            ref byte match = ref Unsafe.Add(ref window, cur_match); // matched string

            /* Skip to next match if the match length cannot increase
             * or if the match length is less than 2.  Note that the checks below
             * for insufficient lookahead only occur occasionally for performance
             * reasons.  Therefore uninitialized memory will be accessed, and
             * conditional jumps will be made that depend on those values.
             * However the length of the match is limited to the lookahead, so
             * the output of deflate is not affected by the uninitialized values.
             */

            if (Unsafe.Add(ref match, best_len) != scan_end
                || Unsafe.Add(ref match, best_len - 1) != scan_end1
                || match != scan
                || (match = ref Unsafe.Add(ref match, 1U)) != Unsafe.Add(ref scan, 1U))
                continue;

            /* The check at best_len-1 can be removed because it will be made
             * again later. (This heuristic is not always a win.)
             * It is not necessary to compare scan[2] and match[2] since they
             * are always equal when the other bytes match, given that
             * the hash keys are equal and that HASH_BITS >= 8.
             */
            scan = ref Unsafe.Add(ref scan, 2U);
            match = ref Unsafe.Add(ref match, 1U);

            Debug.Assert(scan == match, "match[2]?");

            /* We check for insufficient lookahead only every 8th comparison;
             * the 256th check will be made at strstart + 258.
             */
            do
            {
            } while ((scan = ref Unsafe.Add(ref scan, 1U)) == (match = ref Unsafe.Add(ref match, 1U))
                && (scan = ref Unsafe.Add(ref scan, 1U)) == (match = ref Unsafe.Add(ref match, 1U))
                && (scan = ref Unsafe.Add(ref scan, 1U)) == (match = ref Unsafe.Add(ref match, 1U))
                && (scan = ref Unsafe.Add(ref scan, 1U)) == (match = ref Unsafe.Add(ref match, 1U))
                && (scan = ref Unsafe.Add(ref scan, 1U)) == (match = ref Unsafe.Add(ref match, 1U))
                && (scan = ref Unsafe.Add(ref scan, 1U)) == (match = ref Unsafe.Add(ref match, 1U))
                && (scan = ref Unsafe.Add(ref scan, 1U)) == (match = ref Unsafe.Add(ref match, 1U))
                && (scan = ref Unsafe.Add(ref scan, 1U)) == (match = ref Unsafe.Add(ref match, 1U))
                && netUnsafe.IsAddressLessThan(ref scan, ref strend));

            Debug.Assert(scan <= window + (s.window_size - 1), "wild scan");

            len = MaxMatch - (uint)netUnsafe.ByteOffset(ref scan, ref strend);
            scan = ref Unsafe.Subtract(ref strend, (uint)MaxMatch);

            if (len > best_len)
            {
                s.match_start = cur_match;
                best_len = len;
                if (len >= nice_match)
                    break;
                scan_end1 = Unsafe.Add(ref scan, best_len - 1);
                scan_end = Unsafe.Add(ref scan, best_len);
            }
        } while ((cur_match = Unsafe.Add(ref prev, cur_match & wmask)) > limit && --chain_length != 0);

        if (best_len <= s.lookahead)
            return best_len;

        return s.lookahead;
    }

    private static BlockState DeflateSlow(ref ZStream strm, int flush, ref byte pending_buf, ref byte pending_out)
    {
        DeflateState s = strm.deflateState;
#if NET7_0_OR_GREATER
        ref DeflateRefs refs = ref strm.deflateRefs;
        InitRefFields(s, ref refs);
#endif
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
        ref ushort bl_count = ref
#if NET7_0_OR_GREATER
            refs.bl_count;
#else
            MemoryMarshal.GetReference<ushort>(s.bl_count);
#endif
        ref int heap = ref
#if NET7_0_OR_GREATER
            refs.heap;
#else
            MemoryMarshal.GetReference<int>(s.heap);
#endif
        ref byte depth = ref
#if NET7_0_OR_GREATER
            refs.depth;
#else
            MemoryMarshal.GetReference<byte>(s.depth);
#endif

        ref TreeNode sta_ltree = ref
#if NET7_0_OR_GREATER
            refs.sta_ltree;
#else
            MemoryMarshal.GetReference<TreeNode>(Tree.s_ltree);
#endif
        ref TreeNode sta_dtree = ref
#if NET7_0_OR_GREATER
            refs.sta_dtree;
#else
            MemoryMarshal.GetReference<TreeNode>(Tree.s_dtree);
#endif
        ref TreeNode dyn_ltree = ref
#if NET7_0_OR_GREATER
            refs.dyn_ltree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.dyn_ltree);
#endif
        ref TreeNode dyn_dtree = ref
#if NET7_0_OR_GREATER
            refs.dyn_dtree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.dyn_dtree);
#endif
        ref TreeNode bl_tree = ref
#if NET7_0_OR_GREATER
            refs.bl_tree;
#else
            MemoryMarshal.GetReference<TreeNode>(s.bl_tree);
#endif
        ref ushort bl_order = ref
#if NET7_0_OR_GREATER
            refs.bl_order;
#else
            MemoryMarshal.GetReference<ushort>(s_bl_order);
#endif
        ref byte dist_code = ref
#if NET7_0_OR_GREATER
            refs.dist_code;
#else
            MemoryMarshal.GetReference<byte>(s_dist_code);
#endif
        ref byte length_code = ref
#if NET7_0_OR_GREATER
            refs.length_code;
#else
            MemoryMarshal.GetReference<byte>(s_length_code);
#endif
        ref int base_dist = ref
#if NET7_0_OR_GREATER
            refs.base_dist;
#else
            MemoryMarshal.GetReference<int>(s_base_dist);
#endif
        ref int base_length = ref
#if NET7_0_OR_GREATER
            refs.base_length;
#else
            MemoryMarshal.GetReference<int>(s_base_length);
#endif
        ref int extra_dbits = ref
#if NET7_0_OR_GREATER
            refs.extra_dbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_dbits);
#endif
        ref int extra_lbits = ref
#if NET7_0_OR_GREATER
            refs.extra_lbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_lbits);
#endif
        ref int extra_blbits = ref
#if NET7_0_OR_GREATER
            refs.extra_blbits;
#else
            MemoryMarshal.GetReference<int>(s_extra_blbits);
#endif
        bool bflush; // set if current block must be flushed
        BlockState state;
        // Process the input block.
        for (; ; )
        {
            /* Make sure that we always have enough lookahead, except
             * at the end of the input file. We need MaxMatch bytes
             * for the next match, plus MinMatch bytes to insert the
             * string following the next match.
             */
            if (s.lookahead < MinLookAhead)
            {
                FillWindow(ref strm, ref window, ref prev, ref head);
                if (s.lookahead < MinLookAhead && flush == Z_NO_FLUSH)
                {
                    return BlockState.NeedMore;
                }
                if (s.lookahead == 0)
                    break; // flush the current block
            }

            /* Insert the string window[strstart .. strstart+2] in the
             * dictionary, and set hash_head to the head of the hash chain:
             */
            uint hash_head = 0; // head of hash chain
            if (s.lookahead >= MinMatch)
                InsertString(s, s.strstart, ref hash_head, ref window, ref prev, ref head);

            // Find the longest match, discarding those <= prev_length.
            s.prev_length = s.match_length;
            s.prev_match = s.match_start;
            s.match_length = MinMatch - 1;

            if (hash_head != 0 && s.prev_length < s.max_lazy_match &&
                s.strstart - hash_head <= MaxDist(s))
            {
                /* To simplify the code, we prevent matches with the string
                 * of window index 0 (in particular we have to avoid a match
                 * of the string with itself at the start of the input file).
                 */
                s.match_length = LongestMatch(s, hash_head, ref window, ref prev);
                // LongestMatch() sets match_start

                if (s.match_length <= 5 && (s.strategy == Z_FILTERED
                    || s.match_length == MinMatch && s.strstart - s.match_start > TooFar))
                {

                    /* If prev_match is also MinMatch, match_start is garbage
                     * but we will ignore the current match anyway.
                     */
                    s.match_length = MinMatch - 1;
                }
            }
            /* If there was a match at the previous step and the current
             * match is not better, output the previous match:
             */
            if (s.prev_length >= MinMatch && s.match_length <= s.prev_length)
            {
                uint max_insert = s.strstart + s.lookahead - MinMatch;
                // Do not insert strings in hash table beyond this.

                TreeTallyDist(s, s.strstart - 1 - s.prev_match, s.prev_length - MinMatch, out bflush, ref pending_buf,
                    ref dyn_ltree, ref dyn_dtree, ref dist_code, ref length_code);

                /* Insert in hash table all strings up to the end of the match.
                 * strstart-1 and strstart are already inserted. If there is not
                 * enough lookahead, the last two strings are not inserted in
                 * the hash table.
                 */
                s.lookahead -= s.prev_length - 1;
                s.prev_length -= 2;
                do
                {
                    if (++s.strstart <= max_insert)
                        InsertString(s, s.strstart, ref hash_head, ref window, ref prev, ref head);
                } while (--s.prev_length != 0);
                s.match_available = false;
                s.match_length = MinMatch - 1;
                s.strstart++;

                if (bflush && FlushBlock(ref strm, 0, ref window, out state, ref pending_buf, ref pending_out,
                    ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                    ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                    ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
                    return state;

            }
            else if (s.match_available)
            {
                /* If there was no match at the previous position, output a
                 * single literal. If there was a match but the current match
                 * is longer, truncate the previous match to a single literal.
                 */
                byte c = Unsafe.Add(ref window, s.strstart - 1);
                Trace.Tracevv($"{Convert.ToChar(c)}");
                TreeTallyLit(s, c, out bflush, ref pending_buf, ref dyn_ltree, ref dyn_dtree,
                    ref dist_code, ref length_code);
                if (bflush)
                    FlushBlockOnly(ref strm, 0, ref pending_buf, ref pending_out, ref window, ref sta_ltree,
                        ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count, ref heap,
                        ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist, ref base_length,
                        ref extra_dbits, ref extra_lbits, ref extra_blbits);
                s.strstart++;
                s.lookahead--;
                if (strm.avail_out == 0)
                    return BlockState.NeedMore;
            }
            else
            {
                // There is no previous match to compare with, wait for the next step to decide.
                s.match_available = true;
                s.strstart++;
                s.lookahead--;
            }
        }
        Debug.Assert(flush != Z_NO_FLUSH, "no flush?");
        if (s.match_available)
        {
            byte b = Unsafe.Add(ref window, s.strstart - 1);
            Trace.Tracevv($"{Convert.ToChar(b)}");
            TreeTallyLit(s, b, out _, ref pending_buf, ref dyn_ltree, ref dyn_dtree,
                ref dist_code, ref length_code);
            s.match_available = false;
        }
        s.insert = s.strstart < MinMatch - 1 ? s.strstart : MinMatch - 1;
        if (flush == Z_FINISH)
        {
            if (FlushBlock(ref strm, 1, ref window, out state, ref pending_buf, ref pending_out,
                ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
                ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
                ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
                return state;
            return BlockState.FinishDone;
        }
        if (s.sym_next != 0 && FlushBlock(ref strm, 0, ref window, out state, ref pending_buf, ref pending_out,
            ref sta_ltree, ref sta_dtree, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count,
            ref heap, ref depth, ref bl_order, ref dist_code, ref length_code, ref base_dist,
            ref base_length, ref extra_dbits, ref extra_lbits, ref extra_blbits))
            return state;

        return BlockState.BlockDone;
    }

#if NET7_0_OR_GREATER
    private static void InitRefFields(DeflateState s, ref DeflateRefs refs)
    {
        if (netUnsafe.IsNullRef(ref refs.bl_order))
        {
            refs.window = ref MemoryMarshal.GetReference<byte>(s.window);
            refs.prev = ref MemoryMarshal.GetReference<ushort>(s.prev);
            refs.bl_count = ref MemoryMarshal.GetReference<ushort>(s.bl_count);
            refs.heap = ref MemoryMarshal.GetReference<int>(s.heap);
            refs.depth = ref MemoryMarshal.GetReference<byte>(s.depth);
            refs.sta_ltree = ref MemoryMarshal.GetReference<TreeNode>(Tree.s_ltree);
            refs.sta_dtree = ref MemoryMarshal.GetReference<TreeNode>(Tree.s_dtree);
            refs.bl_order = ref MemoryMarshal.GetReference<ushort>(s_bl_order);
            refs.dist_code = ref MemoryMarshal.GetReference<byte>(s_dist_code);
            refs.length_code = ref MemoryMarshal.GetReference<byte>(s_length_code);
            refs.base_dist = ref MemoryMarshal.GetReference<int>(s_base_dist);
            refs.base_length = ref MemoryMarshal.GetReference<int>(s_base_length);
            refs.extra_dbits = ref MemoryMarshal.GetReference<int>(s_extra_dbits);
            refs.extra_lbits = ref MemoryMarshal.GetReference<int>(s_extra_lbits);
            refs.extra_blbits = ref MemoryMarshal.GetReference<int>(s_extra_blbits);
        }
    }
#endif
}