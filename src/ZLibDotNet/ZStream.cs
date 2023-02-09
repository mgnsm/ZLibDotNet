// Copyright (C) 2022 Magnus Montin

using System;
using ZLibDotNet.Deflate;
using ZLibDotNet.Inflate;

namespace ZLibDotNet;

/// <summary>
/// Represents a stream of data that can be compressed and uncompressed using the zlib data format.
/// </summary>
#pragma warning disable CA1711
public ref struct ZStream
#pragma warning restore CA1711
{
    internal uint next_in;      // the index of next input byte in the input buffer
    internal uint avail_in;     // number of bytes available at next_in
    internal uint total_in;     // total number of input bytes read so far

    internal uint next_out;     // the index of next output byte in the output buffer
    internal uint avail_out;    // remaining free space at next_out
    internal uint total_out;    // total number of bytes output so far

    internal string msg;        // last error message

    internal InflateState inflateState;
    internal DeflateState deflateState;

    internal int data_type;     // best guess about the data type: binary or text for deflate, or the decoding state for inflate

    internal ReadOnlySpan<byte> _input;
    internal Span<byte> _output;

    /// <summary>
    /// Gets or sets the input buffer.
    /// </summary>
    /// <remarks>Setting the <see cref="Input"/> property resets the <see cref="AvailableIn"/> and <see cref="NextIn"/> properties to their default values.</remarks>
    public ReadOnlySpan<byte> Input
    {
        get => _input;
        set
        {
            _input = value;
            next_in = default;
            avail_in = (uint)value.Length;
        }
    }

    /// <summary>
    /// Gets or sets number of bytes available in <see cref="Input"/>, starting from an offset specified by the <see cref="NextIn"/> property.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="AvailableIn"/> is set to a negative value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="AvailableIn"/> is set to a value that is greater than the length of the <see cref="Input"/> buffer minus the value of the <see cref="NextIn"/> property.</exception>
    /// <remarks>If you choose to set this optional property, you should set it after you have set the <see cref="Input"/> property.</remarks>
    public int AvailableIn
    {
        get => (int)avail_in;
        set
        {
            ValidateAvailableBytes(value, next_in, _input, nameof(Input), nameof(NextIn));
            avail_in = (uint)value;
        }
    }

    /// <summary>
    /// Gets or sets the index of the next input byte in <see cref="Input"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="NextIn"/> is set to a negative value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="NextIn"/> is set to a value that is equal to or greater than the size of the <see cref="Input"/> buffer.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="NextIn"/> is set to a value that is not within the range of available bytes in the <see cref="Input"/> buffer.</exception>
    /// <remarks>If you choose to set this optional property, you should set it after you have set the <see cref="Input"/> property.</remarks>
    public int NextIn
    {
        get => (int)next_in;
        set
        {
            ValidateOffset(value, AvailableIn, _input, nameof(Input));
            next_in = (uint)value;
        }
    }

    /// <summary>
    /// Gets the total number of input bytes read so far.
    /// </summary>
    public int TotalIn => (int)total_in;

    /// <summary>
    /// Gets or sets the output buffer.
    /// </summary>
    /// <remarks>Setting the <see cref="Output"/> property resets the <see cref="AvailableOut"/> and <see cref="NextOut"/> properties to their default values.</remarks>
#pragma warning disable CA1819
    public Span<byte> Output
#pragma warning restore CA1819
    {
        get => _output;
        set
        {
            _output = value;
            next_out = default;
            avail_out = (uint)value.Length;
        }
    }

    /// <summary>
    /// Gets or sets the remaining free space in <see cref="Output"/>, starting from an offset specified by the <see cref="NextOut"/> property.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="AvailableOut"/> is set to a negative value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="AvailableOut"/> is set to a value that is greater than the length of the <see cref="Output"/> buffer minus the value of the <see cref="NextOut"/> property.</exception>
    /// <remarks>If you choose to set this optional property, you should set it after you have set the <see cref="Output"/> property.</remarks>
    public int AvailableOut
    {
        get => (int)avail_out;
        set
        {
            ValidateAvailableBytes(value, next_out, _output, nameof(Output), nameof(NextOut));
            avail_out = (uint)value;
        }
    }

    /// <summary>
    /// Gets or sets the index of the next output byte in <see cref="Output"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="NextOut"/> is set to a negative value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="NextOut"/> is set to a value that is equal to or greater than the size of the <see cref="Output"/> buffer.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="NextOut"/> is set to a value that is not within the range of available bytes in the <see cref="Output"/> buffer.</exception>
    /// <remarks>If you choose to set this optional property, you should set it after you have set the <see cref="Output"/> property.</remarks>
    public int NextOut
    {
        get => (int)next_out;
        set
        {
            ValidateOffset(value, AvailableOut, _output, nameof(Output));
            next_out = (uint)value;
        }
    }

    /// <summary>
    /// Gets the total number of bytes output so far.
    /// </summary>
    public int TotalOut => (int)total_out;

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

    private static void ValidateAvailableBytes(int value, uint offset, ReadOnlySpan<byte> buffer, string bufferName, string offsetPropertyName)
    {
        if (value < 0 || value > buffer.Length - offset)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Value was out of range. Must be non-negative and less than or equal to the size of the {bufferName} buffer minus the value of the {offsetPropertyName} property.");
    }

    private static void ValidateOffset(int value, int availableBytes, ReadOnlySpan<byte> buffer, string bufferName)
    {
        if (value < 0 || value >= buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Value was out of range. Must be non-negative and less than the size of the {bufferName} buffer.");
        if (buffer.Length - value < availableBytes)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"The value must refer to a location within the available bytes of the {bufferName} buffer.");
    }
}