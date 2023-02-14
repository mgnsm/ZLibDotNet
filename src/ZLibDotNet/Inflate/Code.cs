// Original code and comments Copyright (C) 1995-2005, 2010 Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

namespace ZLibDotNet.Inflate;

/* Structure for decoding tables. Each entry provides either the
 * information needed to do the operation requested by the code that
 * indexed that table entry, or it provides a pointer to another
 * table that indexes more bits of the code. op indicates whether
 * the entry is a pointer to another table, a literal, a length or
 * distance, an end-of-block, or an invalid code. For a table
 * pointer, the low four bits of op is the number of index bits of
 * that table. For a length or distance, the low four bits of op
 * is the number of extra bits to get after the code. bits is
 * the number of bits in this code or part of the code to drop off
 * of the bit buffer. val is the actual byte to output in the case
 * of a literal, the base length or distance, or the offset from
 * the current table to the next table. Each entry is four bytes. */
internal readonly struct Code
{
    internal readonly byte op;    // operation, extra bits, table bits
    internal readonly byte bits;  // bits in this part of the code
    internal readonly ushort val; // offset in table or code value

    internal Code(byte op, byte bits, ushort val)
    {
        this.op = op;
        this.bits = bits;
        this.val = val;
    }

    internal static int Size { get; } = netUnsafe.SizeOf<Code>();
}