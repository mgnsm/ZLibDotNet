// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet;

public partial class ZLib
{
#pragma warning disable CS1591
#pragma warning disable CA1707
    // Allowed flush values.
    public const int Z_NO_FLUSH = 0;
    public const int Z_PARTIAL_FLUSH = 1;
    public const int Z_SYNC_FLUSH = 2;
    public const int Z_FULL_FLUSH = 3;
    public const int Z_FINISH = 4;
    public const int Z_BLOCK = 5;
    public const int Z_TREES = 6;

    // Return codes for the compression/decompression methods. Negative values are errors, positive values are used for special but normal events.
    public const int Z_OK = 0;
    public const int Z_STREAM_END = 1;
    public const int Z_NEED_DICT = 2;
    public const int Z_ERRNO = -1;
    public const int Z_STREAM_ERROR = -2;
    public const int Z_DATA_ERROR = -3;
    public const int Z_MEM_ERROR = -4;
    public const int Z_BUF_ERROR = -5;
    public const int Z_VERSION_ERROR = -6;

    // Compression levels.
    public const int Z_NO_COMPRESSION = 0;
    public const int Z_BEST_SPEED = 1;
    public const int Z_BEST_COMPRESSION = 9;
    public const int Z_DEFAULT_COMPRESSION = -1;

    // Compression strategies.
    public const int Z_FILTERED = 1;
    public const int Z_HUFFMAN_ONLY = 2;
    public const int Z_RLE = 3;
    public const int Z_FIXED = 4;
    public const int Z_DEFAULT_STRATEGY = 0;

    // Possible values of the data_type field for deflate().
    public const int Z_BINARY = 0;
    public const int Z_TEXT = 1;
    public const int Z_ASCII = Z_TEXT;
    public const int Z_UNKNOWN = 2;

    // The only supported deflate compression method.
    public const int Z_DEFLATED = 8;
#pragma warning restore CA1707
#pragma warning restore CS1591
}