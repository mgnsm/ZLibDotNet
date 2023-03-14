namespace ZLibDotNet.Inflate;

internal ref struct InflateRefs
{
    internal ref Code codes;
    internal ref ushort lens;
    internal ref ushort work;
    internal ref byte window;
    internal ref Code lencode;
    internal ref Code distcode;
    internal ref ushort order;
    internal ref ushort lbase;
    internal ref ushort lext;
    internal ref ushort dbase;
    internal ref ushort dext;
}