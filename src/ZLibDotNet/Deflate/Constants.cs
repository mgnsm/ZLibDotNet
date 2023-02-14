// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly, Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

namespace ZLibDotNet.Deflate;

internal static class Constants
{
    internal const byte LenghtCodes = 29;               // number of length codes, not counting the special EndBlock code
    internal const ushort Literals = 256;               // number of literal bytes 0..255
    internal const ushort LCodes = Literals + 1 + LenghtCodes;
    internal const byte DCodes = 30;                    // number of distance codes
    internal const byte BlCodes = 19;                   // number of codes used to transfer the bit lengths
    internal const ushort HeapSize = 2 * LCodes + 1;    // maximum heap size
    internal const byte MaxBits = 15;                   // All codes must not exceed MaxBits bits
    internal const byte BufSize = 16;                   // size of bit buffer in bi_buf
    internal const byte MinMatch = 3;                   // The minimum match length
    internal const ushort MaxMatch = 258;               // The maximum match length
}