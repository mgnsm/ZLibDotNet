// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Inflate;

internal static partial class Inflater
{
    internal const ushort EnoughLens = 852;
    internal const ushort EnoughDists = 592;
    private const byte MaxBits = 15;

    private static readonly ushort[] s_lbase = new ushort[31] { // Length codes 257..285 base
        3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
        35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258, 0, 0 };

    private static readonly ushort[] s_lext = new ushort[31] { // Length codes 257..285 extra
        16, 16, 16, 16, 16, 16, 16, 16, 17, 17, 17, 17, 18, 18, 18, 18,
        19, 19, 19, 19, 20, 20, 20, 20, 21, 21, 21, 21, 16, 77, 202 };

    private static readonly ushort[] s_dbase = new ushort[32] { // Distance codes 0..29 base
        1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
        257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
        8193, 12289, 16385, 24577, 0, 0 };

    private static readonly ushort[] s_dext = new ushort[32]{ // Distance codes 0..29 extra
        16, 16, 16, 16, 17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22, 22,
        23, 23, 24, 24, 25, 25, 26, 26, 27, 27,
        28, 28, 29, 29, 64, 64 };

    internal static unsafe int InflateTable(CodeType type, ushort* lens, uint codes, ref Code* table, ref uint bits, ushort* work)
    {
        uint len;               // a code's length in bits
        uint sym;               // index of code symbols
        uint min, max;          // minimum and maximum code lengths
        uint root;              // number of index bits for root table
        uint curr;              // number of index bits for current table
        uint drop;              // code bits to drop for sub-table
        int left;               // number of prefix codes available
        uint used;              // code entries in table used
        uint huff;              // Huffman code
        uint incr;              // for incrementing code, index
        uint fill;              // index for replicating entries
        uint low;               // low bits for current root entry
        uint mask;              // mask for low root bits
        Code here;              // table entry for duplication
        Code* next;             // next available space in table
        ushort* @base;          // base value table to use
        ushort* extra;          // extra bits table to use
        uint match;             // use base and extra for symbol >= match
        ushort[] count = new ushort[MaxBits + 1]; // number of codes of each length
        ushort[] offs = new ushort[MaxBits + 1];  // offsets in table for each length

        // accumulate lengths for codes (assumes lens[] all in 0..MAXBITS)
        for (len = 0; len <= MaxBits; len++)
            count[len] = 0;
        for (sym = 0; sym < codes; sym++)
            count[lens[sym]]++;

        // bound code lengths, force root to be within code lengths
        root = bits;
        for (max = MaxBits; max >= 1; max--)
            if (count[max] != 0)
                break;
        if (root > max)
            root = max;
        if (max == 0) // no symbols to code at all
        {
            here = new Code(64 /* invalid code marker */, 1, 0);
            *table++ = here; // make a table to force an error
            *table++ = here;
            bits = 1;
            return 0; // no symbols, but wait for decoding to report error
        }
        for (min = 1; min < max; min++)
            if (count[min] != 0)
                break;
        if (root < min)
            root = min;

        // check for an over-subscribed or incomplete set of lengths
        left = 1;
        for (len = 1; len <= MaxBits; len++)
        {
            left <<= 1;
            left -= count[len];
            if (left < 0)
                return -1; // over-subscribed
        }
        if (left > 0 && (type == CodeType.Codes || max != 1))
            return -1; // incomplete set

        // generate offsets into symbol table for each length for sorting
        offs[1] = 0;
        for (len = 1; len < MaxBits; len++)
            offs[len + 1] = (ushort)(offs[len] + count[len]);

        // sort symbols by length, by symbol order within each length
        for (sym = 0; sym < codes; sym++)
            if (lens[sym] != 0)
                work[offs[lens[sym]]++] = (ushort)sym;

        fixed (ushort* lbase = s_lbase, lext = s_lext, dbase = s_dbase, dext = s_dext)
        {
            // set up for code type
            switch (type)
            {
                case CodeType.Codes:
                    @base = extra = work; // dummy value--not used
                    match = 20;
                    break;
                case CodeType.Lens:
                    @base = lbase;
                    extra = lext;
                    match = 257;
                    break;
                default: // DISTS
                    @base = dbase;
                    extra = dext;
                    match = 0;
                    break;
            }

            // initialize state for loop
            huff = 0;                   // starting code
            sym = 0;                    // starting code symbol
            len = min;                  // starting code length
            next = table;               // current table to fill in
            curr = root;                // current table index bits
            drop = 0;                   // current bits to drop from code for index
            low = unchecked((uint)-1);  // trigger new sub-table when len > root
            used = 1U << (int)root;     // use root table entries
            mask = used - 1;            // mask for comparing low

            // check available table space
            if (type == CodeType.Lens && used > EnoughLens ||
                type == CodeType.Dists && used > EnoughDists)
                return 1;

            // process all codes and make table entries
            for (; ; )
            {
                // create table entry
                byte temp = (byte)(len - drop);
                if (work[sym] + 1U < match)
                    here = new Code(0, temp, work[sym]);
                else if (work[sym] >= match)
                    here = new Code((byte)extra[work[sym] - match], temp, @base[work[sym] - match]);
                else
                    here = new Code(32 + 64, temp, 0); // end of block

                // replicate for those indices with low len bits equal to huff
                incr = 1U << (int)(len - drop);
                fill = 1U << (int)curr;
                min = fill; // save offset to next table
                do
                {
                    fill -= incr;
                    next[(huff >> (int)drop) + fill] = here;
                } while (fill != 0);

                // backwards increment the len-bit code huff
                incr = 1U << (int)(len - 1);
                while ((huff & incr) != 0)
                    incr >>= 1;
                if (incr != 0)
                {
                    huff &= incr - 1;
                    huff += incr;
                }
                else
                    huff = 0;

                // go to next symbol, update count, len
                sym++;
                if (--count[len] == 0)
                {
                    if (len == max)
                        break;
                    len = lens[work[sym]];
                }

                // create new sub-table if needed
                if (len > root && (huff & mask) != low)
                {
                    // if first time, transition to sub-tables
                    if (drop == 0)
                        drop = root;

                    // increment past last table
                    next += min; // here min is 1 << curr

                    // determine length of next table
                    curr = len - drop;
                    left = 1 << (int)curr;
                    while (curr + drop < max)
                    {
                        left -= count[curr + drop];
                        if (left <= 0)
                            break;
                        curr++;
                        left <<= 1;
                    }

                    // check for enough space
                    used += 1U << (int)curr;
                    if (type == CodeType.Lens && used > EnoughLens ||
                        type == CodeType.Dists && used > EnoughDists)
                        return 1;

                    // point entry in root table to sub-table
                    low = huff & mask;
                    table[low] = new Code((byte)curr, (byte)root, (ushort)(next - table));
                }
            }
        }

        /* Fill in remaining table entry if code is incomplete (guaranteed to have 
         * at most one remaining entry, since if the code is incomplete, the 
         * maximum code length that was allowed to get this far is one bit) */
        if (huff != 0)
            next[huff] = new Code(64 /* invalid code marker */, (byte)(len - drop), 0);

        // set return parameters
        table += used;
        bits = root;
        return 0;
    }
}