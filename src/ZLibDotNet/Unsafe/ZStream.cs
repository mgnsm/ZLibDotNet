// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

using ZLibDotNet.Deflate;
using ZLibDotNet.Inflate;

namespace ZLibDotNet.Unsafe;

/// <summary>
/// Represents a stream of data that can be compressed and uncompressed using the zlib data format.
/// </summary>
#pragma warning disable CA1711
public sealed unsafe class ZStream
#pragma warning restore CA1711
{
    internal byte* next_in;     // next input byte
    internal uint avail_in;     // number of bytes available at next_in
    internal uint total_in;     // total number of input bytes read so far

    internal byte* next_out;    // next output byte will go here
    internal uint avail_out;    // remaining free space at next_out
    internal uint total_out;    // total number of bytes output so far

    internal string msg;        // last error message
    internal InflateState inflateState;
    internal DeflateState deflateState;

    internal int data_type;     // best guess about the data type: binary or text for deflate, or the decoding state for inflate

    /// <summary>
    /// Gets or sets the next input byte.
    /// </summary>
    public byte* NextIn
    {
        get => next_in;
        set => next_in = value;
    }

    /// <summary>
    /// Gets or sets number of bytes available at <see cref="NextIn"/>.
    /// </summary>
    public uint AvailableIn
    {
        get => avail_in;
        set => avail_in = value;
    }

    /// <summary>
    /// Gets the total number of input bytes read so far.
    /// </summary>
    public uint TotalIn => total_in;

    /// <summary>
    /// Gets or sets where the next output byte will go.
    /// </summary>
    public byte* NextOut
    {
        get => next_out;
        set => next_out = value;
    }

    /// <summary>
    /// Gets or sets the remaining free space at <see cref="NextOut"/>.
    /// </summary>
    public uint AvailableOut
    {
        get => avail_out;
        set => avail_out = value;
    }

    /// <summary>
    /// Gets the total number of bytes output so far.
    /// </summary>
    public uint TotalOut => total_out;

    /// <summary>
    /// Gets the last error message, or <see langword="null"/> if no error.
    /// </summary>
    public string Message => msg;

    /// <summary>
    /// Gets a value that represents a best guess about the data type: binary or text for deflate, or the decoding state for inflate.
    /// </summary>
    public int DataType => data_type;

    /// <summary>
    /// Gets the Adler-32 value of the uncompressed data.
    /// </summary>
    public uint Adler { get; internal set; }
}