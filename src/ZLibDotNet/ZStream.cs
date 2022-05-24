// Copyright (C) 2022 Magnus Montin

using System;

namespace ZLibDotNet;

/// <summary>
/// Represents a stream of data that can be compressed and uncompressed using the zlib data format.
/// </summary>
#pragma warning disable CA1711
public sealed class ZStream
#pragma warning restore CA1711
{
    internal readonly Unsafe.ZStream strm = new();
    internal int inputOffset;   // the index of next input byte in the input buffer
    internal int outputOffset;  // the index of next output byte in the output buffer
    private byte[] _input;
    private byte[] _output;

    /// <summary>
    /// Gets or sets the input buffer.
    /// </summary>
    /// <remarks>Setting the <see cref="Input"/> property resets the <see cref="AvailableIn"/> and <see cref="NextIn"/> properties to their default values.</remarks>
#pragma warning disable CA1819
    public byte[] Input
#pragma warning restore CA1819
    {
        get => _input;
        set
        {
            _input = value;
            inputOffset = default;
            AvailableIn = value?.Length ?? default;
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
        get => (int)strm.AvailableIn;
        set
        {
            ValidateAvailableBytes(value, inputOffset, _input, nameof(Input), nameof(NextIn));
            strm.AvailableIn = (uint)value;
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
        get => inputOffset;
        set
        {
            ValidateOffset(value, AvailableIn, _input, nameof(Input));
            inputOffset = value;
        }
    }

    /// <summary>
    /// Gets the total number of input bytes read so far.
    /// </summary>
    public int TotalIn => (int)strm.TotalIn;

    /// <summary>
    /// Gets or sets the output buffer.
    /// </summary>
    /// <remarks>Setting the <see cref="Output"/> property resets the <see cref="AvailableOut"/> and <see cref="NextOut"/> properties to their default values.</remarks>
#pragma warning disable CA1819
    public byte[] Output
#pragma warning restore CA1819
    {
        get => _output;
        set
        {
            _output = value;
            outputOffset = default;
            AvailableOut = value?.Length ?? default;
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
        get => (int)strm.AvailableOut;
        set
        {
            ValidateAvailableBytes(value, outputOffset, _output, nameof(Output), nameof(NextOut));
            strm.AvailableOut = (uint)value;
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
        get => outputOffset;
        set
        {
            ValidateOffset(value, AvailableOut, _output, nameof(Output));
            outputOffset = value;
        }
    }

    /// <summary>
    /// Gets the total number of bytes output so far.
    /// </summary>
    public int TotalOut => (int)strm.TotalOut;

    /// <summary>
    /// Gets the last error message, or <see langword="null"/> if no error.
    /// </summary>
    public string Message => strm.Message;

    /// <summary>
    /// Gets a value that represents a best guess about the data type: binary or text for deflate, or the decoding state for inflate.
    /// </summary>
    public int DataType => strm.data_type;

    /// <summary>
    /// Gets the Adler-32 value of the uncompressed data.
    /// </summary>
    public uint Adler => strm.Adler;

    private static void ValidateAvailableBytes(int value, int offset, byte[] buffer, string bufferName, string offsetPropertyName)
    {
        if (value < 0 || value > (buffer?.Length ?? 0) - offset)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Value was out of range. Must be non-negative and less than or equal to the size of the {bufferName} buffer minus the value of the {offsetPropertyName} property.");
    }

    private static void ValidateOffset(int value, int availableBytes, byte[] buffer, string bufferName)
    {
        int bufferLength = buffer?.Length ?? 0;
        if (value < 0 || value >= bufferLength)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Value was out of range. Must be non-negative and less than the size of the {bufferName} buffer.");
        if (bufferLength - value < availableBytes)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"The value must refer to a location within the available bytes of the {bufferName} buffer.");
    }
}