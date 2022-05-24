// Original code Copyright (C) 1995-2005, 2010 Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Inflate;

/// <summary>
/// Type of code to build for <see cref="Inflater.InflateTable(CodeType, ushort*, uint, ref Code*, ref uint, ushort*)"/>.
/// </summary>
internal enum CodeType : byte
{
    Codes,
    Lens,
    Dists
}