// Original code and comments Copyright (C) 1995-2024 Jean-loup Gailly
// Managed C#/.NET code Copyright (C) 2022-2024 Magnus Montin

namespace ZLibDotNet.Deflate;

internal struct TreeNode
{
    internal ushort fc; // frequency count or bit string
    internal ushort dl; // father node in Huffman tree or length of bit string

    public TreeNode(ushort fc, ushort dl)
    {
        this.fc = fc;
        this.dl = dl;
    }
}