// Original code and comments Copyright (C) 1995-2019 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Inflate;

/// <summary>
/// Possible inflate modes between <see cref="Inflater.Inflate(ref ZStream, int)"/> calls.
/// </summary>
internal enum InflateMode : ushort
{
    Head = 16180,   // i: waiting for magic header 
    Flags,          // i: waiting for method and flags (gzip) 
    Time,           // i: waiting for modification time (gzip) 
    Os,             // i: waiting for extra flags and operating system (gzip) 
    ExLen,          // i: waiting for extra length (gzip) 
    Extra,          // i: waiting for extra bytes (gzip) 
    Name,           // i: waiting for end of file name (gzip) 
    Comment,        // i: waiting for end of comment (gzip) 
    Hcrc,           // i: waiting for header crc (gzip) 
    DictId,         // i: waiting for dictionary check value 
    Dict,           // waiting for inflateSetDictionary() call 
    Type,           // i: waiting for type bits, including last-flag bit 
    Typedo,         // i: same, but skip check to exit inflate on new block 
    Stored,         // i: waiting for stored size (length and complement) 
    Copy_,          // i/o: same as COPY below, but only first time in 
    Copy,           // i/o: waiting for input or output to copy stored block 
    Table,          // i: waiting for dynamic block table lengths 
    LenLens,        // i: waiting for code length code lengths 
    CodeLens,       // i: waiting for length/lit and distance code lengths 
    Len_,           // i: same as LEN below, but only first time in 
    Len,            // i: waiting for length/lit/eob code 
    LenExt,         // i: waiting for length extra bits 
    Dist,           // i: waiting for distance code 
    DistExt,        // i: waiting for distance extra bits 
    Match,          // o: waiting for output space to copy string 
    Lit,            // o: waiting for output space to write literal 
    Check,          // i: waiting for 32-bit check value 
    Length,         // i: waiting for 32-bit length (gzip) 
    Done,           // finished check, done -- remain here until reset 
    Bad,            // got a data error -- remain here until reset 
    Mem,            // got an inflate() memory error -- remain here until reset 
    Sync            // looking for synchronization bytes to restart inflate() 
}