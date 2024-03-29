﻿// Original code and comments Copyright (C) 1995-2024 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

namespace ZLibDotNet.Deflate;

internal sealed class StaticTree
{
    internal readonly TreeNode[] static_tree; // static tree or null
    internal readonly uint extra_base;        // base index for extra_bits
    internal readonly uint elems;             // max number of elements in the tree
    internal readonly uint max_length;        // max bit length for the codes

    public StaticTree(TreeNode[] static_tree, uint extra_base, uint elems, uint max_length)
    {
        this.static_tree = static_tree;
        this.extra_base = extra_base;
        this.elems = elems;
        this.max_length = max_length;
    }
}