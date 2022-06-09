﻿// Original code and comments Copyright (C) 1995-2021 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static ZLibDotNet.Deflate.Constants;

namespace ZLibDotNet.Deflate;

internal static class Tree
{
    private const byte MaxBlBits = 7;       // Bit length codes must not exceed MAX_BL_BITS bits
    private const ushort EndBlock = 256;    // end of block literal code
    private const byte Rep_3_6 = 16;        // repeat previous bit length 3-6 times (2 bits of repeat count)
    private const byte RepZ_3_10 = 17;      // repeat a zero length 3-10 times  (3 bits of repeat count)
    private const byte RepZ_11_138 = 18;    // repeat a zero length 11-138 times  (7 bits of repeat count)
    private const byte Smallest = 1;        // Index within the heap array of least frequent node in the Huffman tree
    private const byte StaticTrees = 1;
    private const byte DynTrees = 2;
    private const int DistCodeLen = 512;

    private static readonly TreeNode[] s_ltree = new TreeNode[LCodes + 2]
    {
        new TreeNode(12, 8), new TreeNode(140, 8), new TreeNode(76, 8), new TreeNode(204, 8), new TreeNode(44, 8),
        new TreeNode(172, 8), new TreeNode(108, 8), new TreeNode(236, 8), new TreeNode(28, 8), new TreeNode(156, 8),
        new TreeNode(92, 8), new TreeNode(220, 8), new TreeNode(60, 8), new TreeNode(188, 8), new TreeNode(124, 8),
        new TreeNode(252, 8), new TreeNode( 2, 8), new TreeNode(130, 8), new TreeNode(66, 8), new TreeNode(194, 8),
        new TreeNode(34, 8), new TreeNode(162, 8), new TreeNode(98, 8), new TreeNode(226, 8), new TreeNode(18, 8),
        new TreeNode(146, 8), new TreeNode(82, 8), new TreeNode(210, 8), new TreeNode(50, 8), new TreeNode(178, 8),
        new TreeNode(114, 8), new TreeNode(242, 8), new TreeNode(10, 8), new TreeNode(138, 8), new TreeNode(74, 8),
        new TreeNode(202, 8), new TreeNode(42, 8), new TreeNode(170, 8), new TreeNode(106, 8), new TreeNode(234, 8),
        new TreeNode(26, 8), new TreeNode(154, 8), new TreeNode(90, 8), new TreeNode(218, 8), new TreeNode(58, 8),
        new TreeNode(186, 8), new TreeNode(122, 8), new TreeNode(250, 8), new TreeNode( 6, 8), new TreeNode(134, 8),
        new TreeNode(70, 8), new TreeNode(198, 8), new TreeNode(38, 8), new TreeNode(166, 8), new TreeNode(102, 8),
        new TreeNode(230, 8), new TreeNode(22, 8), new TreeNode(150, 8), new TreeNode(86, 8), new TreeNode(214, 8),
        new TreeNode(54, 8), new TreeNode(182, 8), new TreeNode(118, 8), new TreeNode(246, 8), new TreeNode(14, 8),
        new TreeNode(142, 8), new TreeNode(78, 8), new TreeNode(206, 8), new TreeNode(46, 8), new TreeNode(174, 8),
        new TreeNode(110, 8), new TreeNode(238, 8), new TreeNode(30, 8), new TreeNode(158, 8), new TreeNode(94, 8),
        new TreeNode(222, 8), new TreeNode(62, 8), new TreeNode(190, 8), new TreeNode(126, 8), new TreeNode(254, 8),
        new TreeNode( 1, 8), new TreeNode(129, 8), new TreeNode(65, 8), new TreeNode(193, 8), new TreeNode(33, 8),
        new TreeNode(161, 8), new TreeNode(97, 8), new TreeNode(225, 8), new TreeNode(17, 8), new TreeNode(145, 8),
        new TreeNode(81, 8), new TreeNode(209, 8), new TreeNode(49, 8), new TreeNode(177, 8), new TreeNode(113, 8),
        new TreeNode(241, 8), new TreeNode( 9, 8), new TreeNode(137, 8), new TreeNode(73, 8), new TreeNode(201, 8),
        new TreeNode(41, 8), new TreeNode(169, 8), new TreeNode(105, 8), new TreeNode(233, 8), new TreeNode(25, 8),
        new TreeNode(153, 8), new TreeNode(89, 8), new TreeNode(217, 8), new TreeNode(57, 8), new TreeNode(185, 8),
        new TreeNode(121, 8), new TreeNode(249, 8), new TreeNode( 5, 8), new TreeNode(133, 8), new TreeNode(69, 8),
        new TreeNode(197, 8), new TreeNode(37, 8), new TreeNode(165, 8), new TreeNode(101, 8), new TreeNode(229, 8),
        new TreeNode(21, 8), new TreeNode(149, 8), new TreeNode(85, 8), new TreeNode(213, 8), new TreeNode(53, 8),
        new TreeNode(181, 8), new TreeNode(117, 8), new TreeNode(245, 8), new TreeNode(13, 8), new TreeNode(141, 8),
        new TreeNode(77, 8), new TreeNode(205, 8), new TreeNode(45, 8), new TreeNode(173, 8), new TreeNode(109, 8),
        new TreeNode(237, 8), new TreeNode(29, 8), new TreeNode(157, 8), new TreeNode(93, 8), new TreeNode(221, 8),
        new TreeNode(61, 8), new TreeNode(189, 8), new TreeNode(125, 8), new TreeNode(253, 8), new TreeNode(19, 9),
        new TreeNode(275, 9), new TreeNode(147, 9), new TreeNode(403, 9), new TreeNode(83, 9), new TreeNode(339, 9),
        new TreeNode(211, 9), new TreeNode(467, 9), new TreeNode(51, 9), new TreeNode(307, 9), new TreeNode(179, 9),
        new TreeNode(435, 9), new TreeNode(115, 9), new TreeNode(371, 9), new TreeNode(243, 9), new TreeNode(499, 9),
        new TreeNode(11, 9), new TreeNode(267, 9), new TreeNode(139, 9), new TreeNode(395, 9), new TreeNode(75, 9),
        new TreeNode(331, 9), new TreeNode(203, 9), new TreeNode(459, 9), new TreeNode(43, 9), new TreeNode(299, 9),
        new TreeNode(171, 9), new TreeNode(427, 9), new TreeNode(107, 9), new TreeNode(363, 9), new TreeNode(235, 9),
        new TreeNode(491, 9), new TreeNode(27, 9), new TreeNode(283, 9), new TreeNode(155, 9), new TreeNode(411, 9),
        new TreeNode(91, 9), new TreeNode(347, 9), new TreeNode(219, 9), new TreeNode(475, 9), new TreeNode(59, 9),
        new TreeNode(315, 9), new TreeNode(187, 9), new TreeNode(443, 9), new TreeNode(123, 9), new TreeNode(379, 9),
        new TreeNode(251, 9), new TreeNode(507, 9), new TreeNode( 7, 9), new TreeNode(263, 9), new TreeNode(135, 9),
        new TreeNode(391, 9), new TreeNode(71, 9), new TreeNode(327, 9), new TreeNode(199, 9), new TreeNode(455, 9),
        new TreeNode(39, 9), new TreeNode(295, 9), new TreeNode(167, 9), new TreeNode(423, 9), new TreeNode(103, 9),
        new TreeNode(359, 9), new TreeNode(231, 9), new TreeNode(487, 9), new TreeNode(23, 9), new TreeNode(279, 9),
        new TreeNode(151, 9), new TreeNode(407, 9), new TreeNode(87, 9), new TreeNode(343, 9), new TreeNode(215, 9),
        new TreeNode(471, 9), new TreeNode(55, 9), new TreeNode(311, 9), new TreeNode(183, 9), new TreeNode(439, 9),
        new TreeNode(119, 9), new TreeNode(375, 9), new TreeNode(247, 9), new TreeNode(503, 9), new TreeNode(15, 9),
        new TreeNode(271, 9), new TreeNode(143, 9), new TreeNode(399, 9), new TreeNode(79, 9), new TreeNode(335, 9),
        new TreeNode(207, 9), new TreeNode(463, 9), new TreeNode(47, 9), new TreeNode(303, 9), new TreeNode(175, 9),
        new TreeNode(431, 9), new TreeNode(111, 9), new TreeNode(367, 9), new TreeNode(239, 9), new TreeNode(495, 9),
        new TreeNode(31, 9), new TreeNode(287, 9), new TreeNode(159, 9), new TreeNode(415, 9), new TreeNode(95, 9),
        new TreeNode(351, 9), new TreeNode(223, 9), new TreeNode(479, 9), new TreeNode(63, 9), new TreeNode(319, 9),
        new TreeNode(191, 9), new TreeNode(447, 9), new TreeNode(127, 9), new TreeNode(383, 9), new TreeNode(255, 9),
        new TreeNode(511, 9), new TreeNode( 0, 7), new TreeNode(64, 7), new TreeNode(32, 7), new TreeNode(96, 7),
        new TreeNode(16, 7), new TreeNode(80, 7), new TreeNode(48, 7), new TreeNode(112, 7), new TreeNode( 8, 7),
        new TreeNode(72, 7), new TreeNode(40, 7), new TreeNode(104, 7), new TreeNode(24, 7), new TreeNode(88, 7),
        new TreeNode(56, 7), new TreeNode(120, 7), new TreeNode( 4, 7), new TreeNode(68, 7), new TreeNode(36, 7),
        new TreeNode(100, 7), new TreeNode(20, 7), new TreeNode(84, 7), new TreeNode(52, 7), new TreeNode(116, 7),
        new TreeNode( 3, 8), new TreeNode(131, 8), new TreeNode(67, 8), new TreeNode(195, 8), new TreeNode(35, 8),
        new TreeNode(163, 8), new TreeNode(99, 8), new TreeNode(227, 8)
    };

    private static readonly TreeNode[] s_dtree = new TreeNode[DCodes]
    {
        new TreeNode(0, 5), new TreeNode(16, 5), new TreeNode(8, 5), new TreeNode(24, 5), new TreeNode(4, 5),
        new TreeNode(20, 5), new TreeNode(12, 5), new TreeNode(28, 5), new TreeNode(2, 5), new TreeNode(18, 5),
        new TreeNode(10, 5), new TreeNode(26, 5), new TreeNode(6, 5), new TreeNode(22, 5), new TreeNode(14, 5),
        new TreeNode(30, 5), new TreeNode(1, 5), new TreeNode(17, 5), new TreeNode(9, 5), new TreeNode(25, 5),
        new TreeNode(5, 5), new TreeNode(21, 5), new TreeNode(13, 5), new TreeNode(29, 5), new TreeNode(3, 5),
        new TreeNode(19, 5), new TreeNode(11, 5), new TreeNode(27, 5), new TreeNode(7, 5), new TreeNode(23, 5)
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

    internal static readonly byte[] s_length_code = new byte[MaxMatch - MinMatch + 1] {
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

    private static readonly int[] s_base_dist = new int[DCodes] {
        0,     1,     2,     3,     4,     6,     8,    12,    16,    24,
        32,    48,    64,    96,   128,   192,   256,   384,   512,   768,
        1024,  1536,  2048,  3072,  4096,  6144,  8192, 12288, 16384, 24576
    };

    private static readonly int[] s_extra_lbits = // extra bits for each length code
        new int[LenghtCodes] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };

    private static readonly int[] s_extra_dbits = // extra bits for each distance code
        new int[DCodes] { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };

    private static readonly int[] s_extra_blbits = // extra bits for each bit length code
        new int[BlCodes] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 7 };

    private static readonly ushort[] s_bl_order = // The lengths of the bit length codes are sent in order of decreasing probability, to avoid transmitting the lengths for unused bit length codes.
        new ushort[BlCodes] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

    private static readonly StaticTree s_l_desc = new(s_ltree, s_extra_lbits, Literals + 1, LCodes, MaxBits);

    private static readonly StaticTree s_d_desc = new(s_dtree, s_extra_dbits, 0, DCodes, MaxBits);

    private static readonly StaticTree s_bl_desc = new(null, s_extra_blbits, 0, BlCodes, MaxBlBits);

    /// <summary>
    /// Initializes the tree data structures for a new zlib stream.
    /// </summary>
    internal static void Init(DeflateState s)
    {
        s.l_desc = new(s.dyn_ltree, s_l_desc);
        s.d_desc = new(s.dyn_dtree, s_d_desc);
        s.bl_desc = new(s.bl_tree, s_bl_desc);

        s.bi_buf = 0;
        s.bi_valid = 0;
#if DEBUG
        s.compressed_len = 0;
        s.bits_sent = 0;
#endif
        // Initialize the first block of the first file:
        InitBlock(s);
    }

    /// <summary>
    /// Flush the bits in the bit buffer to pending output (leaves at most 7 bits).
    /// </summary>
    internal static void FlushBits(DeflateState s)
    {
        if (s.bi_valid == 16)
        {
            PutShort(s, s.bi_buf);
            s.bi_buf = 0;
            s.bi_valid = 0;
        }
        else if (s.bi_valid >= 8)
        {
            Deflater.PutByte(s, (byte)s.bi_buf);
            s.bi_buf >>= 8;
            s.bi_valid -= 8;
        }
    }

    /// <summary>
    /// Sends one empty static block to give enough lookahead for inflate. This takes 10 bits, of which 7 may remain in the bit buffer.
    /// </summary>
    internal static void Align(DeflateState s)
    {
        SendBits(s, StaticTrees << 1, 3);
        SendCode(s, EndBlock, s_ltree);
#if DEBUG
        s.compressed_len += 10U; // 3 for block type, 7 for EOB
#endif
        FlushBits(s);
    }

    /// <summary>
    /// Determines the best encoding for the current block: dynamic trees, static trees or store, and writes out the encoded block.
    /// </summary>
    internal static unsafe void FlushBlock(DeflateState s, byte* buf, uint stored_len, int last)
    {
        uint opt_lenb, static_lenb; // opt_len and static_len in bytes
        int max_blindex = 0;  // index of last bit length code of non zero freq */

        // Build the Huffman trees unless a stored block is forced
        if (s.level > 0)
        {
            // Check if the file is binary or text
            if (s.strm.data_type == Z_UNKNOWN)
                s.strm.data_type = DetectDataType(s);

            // Construct the literal and distance trees
            BuildTree(s, s.l_desc);
            Trace.Tracev($"\nlit data: dyn {s.opt_len}, stat {s.static_len}");

            BuildTree(s, s.d_desc);
            Trace.Tracev($"\ndist data: dyn {s.opt_len}, stat {s.static_len}");
            /* At this point, opt_len and static_len are the total bit lengths of
             * the compressed block data, excluding the tree representations.
             */

            /* Build the bit length tree for the above two trees, and get the index
             * in bl_order of the last bit length code to send.
             */
            max_blindex = BuildBlTree(s);

            // Determine the best encoding. Compute the block lengths in bytes.
            opt_lenb = (s.opt_len + 3 + 7) >> 3;
            static_lenb = (s.static_len + 3 + 7) >> 3;

            Trace.Tracev($"\nopt {opt_lenb}({s.opt_len}) stat {static_lenb}({s.static_len}) stored {stored_len} lit {s.sym_next / 3} ");

            if (static_lenb <= opt_lenb)
                opt_lenb = static_lenb;
        }
        else
        {
            Debug.Assert(buf != null, "lost buf");
            opt_lenb = static_lenb = stored_len + 5; /* force a stored block */
        }

        if (stored_len + 4 <= opt_lenb && buf != null)
        {
            // 4: two words for the lengths

            /* The test buf != NULL is only necessary if LIT_BUFSIZE > WSIZE.
             * Otherwise we can't have processed more than WSIZE input bytes since
             * the last block flush, because compression would have been
             * successful. If LIT_BUFSIZE <= WSIZE, it is never too late to
             * transform a block into a stored block.
             */
            StoredBlock(s, buf, stored_len, last);
        }
        else if (s.strategy == Z_FIXED || static_lenb == opt_lenb)
        {
            SendBits(s, (StaticTrees << 1) + last, 3);
            CompressBlock(s, s_ltree, s_dtree);
#if DEBUG
            s.compressed_len += 3 + s.static_len;
#endif
        }
        else
        {
            SendBits(s, (DynTrees << 1) + last, 3);
            SendAllTrees(s, s.l_desc.max_code + 1, s.d_desc.max_code + 1, max_blindex + 1);
            CompressBlock(s, s.dyn_ltree, s.dyn_dtree);
#if DEBUG
            s.compressed_len += 3 + s.opt_len;
#endif
        }
#if DEBUG
        Debug.Assert(s.compressed_len == s.bits_sent, "bad compressed size");
#endif
        InitBlock(s);

        if (last != 0)
        {
            Windup(s);
#if DEBUG
            s.compressed_len += 7;  // align on byte boundary
#endif
        }
#if DEBUG
        Trace.Tracev($"\ncomprlen {s.compressed_len >> 3}({s.compressed_len - 7 * last}) ");
#endif
    }

    /// <summary>
    /// Sends a stored block.
    /// </summary>
    internal static unsafe void StoredBlock(DeflateState s, byte* buf, uint stored_len, int last)
    {
        const int STORED_BLOCK = 0;
        SendBits(s, (STORED_BLOCK << 1) + last, 3); // send block type
        Windup(s); // align on byte boundary
        PutShort(s, (ushort)stored_len);
        PutShort(s, (ushort)~stored_len);
        if (buf != null && stored_len != 0)
            Buffer.MemoryCopy(buf, s.pending_buf + s.pending, stored_len, stored_len);
        s.pending += stored_len;
#if DEBUG
        s.compressed_len = (s.compressed_len + 3 + 7) & unchecked((uint)~7);
        s.compressed_len += (stored_len + 4) << 3;
        s.bits_sent += 2 * 16;
        s.bits_sent += stored_len << 3;
#endif
    }

    /// <summary>
    ///  Initializes a new block.
    /// </summary>
    private static void InitBlock(DeflateState s)
    {
        // Initialize the trees.
        unsafe
        {
            fixed (TreeNode* dyn_ltree = s.dyn_ltree, dyn_dtree = s.dyn_dtree, bl_tree = s.bl_tree)
            {
                int n;
                for (n = 0; n < LCodes; n++)
                    dyn_ltree[n].fc = 0;
                for (n = 0; n < DCodes; n++)
                    dyn_dtree[n].fc = 0;
                for (n = 0; n < BlCodes; n++)
                    bl_tree[n].fc = 0;

                dyn_ltree[EndBlock].fc = 1;
            }
        }

        s.opt_len = s.static_len = 0;
        s.sym_next = s.matches = 0;
    }

    /// <summary>
    /// Outputs an unsigned 16-bit integer value, with the least significant bits first, on the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PutShort(DeflateState s, ushort w)
    {
        Deflater.PutByte(s, (byte)((w) & 0xff));
        Deflater.PutByte(s, (byte)(w >> 8));
    }

    private static void SendBits(DeflateState s, int value, int length)
    {
#if DEBUG
        Trace.Tracevv($" l {length,2} v {value,4:x} ");
        Debug.Assert(length > 0 && length <= 15, "invalid length");
        s.bits_sent += (uint)length;

        /* If not enough room in bi_buf, use (valid) bits from bi_buf and
         * (16 - bi_valid) bits from value, leaving (width - (16-bi_valid))
         * unused bits in value.
         */
        if (s.bi_valid > BufSize - length)
        {
            s.bi_buf |= (ushort)(value << s.bi_valid);
            PutShort(s, s.bi_buf);
            s.bi_buf = (ushort)(value >> (BufSize - s.bi_valid));
            s.bi_valid += length - BufSize;
        }
        else
        {
            s.bi_buf |= (ushort)(value << s.bi_valid);
            s.bi_valid += length;
        }
#else
        if (s.bi_valid > BufSize - length)
        {
            s.bi_buf |= (ushort)(value << s.bi_valid);
            PutShort(s, s.bi_buf);
            s.bi_buf = (ushort)(value >> (BufSize - s.bi_valid));
            s.bi_valid += length - BufSize;
        }
        else
        {
            s.bi_buf |= (ushort)(value << s.bi_valid);
            s.bi_valid += length;
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SendCode(DeflateState s, int c, TreeNode[] tree) =>
        SendBits(s, tree[c].fc, tree[c].dl);

    /// <summary>
    /// Flushes the bit buffer and align the output on a byte boundary.
    /// </summary>
    private static void Windup(DeflateState s)
    {
        if (s.bi_valid > 8)
            PutShort(s, s.bi_buf);
        else if (s.bi_valid > 0)
            Deflater.PutByte(s, (byte)s.bi_buf);
        s.bi_buf = 0;
        s.bi_valid = 0;
#if DEBUG
        s.bits_sent = (s.bits_sent + 7U) & ~7U;
#endif
    }

    private static int DetectDataType(DeflateState s)
    {
        /* black_mask is the bit mask of black-listed bytes
         * set bits 0..6, 14..25, and 28..31
         * 0xf3ffc07f = binary 11110011111111111100000001111111
         */
        uint black_mask = 0xf3ffc07f;
        int n;

        // Check for non-textual ("black-listed") bytes.
        for (n = 0; n <= 31; n++, black_mask >>= 1)
            if ((black_mask & 1) != 0 && s.dyn_ltree[n].fc != 0)
                return Z_BINARY;

        // Check for textual ("white-listed") bytes.
        if (s.dyn_ltree[9].fc != 0 || s.dyn_ltree[10].fc != 0
                || s.dyn_ltree[13].fc != 0)
            return Z_TEXT;
        for (n = 32; n < Literals; n++)
            if (s.dyn_ltree[n].fc != 0)
                return Z_TEXT;

        /* There are no "black-listed" or "white-listed" bytes:
         * this stream either is empty or has tolerated ("gray-listed") bytes only.
         */
        return Z_BINARY;
    }

    private static void BuildTree(DeflateState s, TreeDescriptor desc)
    {
        TreeNode[] tree = desc.dyn_tree;
        TreeNode[] stree = desc.stat_desc.static_tree;
        int elems = desc.stat_desc.elems;
        int n, m;          // iterate over heap elements
        int max_code = -1; // largest code with non zero frequency
        int node;          // new node being created

        /* Construct the initial heap, with least frequent element in
         * heap[SMALLEST]. The sons of heap[n] are heap[2*n] and heap[2*n+1].
         * heap[0] is not used.
         */
        s.heap_len = 0;
        s.heap_max = HeapSize;

        for (n = 0; n < elems; n++)
        {
            if (tree[n].fc != 0)
            {
                s.heap[++s.heap_len] = max_code = n;
                s.depth[n] = 0;
            }
            else
            {
                tree[n].dl = 0;
            }
        }

        /* The pkzip format requires that at least one distance code exists,
         * and that at least one bit should be sent even if there is only one
         * possible code. So to avoid special checks later on we force at least
         * two codes of non zero frequency.
         */
        while (s.heap_len < 2)
        {
            node = s.heap[++s.heap_len] = max_code < 2 ? ++max_code : 0;
            tree[node].fc = 1;
            s.depth[node] = 0;
            s.opt_len--;
            if (stree != null)
                s.static_len -= stree[node].dl;
            // node is 0 or 1 so it does not have extra bits
        }
        desc.max_code = max_code;

        /* The elements heap[heap_len/2+1 .. heap_len] are leaves of the tree,
         * establish sub-heaps of increasing lengths:
         */
        for (n = s.heap_len / 2; n >= 1; n--)
            PqDownHeap(s, tree, n);

        /* Construct the Huffman tree by repeatedly combining the least two
         * frequent nodes.
         */
        node = elems; // next internal node of the tree
        do
        {
            PqRemove(s, tree, ref n); // n = node of least frequency
            m = s.heap[Smallest]; // m = node of next least frequency

            s.heap[--s.heap_max] = n; // keep the nodes sorted by frequency
            s.heap[--s.heap_max] = m;

            // Create a new node father of n and m
            tree[node].fc = (ushort)(tree[n].fc + tree[m].fc);
            s.depth[node] = (byte)((s.depth[n] >= s.depth[m] ?
                                    s.depth[n] : s.depth[m]) + 1);
            tree[n].dl = tree[m].dl = (ushort)node;

            // and insert the new node in the heap
            s.heap[Smallest] = node++;
            PqDownHeap(s, tree, Smallest);

        } while (s.heap_len >= 2);

        s.heap[--s.heap_max] = s.heap[Smallest];

        /* At this point, the fields freq and dad are set. We can now
         * generate the bit lengths.
         */
        GenBitLen(s, desc);

        // The field len is now set, we can generate the bit codes
        GenCodes(tree, max_code, s.bl_count);
    }

    /// <summary>
    /// Restore the heap property by moving down the tree starting at node k, exchanging a node with the smallest of its two sons if necessary, stopping when the heap property is re-established (each father smaller than its two sons).
    /// </summary>
    private static void PqDownHeap(DeflateState s, TreeNode[] tree, int k)
    {
        int v = s.heap[k];
        int j = k << 1; // left son of k
        while (j <= s.heap_len)
        {
            // Set j to the smallest of the two sons:
            if (j < s.heap_len &&
                Smaller(tree, s.heap[j + 1], s.heap[j], s.depth))
                j++;
            // Exit if v is smaller than both sons
            if (Smaller(tree, v, s.heap[j], s.depth))
                break;

            // Exchange v with the smallest son
            s.heap[k] = s.heap[j];
            k = j;

            // And continue down the tree, setting j to the left son of k
            j <<= 1;
        }
        s.heap[k] = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Smaller(TreeNode[] tree, int n, int m, byte[] depth) =>
        tree[n].fc < tree[m].fc
            || tree[n].fc == tree[m].fc && depth[n] <= depth[m];

    /// <summary>
    /// Removes the smallest element from the heap and recreate the heap with one less element. Updates heap and heap_len.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PqRemove(DeflateState s, TreeNode[] tree, ref int top)
    {
        top = s.heap[Smallest];
        s.heap[Smallest] = s.heap[s.heap_len--];
        PqDownHeap(s, tree, Smallest);
    }

    /// <summary>
    /// Computes the optimal bit lengths for a tree and update the total bit length for the current block.
    /// </summary>
    private static void GenBitLen(DeflateState s, TreeDescriptor desc)
    {
        TreeNode[] tree = desc.dyn_tree;
        int max_code = desc.max_code;
        TreeNode[] stree = desc.stat_desc.static_tree;
        int[] extra = desc.stat_desc.extra_bits;
        int @base = desc.stat_desc.extra_base;
        int max_length = desc.stat_desc.max_length;
        int h;              // heap index
        int n, m;           // iterate over the tree elements
        int bits;           // bit length
        int xbits;          // extra bits
        ushort f;           // frequency
        int overflow = 0;   // number of elements with bit length too large

        for (bits = 0; bits <= MaxBits; bits++)
            s.bl_count[bits] = 0;

        /* In a first pass, compute the optimal bit lengths (which may
         * overflow in the case of the bit length tree).
         */
        tree[s.heap[s.heap_max]].dl = 0; // root of the heap

        for (h = s.heap_max + 1; h < HeapSize; h++)
        {
            n = s.heap[h];
            bits = tree[tree[n].dl].dl + 1;
            if (bits > max_length)
            {
                bits = max_length;
                overflow++;
            }
            tree[n].dl = (ushort)bits;
            // We overwrite tree[n].Dad which is no longer needed

            if (n > max_code)
                continue; // not a leaf node

            s.bl_count[bits]++;
            xbits = 0;
            if (n >= @base)
                xbits = extra[n - @base];
            f = tree[n].fc;
            s.opt_len += f * (uint)(bits + xbits);
            if (stree != null)
                s.static_len += f * (uint)(stree[n].dl + xbits);
        }
        if (overflow == 0)
            return;

        Trace.Tracev("\nbit length overflow\n");
        // This happens for example on obj2 and pic of the Calgary corpus

        // Find the first bit length which could increase:
        do
        {
            bits = max_length - 1;
            while (s.bl_count[bits] == 0)
                bits--;
            s.bl_count[bits]--; // move one leaf down the tree
            s.bl_count[bits + 1] += 2; // move one overflow item as its brother
            s.bl_count[max_length]--;
            /* The brother of the overflow item also moves one step up,
             * but this does not affect bl_count[max_length]
             */
            overflow -= 2;
        } while (overflow > 0);

        /* Now recompute all bit lengths, scanning in increasing frequency.
         * h is still equal to HEAP_SIZE. (It is simpler to reconstruct all
         * lengths instead of fixing only the wrong ones. This idea is taken
         * from 'ar' written by Haruhiko Okumura.)
         */
        for (bits = max_length; bits != 0; bits--)
        {
            n = s.bl_count[bits];
            while (n != 0)
            {
                m = s.heap[--h];
                if (m > max_code)
                    continue;
                if (tree[m].dl != (uint)bits)
                {
                    Trace.Tracev($"code {m} bits {tree[m].dl}->{bits}\n");
                    s.opt_len += ((uint)bits - tree[m].dl) * tree[m].fc;
                    tree[m].dl = (ushort)bits;
                }
                n--;
            }
        }
    }

    /// <summary>
    /// Generates the codes for a given tree and bit counts (which need not be optimal).
    /// </summary>
    private static void GenCodes(TreeNode[] tree, int max_code, ushort[] bl_count)
    {
        ushort[] next_code = new ushort[MaxBits + 1]; /* next code value for each bit length */
        uint code = 0;  // running code value
        int bits;       // bit index
        int n;          // code index

        /* The distribution counts are first used to generate the code values
         * without bit reversal.
         */
        for (bits = 1; bits <= MaxBits; bits++)
        {
            code = (code + bl_count[bits - 1]) << 1;
            next_code[bits] = (ushort)code;
        }
        /* Check that the bit counts in bl_count are consistent. The last code
         * must be all ones.
         */
        Debug.Assert(code + bl_count[MaxBits] - 1 == (1 << MaxBits) - 1, "inconsistent bit counts");
        Trace.Tracev($"\ngen_codes: max_code {max_code} ");

        for (n = 0; n <= max_code; n++)
        {
            int len = tree[n].dl;
            if (len == 0)
                continue;
            // Now reverse the bits
            tree[n].fc = (ushort)BiReverse(next_code[len]++, len);
#if DEBUG
            Trace.Tracecv(tree != s_ltree, $"\nn {n,3} {(IsGraph(n) ? Convert.ToChar(n) : ' ')} l {len,2} c {tree[n].dl,4:x} ({next_code[len] - 1:x)}) ");
#endif
        }
    }

    /// <summary>
    /// Reverse the first len bits of a code, using straightforward code (a faster method would use a table).
    /// </summary>
    private static uint BiReverse(uint code, int len)
    {
        uint res = 0;
        do
        {
            res |= code & 1;
            code >>= 1;
            res <<= 1;
        } while (--len > 0);
        return res >> 1;
    }

    /// <summary>
    /// Construct the Huffman tree for the bit lengths and return the index in bl_order of the last bit length code to send.
    /// </summary>
    private static int BuildBlTree(DeflateState s)
    {
        int max_blindex; // index of last bit length code of non zero freq

        // Determine the bit length frequencies for literal and distance trees
        ScanTree(s, s.dyn_ltree, s.l_desc.max_code);
        ScanTree(s, s.dyn_dtree, s.d_desc.max_code);

        // Build the bit length tree:
        BuildTree(s, s.bl_desc);
        /* opt_len now includes the length of the tree representations, except
         * the lengths of the bit lengths codes and the 5+5+4 bits for the counts.
         */

        /* Determine the number of bit length codes to send. The pkzip format
         * requires that at least 4 bit length codes be sent. (appnote.txt says
         * 3 but the actual value used is 4.)
         */
        for (max_blindex = BlCodes - 1; max_blindex >= 3; max_blindex--)
        {
            if (s.bl_tree[s_bl_order[max_blindex]].dl != 0)
                break;
        }
        // Update opt_len to include the bit length tree and counts
        s.opt_len += 3 * ((uint)max_blindex + 1) + 5 + 5 + 4;
        Trace.Tracev($"\ndyn trees: dyn {s.opt_len}, stat {s.static_len}");

        return max_blindex;
    }

    private static void ScanTree(DeflateState s, TreeNode[] tree, int max_code)
    {
        int n;                      // iterates over all tree elements */
        int prevlen = -1;           // last emitted length
        int curlen;                 // length of current code
        int nextlen = tree[0].dl;   // length of next code
        int count = 0;              // repeat count of the current code
        int max_count = 7;          // max repeat count
        int min_count = 4;          // min repeat count

        if (nextlen == 0)
        {
            max_count = 138;
            min_count = 3;
        }
        tree[max_code + 1].dl = 0xffff; // guard

        for (n = 0; n <= max_code; n++)
        {
            curlen = nextlen;
            nextlen = tree[n + 1].dl;
            if (++count < max_count && curlen == nextlen)
            {
                continue;
            }
            else if (count < min_count)
            {
                s.bl_tree[curlen].fc += (ushort)count;
            }
            else if (curlen != 0)
            {
                if (curlen != prevlen)
                    s.bl_tree[curlen].fc++;
                s.bl_tree[Rep_3_6].fc++;
            }
            else if (count <= 10)
            {
                s.bl_tree[RepZ_3_10].fc++;
            }
            else
            {
                s.bl_tree[RepZ_11_138].fc++;
            }
            count = 0;
            prevlen = curlen;
            if (nextlen == 0)
            {
                max_count = 138;
                min_count = 3;
            }
            else if (curlen == nextlen)
            {
                max_count = 6;
                min_count = 3;
            }
            else
            {
                max_count = 7;
                min_count = 4;
            }
        }
    }

    /// <summary>
    /// Saves the match info and tally the frequency counts.
    /// </summary>
    /// <param name="s">The current state of the deflate stream.</param>
    /// <param name="dist">The distance of matched string.</param>
    /// <param name="lc">The match length - MIN_MATCH or unmatched char (if dist==0).</param>
    /// <returns><see langword="true"/> if the current block must be flushed.</returns>
    internal static unsafe bool Tally(DeflateState s, uint dist, uint lc)
    {
        s.sym_buf[s.sym_next++] = (byte)dist;
        s.sym_buf[s.sym_next++] = (byte)(dist >> 8);
        s.sym_buf[s.sym_next++] = (byte)lc;
        if (dist == 0)
        {
            // lc is the unmatched char
            s.dyn_ltree[lc].fc++;
        }
        else
        {
            s.matches++;
            // Here, lc is the match length - MinMatch
            dist--; // dist = match distance - 1
            Debug.Assert(dist < Deflater.MaxDist(s)
                && lc <= MaxMatch - MinMatch
                && DCode(dist) < DCodes, "_tr_tally: bad match");

            s.dyn_ltree[s_length_code[lc] + Literals + 1].fc++;
            s.dyn_dtree[DCode(dist)].fc++;
        }
        return s.sym_next == s.sym_end;
    }

    /// <summary>
    /// Sends the block data compressed using the given Huffman trees.
    /// </summary>
    private static void CompressBlock(DeflateState s, TreeNode[] ltree, TreeNode[] dtree)
    {
        if (s.sym_next != 0)
        {
            int dist;       // distance of matched string
            int lc;         // match length or unmatched char (if dist == 0)
            uint sx = 0;    // running index in sym_buf
            uint code;      // the code to send
            int extra;      // number of extra bits to send
            unsafe
            {
                do
                {
                    dist = s.sym_buf[sx++] & 0xff;
                    dist += (s.sym_buf[sx++] & 0xff) << 8;
                    lc = s.sym_buf[sx++];
                    if (dist == 0)
                    {
                        SendCode(s, lc, ltree); // send a literal byte
#if DEBUG
                        Trace.Tracecv(IsGraph(lc), $" '{lc}' ");
#endif
                    }
                    else
                    {
                        // Here, lc is the match length - MIN_MATCH
                        code = s_length_code[lc];
                        SendCode(s, (int)(code + Literals + 1), ltree); // send the length code
                        extra = s_extra_lbits[code];
                        if (extra != 0)
                        {
                            lc -= s_base_length[code];
                            SendBits(s, lc, extra); // send the extra length bits
                        }
                        dist--; // dist is now the match distance - 1
                        code = DCode((uint)dist);
                        Debug.Assert(code < DCodes, "bad d_code");

                        SendCode(s, (int)code, dtree); // send the distance code
                        extra = s_extra_dbits[code];
                        if (extra != 0)
                        {
                            dist -= s_base_dist[code];
                            SendBits(s, dist, extra); // send the extra distance bits
                        }
                    } // literal or match pair ?

                    // Check that the overlay between pending_buf and sym_buf is ok:
                    Debug.Assert(s.pending < s.lit_bufsize + sx, "pendingBuf overflow");

                } while (sx < s.sym_next);
            }
        }

        SendCode(s, EndBlock, ltree);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint DCode(uint dist) =>
        dist < 256 ? s_dist_code[dist] : s_dist_code[256 + ((dist) >> 7)];

    /// <summary>
    /// Sends the header for a block using dynamic Huffman trees: the counts, the lengths of the bit length codes, the literal tree and the distance tree.
    /// </summary>
    private static void SendAllTrees(DeflateState s, int lcodes, int dcodes, int blcodes)
    {
        Debug.Assert(lcodes >= 257 && dcodes >= 1 && blcodes >= 4, "not enough codes");
        Debug.Assert(lcodes <= LCodes && dcodes <= DCodes && blcodes <= BlCodes, "too many codes");
        Trace.Tracev("\nbl counts: ");
        SendBits(s, lcodes - 257, 5); // not +255 as stated in appnote.txt
        SendBits(s, dcodes - 1, 5);
        SendBits(s, blcodes - 4, 4); // not -3 as stated in appnote.txt
        for (int rank = 0; rank < blcodes; rank++)
        {
            Trace.Tracev($"\nbl code {s_bl_order[rank],2} ");
            SendBits(s, s.bl_tree[s_bl_order[rank]].dl, 3);
        }
#if DEBUG
        Trace.Tracev($"\nbl tree: sent {s.bits_sent}");
#endif

        SendTree(s, s.dyn_ltree, lcodes - 1); // literal tree
#if DEBUG
        Trace.Tracev($"\nlit tree: sent {s.bits_sent}");
#endif

        SendTree(s, s.dyn_dtree, dcodes - 1); // distance tree
#if DEBUG
        Trace.Tracev($"\ndist tree: sent {s.bits_sent}");
#endif
    }

    /// <summary>
    /// Sends a literal or distance tree in compressed form, using the codes in bl_tree.
    /// </summary>
    private static void SendTree(DeflateState s, TreeNode[] tree, int max_code)
    {
        int prevlen = -1;           // last emitted length
        int curlen;                 // length of current code
        int nextlen = tree[0].dl;   // length of next code
        int count = 0;              // repeat count of the current code
        int max_count = 7;          // max repeat count
        int min_count = 4;          // min repeat count

        if (nextlen == 0)
        {
            max_count = 138;
            min_count = 3;
        }

        for (int n = 0; n <= max_code; n++)
        {
            curlen = nextlen;
            nextlen = tree[n + 1].dl;
            if (++count < max_count && curlen == nextlen)
            {
                continue;
            }
            else if (count < min_count)
            {
                do
                {
                    SendCode(s, curlen, s.bl_tree);
                } while (--count != 0);
            }
            else if (curlen != 0)
            {
                if (curlen != prevlen)
                {
                    SendCode(s, curlen, s.bl_tree);
                    count--;
                }
                Debug.Assert(count >= 3 && count <= 6, " 3_6?");
                SendCode(s, Rep_3_6, s.bl_tree);
                SendBits(s, count - 3, 2);

            }
            else if (count <= 10)
            {
                SendCode(s, RepZ_3_10, s.bl_tree);
                SendBits(s, count - 3, 3);
            }
            else
            {
                SendCode(s, RepZ_11_138, s.bl_tree);
                SendBits(s, count - 11, 7);
            }
            count = 0;
            prevlen = curlen;
            if (nextlen == 0)
            {
                max_count = 138;
                min_count = 3;
            }
            else if (curlen == nextlen)
            {
                max_count = 6;
                min_count = 3;
            }
            else
            {
                max_count = 7;
                min_count = 4;
            }
        }
    }
#if DEBUG
    private static bool IsGraph(int ch) => ch > 32 && ch < 127;
#endif
}