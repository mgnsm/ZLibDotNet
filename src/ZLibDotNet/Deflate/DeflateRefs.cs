using System.Runtime.InteropServices;

namespace ZLibDotNet.Deflate;

internal ref struct DeflateRefs
{
    public DeflateRefs() =>
        configuration_table = ref MemoryMarshal.GetReference<Config>(Deflater.s_configuration_table);

    internal ref byte pending_buf;
    internal ref byte pending_out;

    internal ref byte window;
    internal ref ushort prev;
    internal ref ushort head;

    internal ref TreeNode dyn_ltree;
    internal ref TreeNode dyn_dtree;
    internal ref TreeNode bl_tree;

    internal ref ushort bl_count;
    internal ref int heap;
    internal ref byte depth;

    internal ref TreeNode sta_ltree;
    internal ref TreeNode sta_dtree;

    internal ref ushort bl_order;
    internal ref byte dist_code;
    internal ref byte length_code;
    internal ref int base_dist;
    internal ref int base_length;
    internal ref int extra_dbits;
    internal ref int extra_lbits;
    internal ref int extra_blbits;
    internal readonly ref Config configuration_table;
}