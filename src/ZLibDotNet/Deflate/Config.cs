// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Deflate;

internal readonly struct Config
{
    internal readonly ushort good_length;   // reduce lazy search above this match length
    internal readonly ushort max_lazy;      // do not perform lazy search above this match length
    internal readonly ushort nice_length;   // quit search above this match length
    internal readonly ushort max_chain;
    internal readonly DeflateType deflate_type;

    internal Config(ushort good_length, ushort max_lazy, ushort nice_length, ushort max_chain, DeflateType deflate_type)
    {
        this.good_length = good_length;
        this.max_lazy = max_lazy;
        this.nice_length = nice_length;
        this.max_chain = max_chain;
        this.deflate_type = deflate_type;
    }

    internal enum DeflateType : byte
    {
        Stored,
        Fast,
        Slow
    }
}