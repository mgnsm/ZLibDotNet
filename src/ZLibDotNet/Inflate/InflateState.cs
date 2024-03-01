// Original code and comments Copyright (C) 1995-2019 Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

namespace ZLibDotNet.Inflate;

/// <summary>
/// State maintained between <see cref="ZLib.Inflate(ref ZStream, int)"/> calls.
/// </summary>
internal sealed class InflateState
{
    private const ushort Enough = Inflater.EnoughLens + Inflater.EnoughDists;

    internal InflateMode mode;      // current inflate mode
    internal int last;              // true if processing last block
    internal int wrap;              // bit 0 true for zlib, bit 1 true for gzip, bit 2 true to validate check value
    internal int havedict;          // true if dictionary provided
    internal int flags;             // gzip header method and flags, 0 if zlib, or -1 if raw or no header yet
    internal uint dmax;             // zlib header max distance (INFLATE_STRICT)
    internal uint check;            // protected copy of check value
    internal uint total;            // protected copy of output count
    internal uint wbits;            // log base 2 of requested window size
    internal uint wsize;            // window size or zero if not using window
    internal uint whave;            // valid bytes in the window
    internal uint wnext;            // window write index
    internal byte[] window;         // allocated sliding window, if needed
    internal uint hold;             // input bit accumulator
    internal uint bits;             // number of bits in "in"
    internal uint length;           // literal or length of data to copy
    internal uint offset;           // distance back to copy string from
    internal uint extra;            // extra bits needed
    internal Code[] lencode;        // starting table for length/literal codes
    internal Code[] distcode;       // starting table for distance codes
    internal int lenbits;           // index bits for lencode
    internal int distbits;          // index bits for distcode
    internal uint ncode;            // number of code length code lengths
    internal uint nlen;             // number of length code lengths
    internal uint ndist;            // number of distance code lengths
    internal uint have;             // number of code lengths in lens[]
    internal uint next;             // next available space in codes[]
    internal uint diststart;        // starting index in codes[] for distance codes
    internal readonly ushort[] lens = new ushort[320]; // temporary storage for code lengths
    internal readonly ushort[] work = new ushort[288]; // work area for code table building
    internal readonly Code[] codes = new Code[Enough]; // space for code tables
    internal int sane;              // if false, allow invalid distance too far
    internal int back;              // bits back of last unprocessed length/lit
    internal uint was;              // initial length of match
}