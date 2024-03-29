﻿// Original code and comments Copyright (C) 1995-2024 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

using static ZLibDotNet.Deflate.Constants;

namespace ZLibDotNet.Deflate;

/// <summary>
/// State maintained between <see cref="ZLib.Deflate(ref ZStream, int)"/> calls.
/// </summary>
internal sealed class DeflateState
{
    private const byte MaxBlBits = 7; // Bit length codes must not exceed MAX_BL_BITS bits

    private static readonly StaticTree s_l_desc = new(Tree.s_ltree, Literals + 1, LCodes, MaxBits);

    private static readonly StaticTree s_d_desc = new(Tree.s_dtree, 0, DCodes, MaxBits);

    private static readonly StaticTree s_bl_desc = new(null, 0, BlCodes, MaxBlBits);

    /// <summary>
    /// Creates an instance of the <see cref="DeflateState"/> class.
    /// </summary>
    public DeflateState()
    {
        l_desc = new(dyn_ltree, s_l_desc);
        d_desc = new(dyn_dtree, s_d_desc);
        bl_desc = new(bl_tree, s_bl_desc);
    }

    internal uint pendingOutOffset;

    internal int status;            // as the name implies
    internal byte[] pending_buf;    // output still pending

    internal uint pending_buf_size; // size of pending_buf
    internal int wrap;              // bit 0 true for zlib, bit 1 true for gzip

    internal byte[] pending_out;    // next pending byte to output to the stream

    internal uint pending;          // nb of bytes in the pending buffer

    internal byte method;           // can only be DEFLATED
    internal int last_flush;        // value of flush param for previous deflate call

    internal uint w_size;           // LZ77 window size (32K by default)
    internal uint w_bits;           // log2(w_size)  (8..16)
    internal uint w_mask;           // w_size - 1

    internal byte[] window;
    /* Sliding window. Input bytes are read into the second half of the window,
     * and move to the first half later to keep a dictionary of at least wSize
     * bytes. With this organization, matches are limited to a distance of
     * wSize-MAX_MATCH bytes, but this ensures that IO is always
     * performed with a length multiple of the block size. Also, it limits
     * the window size to 64K, which is quite useful on MSDOS.
     * To do: use the user input buffer as sliding window.
     */

    internal uint window_size; // Actual size of window: 2*wSize, except when the user input buffer is directly used as sliding window.

    internal ushort[] prev;
    /* Link to older string with same hash index. To limit the size of this
     * array to 64K, this link is maintained only for the last 32K strings.
     * An index in this array is thus a window index modulo 32K.
     */

    internal ushort[] head; // Heads of the hash chains or null.

    internal uint ins_h;            // hash index of string to be inserted
    internal uint hash_size;        // number of elements in hash table
    internal uint hash_bits;        // log2(hash_size)
    internal uint hash_mask;        // hash_size-1

    internal int hash_shift;
    /* Number of bits by which ins_h must be shifted at each input
     * step. It must be such that after MIN_MATCH steps, the oldest
     * byte no longer takes part in the hash key, that is:
     *   hash_shift * MIN_MATCH >= hash_bits
     */

    internal int block_start;       // Window position at the beginning of the current output block. Gets negative when the window is moved backwards.

    internal uint match_length;     // length of best match
    internal uint prev_match;       // previous match
    internal bool match_available;  // set if previous match exists
    internal uint strstart;         // start of string to insert
    internal uint match_start;      // start of matching string
    internal uint lookahead;        // number of valid bytes ahead in window

    internal uint prev_length;
    /* Length of the best match at previous step. Matches not greater than this
     * are discarded. This is used in the lazy match evaluation.
     */

    internal uint max_chain_length;
    /* To speed up deflation, hash chains are never searched beyond this
     * length.  A higher limit improves compression ratio but degrades the
     * speed.
     */

    internal uint max_lazy_match;
    /* Attempt to find a better match only when the current match is strictly
     * smaller than this value. This mechanism is used only for compression
     * levels >= 4.
     */

    internal int level;         // compression level (1..9)
    internal int strategy;      // favor or force Huffman coding

    internal uint good_match;   // Use a faster search when the previous match is longer than this

    internal int nice_match;    // Stop searching when current match exceeds this

    internal readonly TreeNode[] dyn_ltree = new TreeNode[HeapSize];        // literal and length tree
    internal readonly TreeNode[] dyn_dtree = new TreeNode[2 * DCodes + 1];  // distance tree
    internal readonly TreeNode[] bl_tree = new TreeNode[2 * BlCodes + 1];   // Huffman tree for bit lengths

    internal readonly TreeDescriptor l_desc;    // desc. for literal tree
    internal readonly TreeDescriptor d_desc;    // desc. for distance tree
    internal readonly TreeDescriptor bl_desc;   // desc. for bit length tree

    internal readonly ushort[] bl_count = new ushort[MaxBits + 1]; // number of codes at each bit length for an optimal tree

    internal readonly int[] heap = new int[2 * LCodes + 1];  // heap used to build the Huffman trees
    internal uint heap_len;                         // number of elements in the heap
    internal uint heap_max;                         // element of largest frequency

    internal readonly byte[] depth = new byte[2 * LCodes + 1]; // Depth of each subtree used as tie breaker for trees of equal frequency

    internal uint lit_bufsize;
    /* Size of match buffer for literals/lengths.  There are 4 reasons for
     * limiting lit_bufsize to 64K:
     *   - frequencies can be kept in 16 bit counters
     *   - if compression is not successful for the first block, all input
     *     data is still in the window so we can still emit a stored block even
     *     when input comes from standard input.  (This can also be done for
     *     all blocks if lit_bufsize is not greater than 32K.)
     *   - if compression is not successful for a file smaller than 64K, we can
     *     even emit a stored file instead of a stored block (saving 5 bytes).
     *     This is applicable only for zip (not gzip or zlib).
     *   - creating new Huffman trees less frequently may not provide fast
     *     adaptation to changes in the input data statistics. (Take for
     *     example a binary file with poorly compressible code followed by
     *     a highly compressible string table.) Smaller buffer sizes give
     *     fast adaptation but have of course the overhead of transmitting
     *     trees more frequently.
     *   - I can't count above 4
     */

    internal uint sym_next;         // running index in sym_buf
    internal uint sym_end;          // symbol table full when sym_next reaches this

    internal uint opt_len;          // bit length of current block with optimal trees
    internal uint static_len;       // bit length of current block with static trees
    internal uint matches;          // number of string matches in current block
    internal uint insert;           // bytes at end of window left to insert

#if DEBUG
    internal uint compressed_len;   // total bit length of compressed file mod 2^32
    internal uint bits_sent;        // bit length of compressed data sent mod 2^32
#endif

    internal ushort bi_buf; //Output buffer. bits are inserted starting at the bottom (least significant bits).

    internal int bi_valid;  //Number of valid bits in bi_buf. All bits above the last valid bit are always zero.

    internal uint high_water;
    /* High water mark offset in window for initialized bytes -- bytes above
     * this are set to zero in order to avoid memory check warnings when
     * longest match routines access bytes past the input.  This is then
     * updated to the new high water mark.
     */
}