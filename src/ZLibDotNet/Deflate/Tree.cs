﻿// Original code and comments Copyright (C) 1995-2024 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static ZLibDotNet.Deflate.Constants;

namespace ZLibDotNet.Deflate;

internal static class Tree
{
    private const uint EndBlock = 256;      // end of block literal code
    private const uint Rep_3_6 = 16;        // repeat previous bit length 3-6 times (2 bits of repeat count)
    private const uint RepZ_3_10 = 17;      // repeat a zero length 3-10 times  (3 bits of repeat count)
    private const uint RepZ_11_138 = 18;    // repeat a zero length 11-138 times  (7 bits of repeat count)
    private const uint Smallest = 1;        // Index within the heap array of least frequent node in the Huffman tree
    private const byte StaticTrees = 1;
    private const byte DynTrees = 2;

    internal static readonly TreeNode[] s_dtree = new TreeNode[DCodes]
    {
        new(0, 5), new(16, 5), new(8, 5), new(24, 5), new(4, 5),
        new(20, 5), new(12, 5), new(28, 5), new(2, 5), new(18, 5),
        new(10, 5), new(26, 5), new(6, 5), new(22, 5), new(14, 5),
        new(30, 5), new(1, 5), new(17, 5), new(9, 5), new(25, 5),
        new(5, 5), new(21, 5), new(13, 5), new(29, 5), new(3, 5),
        new(19, 5), new(11, 5), new(27, 5), new(7, 5), new(23, 5)
    };

    internal static readonly TreeNode[] s_ltree = new TreeNode[LCodes + 2]
    {
        new(12, 8), new(140, 8), new(76, 8), new(204, 8), new(44, 8),
        new(172, 8), new(108, 8), new(236, 8), new(28, 8), new(156, 8),
        new(92, 8), new(220, 8), new(60, 8), new(188, 8), new(124, 8),
        new(252, 8), new( 2, 8), new(130, 8), new(66, 8), new(194, 8),
        new(34, 8), new(162, 8), new(98, 8), new(226, 8), new(18, 8),
        new(146, 8), new(82, 8), new(210, 8), new(50, 8), new(178, 8),
        new(114, 8), new(242, 8), new(10, 8), new(138, 8), new(74, 8),
        new(202, 8), new(42, 8), new(170, 8), new(106, 8), new(234, 8),
        new(26, 8), new(154, 8), new(90, 8), new(218, 8), new(58, 8),
        new(186, 8), new(122, 8), new(250, 8), new( 6, 8), new(134, 8),
        new(70, 8), new(198, 8), new(38, 8), new(166, 8), new(102, 8),
        new(230, 8), new(22, 8), new(150, 8), new(86, 8), new(214, 8),
        new(54, 8), new(182, 8), new(118, 8), new(246, 8), new(14, 8),
        new(142, 8), new(78, 8), new(206, 8), new(46, 8), new(174, 8),
        new(110, 8), new(238, 8), new(30, 8), new(158, 8), new(94, 8),
        new(222, 8), new(62, 8), new(190, 8), new(126, 8), new(254, 8),
        new( 1, 8), new(129, 8), new(65, 8), new(193, 8), new(33, 8),
        new(161, 8), new(97, 8), new(225, 8), new(17, 8), new(145, 8),
        new(81, 8), new(209, 8), new(49, 8), new(177, 8), new(113, 8),
        new(241, 8), new( 9, 8), new(137, 8), new(73, 8), new(201, 8),
        new(41, 8), new(169, 8), new(105, 8), new(233, 8), new(25, 8),
        new(153, 8), new(89, 8), new(217, 8), new(57, 8), new(185, 8),
        new(121, 8), new(249, 8), new( 5, 8), new(133, 8), new(69, 8),
        new(197, 8), new(37, 8), new(165, 8), new(101, 8), new(229, 8),
        new(21, 8), new(149, 8), new(85, 8), new(213, 8), new(53, 8),
        new(181, 8), new(117, 8), new(245, 8), new(13, 8), new(141, 8),
        new(77, 8), new(205, 8), new(45, 8), new(173, 8), new(109, 8),
        new(237, 8), new(29, 8), new(157, 8), new(93, 8), new(221, 8),
        new(61, 8), new(189, 8), new(125, 8), new(253, 8), new(19, 9),
        new(275, 9), new(147, 9), new(403, 9), new(83, 9), new(339, 9),
        new(211, 9), new(467, 9), new(51, 9), new(307, 9), new(179, 9),
        new(435, 9), new(115, 9), new(371, 9), new(243, 9), new(499, 9),
        new(11, 9), new(267, 9), new(139, 9), new(395, 9), new(75, 9),
        new(331, 9), new(203, 9), new(459, 9), new(43, 9), new(299, 9),
        new(171, 9), new(427, 9), new(107, 9), new(363, 9), new(235, 9),
        new(491, 9), new(27, 9), new(283, 9), new(155, 9), new(411, 9),
        new(91, 9), new(347, 9), new(219, 9), new(475, 9), new(59, 9),
        new(315, 9), new(187, 9), new(443, 9), new(123, 9), new(379, 9),
        new(251, 9), new(507, 9), new( 7, 9), new(263, 9), new(135, 9),
        new(391, 9), new(71, 9), new(327, 9), new(199, 9), new(455, 9),
        new(39, 9), new(295, 9), new(167, 9), new(423, 9), new(103, 9),
        new(359, 9), new(231, 9), new(487, 9), new(23, 9), new(279, 9),
        new(151, 9), new(407, 9), new(87, 9), new(343, 9), new(215, 9),
        new(471, 9), new(55, 9), new(311, 9), new(183, 9), new(439, 9),
        new(119, 9), new(375, 9), new(247, 9), new(503, 9), new(15, 9),
        new(271, 9), new(143, 9), new(399, 9), new(79, 9), new(335, 9),
        new(207, 9), new(463, 9), new(47, 9), new(303, 9), new(175, 9),
        new(431, 9), new(111, 9), new(367, 9), new(239, 9), new(495, 9),
        new(31, 9), new(287, 9), new(159, 9), new(415, 9), new(95, 9),
        new(351, 9), new(223, 9), new(479, 9), new(63, 9), new(319, 9),
        new(191, 9), new(447, 9), new(127, 9), new(383, 9), new(255, 9),
        new(511, 9), new( 0, 7), new(64, 7), new(32, 7), new(96, 7),
        new(16, 7), new(80, 7), new(48, 7), new(112, 7), new( 8, 7),
        new(72, 7), new(40, 7), new(104, 7), new(24, 7), new(88, 7),
        new(56, 7), new(120, 7), new( 4, 7), new(68, 7), new(36, 7),
        new(100, 7), new(20, 7), new(84, 7), new(52, 7), new(116, 7),
        new( 3, 8), new(131, 8), new(67, 8), new(195, 8), new(35, 8),
        new(163, 8), new(99, 8), new(227, 8)
    };

    /// <summary>
    /// Initializes the tree data structures for a new zlib stream.
    /// </summary>
    internal static void Init(ref ZStream strm)
    {
        DeflateState s = strm.deflateState;
        s.bi_buf = 0;
        s.bi_valid = 0;
#if DEBUG
        s.compressed_len = 0;
        s.bits_sent = 0;
#endif
        ref TreeNode dyn_ltree = ref MemoryMarshal.GetReference<TreeNode>(s.dyn_ltree);
        ref TreeNode dyn_dtree = ref MemoryMarshal.GetReference<TreeNode>(s.dyn_dtree);
        ref TreeNode bl_tree = ref MemoryMarshal.GetReference<TreeNode>(s.bl_tree);
#if NET7_0_OR_GREATER
        ref DeflateRefs refs = ref strm.deflateRefs;
        refs.dyn_ltree = ref dyn_ltree;
        refs.dyn_dtree = ref dyn_dtree;
        refs.bl_tree = ref bl_tree;
#endif
        // Initialize the first block of the first file:
        InitBlock(s, ref dyn_ltree, ref dyn_dtree, ref bl_tree);
    }

    /// <summary>
    /// Flush the bits in the bit buffer to pending output (leaves at most 7 bits).
    /// </summary>
    internal static void FlushBits(DeflateState s, ref byte pending_buf)
    {
        if (s.bi_valid == 16)
        {
            PutShort(s, s.bi_buf, ref pending_buf);
            s.bi_buf = 0;
            s.bi_valid = 0;
        }
        else if (s.bi_valid >= 8)
        {
            Unsafe.Add(ref pending_buf, s.pending++) = (byte)s.bi_buf;
            s.bi_buf >>= 8;
            s.bi_valid -= 8;
        }
    }

    /// <summary>
    /// Sends one empty static block to give enough lookahead for inflate. This takes 10 bits, of which 7 may remain in the bit buffer.
    /// </summary>
    internal static void Align(DeflateState s, ref byte pending_buf, ref TreeNode sta_ltree)
    {
        SendBits(s, StaticTrees << 1, 3, ref pending_buf);
        SendCode(s, ref Unsafe.Add(ref sta_ltree, EndBlock), ref pending_buf);
#if DEBUG
        s.compressed_len += 10U; // 3 for block type, 7 for EOB
#endif
        FlushBits(s, ref pending_buf);
    }

    /// <summary>
    /// Determines the best encoding for the current block: dynamic trees, static trees or store, and writes out the encoded block.
    /// </summary>
    internal static void FlushBlock(ref ZStream strm, ref byte buf, uint stored_len, uint last,
        ref byte pending_buf, ref TreeNode sta_ltree, ref TreeNode sta_dtree, ref TreeNode dyn_ltree,
        ref TreeNode dyn_dtree, ref TreeNode bl_tree, ref ushort bl_count, ref int heap, ref byte depth,
        ref ushort bl_order, ref byte dist_code, ref byte length_code, ref int base_dist, ref int base_length,
        ref int extra_dbits, ref int extra_lbits, ref int extra_blbits)
    {
        DeflateState s = strm.deflateState;
        uint opt_lenb, static_lenb; // opt_len and static_len in bytes
        uint max_blindex = 0;  // index of last bit length code of non zero freq

        // Build the Huffman trees unless a stored block is forced
        if (s.level > 0)
        {
            // Check if the file is binary or text
            if (strm.data_type == Z_UNKNOWN)
                strm.data_type = DetectDataType(ref dyn_ltree);

            // Construct the literal and distance trees
            BuildTree(s, s.l_desc, ref dyn_ltree, ref sta_ltree, ref extra_lbits, ref bl_count, ref heap, ref depth);
            Trace.Tracev($"\nlit data: dyn {s.opt_len}, stat {s.static_len}");

            BuildTree(s, s.d_desc, ref dyn_dtree, ref sta_dtree, ref extra_dbits, ref bl_count, ref heap, ref depth);
            Trace.Tracev($"\ndist data: dyn {s.opt_len}, stat {s.static_len}");
            /* At this point, opt_len and static_len are the total bit lengths of
             * the compressed block data, excluding the tree representations.
             */

            /* Build the bit length tree for the above two trees, and get the index
             * in bl_order of the last bit length code to send.
             */
            max_blindex = BuildBlTree(s, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref extra_blbits, ref bl_count, ref heap, ref depth,
                ref bl_order);

            // Determine the best encoding. Compute the block lengths in bytes.
            opt_lenb = (s.opt_len + 3 + 7) >> 3;
            static_lenb = (s.static_len + 3 + 7) >> 3;

            Trace.Tracev($"\nopt {opt_lenb}({s.opt_len}) stat {static_lenb}({s.static_len}) stored {stored_len} lit {s.sym_next / 3} ");

            if (static_lenb <= opt_lenb || s.strategy == Z_FIXED)
                opt_lenb = static_lenb;
        }
        else
        {
            Debug.Assert(!netUnsafe.IsNullRef(ref buf), "lost buf");
            opt_lenb = static_lenb = stored_len + 5; // force a stored block
        }

        if (stored_len + 4 <= opt_lenb && !netUnsafe.IsNullRef(ref buf))
        {
            // 4: two words for the lengths

            /* The test buf != NULL is only necessary if LIT_BUFSIZE > WSIZE.
             * Otherwise we can't have processed more than WSIZE input bytes since
             * the last block flush, because compression would have been
             * successful. If LIT_BUFSIZE <= WSIZE, it is never too late to
             * transform a block into a stored block.
             */
            StoredBlock(s, ref buf, stored_len, last, ref pending_buf);
        }
        else if (static_lenb == opt_lenb)
        {
            SendBits(s, (StaticTrees << 1) + last, 3, ref pending_buf);
            CompressBlock(s, ref sta_ltree, ref sta_dtree, ref pending_buf, ref dist_code, ref length_code,
                ref base_dist, ref base_length, ref extra_dbits, ref extra_lbits);
#if DEBUG
            s.compressed_len += 3 + s.static_len;
#endif
        }
        else
        {
            SendBits(s, (DynTrees << 1) + last, 3, ref pending_buf);
            SendAllTrees(s, (uint)(s.l_desc.max_code + 1), (uint)(s.d_desc.max_code + 1), max_blindex + 1,
                ref pending_buf, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_order);
            CompressBlock(s, ref dyn_ltree, ref dyn_dtree, ref pending_buf, ref dist_code, ref length_code,
                ref base_dist, ref base_length, ref extra_dbits, ref extra_lbits);
#if DEBUG
            s.compressed_len += 3 + s.opt_len;
#endif
        }
#if DEBUG
        Debug.Assert(s.compressed_len == s.bits_sent, "bad compressed size");
#endif
        InitBlock(s, ref dyn_ltree, ref dyn_dtree, ref bl_tree);

        if (last != 0)
        {
            Windup(s, ref pending_buf);
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
    internal static void StoredBlock(DeflateState s, ref byte buf, uint stored_len, uint last, ref byte pending_buf)
    {
        const int STORED_BLOCK = 0;
        SendBits(s, (STORED_BLOCK << 1) + last, 3, ref pending_buf); // send block type
        Windup(s, ref pending_buf); // align on byte boundary
        PutShort(s, (ushort)stored_len, ref pending_buf);
        PutShort(s, (ushort)~stored_len, ref pending_buf);
        if (!netUnsafe.IsNullRef(ref buf) && stored_len != 0)
            netUnsafe.CopyBlockUnaligned(ref Unsafe.Add(ref pending_buf, s.pending), ref buf, stored_len);
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
    private static void InitBlock(DeflateState s, ref TreeNode dyn_ltree, ref TreeNode dyn_dtree, ref TreeNode bl_tree)
    {
        // Initialize the trees.
        uint n = 0;
        for (; n < LCodes; n++)
            Unsafe.Add(ref dyn_ltree, n).fc = 0;
        for (n = 0; n < DCodes; n++)
            Unsafe.Add(ref dyn_dtree, n).fc = 0;
        for (n = 0; n < BlCodes; n++)
            Unsafe.Add(ref bl_tree, n).fc = 0;

        Unsafe.Add(ref dyn_ltree, EndBlock).fc = 1;

        s.opt_len = s.static_len = 0;
        s.sym_next = s.matches = 0;
    }

    /// <summary>
    /// Outputs an unsigned 16-bit integer value, with the least significant bits first, on the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PutShort(DeflateState s, ushort w, ref byte pending_buf)
    {
        Unsafe.Add(ref pending_buf, s.pending++) = (byte)((w) & 0xff);
        Unsafe.Add(ref pending_buf, s.pending++) = (byte)(w >> 8);
    }

    private static void SendBits(DeflateState s, uint value, int length, ref byte pending_buf)
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
            PutShort(s, s.bi_buf, ref pending_buf);
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
            PutShort(s, s.bi_buf, ref pending_buf);
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
    private static void SendCode(DeflateState s, ref TreeNode treeNode, ref byte pending_buf) =>
        SendBits(s, treeNode.fc, treeNode.dl, ref pending_buf);

    /// <summary>
    /// Flushes the bit buffer and align the output on a byte boundary.
    /// </summary>
    private static void Windup(DeflateState s, ref byte pending_buf)
    {
        if (s.bi_valid > 8)
            PutShort(s, s.bi_buf, ref pending_buf);
        else if (s.bi_valid > 0)
            Unsafe.Add(ref pending_buf, s.pending++) = (byte)s.bi_buf;
        s.bi_buf = 0;
        s.bi_valid = 0;
#if DEBUG
        s.bits_sent = (s.bits_sent + 7U) & ~7U;
#endif
    }

    private static int DetectDataType(ref TreeNode dyn_ltree)
    {
        /* black_mask is the bit mask of black-listed bytes
         * set bits 0..6, 14..25, and 28..31
         * 0xf3ffc07f = binary 11110011111111111100000001111111
         */
        uint black_mask = 0xf3ffc07f;
        uint n = 0;

        // Check for non-textual ("black-listed") bytes.
        for (; n <= 31; n++, black_mask >>= 1)
            if ((black_mask & 1) != 0 && Unsafe.Add(ref dyn_ltree, n).fc != 0)
                return Z_BINARY;

        // Check for textual ("white-listed") bytes.
        if (Unsafe.Add(ref dyn_ltree, 9U).fc != 0 || Unsafe.Add(ref dyn_ltree, 10U).fc != 0
                || Unsafe.Add(ref dyn_ltree, 13U).fc != 0)
            return Z_TEXT;
        for (n = 32; n < Literals; n++)
            if (Unsafe.Add(ref dyn_ltree, n).fc != 0)
                return Z_TEXT;

        /* There are no "black-listed" or "white-listed" bytes:
         * this stream either is empty or has tolerated ("gray-listed") bytes only.
         */
        return Z_BINARY;
    }

    private static void BuildTree(DeflateState s, TreeDescriptor desc, ref TreeNode tree, ref TreeNode stree, ref int extra,
        ref ushort bl_count, ref int heap, ref byte depth)
    {
        uint elems = desc.stat_desc.elems;
        int max_code = -1; // largest code with non zero frequency
        uint node;         // new node being created

        /* Construct the initial heap, with least frequent element in
         * heap[SMALLEST]. The sons of heap[n] are heap[2*n] and heap[2*n+1].
         * heap[0] is not used.
         */
        s.heap_len = 0;
        s.heap_max = HeapSize;

        uint n = 0;
        for (; n < elems; n++)
        {
            ref TreeNode tn = ref Unsafe.Add(ref tree, n);
            if (tn.fc != 0)
            {
                Unsafe.Add(ref heap, ++s.heap_len) = max_code = (int)n;
                Unsafe.Add(ref depth, n) = 0;
            }
            else
            {
                tn.dl = 0;
            }
        }

        /* The pkzip format requires that at least one distance code exists,
         * and that at least one bit should be sent even if there is only one
         * possible code. So to avoid special checks later on we force at least
         * two codes of non zero frequency.
         */
        while (s.heap_len < 2)
        {
            node = (uint)(Unsafe.Add(ref heap, ++s.heap_len) = max_code < 2 ? ++max_code : 0);
            Unsafe.Add(ref tree, node).fc = 1;
            Unsafe.Add(ref depth, node) = 0;
            s.opt_len--;
            if (desc.stat_desc.static_tree != null)
                s.static_len -= Unsafe.Add(ref stree, node).dl;
            // node is 0 or 1 so it does not have extra bits
        }
        desc.max_code = max_code;

        /* The elements heap[heap_len/2+1 .. heap_len] are leaves of the tree,
         * establish sub-heaps of increasing lengths:
         */
        for (n = s.heap_len / 2; n >= 1; n--)
            PqDownHeap(s, ref tree, n, ref heap, ref depth);

        /* Construct the Huffman tree by repeatedly combining the least two
         * frequent nodes.
         */
        node = elems; // next internal node of the tree
        do
        {
            int nn = default;
            PqRemove(s, ref tree, ref nn, ref heap, ref depth); // n = node of least frequency
            int mm = Unsafe.Add(ref heap, Smallest); // m = node of next least frequency

            Unsafe.Add(ref heap, --s.heap_max) = nn; // keep the nodes sorted by frequency
            n = (uint)nn;
            Unsafe.Add(ref heap, --s.heap_max) = mm;
            uint m = (uint)mm;

            // Create a new node father of n and m
            Unsafe.Add(ref tree, node).fc = (ushort)(Unsafe.Add(ref tree, n).fc + Unsafe.Add(ref tree, m).fc);
            byte dn = Unsafe.Add(ref depth, n);
            byte dm = Unsafe.Add(ref depth, m);
            Unsafe.Add(ref depth, node) = (byte)((dn >= dm ? dn : dm) + 1);
            Unsafe.Add(ref tree, n).dl = Unsafe.Add(ref tree, m).dl = (ushort)node;

            // and insert the new node in the heap
            Unsafe.Add(ref heap, Smallest) = (int)node++;
            PqDownHeap(s, ref tree, Smallest, ref heap, ref depth);

        } while (s.heap_len >= 2);

        Unsafe.Add(ref heap, --s.heap_max) = Unsafe.Add(ref heap, Smallest);

        /* At this point, the fields freq and dad are set. We can now
         * generate the bit lengths.
         */
        GenBitLen(s, desc, ref tree, ref stree, ref extra, ref bl_count, ref heap);

        // The field len is now set, we can generate the bit codes
        GenCodes(ref tree, max_code, ref bl_count);
    }

    /// <summary>
    /// Restore the heap property by moving down the tree starting at node k, exchanging a node with the smallest of its two sons if necessary, stopping when the heap property is re-established (each father smaller than its two sons).
    /// </summary>
    private static void PqDownHeap(DeflateState s, ref TreeNode tree, uint k, ref int heap, ref byte depth)
    {
        uint v = (uint)Unsafe.Add(ref heap, k);
        uint j = k << 1; // left son of k
        while (j <= s.heap_len)
        {
            // Set j to the smallest of the two sons:
            if (j < s.heap_len &&
                Smaller(ref tree, (uint)Unsafe.Add(ref heap, j + 1), (uint)Unsafe.Add(ref heap, j), ref depth))
                j++;
            // Exit if v is smaller than both sons
            if (Smaller(ref tree, v, (uint)Unsafe.Add(ref heap, j), ref depth))
                break;

            // Exchange v with the smallest son
            Unsafe.Add(ref heap, k) = Unsafe.Add(ref heap, j);
            k = j;

            // And continue down the tree, setting j to the left son of k
            j <<= 1;
        }
        Unsafe.Add(ref heap, k) = (int)v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Smaller(ref TreeNode tree, uint n, uint m, ref byte depth)
    {
        ref TreeNode tn = ref Unsafe.Add(ref tree, n);
        ref TreeNode tm = ref Unsafe.Add(ref tree, m);
        return tn.fc < tm.fc || tn.fc == tm.fc && Unsafe.Add(ref depth, n) <= Unsafe.Add(ref depth, m);
    }

    /// <summary>
    /// Removes the smallest element from the heap and recreate the heap with one less element. Updates heap and heap_len.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PqRemove(DeflateState s, ref TreeNode tree, ref int top, ref int heap, ref byte depth)
    {
        top = Unsafe.Add(ref heap, Smallest);
        Unsafe.Add(ref heap, Smallest) = Unsafe.Add(ref heap, s.heap_len--);
        PqDownHeap(s, ref tree, Smallest, ref heap, ref depth);
    }

    /// <summary>
    /// Computes the optimal bit lengths for a tree and update the total bit length for the current block.
    /// </summary>
    private static void GenBitLen(DeflateState s, TreeDescriptor desc, ref TreeNode tree, ref TreeNode stree,
        ref int extra, ref ushort bl_count, ref int heap)
    {
        int max_code = desc.max_code;
        uint @base = desc.stat_desc.extra_base;
        uint max_length = desc.stat_desc.max_length;
        uint h;             // heap index
        uint n;             // iterate over the tree elements
        uint bits;          // bit length
        int overflow = 0;   // number of elements with bit length too large

        netUnsafe.InitBlock(ref netUnsafe.As<ushort, byte>(ref bl_count), 0, MaxBits * sizeof(ushort));

        /* In a first pass, compute the optimal bit lengths (which may
         * overflow in the case of the bit length tree).
         */
        Unsafe.Add(ref tree, (uint)Unsafe.Add(ref heap, s.heap_max)).dl = 0; // root of the heap

        for (h = s.heap_max + 1; h < HeapSize; h++)
        {
            n = (uint)Unsafe.Add(ref heap, h);
            bits = Unsafe.Add(ref tree, (uint)Unsafe.Add(ref tree, n).dl).dl + 1U;
            if (bits > max_length)
            {
                bits = max_length;
                overflow++;
            }
            Unsafe.Add(ref tree, n).dl = (ushort)bits;
            // We overwrite tree[n].Dad which is no longer needed

            if (n > max_code)
                continue; // not a leaf node

            Unsafe.Add(ref bl_count, bits)++;
            int xbits = 0; // extra bits
            if (n >= @base)
                xbits = Unsafe.Add(ref extra, n - @base);
            ushort f = Unsafe.Add(ref tree, n).fc; // frequency
            s.opt_len += f * (uint)(bits + xbits);
            if (desc.stat_desc.static_tree != null)
                s.static_len += f * (uint)(Unsafe.Add(ref stree, n).dl + xbits);
        }
        if (overflow == 0)
            return;

        Trace.Tracev("\nbit length overflow\n");
        // This happens for example on obj2 and pic of the Calgary corpus

        // Find the first bit length which could increase:
        do
        {
            bits = max_length - 1;
            while (Unsafe.Add(ref bl_count, bits) == 0)
                bits--;
            Unsafe.Add(ref bl_count, bits)--; // move one leaf down the tree
            Unsafe.Add(ref bl_count, bits + 1) += 2; // move one overflow item as its brother
            Unsafe.Add(ref bl_count, max_length)--;
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
            n = Unsafe.Add(ref bl_count, bits);
            while (n != 0)
            {
                int m = Unsafe.Add(ref heap, --h);
                if (m > max_code)
                    continue;
                ref TreeNode tm = ref Unsafe.Add(ref tree, (uint)m);
                if (tm.dl != bits)
                {
                    Trace.Tracev($"code {m} bits {tm.dl}->{bits}\n");
                    s.opt_len += (bits - tm.dl) * tm.fc;
                    tm.dl = (ushort)bits;
                }
                n--;
            }
        }
    }

    /// <summary>
    /// Generates the codes for a given tree and bit counts (which need not be optimal).
    /// </summary>
    private static void GenCodes(ref TreeNode tree, int max_code, ref ushort bl_count)
    {
        Span<ushort> next_codes = stackalloc ushort[MaxBits + 1]; // next code value for each bit length
        uint code = 0;  // running code value
        ref ushort next_code = ref MemoryMarshal.GetReference(next_codes);
        /* The distribution counts are first used to generate the code values
         * without bit reversal.
         */
        for (uint bits = 1; bits <= MaxBits; bits++)
        {
            code = (code + Unsafe.Add(ref bl_count, bits - 1)) << 1;
            Unsafe.Add(ref next_code, bits) = (ushort)code;
        }
        /* Check that the bit counts in bl_count are consistent. The last code
         * must be all ones.
         */
        Debug.Assert(code + Unsafe.Add(ref bl_count, (uint)MaxBits) - 1 == (1 << MaxBits) - 1, "inconsistent bit counts");
        Trace.Tracev($"\ngen_codes: max_code {max_code} ");

        for (uint n = 0; n <= max_code; n++)
        {
            uint len = Unsafe.Add(ref tree, n).dl;
            if (len == 0)
                continue;
            // Now reverse the bits
            Unsafe.Add(ref tree, n).fc = (ushort)BiReverse(Unsafe.Add(ref next_code, len)++, len);
#if DEBUG
            Trace.Tracecv(!netUnsafe.AreSame(ref tree, ref MemoryMarshal.GetReference<TreeNode>(s_ltree)),
                $"\nn {n,3} {(IsGraph(n) ? Convert.ToChar(n) : ' ')} l {len,2} c {Unsafe.Add(ref tree, n).dl,4:x} ({Unsafe.Add(ref next_code, len) - 1:x)}) ");
#endif
        }
    }

    /// <summary>
    /// Reverse the first len bits of a code, using straightforward code (a faster method would use a table).
    /// </summary>
    private static uint BiReverse(uint code, uint len)
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
    private static uint BuildBlTree(DeflateState s, ref TreeNode dyn_ltree, ref TreeNode dyn_dtree, ref TreeNode bl_tree,
        ref int extra, ref ushort bl_count, ref int heap, ref byte depth, ref ushort bl_order)
    {
        uint max_blindex; // index of last bit length code of non zero freq

        // Determine the bit length frequencies for literal and distance trees
        ScanTree(ref dyn_ltree, s.l_desc.max_code, ref bl_tree);
        ScanTree(ref dyn_dtree, s.d_desc.max_code, ref bl_tree);

        // Build the bit length tree:
        BuildTree(s, s.bl_desc, ref bl_tree, ref netUnsafe.NullRef<TreeNode>(), ref extra, ref bl_count, ref heap, ref depth);
        /* opt_len now includes the length of the tree representations, except
         * the lengths of the bit lengths codes and the 5+5+4 bits for the counts.
         */

        /* Determine the number of bit length codes to send. The pkzip format
         * requires that at least 4 bit length codes be sent. (appnote.txt says
         * 3 but the actual value used is 4.)
         */
        for (max_blindex = BlCodes - 1; max_blindex >= 3; max_blindex--)
        {
            if (Unsafe.Add(ref bl_tree, (uint)Unsafe.Add(ref bl_order, max_blindex)).dl != 0)
                break;
        }
        // Update opt_len to include the bit length tree and counts
        s.opt_len += 3 * (max_blindex + 1) + 5 + 5 + 4;
        Trace.Tracev($"\ndyn trees: dyn {s.opt_len}, stat {s.static_len}");

        return max_blindex;
    }

    private static void ScanTree(ref TreeNode tree, int max_code, ref TreeNode bl_tree)
    {
        uint prevlen = uint.MaxValue; // last emitted length
        uint nextlen = tree.dl;       // length of next code
        int count = 0;                // repeat count of the current code
        int max_count = 7;            // max repeat count
        int min_count = 4;            // min repeat count

        if (nextlen == 0)
        {
            max_count = 138;
            min_count = 3;
        }
        Unsafe.Add(ref tree, (uint)(max_code + 1)).dl = 0xffff; // guard

        for (uint n = 0; n <= max_code; n++)
        {
            uint curlen = nextlen; // length of current code
            nextlen = Unsafe.Add(ref tree, n + 1).dl;
            if (++count < max_count && curlen == nextlen)
            {
                continue;
            }
            else if (count < min_count)
            {
                Unsafe.Add(ref bl_tree, curlen).fc += (ushort)count;
            }
            else if (curlen != 0)
            {
                if (curlen != prevlen)
                    Unsafe.Add(ref bl_tree, curlen).fc++;
                Unsafe.Add(ref bl_tree, Rep_3_6).fc++;
            }
            else if (count <= 10)
            {
                Unsafe.Add(ref bl_tree, RepZ_3_10).fc++;
            }
            else
            {
                Unsafe.Add(ref bl_tree, RepZ_11_138).fc++;
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
    internal static bool Tally(DeflateState s, uint dist, uint lc,
        ref byte pending_buf, ref TreeNode dyn_ltree, ref TreeNode dyn_dtree,
        ref byte dist_code, ref byte length_code)
    {
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = (byte)dist;
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = (byte)(dist >> 8);
        Unsafe.Add(ref pending_buf, s.lit_bufsize + s.sym_next++) = (byte)lc;

        if (dist == 0)
        {
            // lc is the unmatched char
            Unsafe.Add(ref dyn_ltree, lc).fc++;
        }
        else
        {
            s.matches++;
            // Here, lc is the match length - MinMatch
            dist--; // dist = match distance - 1
            Debug.Assert(dist < Deflater.MaxDist(s)
                && lc <= MaxMatch - MinMatch
                && DCode(dist, ref dist_code) < DCodes, "_tr_tally: bad match");

            Unsafe.Add(ref dyn_ltree, (uint)Unsafe.Add(ref length_code, lc) + Literals + 1).fc++;
            Unsafe.Add(ref dyn_dtree, DCode(dist, ref dist_code)).fc++;
        }
        return s.sym_next == s.sym_end;
    }

    /// <summary>
    /// Sends the block data compressed using the given Huffman trees.
    /// </summary>
    private static void CompressBlock(DeflateState s, ref TreeNode ltree, ref TreeNode dtree, ref byte pending_buf,
        ref byte dist_code, ref byte length_code, ref int base_dist, ref int base_length, ref int extra_dbits, ref int extra_lbits)
    {
        if (s.sym_next != 0)
        {
            uint sx = 0; // running index in sym_buf
            do
            {
                uint dist = Unsafe.Add(ref pending_buf, s.lit_bufsize + sx++) & 0xffU; // distance of matched string
                dist += (Unsafe.Add(ref pending_buf, s.lit_bufsize + sx++) & 0xffU) << 8;
                uint lc = Unsafe.Add(ref pending_buf, s.lit_bufsize + sx++); // match length or unmatched char (if dist == 0)
                if (dist == 0)
                {
                    SendCode(s, ref Unsafe.Add(ref ltree, lc), ref pending_buf); // send a literal byte
#if DEBUG
                    Trace.Tracecv(IsGraph(lc), $" '{lc}' ");
#endif
                }
                else
                {
                    // Here, lc is the match length - MIN_MATCH
                    uint code = Unsafe.Add(ref length_code, lc); // the code to send
                    SendCode(s, ref Unsafe.Add(ref ltree, code + Literals + 1), ref pending_buf); // send length code
                    int extra = Unsafe.Add(ref extra_lbits, code); // number of extra bits to send
                    if (extra != 0)
                    {
                        lc -= (uint)Unsafe.Add(ref base_length, code);
                        SendBits(s, lc, extra, ref pending_buf); // send the extra length bits
                    }
                    dist--; // dist is now the match distance - 1
                    code = DCode(dist, ref dist_code);
                    Debug.Assert(code < DCodes, "bad d_code");

                    SendCode(s, ref Unsafe.Add(ref dtree, code), ref pending_buf); // send the distance code
                    extra = Unsafe.Add(ref extra_dbits, code);
                    if (extra != 0)
                    {
                        dist -= (uint)Unsafe.Add(ref base_dist, code);
                        SendBits(s, dist, extra, ref pending_buf); // send the extra distance bits
                    }
                } // literal or match pair ?

                // Check that the overlay between pending_buf and sym_buf is ok:
                Debug.Assert(s.pending < s.lit_bufsize + sx, "pendingBuf overflow");

            } while (sx < s.sym_next);

        }

        SendCode(s, ref Unsafe.Add(ref ltree, EndBlock), ref pending_buf);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint DCode(uint dist, ref byte dist_code) =>
        dist < 256 ? Unsafe.Add(ref dist_code, dist) : Unsafe.Add(ref dist_code, 256 + ((dist) >> 7));

    /// <summary>
    /// Sends the header for a block using dynamic Huffman trees: the counts, the lengths of the bit length codes, the literal tree and the distance tree.
    /// </summary>
    private static void SendAllTrees(DeflateState s, uint lcodes, uint dcodes, uint blcodes,
        ref byte pending_buf, ref TreeNode dyn_ltree, ref TreeNode dyn_dtree, ref TreeNode bl_tree, ref ushort bl_order)
    {
        Debug.Assert(lcodes >= 257 && dcodes >= 1 && blcodes >= 4, "not enough codes");
        Debug.Assert(lcodes <= LCodes && dcodes <= DCodes && blcodes <= BlCodes, "too many codes");
        Trace.Tracev("\nbl counts: ");
        SendBits(s, lcodes - 257, 5, ref pending_buf); // not +255 as stated in appnote.txt
        SendBits(s, dcodes - 1, 5, ref pending_buf);
        SendBits(s, blcodes - 4, 4, ref pending_buf); // not -3 as stated in appnote.txt
        for (uint rank = 0; rank < blcodes; rank++)
        {
            uint code = Unsafe.Add(ref bl_order, rank);
            Trace.Tracev($"\nbl code {code,2} ");
            SendBits(s, Unsafe.Add(ref bl_tree, code).dl, 3, ref pending_buf);
        }
#if DEBUG
        Trace.Tracev($"\nbl tree: sent {s.bits_sent}");
#endif

        SendTree(s, ref dyn_ltree, lcodes - 1, ref pending_buf, ref bl_tree); // literal tree
#if DEBUG
        Trace.Tracev($"\nlit tree: sent {s.bits_sent}");
#endif

        SendTree(s, ref dyn_dtree, dcodes - 1, ref pending_buf, ref bl_tree); // distance tree
#if DEBUG
        Trace.Tracev($"\ndist tree: sent {s.bits_sent}");
#endif
    }

    /// <summary>
    /// Sends a literal or distance tree in compressed form, using the codes in bl_tree.
    /// </summary>
    private static void SendTree(DeflateState s, ref TreeNode tree, uint max_code, ref byte pending_buf, ref TreeNode bl_tree)
    {
        uint prevlen = uint.MaxValue;   // last emitted length
        uint nextlen = tree.dl;         // length of next code
        uint count = 0;                 // repeat count of the current code
        int max_count = 7;              // max repeat count
        int min_count = 4;              // min repeat count

        if (nextlen == 0)
        {
            max_count = 138;
            min_count = 3;
        }

        for (uint n = 0; n <= max_code; n++)
        {
            uint curlen = nextlen; // length of current code
            nextlen = Unsafe.Add(ref tree, n + 1).dl;
            if (++count < max_count && curlen == nextlen)
            {
                continue;
            }
            else if (count < min_count)
            {
                do
                {
                    SendCode(s, ref Unsafe.Add(ref bl_tree, curlen), ref pending_buf);
                } while (--count != 0);
            }
            else if (curlen != 0)
            {
                if (curlen != prevlen)
                {
                    SendCode(s, ref Unsafe.Add(ref bl_tree, curlen), ref pending_buf);
                    count--;
                }
                Debug.Assert(count >= 3 && count <= 6, " 3_6?");
                SendCode(s, ref Unsafe.Add(ref bl_tree, Rep_3_6), ref pending_buf);
                SendBits(s, count - 3, 2, ref pending_buf);

            }
            else if (count <= 10)
            {
                SendCode(s, ref Unsafe.Add(ref bl_tree, RepZ_3_10), ref pending_buf);
                SendBits(s, count - 3, 3, ref pending_buf);
            }
            else
            {
                SendCode(s, ref Unsafe.Add(ref bl_tree, RepZ_11_138), ref pending_buf);
                SendBits(s, count - 11, 7, ref pending_buf);
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
    private static bool IsGraph(uint ch) => ch > 32 && ch < 127;
#endif
}