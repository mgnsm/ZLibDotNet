// Original code and comments Copyright (C) 1995-2021 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Deflate;

internal class StaticTree
{
    internal readonly TreeNode[] static_tree; // static tree or null
    internal readonly int[] extra_bits;       // extra bits for each code or null
    internal readonly int extra_base;         // base index for extra_bits
    internal readonly int elems;              // max number of elements in the tree
    internal readonly int max_length;         // max bit length for the codes

    public StaticTree(TreeNode[] static_tree, int[] extra_bits, int extra_base, int elems, int max_length)
    {
        this.static_tree = static_tree;
        this.extra_bits = extra_bits;
        this.extra_base = extra_base;
        this.elems = elems;
        this.max_length = max_length;
    }
}