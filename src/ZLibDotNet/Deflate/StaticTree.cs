// Original code and comments Copyright (C) 1995-2021 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022-2023 Magnus Montin

namespace ZLibDotNet.Deflate;

internal sealed class StaticTree
{
    internal readonly TreeNode[] static_tree; // static tree or null
    internal readonly int[] extra_bits;       // extra bits for each code or null
    internal readonly uint extra_base;         // base index for extra_bits
    internal readonly uint elems;              // max number of elements in the tree
    internal readonly uint max_length;         // max bit length for the codes

    public StaticTree(TreeNode[] static_tree, int[] extra_bits, uint extra_base, uint elems, uint max_length)
    {
        this.static_tree = static_tree;
        this.extra_bits = extra_bits;
        this.extra_base = extra_base;
        this.elems = elems;
        this.max_length = max_length;
    }
}