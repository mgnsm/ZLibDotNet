// Original code and comments Copyright (C) 1995-2024 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

namespace ZLibDotNet.Deflate;

internal sealed class TreeDescriptor
{
    internal readonly TreeNode[] dyn_tree;  // the dynamic tree
    internal readonly StaticTree stat_desc; // the corresponding static tree
    internal int max_code;                  // largest code with non zero frequency

    internal TreeDescriptor(TreeNode[] dyn_tree, StaticTree stat_desc)
    {
        this.dyn_tree = dyn_tree;
        this.stat_desc = stat_desc;
    }
}