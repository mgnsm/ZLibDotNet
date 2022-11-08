// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

global using static ZLibDotNet.ZLib;
using System;
using System.Runtime.InteropServices;
using ZLibDotNet.Deflate;
using ZLibDotNet.Inflate;
using Adler = ZLibDotNet.Adler32;

namespace ZLibDotNet;

#pragma warning disable CA1062
/// <summary>
/// Provides in-memory compression and decompression methods, including integrity checks of the uncompressed data. 
/// <para>The compressed data format used is the zlib format, which is a zlib wrapper documented in RFC 1950, wrapped around a deflate stream, which is itself documented in RFC 1951.</para>
/// </summary>
public partial class ZLib : IZLib
{
    internal const int MaxWindowBits = 15; // Maximum value for windowBits in deflateInit2 and inflateInit2. 32K LZ77 window.
    internal const int DefaultWindowBits = MaxWindowBits; // default windowBits for decompression

    /// <summary>
    /// Initializes the internal stream state for compression.
    /// </summary>
    /// <param name="strm">The stream to be initialized for compression.</param>
    /// <param name="level">The compression level. It must be <see cref="Z_DEFAULT_COMPRESSION" />, or between 0 and 9: 1 gives best speed, 9 gives best compression, 0 gives no compression at all (the input data is simply copied a block at a time). <see cref="Z_DEFAULT_COMPRESSION" /> requests a default compromise between speed and compression (equivalent to level 6).</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, or <see cref="Z_STREAM_ERROR"/> if level is not a valid compression level.</returns>
    /// <remarks>This method does not perform any compression. Actual compression will be done by <see cref="Deflate(ZStream, int)"/>.</remarks>
    public int DeflateInit(ZStream strm, int level) => Deflater.DeflateInit(strm, level);

    /// <summary>
    /// Initializes the internal stream state for compression.
    /// </summary>
    /// <param name="strm">The stream to be initialized for compression.</param>
    /// <param name="level">The compression level. It must be <see cref="Z_DEFAULT_COMPRESSION" />, or between 0 and 9: 1 gives best speed, 9 gives best compression, 0 gives no compression at all (the input data is simply copied a block at a time). <see cref="Z_DEFAULT_COMPRESSION" /> requests a default compromise between speed and compression (equivalent to level 6).</param>
    /// <param name="method">The compression method. It must be <see cref="Z_DEFLATED"/>.</param>
    /// <param name="windowBits">The base two logarithm of the window size (the size of the history buffer). It should be in the range 8..15 or -8..-15 for raw deflate. Larger values of this parameter result in better compression at the expense of memory usage. The default value is 15 if the <see cref="DeflateInit(ZStream, int)"/> overload is used instead.</param>
    /// <param name="memLevel">Specifies how much memory should be allocated for the internal compression state. <paramref name="memLevel"/>=1 uses minimum memory but is slow and reduces compression ratio; <paramref name="memLevel"/>=9 uses maximum memory for optimal speed. The default value is 8.</param>
    /// <param name="strategy">Used to tune the compression algorithm. Use the value <see cref="Z_DEFAULT_STRATEGY"/> for normal data, <see cref="Z_FILTERED"/> for data produced by a filter(or predictor), <see cref="Z_HUFFMAN_ONLY"/> to force Huffman encoding only (no string match), or <see cref="Z_RLE"/> to limit match distances to one (run-length encoding).</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, or <see cref="Z_STREAM_ERROR"/> if any parameter is invalid (such as an invalid method).</returns>
    /// <remarks>This method does not perform any compression. Actual compression will be done by <see cref="Deflate(ZStream, int)"/>.</remarks>
    public int DeflateInit(ZStream strm, int level, int method, int windowBits, int memLevel, int strategy) =>
        Deflater.DeflateInit(strm, level, method, windowBits, memLevel, strategy);

    /// <summary>
    /// Compresses as much data as possible, and stops when the input buffer becomes empty or the output buffer becomes full. It may introduce some output latency(reading input without producing any output) except when forced to flush.
    /// <para>Note that the sizes of the input and output buffers, i.e. the available input and available output space, are determined by the values of the <see cref="ZStream.AvailableIn"/> and <see cref="ZStream.AvailableOut"/> properties of the <paramref name="strm"/> respectively and not by the actual <see cref="Array.Length"/> of the <see cref="ZStream.Input"/> and <see cref="ZStream.Output"/> properties.</para>
    /// <para>The detailed semantics are as follows. The method performs one or both of the following actions:</para>
    /// <list type="bullet"><item><description>Compress more input starting at index <see cref="ZStream.NextIn"/> of <see cref="ZStream.Input"/> and update <see cref="ZStream.NextIn"/> and <see cref="ZStream.AvailableIn"/> of <paramref name="strm"/> accordingly. If not all input can be processed (because there is not enough room in the output buffer), the <see cref="ZStream.NextIn"/> and <see cref="ZStream.AvailableIn"/> properties of <paramref name="strm"/> are updated and processing will resume at this point for the next call of the same method.</description></item>
    /// <item><description>Generate more output starting at index <see cref="ZStream.NextOut"/> of <see cref="ZStream.Output"/> and update the <see cref="ZStream.NextOut"/> and <see cref="ZStream.AvailableOut"/> properties of <paramref name="strm"/> accordingly. This action is forced if the parameter <paramref name="flush" /> is non zero. Forcing <paramref name="flush" /> frequently degrades the compression ratio, so this parameter should be set only when necessary. Some output may be provided even if <paramref name="flush" /> is zero.</description></item></list>
    /// <para>Before calling this method, the application should ensure that at least one of the actions is possible, by providing more input and/or consuming more output, and updating Next* and Available* properties accordingly; <see cref="ZStream.AvailableOut"/> should never be zero before the call.  The application can consume the compressed output when it wants, for example when the output buffer is full (<see cref="ZStream.AvailableOut"/> == 0), or after each call of the method. If the method returns <see cref="Z_OK" /> and the <see cref="ZStream.AvailableOut"/> property of <paramref name="strm" /> is zero, it must be called again after making room in the output buffer because there might be more output pending.</para>
    /// </summary>
    /// <param name="strm">The stream to be compressed.</param>
    /// <param name="flush">The flush parameter. Should be set to <see cref="Z_NO_FLUSH" />, <see cref="Z_SYNC_FLUSH" />, <see cref="Z_PARTIAL_FLUSH" />, <see cref="Z_BLOCK" />, <see cref="Z_FULL_FLUSH" />, or <see cref="Z_FINISH" />.</param>
    /// <returns><see cref="Z_OK"/> if some progress has been made (more input processed or more output produced), <see cref="Z_STREAM_END" /> if all input has been consumed and all output has been produced (only when <param ref="flush" /> is set to <see cref="Z_FINISH" />), <see cref="Z_STREAM_ERROR" /> if the stream state was inconsistent (for example if <see cref="ZStream.NextIn"/> or <see cref="ZStream.NextOut"/> of <paramref name="strm"/> was <see langword="null" /> or the state was inadvertently written over by the application), or <see cref="Z_BUF_ERROR" /> if no progress is possible (for example <see cref="ZStream.AvailableIn"/> or <see cref="ZStream.AvailableOut"/> of <paramref name="strm"/> was zero).</returns>
    public int Deflate(ZStream strm, int flush) => Deflater.Deflate(strm, flush);

    /// <summary>
    /// Resets the state of a stream. Any dynamically allocated resources for the stream are freed.
    /// </summary>
    /// <param name="strm">The stream to be reset.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_STREAM_ERROR"/> if the stream state was inconsistent, or <see cref="Z_DATA_ERROR"/> if some input or output was discarded.</returns>
    /// <remarks>This method discards any unprocessed input and does not flush any pending output.</remarks>
    public int DeflateEnd(ZStream strm) => Deflater.DeflateEnd(strm);

    /// <summary>
    /// Initializes the internal stream state for decompression.
    /// </summary>
    /// <param name="strm">The stream to be initialized for decompression.</param>
    /// <returns><see cref="Z_OK"/> on success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, or <see cref="Z_STREAM_ERROR"/> if <paramref name="strm"/> is <see langword="null"/>.</returns>
    /// <remarks>This method does not perform any decompression. Actual decompression will be done by <see cref="Inflate(ZStream, int)"/>.</remarks>
    public int InflateInit(ZStream strm) => Inflater.InflateInit(strm, DefaultWindowBits);

    /// <summary>
    /// Initializes the internal stream state for decompression.
    /// </summary>
    /// <param name="strm">The stream to be initialized for decompression.</param>
    /// <param name="windowBits">The base two logarithm of the window size (the size of the history buffer). It should be in the range 8..15 or -8..-15 for raw deflate. Larger values of this parameter result in better compression at the expense of memory usage. The default value is 15 if the <see cref="InflateInit(ZStream)"/> overload is used instead.</param>
    /// <returns><see cref="Z_OK"/> on success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, or <see cref="Z_STREAM_ERROR"/> if the parameters are invalid, such as <paramref name="strm"/> being <see langword="null"/>.</returns>
    /// <remarks>This method does not perform any decompression apart from possibly reading the zlib header if present: actual decompression will be done by <see cref="Inflate(ZStream, int)"/>.</remarks>
    public int InflateInit(ZStream strm, int windowBits) => Inflater.InflateInit(strm, windowBits);

    /// <summary>
    /// Decompresses as much data as possible, and stops when the input buffer becomes empty or the output buffer becomes full. It may introduce some output latency (reading input without producing any output) except when forced to flush.
    /// <para>Note that the sizes of the input and output buffers, i.e. the available input and available output space, are determined by the values of the <see cref="ZStream.AvailableIn"/> and <see cref="ZStream.AvailableOut"/> properties of the <paramref name="strm"/> respectively and not by the actual <see cref="Array.Length"/> of the <see cref="ZStream.Input"/> and <see cref="ZStream.Output"/> properties.</para>
    /// <para>The detailed semantics are as follows. The method performs one or both of the following actions:</para>
    /// <list type="bullet"><item><description>Decompress more input starting at index <see cref="ZStream.NextIn"/> of <see cref="ZStream.Input"/> and update <see cref="ZStream.NextIn"/> and <see cref="ZStream.AvailableIn"/> of <paramref name="strm"/> accordingly. If not all input can be processed (because there is not enough room in the output buffer), then the <see cref="ZStream.NextIn"/> and <see cref="ZStream.AvailableIn"/> properties of <paramref name="strm"/> are updated accordingly, and processing will resume at this point for the next call of the same method.</description></item>
    /// <item><description>Generate more output starting at index <see cref="ZStream.NextOut"/> of <see cref="ZStream.Output"/> and update the <see cref="ZStream.NextOut"/> and <see cref="ZStream.AvailableOut"/> properties of <paramref name="strm"/> accordingly. The method provides as much output as possible, until there is no more input data or no more space in the output buffer (see below about the <paramref name="flush"/> parameter).</description></item></list>
    /// <para>Before calling this method, the application should ensure that at least one of the actions is possible, by providing more input and/or consuming more output, and updating the Next* and Available* properties accordingly. If the caller of the method does not provide both available input and available output space, it is possible that there will be no progress made. The application can consume the uncompressed output when it wants, for example when the output buffer is full (the <see cref="ZStream.AvailableOut"/> property of <paramref name="strm"/> then returns 0), or after each call of this method. If the method returns <see cref="Z_OK"/> and the <see cref="ZStream.AvailableOut"/> property of <paramref name="strm"/> returns 0, it must be called again after making room in the output buffer because there might be more output pending.</para>
    /// <para>A <paramref name="flush"/> parameter value of <see cref="Z_SYNC_FLUSH" /> requests the method to flush as much output as possible to the output buffer. <see cref="Z_BLOCK" /> requests the method to stop if and when it gets to the next deflate block boundary. This will cause it to return immediately after the header and before the first block. The <see cref="Z_TREES" /> option behaves as <see cref="Z_BLOCK" /> does, but it also returns when the end of each deflate block header is reached, before any actual data in that block is decoded.</para>
    /// <para>The method should normally be called until it returns <see cref="Z_STREAM_END" /> or an error. However if all decompression is to be performed in a single step (a single call of the method), the <paramref name="flush"/> parameter should be set to <see cref="Z_FINISH" />. In this case all pending input is processed and all pending output is flushed; the <see cref="ZStream.AvailableOut" /> property of <paramref name="strm"/> must be large enough to hold all of the uncompressed data for the operation to complete (the size of the uncompressed data may have been saved by the compressor for this purpose). The use of <see cref="Z_FINISH" /> is not required to perform an inflation in one step. However it may be used to inform the method that a faster approach can be used for the single method call.</para>
    /// </summary>
    /// <param name="strm">The stream to be decompressed.</param>
    /// <param name="flush">The flush parameter. Should be set to <see cref="Z_NO_FLUSH" />, <see cref="Z_SYNC_FLUSH" />, <see cref="Z_FINISH" />, <see cref="Z_BLOCK" />, or <see cref="Z_TREES" />.</param>
    /// <returns><see cref="Z_OK"/> if some progress has been made (more input processed or more output produced), <see cref="Z_STREAM_END" /> if the end of the compressed data has been reached and all uncompressed output has been produced, <see cref="Z_NEED_DICT" /> if a preset dictionary is needed at this point, <see cref="Z_DATA_ERROR" /> if the input data was corrupted (input stream not conforming to the zlib format or incorrect check value, in which case the <see cref="ZStream.Message"/> property of the <paramref name="strm"/> returns a <see cref="string" /> with a more specific error), <see cref="Z_STREAM_ERROR" /> if the stream structure was inconsistent (for example if <see cref="ZStream.NextIn"/> or <see cref="ZStream.NextOut"/>of <paramref name="strm"/> was <see langword="null" />, or the state was inadvertently written over by the application), <see cref="Z_MEM_ERROR" /> if there was not enough memory, <see cref="Z_BUF_ERROR" /> if no progress was possible or if there was not enough room in the output buffer when <see cref="Z_FINISH" /> is used.</returns>
    public int Inflate(ZStream strm, int flush) => Inflater.Inflate(strm, flush);

    /// <summary>
    /// Discards any unprocessed input and resets the state of a stream.
    /// </summary>
    /// <param name="strm">The stream to be reset.</param>
    /// <returns><see cref="Z_OK"/> on success, or <see cref="Z_STREAM_ERROR"/> if the stream state was inconsistent.</returns>
    /// <remarks>This method does not flush any pending output.</remarks>
    public int InflateEnd(ZStream strm) => Inflater.InflateEnd(strm);

    /// <summary>
    /// Dynamically updates the compression level and compression strategy of a stream.
    /// </summary>
    /// <param name="strm">The stream to be updated.</param>
    /// <param name="level">The compression level. It must be <see cref="Z_DEFAULT_COMPRESSION" />, or between 0 and 9: 1 gives best speed, 9 gives best compression, 0 gives no compression at all (the input data is simply copied a block at a time). <see cref="Z_DEFAULT_COMPRESSION" /> requests a default compromise between speed and compression (equivalent to level 6).</param>
    /// <param name="strategy">The strategy. Used to tune the compression algorithm. Use the value <see cref="Z_DEFAULT_STRATEGY" /> for normal data, <see cref="Z_FILTERED" /> for data produced by a filter (or predictor), <see cref="Z_HUFFMAN_ONLY" /> to force Huffman encoding only (no string match), or <see cref="Z_RLE" /> to limit match distances to one (run-length encoding).</param>
    /// <returns><see cref="Z_OK"/> on success, <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent or if a parameter was invalid, or <see cref="Z_BUF_ERROR"/> if there was not enough output space to complete the compression of the available input data before a change in the strategy or approach.</returns>
    public int DeflateParams(ZStream strm, int level, int strategy) => Deflater.DeflateParams(strm, level, strategy);

    /// <summary>
    /// Initializes the compression dictionary from the given byte array without producing any compressed output.
    /// </summary>
    /// <param name="strm">An initialized compression stream.</param>
    /// <param name="dictionary">The compression dictionary.</param>
    /// <returns><see cref="Z_OK"/> if success, or <see cref="Z_STREAM_ERROR"/> if a parameter is invalid (e.g. <paramref name="dictionary"/> being <see langword="null"/>) or the stream state is inconsistent (for example if <see cref="Deflate(ZStream, int)"/> has already been called for this stream or if not at a block boundary for raw deflate).</returns>
    /// <remarks>Upon return of this method, the <see cref="ZStream.Adler"/> property of the <paramref name="strm"/> is set to the Adler-32 value of the dictionary; the decompressor may later use this value to determine which dictionary has been used by the compressor.</remarks>
    public int DeflateSetDictionary(ZStream strm, byte[] dictionary) => DeflateSetDictionary(strm, dictionary, dictionary?.Length ?? -1);

    /// <summary>
    /// Initializes the compression dictionary from the given byte array without producing any compressed output.
    /// </summary>
    /// <param name="strm">An initialized compression stream.</param>
    /// <param name="dictionary">The compression dictionary.</param>
    /// <param name="dictLength">The number of bytes available in the compression dictionary <paramref name="dictionary"/>.</param>
    /// <returns><see cref="Z_OK"/> if success, or <see cref="Z_STREAM_ERROR"/> if a parameter is invalid (e.g. <paramref name="dictionary"/> being <see langword="null"/>) or the stream state is inconsistent (for example if <see cref="Deflate(ZStream, int)"/> has already been called for this stream or if not at a block boundary for raw deflate).</returns>
    /// <remarks>Upon return of this method, the <see cref="ZStream.Adler"/> property of the <paramref name="strm"/> is set to the Adler-32 value of the dictionary; the decompressor may later use this value to determine which dictionary has been used by the compressor.</remarks>
    public int DeflateSetDictionary(ZStream strm, byte[] dictionary, int dictLength)
    {
        if (strm == null || dictionary == null || dictLength < 0 || dictLength > dictionary.Length)
            return Z_STREAM_ERROR;

        return Deflater.DeflateSetDictionary(strm, dictionary, dictLength);
    }

    /// <summary>
    /// Initializes the decompression dictionary from the given uncompressed byte sequence.
    /// <para>This method must be called immediately after a call of <see cref="Inflate(ZStream, int)"/>, if that call returned <see cref="Z_NEED_DICT"/>. The dictionary chosen by the compressor can be determined from the Adler-32 value returned by that call of <see cref="Inflate(ZStream, int)"/>. The compressor and decompressor must use exactly the same dictionary (see <see cref="DeflateSetDictionary(ZStream, byte[])"/>).  For raw inflate, this function can be called at any time to set the dictionary. If the provided dictionary is smaller than the window and there is already data in the window, then the provided dictionary will amend what's there. The application must insure that the dictionary that was used for compression is provided.</para>
    /// </summary>
    /// <param name="strm">An initialized decompression stream.</param>
    /// <param name="dictionary">The decompression dictionary.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_STREAM_ERROR"/> if a parameter is invalid (e.g. <paramref name="dictionary"/> being <see langword="null"/>) or the stream state is inconsistent, <see cref="Z_DATA_ERROR"/> if the given dictionary doesn't match the expected one (incorrect Adler-32 value).</returns>
    /// <remarks>This method does not perform any decompression: this will be done by subsequent calls of <see cref="Inflate(ZStream, int)"/>.</remarks>
    public int InflateSetDictionary(ZStream strm, byte[] dictionary) => InflateSetDictionary(strm, dictionary, dictionary?.Length ?? -1);

    /// <summary>
    /// Initializes the decompression dictionary from the given uncompressed byte sequence.
    /// <para>This method must be called immediately after a call of <see cref="Inflate(ZStream, int)"/>, if that call returned <see cref="Z_NEED_DICT"/>. The dictionary chosen by the compressor can be determined from the Adler-32 value returned by that call of <see cref="Inflate(ZStream, int)"/>. The compressor and decompressor must use exactly the same dictionary (see <see cref="DeflateSetDictionary(ZStream, byte[])"/>).  For raw inflate, this function can be called at any time to set the dictionary. If the provided dictionary is smaller than the window and there is already data in the window, then the provided dictionary will amend what's there. The application must insure that the dictionary that was used for compression is provided.</para>
    /// </summary>
    /// <param name="strm">An initialized decompression stream.</param>
    /// <param name="dictionary">The decompression dictionary.</param>
    /// <param name="dictLength">The number of bytes available in the decompression dictionary <paramref name="dictionary"/>.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_STREAM_ERROR"/> if a parameter is invalid (e.g. <paramref name="dictionary"/> being <see langword="null"/>) or the stream state is inconsistent, <see cref="Z_DATA_ERROR"/> if the given dictionary doesn't match the expected one (incorrect Adler-32 value).</returns>
    /// <remarks>This method does not perform any decompression: this will be done by subsequent calls of <see cref="Inflate(ZStream, int)"/>.</remarks>
    public int InflateSetDictionary(ZStream strm, byte[] dictionary, int dictLength)
    {
        if (strm == null || dictionary == null || dictLength < 0 || dictLength > dictionary.Length)
            return Z_STREAM_ERROR;

        ref byte dict = ref MemoryMarshal.GetReference(dictionary.AsSpan());
        return Inflater.InflateSetDictionary(strm, ref dict, (uint)dictLength);
    }

    /// <summary>
    /// Skips invalid compressed data until a possible full flush point can be found, or until all available input is skipped. No output is provided.
    /// <para>Searches for a 00 00 FF FF pattern in the compressed data. All full flush points have this pattern, but not all occurrences of this pattern are full flush points.</para>
    /// </summary>
    /// <param name="strm">The compressed data stream.</param>
    /// <returns><see cref="Z_OK"/> if a possible full flush point has been found, <see cref="Z_BUF_ERROR"/> if no more input was provided, <see cref="Z_DATA_ERROR"/> if no flush point has been found, or <see cref="Z_STREAM_ERROR"/> if the stream structure was inconsistent.</returns>
    public int InflateSync(ZStream strm) => Inflater.InflateSync(strm);

    /// <summary>
    /// Sets the destination stream as a complete copy of the source stream.
    /// </summary>
    /// <param name="source">The source stream.</param>
    /// <param name="dest">The destination stream.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent. <see cref="ZStream.Message"/> is left unchanged in both <paramref name="source"/> and <paramref name="dest"/>.</returns>
    public int InflateCopy(ZStream source, ZStream dest)
    {
        if (source == null || dest == null)
            return Z_STREAM_ERROR;

        dest.Input = source.Input;
        dest.Output = source.Output;

        return Inflater.InflateCopy(dest, source);
    }

    /// <summary>
    /// Equivalent to <see cref="InflateEnd(ZStream)"/> followed by <see cref="InflateInit(ZStream)"/>, but does not reallocate the internal decompression state. The stream will keep attributes that may have been set by <see cref="InflateInit(ZStream, int)"/>.
    /// </summary>
    /// <param name="strm">A decompression stream to be reset.</param>
    /// <returns><see cref="Z_OK"/> if success, or <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent (such as <paramref name="strm"/> being <see langword="null"/>).</returns>
    public int InflateReset(ZStream strm) => Inflater.InflateReset(strm);

    /// <summary>
    /// This method is the same as <see cref="InflateReset(ZStream)"/>, but it also permits changing the wrap and window size requests.
    /// </summary>
    /// <param name="strm">A decompression stream to be reset.</param>
    /// <param name="windowBits">The base two logarithm of the window size (the size of the history buffer). It should be in the range 8..15 or -8..-15 for raw deflate.</param>
    /// <returns><see cref="Z_OK"/> if success, or <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent (such as <paramref name="strm"/> being <see langword="null"/>), or if the <paramref name="windowBits"/> parameter is invalid.</returns>
    public int InflateReset(ZStream strm, int windowBits) => Inflater.InflateReset(strm, windowBits);

    /// <summary>
    /// Inserts bits in the inflate input stream. The intent is that this method is used to start inflating at a bit position in the middle of a byte.
    /// </summary>
    /// <param name="strm">An initialized decompression stream.</param>
    /// <param name="bits">The provided bits to be used before any bytes are used from the byte at index <see cref="ZStream.NextIn"/> of the <see cref="ZStream.Input"/> of <paramref name="strm"/>. Must be less than or equal to 16. If <paramref name="bits"/> is negative, then the input stream bit buffer is emptied. Then this method can be called again to put bits in the buffer. This is used  to clear out bits leftover after feeding inflate a block description prior  to feeding inflate codes.</param>
    /// <param name="value">A value whose <paramref name="bits"/> least significant bits will be inserted in the input.</param>
    /// <returns><see cref="Z_OK"/> if success, or <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent.</returns>
    /// <remarks>This mehtod should only be used with raw inflate, and should be used before the first <see cref="Inflate(ZStream, int)"/> call after <see cref="InflateInit(ZStream, int)"/> or <see cref="InflateReset(ZStream)"/>().</remarks>
    public int InflatePrime(ZStream strm, int bits, int value) => Inflater.InflatePrime(strm, bits, value);

    /// <summary>
    /// Compresses the source buffer into the destination buffer.
    /// </summary>
    /// <param name="dest">The destination buffer.</param>
    /// <param name="destLen">The actual size of the compressed buffer upon exit.</param>
    /// <param name="source">The source buffer.</param>
    /// <param name="sourceLen">The byte length of the source buffer.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_BUF_ERROR"/> if there was not enough room in the output buffer, or Z_STREAM_ERROR if any of the level parameters are invalid.</returns>
    /// <remarks><see cref="Compress(byte[], out int, byte[], int)"/> is equivalent to <see cref="Compress(byte[], out int, byte[], int, int)"/> with a level parameter of <see cref="Z_DEFAULT_COMPRESSION"/>.</remarks>
    public int Compress(byte[] dest, out int destLen, byte[] source, int sourceLen) => Compress(dest, out destLen, source, sourceLen, Z_DEFAULT_COMPRESSION);

    /// <summary>
    /// Compresses the source buffer into the destination buffer.
    /// </summary>
    /// <param name="dest">The destination buffer.</param>
    /// <param name="destLen">The actual size of the compressed buffer upon exit.</param>
    /// <param name="source">The source buffer.</param>
    /// <param name="sourceLen">The byte length of the source buffer.</param>
    /// <param name="level">The compression level.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_BUF_ERROR"/> if there was not enough room in the output buffer, or Z_STREAM_ERROR if any of the level parameters are invalid.</returns>
    public int Compress(byte[] dest, out int destLen, byte[] source, int sourceLen, int level)
    {
        int bytesCompressed = dest?.Length ?? 0;
        int ret = Compressor.Compress(dest, ref bytesCompressed, source, sourceLen, level);
        destLen = bytesCompressed;
        return ret;
    }

    /// <summary>
    /// Calculates an upper bound on the compressed size of a destination buffer.
    /// </summary>
    /// <param name="sourceLen">The number of bytes to be compressed.</param>
    /// <returns>An upper bound on the compressed size after compressing <paramref name="sourceLen"/> bytes.</returns>
    /// <remarks>It would be used before a <see cref="Compress(byte[], out int, byte[], int)"/> or <see cref="Compress(byte[], out int, byte[], int, int)"/> call to allocate the destination buffer.</remarks>
    public uint CompressBound(uint sourceLen) => Compressor.CompressBound(sourceLen);

    /// <summary>
    /// Decompresses the source buffer into the destination buffer.
    /// </summary>
    /// <param name="dest">The destination buffer.</param>
    /// <param name="destLen">The actual size of the uncompressed data upon exit.</param>
    /// <param name="source">The source buffer.</param>
    /// <param name="sourceLen">The number of source bytes to be consumed.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_BUF_ERROR"/> if there was not enough room in the output buffer, or <see cref="Z_DATA_ERROR"/> if the input data was corrupted or incomplete or if <paramref name="sourceLen"/> is negative or greater than the length of <paramref name="source"/>.</returns>
    /// <remarks>In the case where there is not enough room, the method will fill the destination buffer with the uncompressed data up to that point.</remarks>
    public int Uncompress(byte[] dest, out int destLen, byte[] source, int sourceLen)
    {
        int bytesUncompressed = dest?.Length ?? 0;
        if (sourceLen < 0 || sourceLen > bytesUncompressed)
        {
            destLen = default;
            return Z_STREAM_ERROR;
        }

        int ret = Compressor.Uncompress(dest, ref bytesUncompressed, source, ref sourceLen);
        destLen = bytesUncompressed;
        return ret;
    }

    /// <summary>
    /// Decompresses the source buffer into the destination buffer.
    /// </summary>
    /// <param name="dest">The destination buffer.</param>
    /// <param name="destLen">The actual size of the uncompressed data upon exit.</param>
    /// <param name="source">The source buffer.</param>
    /// <param name="sourceLen">The number of source bytes consumed upon exit.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_BUF_ERROR"/> if there was not enough room in the output buffer, or <see cref="Z_DATA_ERROR"/> if the input data was corrupted or incomplete.</returns>
    /// <remarks>In the case where there is not enough room, the method will fill the destination buffer with the uncompressed data up to that point.</remarks>
    public int Uncompress(byte[] dest, out int destLen, byte[] source, out int sourceLen)
    {
        int bytesUncompressed = dest?.Length ?? 0;
        int bytesConsumed = source?.Length ?? 0;
        int ret = Compressor.Uncompress(dest, ref bytesUncompressed, source, ref bytesConsumed);
        destLen = bytesUncompressed;
        sourceLen = bytesConsumed;
        return ret;
    }
    /// <summary>
    /// Updates a running Adler-32 checksum and returns the updated checksum.
    /// </summary>
    /// <param name="adler">The checksum to be updated.</param>
    /// <param name="buf">The bytes of data to be added to the checksum.</param>
    /// <returns>An Adler-32 checksum.</returns>
    public uint Adler32(uint adler, ReadOnlySpan<byte> buf) =>
        Adler.Update(adler, ref MemoryMarshal.GetReference(buf), (uint)buf.Length);
}
#pragma warning restore CA1062