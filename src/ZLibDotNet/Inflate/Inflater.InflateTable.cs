// Original code and comments Copyright (C) 1995-2022 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    internal static int InflateTable(CodeType type, ref ushort lens, int codes, ref Code table, ref int bits, ref ushort work, ref int offset)
    {
        int len;                   // a code's length in bits
        int sym;                   // index of code symbols
        int min, max;              // minimum and maximum code lengths
        int root;                  // number of index bits for root table
        int incr;                  // for incrementing code, index
        int fill;                  // index for replicating entries
        Code here;                 // table entry for duplication
        ushort[] count = default;  // number of codes of each length
        ushort[] offs = default;   // offsets in table for each length

        try
        {
            const int Length = MaxBits + 1;
            count = ArrayPool<ushort>.Shared.Rent(Length);
            ref ushort ptrToCount = ref MemoryMarshal.GetReference(count.AsSpan());

            // accumulate lengths for codes (assumes lens[] all in 0..MAXBITS)
            Unsafe.InitBlock(ref Unsafe.As<ushort, byte>(ref ptrToCount), 0, Length * sizeof(ushort));

            for (sym = 0; sym < codes; sym++)
                Unsafe.Add(ref ptrToCount, Unsafe.Add(ref lens, sym))++;

            // bound code lengths, force root to be within code lengths
            root = bits;
            for (max = MaxBits; max >= 1; max--)
                if (Unsafe.Add(ref ptrToCount, max) != 0)
                    break;
            if (root > max)
                root = max;
            if (max == 0) // no symbols to code at all
            {
                here = new Code(64 /* invalid code marker */, 1, 0);
                table = here; // make a table to force an error
                table = ref Unsafe.Add(ref table, 1);
                table = here;
                table = ref Unsafe.Add(ref table, 1);
                offset += 2;
                bits = 1;
                return 0; // no symbols, but wait for decoding to report error
            }
            for (min = 1; min < max; min++)
                if (Unsafe.Add(ref ptrToCount, min) != 0)
                    break;
            if (root < min)
                root = min;

            // check for an over-subscribed or incomplete set of lengths
            int left = 1; // number of prefix codes available
            for (len = 1; len <= MaxBits; len++)
            {
                left <<= 1;
                left -= Unsafe.Add(ref ptrToCount, len);
                if (left < 0)
                    return -1; // over-subscribed
            }
            if (left > 0 && (type == CodeType.Codes || max != 1))
                return -1; // incomplete set

            // generate offsets into symbol table for each length for sorting
            offs = ArrayPool<ushort>.Shared.Rent(Length);
            ref ushort ptrToOffs = ref MemoryMarshal.GetReference(offs.AsSpan());
            Unsafe.Add(ref ptrToOffs, 1) = 0;
            for (len = 1; len < MaxBits; len++)
                Unsafe.Add(ref ptrToOffs, len + 1) = (ushort)(Unsafe.Add(ref ptrToOffs, len) + Unsafe.Add(ref ptrToCount, len));

            // sort symbols by length, by symbol order within each length
            for (sym = 0; sym < codes; sym++)
                if (Unsafe.Add(ref lens, sym) != 0)
                    Unsafe.Add(ref work, Unsafe.Add(ref ptrToOffs, Unsafe.Add(ref lens, sym))++) = (ushort)sym;

            ref ushort lbase = ref MemoryMarshal.GetReference(s_lbase.AsSpan());
            ref ushort lext = ref MemoryMarshal.GetReference(s_lext.AsSpan());
            ref ushort dbase = ref MemoryMarshal.GetReference(s_dbase.AsSpan());
            ref ushort dext = ref MemoryMarshal.GetReference(s_dext.AsSpan());
            ref ushort @base = ref Unsafe.NullRef<ushort>(); // base value table to use
            ref ushort extra = ref Unsafe.NullRef<ushort>(); // extra bits table to use
            int match; // use base and extra for symbol >= match
            // set up for code type
            switch (type)
            {
                case CodeType.Codes:
                    @base = ref work; // dummy value--not used
                    extra = ref work; // dummy value--not used
                    match = 20;
                    break;
                case CodeType.Lens:
                    @base = ref lbase;
                    extra = ref lext;
                    match = 257;
                    break;
                default: // DISTS
                    @base = ref dbase;
                    extra = ref dext;
                    match = 0;
                    break;
            }

            // initialize state for loop
            int huff = 0;           // starting code
            sym = 0;                // starting code symbol
            len = min;              // starting code length
            int next = 0;           // current offset to table to fill in
            int curr = root;        // current table index bits
            int drop = 0;           // current bits to drop from code for index
            int low = int.MaxValue; // trigger new sub-table when len > root
            int used = 1 << root;   // use root table entries
            int mask = used - 1;    // mask for comparing low

            // check available table space
            if (type == CodeType.Lens && used > EnoughLens ||
                type == CodeType.Dists && used > EnoughDists)
                return 1;

            // process all codes and make table entries
            for (; ; )
            {
                // create table entry
                byte temp = (byte)(len - drop);
                ushort wsym = Unsafe.Add(ref work, sym);
                int diff = wsym - match;
                if (wsym + 1 < match)
                    here = new Code(0, temp, wsym);
                else if (wsym >= match)
                    here = new Code((byte)Unsafe.Add(ref extra, diff), temp, Unsafe.Add(ref @base, diff));
                else
                    here = new Code(32 + 64, temp, 0); // end of block

                // replicate for those indices with low len bits equal to huff
                incr = 1 << (len - drop);
                fill = 1 << curr;
                min = fill; // save offset to next table
                do
                {
                    fill -= incr;
                    Unsafe.Add(ref table, next + (huff >> drop) + fill) = here;
                } while (fill != 0);

                // backwards increment the len-bit code huff
                incr = 1 << (len - 1);
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
                if (--Unsafe.Add(ref ptrToCount, len) == 0)
                {
                    if (len == max)
                        break;
                    len = Unsafe.Add(ref lens, Unsafe.Add(ref work, sym));
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
                    left = 1 << curr;
                    while (curr + drop < max)
                    {
                        left -= Unsafe.Add(ref ptrToCount, curr + drop);
                        if (left <= 0)
                            break;
                        curr++;
                        left <<= 1;
                    }

                    // check for enough space
                    used += 1 << curr;
                    if (type == CodeType.Lens && used > EnoughLens ||
                        type == CodeType.Dists && used > EnoughDists)
                        return 1;

                    // point entry in root table to sub-table
                    low = huff & mask;
                    Unsafe.Add(ref table, low) = new Code((byte)curr, (byte)root, (ushort)next);
                }
            }

            /* Fill in remaining table entry if code is incomplete (guaranteed to have 
             * at most one remaining entry, since if the code is incomplete, the 
             * maximum code length that was allowed to get this far is one bit) */
            if (huff != 0)
                Unsafe.Add(ref table, next + huff) = new Code(64 /* invalid code marker */, (byte)(len - drop), 0);

            // set return parameters
            offset += used;
        }
        finally
        {
            if (count != default)
                ArrayPool<ushort>.Shared.Return(count);
            if (offs != default)
                ArrayPool<ushort>.Shared.Return(offs);
        }

        bits = root;
        return 0;
    }
}