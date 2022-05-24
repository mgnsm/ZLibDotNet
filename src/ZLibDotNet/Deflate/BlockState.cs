// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Deflate;

internal enum BlockState : byte
{
    NeedMore,       // block not completed, need more input or more output
    BlockDone,      // block flush performed
    FinishStarted,  // finish started, need only more output at next deflate
    FinishDone      // finish done, accept no more input or output
}