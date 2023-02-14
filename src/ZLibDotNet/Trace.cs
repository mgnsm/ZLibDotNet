// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

using System.Diagnostics;

namespace ZLibDotNet;

internal static class Trace
{
    internal static byte Level { get; set; }

    [Conditional("DEBUG")]
    public static void Tracev(string message)
    {
        if (Level > 0)
            Debug.Write(message);
    }

    [Conditional("DEBUG")]
    public static void Tracevv(string message)
    {
        if (Level > 1)
            Debug.Write(message);
    }

    [Conditional("DEBUG")]
    public static void Tracecv(bool condition, string message)
    {
        if (Level > 1 && condition)
            Debug.Write(message);
    }
}