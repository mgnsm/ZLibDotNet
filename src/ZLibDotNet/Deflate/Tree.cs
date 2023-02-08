// Original code and comments Copyright (C) 1995-2021 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static ZLibDotNet.Deflate.Constants;

namespace ZLibDotNet.Deflate;

internal static class Tree
{
    private const byte MaxBlBits = 7;       // Bit length codes must not exceed MAX_BL_BITS bits
    private const uint EndBlock = 256;      // end of block literal code
    private const uint Rep_3_6 = 16;        // repeat previous bit length 3-6 times (2 bits of repeat count)
    private const uint RepZ_3_10 = 17;      // repeat a zero length 3-10 times  (3 bits of repeat count)
    private const uint RepZ_11_138 = 18;    // repeat a zero length 11-138 times  (7 bits of repeat count)
    private const uint Smallest = 1;        // Index within the heap array of least frequent node in the Huffman tree
    private const byte StaticTrees = 1;
    private const byte DynTrees = 2;

    private static readonly TreeNode[] s_dtree = new TreeNode[DCodes]
    {
            new TreeNode(0, 5), new TreeNode(16, 5), new TreeNode(8, 5), new TreeNode(24, 5), new TreeNode(4, 5),
            new TreeNode(20, 5), new TreeNode(12, 5), new TreeNode(28, 5), new TreeNode(2, 5), new TreeNode(18, 5),
            new TreeNode(10, 5), new TreeNode(26, 5), new TreeNode(6, 5), new TreeNode(22, 5), new TreeNode(14, 5),
            new TreeNode(30, 5), new TreeNode(1, 5), new TreeNode(17, 5), new TreeNode(9, 5), new TreeNode(25, 5),
            new TreeNode(5, 5), new TreeNode(21, 5), new TreeNode(13, 5), new TreeNode(29, 5), new TreeNode(3, 5),
            new TreeNode(19, 5), new TreeNode(11, 5), new TreeNode(27, 5), new TreeNode(7, 5), new TreeNode(23, 5)
    };

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

    internal static readonly int[] s_extra_dbits = // extra bits for each distance code
        new int[DCodes] { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };

    internal static readonly int[] s_extra_lbits = // extra bits for each length code
        new int[LenghtCodes] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };

    private static readonly int[] s_extra_blbits = // extra bits for each bit length code
        new int[BlCodes] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 7 };

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
        InitBlock(s, ref MemoryMarshal.GetReference(s.dyn_ltree.AsSpan()),
            ref MemoryMarshal.GetReference(s.dyn_dtree.AsSpan()),
            ref MemoryMarshal.GetReference(s.bl_tree.AsSpan()));
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
    internal static void Align(DeflateState s, ref byte pending_buf)
    {
        SendBits(s, StaticTrees << 1, 3, ref pending_buf);
        SendCode(s, ref Unsafe.Add(ref MemoryMarshal.GetReference(s_ltree.AsSpan()), EndBlock), ref pending_buf);
#if DEBUG
        s.compressed_len += 10U; // 3 for block type, 7 for EOB
#endif
        FlushBits(s, ref pending_buf);
    }

    /// <summary>
    /// Determines the best encoding for the current block: dynamic trees, static trees or store, and writes out the encoded block.
    /// </summary>
    internal static void FlushBlock(DeflateState s, ref byte buf, uint stored_len, uint last,
        ref byte pending_buf, ref TreeNode dyn_ltree, ref TreeNode dyn_dtree, ref TreeNode bl_tree,
        ref ushort bl_count, ref int heap, ref byte depth, ref ushort bl_order, ref byte dist_code,
        ref byte length_code, ref int base_dist, ref int base_length, ref int extra_dbits, ref int extra_lbits)
    {
        uint opt_lenb, static_lenb; // opt_len and static_len in bytes
        uint max_blindex = 0;  // index of last bit length code of non zero freq */

        // Build the Huffman trees unless a stored block is forced
        if (s.level > 0)
        {
            // Check if the file is binary or text
            if (s.strm.data_type == Z_UNKNOWN)
                s.strm.data_type = DetectDataType(ref dyn_ltree);

            // Construct the literal and distance trees
            BuildTree(s, s.l_desc, ref bl_count, ref heap, ref depth);
            Trace.Tracev($"\nlit data: dyn {s.opt_len}, stat {s.static_len}");

            BuildTree(s, s.d_desc, ref bl_count, ref heap, ref depth);
            Trace.Tracev($"\ndist data: dyn {s.opt_len}, stat {s.static_len}");
            /* At this point, opt_len and static_len are the total bit lengths of
             * the compressed block data, excluding the tree representations.
             */

            /* Build the bit length tree for the above two trees, and get the index
             * in bl_order of the last bit length code to send.
             */
            max_blindex = BuildBlTree(s, ref dyn_ltree, ref dyn_dtree, ref bl_tree, ref bl_count, ref heap, ref depth,
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
            CompressBlock(s, ref MemoryMarshal.GetReference(s_ltree.AsSpan()),
                ref MemoryMarshal.GetReference(s_dtree.AsSpan()), ref pending_buf, ref dist_code, ref length_code,
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
        uint n;
        for (n = 0; n < LCodes; n++)
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
        uint n;

        // Check for non-textual ("black-listed") bytes.
        for (n = 0; n <= 31; n++, black_mask >>= 1)
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

    private static void BuildTree(DeflateState s, TreeDescriptor desc, ref ushort bl_count, ref int heap, ref byte depth)
    {
        uint elems = desc.stat_desc.elems;
        int max_code = -1; // largest code with non zero frequency
        uint node;         // new node being created
        ref TreeNode tree = ref MemoryMarshal.GetReference(desc.dyn_tree.AsSpan());
        ref TreeNode stree = ref desc.stat_desc.static_tree == null ? ref netUnsafe.NullRef<TreeNode>()
            : ref MemoryMarshal.GetReference(desc.stat_desc.static_tree.AsSpan());

        /* Construct the initial heap, with least frequent element in
         * heap[SMALLEST]. The sons of heap[n] are heap[2*n] and heap[2*n+1].
         * heap[0] is not used.
         */
        s.heap_len = 0;
        s.heap_max = HeapSize;

        uint n = 0;
        for (; n < elems; n++)
        {
            if (Unsafe.Add(ref tree, n).fc != 0)
            {
                Unsafe.Add(ref heap, ++s.heap_len) = max_code = (int)n;
                Unsafe.Add(ref depth, n) = 0;
            }
            else
            {
                Unsafe.Add(ref tree, n).dl = 0;
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
        GenBitLen(s, desc, ref bl_count, ref heap);

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
    private static void GenBitLen(DeflateState s, TreeDescriptor desc, ref ushort bl_count, ref int heap)
    {
        int max_code = desc.max_code;
        uint @base = desc.stat_desc.extra_base;
        uint max_length = desc.stat_desc.max_length;
        uint h;             // heap index
        uint n;             // iterate over the tree elements
        uint bits;          // bit length
        int xbits;          // extra bits
        ushort f;           // frequency
        int overflow = 0;   // number of elements with bit length too large
        ref TreeNode tree = ref MemoryMarshal.GetReference(desc.dyn_tree.AsSpan());
        ref TreeNode stree = ref desc.stat_desc.static_tree == null ? ref netUnsafe.NullRef<TreeNode>()
            : ref MemoryMarshal.GetReference(desc.stat_desc.static_tree.AsSpan());
        ref int extra = ref MemoryMarshal.GetReference(desc.stat_desc.extra_bits.AsSpan());

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
            xbits = 0;
            if (n >= @base)
                xbits = Unsafe.Add(ref extra, n - @base);
            f = Unsafe.Add(ref tree, n).fc;
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
        Span<ushort> next_codes = new ushort[MaxBits + 1]; // next code value for each bit length
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
            Trace.Tracecv(!netUnsafe.AreSame(ref tree, ref MemoryMarshal.GetReference(s_ltree.AsSpan())),
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
        ref ushort bl_count, ref int heap, ref byte depth, ref ushort bl_order)
    {
        uint max_blindex; // index of last bit length code of non zero freq

        // Determine the bit length frequencies for literal and distance trees
        ScanTree(ref dyn_ltree, s.l_desc.max_code, ref bl_tree);
        ScanTree(ref dyn_dtree, s.d_desc.max_code, ref bl_tree);

        // Build the bit length tree:
        BuildTree(s, s.bl_desc, ref bl_count, ref heap, ref depth);
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
        uint curlen;                  // length of current code
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
            curlen = nextlen;
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
        uint curlen;                    // length of current code
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
            curlen = nextlen;
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