// Original code and comments Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
// Managed C#/.NET code Copyright (C) 2022 Magnus Montin

namespace ZLibDotNet.Unsafe;

/// <summary>
/// Provides in-memory compression and decompression methods, including integrity checks of the uncompressed data. 
/// <para>The compressed data format used is the zlib format, which is a zlib wrapper documented in RFC 1950, wrapped around a deflate stream, which is itself documented in RFC 1951.</para>
/// </summary>
public interface IZLib
{
    /// <summary>
    /// Initializes the internal stream state for compression.
    /// </summary>
    /// <param name="strm">The stream to be initialized for compression.</param>
    /// <param name="level">The compression level. It must be <see cref="Z_DEFAULT_COMPRESSION" />, or between 0 and 9: 1 gives best speed, 9 gives best compression, 0 gives no compression at all (the input data is simply copied a block at a time). <see cref="Z_DEFAULT_COMPRESSION" /> requests a default compromise between speed and compression (equivalent to level 6).</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, or <see cref="Z_STREAM_ERROR"/> if level is not a valid compression level.</returns>
    /// <remarks>This method does not perform any compression. Actual compression will be done by <see cref="Deflate(ZStream, int)"/>.</remarks>
    int DeflateInit(ZStream strm, int level);

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
    int DeflateInit(ZStream strm, int level, int method, int windowBits, int memLevel, int strategy);

    /// <summary>
    /// Compresses as much data as possible, and stops when the input buffer becomes empty or the output buffer becomes full. It may introduce some output latency(reading input without producing any output) except when forced to flush.
    /// <para>The detailed semantics are as follows. The method performs one or both of the following actions:</para>
    /// <list type="bullet"><item><description>Compress more input starting at <see cref="ZStream.NextIn"/> and update <see cref="ZStream.NextIn"/> and <see cref="ZStream.AvailableIn"/> of <paramref name="strm"/> accordingly. If not all input can be processed (because there is not enough room in the output buffer), the <see cref="ZStream.NextIn"/> and <see cref="ZStream.AvailableIn"/> properties of <paramref name="strm"/> are updated and processing will resume at this point for the next call of the same method.</description></item>
    /// <item><description>Generate more output starting at <see cref="ZStream.NextOut"/> and update the <see cref="ZStream.NextOut"/> and <see cref="ZStream.AvailableOut"/> of <paramref name="strm"/> accordingly. This action is forced if the parameter <paramref name="flush" /> is non zero. Forcing <paramref name="flush" /> frequently degrades the compression ratio, so this parameter should be set only when necessary. Some output may be provided even if <paramref name="flush" /> is zero.</description></item></list>
    /// <para>Before calling this method, the application should ensure that at least one of the actions is possible, by providing more input and/or consuming more output, and updating Next* and Available* properties accordingly; <see cref="ZStream.AvailableOut"/> should never be zero before the call.  The application can consume the compressed output when it wants, for example when the output buffer is full (<see cref="ZStream.AvailableOut"/> == 0), or after each call of the method. If the method returns <see cref="Z_OK" /> and the <see cref="ZStream.AvailableOut"/> property of <paramref name="strm" /> is zero, it must be called again after making room in the output buffer because there might be more output pending.</para>
    /// </summary>
    /// <param name="strm">The stream to be compressed.</param>
    /// <param name="flush">The flush parameter. Should be set to <see cref="Z_NO_FLUSH" />, <see cref="Z_SYNC_FLUSH" />, <see cref="Z_PARTIAL_FLUSH" />, <see cref="Z_BLOCK" />, <see cref="Z_FULL_FLUSH" />, or <see cref="Z_FINISH" />.</param>
    /// <returns><see cref="Z_OK"/> if some progress has been made (more input processed or more output produced), <see cref="Z_STREAM_END" /> if all input has been consumed and all output has been produced (only when <param ref="flush" /> is set to <see cref="Z_FINISH" />), <see cref="Z_STREAM_ERROR" /> if the stream state was inconsistent (for example if <see cref="ZStream.NextIn"/> or <see cref="ZStream.NextOut"/> of <paramref name="strm"/> was <see langword="null" /> or the state was inadvertently written over by the application), or <see cref="Z_BUF_ERROR" /> if no progress is possible (for example <see cref="ZStream.AvailableIn"/> or <see cref="ZStream.AvailableOut"/> of <paramref name="strm"/> was zero).</returns>
    int Deflate(ZStream strm, int flush);

    /// <summary>
    /// Resets the state of a stream. Any dynamically allocated resources for the stream are freed.
    /// </summary>
    /// <param name="strm">The stream to be reset.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_STREAM_ERROR"/> if the stream state was inconsistent, or <see cref="Z_DATA_ERROR"/> if some input or output was discarded.</returns>
    /// <remarks>This method discards any unprocessed input and does not flush any pending output.</remarks>
    int DeflateEnd(ZStream strm);

    /// <summary>
    /// Initializes the internal stream state for decompression.
    /// </summary>
    /// <param name="strm">The stream to be initialized for decompression.</param>
    /// <returns><see cref="Z_OK"/> on success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, or <see cref="Z_STREAM_ERROR"/> if <paramref name="strm"/> is <see langword="null"/>.</returns>
    /// <remarks>This method does not perform any decompression. Actual decompression will be done by <see cref="Inflate(ZStream, int)"/>.</remarks>
    int InflateInit(ZStream strm);

    /// <summary>
    /// Initializes the internal stream state for decompression.
    /// </summary>
    /// <param name="strm">The stream to be initialized for decompression.</param>
    /// <param name="windowBits">The base two logarithm of the window size (the size of the history buffer). It should be in the range 8..15 or -8..-15 for raw deflate. Larger values of this parameter result in better compression at the expense of memory usage. The default value is 15 if the <see cref="InflateInit(ZStream)"/> overload is used instead.</param>
    /// <returns><see cref="Z_OK"/> on success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, or <see cref="Z_STREAM_ERROR"/> if the parameters are invalid, such as <paramref name="strm"/> being <see langword="null"/>.</returns>
    /// <remarks>This method does not perform any decompression apart from possibly reading the zlib header if present: actual decompression will be done by <see cref="Inflate(ZStream, int)"/>.</remarks>
    int InflateInit(ZStream strm, int windowBits);

    /// <summary>
    /// Decompresses as much data as possible, and stops when the input buffer becomes empty or the output buffer becomes full. It may introduce some output latency (reading input without producing any output) except when forced to flush.
    /// <para>The detailed semantics are as follows. The method performs one or both of the following actions:</para>
    /// <list type="bullet"><item><description>Decompress more input starting at <see cref="ZStream.NextIn"/> and update <see cref="ZStream.NextIn"/> and <see cref="ZStream.AvailableIn"/> of <paramref name="strm"/> accordingly. If not all input can be processed (because there is not enough room in the output buffer), the <see cref="ZStream.NextIn"/> and <see cref="ZStream.AvailableIn"/> properties of <paramref name="strm"/> are updated accordingly, and processing will resume at this point for the next call of the same method.</description></item>
    /// <item><description>Generate more output starting at <see cref="ZStream.NextOut"/> and update the <see cref="ZStream.NextOut"/> and <see cref="ZStream.AvailableOut"/> properties of <paramref name="strm"/> accordingly. The method provides as much output as possible, until there is no more input data or no more space in the output buffer (see below about the <paramref name="flush"/> parameter).</description></item></list>
    /// <para>Before calling this method, the application should ensure that at least one of the actions is possible, by providing more input and/or consuming more output, and updating the Next* and Available* properties accordingly. If the caller of the method does not provide both available input and available output space, it is possible that there will be no progress made. The application can consume the uncompressed output when it wants, for example when the output buffer is full (the <see cref="ZStream.AvailableOut"/> property of <paramref name="strm"/> then returns 0), or after each call of this method. If the method returns <see cref="Z_OK"/> and the <see cref="ZStream.AvailableOut"/> property of <paramref name="strm"/> returns 0, it must be called again after making room in the output buffer because there might be more output pending.</para>
    /// <para>A <paramref name="flush"/> parameter value of <see cref="Z_SYNC_FLUSH" /> requests the method to flush as much output as possible to the output buffer. <see cref="Z_BLOCK" /> requests the method to stop if and when it gets to the next deflate block boundary. This will cause it to return immediately after the header and before the first block. The <see cref="Z_TREES" /> option behaves as <see cref="Z_BLOCK" /> does, but it also returns when the end of each deflate block header is reached, before any actual data in that block is decoded.</para>
    /// <para>The method should normally be called until it returns <see cref="Z_STREAM_END" /> or an error. However if all decompression is to be performed in a single step (a single call of the method), the <paramref name="flush"/> parameter should be set to <see cref="Z_FINISH" />. In this case all pending input is processed and all pending output is flushed; the <see cref="ZStream.AvailableOut" /> property of <paramref name="strm"/> must be large enough to hold all of the uncompressed data for the operation to complete (the size of the uncompressed data may have been saved by the compressor for this purpose). The use of <see cref="Z_FINISH" /> is not required to perform an inflation in one step. However it may be used to inform the method that a faster approach can be used for the single method call.</para>
    /// </summary>
    /// <param name="strm">The stream to be decompressed.</param>
    /// <param name="flush">The flush parameter. Should be set to <see cref="Z_NO_FLUSH" />, <see cref="Z_SYNC_FLUSH" />, <see cref="Z_FINISH" />, <see cref="Z_BLOCK" />, or <see cref="Z_TREES" />.</param>
    /// <returns><see cref="Z_OK"/> if some progress has been made (more input processed or more output produced), <see cref="Z_STREAM_END" /> if the end of the compressed data has been reached and all uncompressed output has been produced, <see cref="Z_NEED_DICT" /> if a preset dictionary is needed at this point, <see cref="Z_DATA_ERROR" /> if the input data was corrupted (input stream not conforming to the zlib format or incorrect check value, in which case the <see cref="ZStream.Message"/> property of the <paramref name="strm"/> returns a <see cref="string" /> with a more specific error), <see cref="Z_STREAM_ERROR" /> if the stream structure was inconsistent (for example if <see cref="ZStream.NextIn"/> or <see cref="ZStream.NextOut"/>of <paramref name="strm"/> was <see langword="null" />, or the state was inadvertently written over by the application), <see cref="Z_MEM_ERROR" /> if there was not enough memory, <see cref="Z_BUF_ERROR" /> if no progress was possible or if there was not enough room in the output buffer when <see cref="Z_FINISH" /> is used.</returns>
    int Inflate(ZStream strm, int flush);

    /// <summary>
    /// Discards any unprocessed input and resets the state of a stream.
    /// </summary>
    /// <param name="strm">The stream to be reset.</param>
    /// <returns><see cref="Z_OK"/> on success, or <see cref="Z_STREAM_ERROR"/> if the stream state was inconsistent.</returns>
    /// <remarks>This method does not flush any pending output.</remarks>
    int InflateEnd(ZStream strm);

    /// <summary>
    /// Dynamically updates the compression level and compression strategy of a stream.
    /// </summary>
    /// <param name="strm">The stream to be updated.</param>
    /// <param name="level">The compression level. It must be <see cref="Z_DEFAULT_COMPRESSION" />, or between 0 and 9: 1 gives best speed, 9 gives best compression, 0 gives no compression at all (the input data is simply copied a block at a time). <see cref="Z_DEFAULT_COMPRESSION" /> requests a default compromise between speed and compression (equivalent to level 6).</param>
    /// <param name="strategy">The strategy. Used to tune the compression algorithm. Use the value <see cref="Z_DEFAULT_STRATEGY" /> for normal data, <see cref="Z_FILTERED" /> for data produced by a filter (or predictor), <see cref="Z_HUFFMAN_ONLY" /> to force Huffman encoding only (no string match), or <see cref="Z_RLE" /> to limit match distances to one (run-length encoding).</param>
    /// <returns><see cref="Z_OK"/> on success, <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent or if a parameter was invalid, or <see cref="Z_BUF_ERROR"/> if there was not enough output space to complete the compression of the available input data before a change in the strategy or approach.</returns>
    int DeflateParams(ZStream strm, int level, int strategy);

    /// <summary>
    /// Skips invalid compressed data until a possible full flush point can be found, or until all available input is skipped. No output is provided.
    /// <para>Searches for a 00 00 FF FF pattern in the compressed data. All full flush points have this pattern, but not all occurrences of this pattern are full flush points.</para>
    /// </summary>
    /// <param name="strm">The compressed data stream.</param>
    /// <returns><see cref="Z_OK"/> if a possible full flush point has been found, <see cref="Z_BUF_ERROR"/> if no more input was provided, <see cref="Z_DATA_ERROR"/> if no flush point has been found, or <see cref="Z_STREAM_ERROR"/> if the stream structure was inconsistent.</returns>
    int InflateSync(ZStream strm);

    /// <summary>
    /// Sets the destination stream as a complete copy of the source stream.
    /// </summary>
    /// <param name="dest">The destination stream.</param>
    /// <param name="source">The source stream.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent.  <see cref="ZStream.Message"/> is left unchanged in both <paramref name="source"/> and <paramref name="dest"/>.</returns>
    int InflateCopy(ZStream dest, ZStream source);

    /// <summary>
    /// Equivalent to <see cref="InflateEnd(ZStream)"/> followed by <see cref="InflateInit(ZStream)"/>, but does not reallocate the internal decompression state. The stream will keep attributes that may have been set by <see cref="InflateInit(ZStream, int)"/>.
    /// </summary>
    /// <param name="strm">A decompression stream to be reset.</param>
    /// <returns><see cref="Z_OK"/> if success, or <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent (such as <paramref name="strm"/> being <see langword="null"/>).</returns>
    int InflateReset(ZStream strm);

    /// <summary>
    /// This method is the same as <see cref="InflateReset(ZStream)"/>, but it also permits changing the wrap and window size requests.
    /// </summary>
    /// <param name="strm">A decompression stream to be reset.</param>
    /// <param name="windowBits">The base two logarithm of the window size (the size of the history buffer). It should be in the range 8..15 or -8..-15 for raw deflate.</param>
    /// <returns><see cref="Z_OK"/> if success, or <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent (such as <paramref name="strm"/> being <see langword="null"/>), or if the <paramref name="windowBits"/> parameter is invalid.</returns>
    int InflateReset(ZStream strm, int windowBits);

    /// <summary>
    /// Inserts bits in the inflate input stream. The intent is that this method is used to start inflating at a bit position in the middle of a byte.
    /// </summary>
    /// <param name="strm">An initialized decompression stream.</param>
    /// <param name="bits">The provided bits to be used before any bytes are used from <see cref="ZStream.NextIn"/> of <paramref name="strm"/>. Must be less than or equal to 16. If <paramref name="bits"/> is negative, then the input stream bit buffer is emptied. Then this method can be called again to put bits in the buffer. This is used  to clear out bits leftover after feeding inflate a block description prior  to feeding inflate codes.</param>
    /// <param name="value">A value whose <paramref name="bits"/> least significant bits will be inserted in the input.</param>
    /// <returns><see cref="Z_OK"/> if success, or <see cref="Z_STREAM_ERROR"/> if the source stream state was inconsistent.</returns>
    /// <remarks>This mehtod should only be used with raw inflate, and should be used before the first <see cref="Inflate(ZStream, int)"/> call after <see cref="InflateInit(ZStream, int)"/> or <see cref="InflateReset(ZStream)"/>().</remarks>
    int InflatePrime(ZStream strm, int bits, int value);

    /// <summary>
    /// Compresses the source buffer into the destination buffer.
    /// </summary>
    /// <param name="dest">A pointer to the destination buffer.</param>
    /// <param name="destLen">The total size of the destination buffer upon entry and the actual size of the compressed data upon exit.</param>
    /// <param name="source">A pointer to the source buffer</param>
    /// <param name="sourceLen">The byte length of the source buffer.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_BUF_ERROR"/> if there was not enough room in the output buffer.</returns>
    /// <remarks><see cref="Compress(byte*, uint*, byte*, uint)"/> is equivalent to <see cref="Compress(byte*, uint*, byte*, uint, int)"/> with a level parameter of <see cref="Z_DEFAULT_COMPRESSION"/>.</remarks>
    unsafe int Compress(byte* dest, uint* destLen, byte* source, uint sourceLen);

    /// <summary>
    /// Compresses the source buffer into the destination buffer.
    /// </summary>
    /// <param name="dest">A pointer to the destination buffer.</param>
    /// <param name="destLen">The total size of the destination buffer upon entry and the actual size of the compressed data upon exit.</param>
    /// <param name="source">A pointer to the source buffer</param>
    /// <param name="sourceLen">The byte length of the source buffer.</param>
    /// <param name="level">The compression level.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_BUF_ERROR"/> if there was not enough room in the output buffer.</returns>
    unsafe int Compress(byte* dest, uint* destLen, byte* source, uint sourceLen, int level);

    /// <summary>
    /// Calculates an upper bound on the compressed size of a destination buffer.
    /// </summary>
    /// <param name="sourceLen">The number of bytes to be compressed.</param>
    /// <returns>An upper bound on the compressed size after compressing <paramref name="sourceLen"/> bytes.</returns>
    /// <remarks>It would be used before a <see cref="Compress(byte*, uint*, byte*, uint)"/> or <see cref="Compress(byte*, uint*, byte*, uint, int)"/> call to allocate the destination buffer.</remarks>
    uint CompressBound(uint sourceLen);

    /// <summary>
    /// Decompresses the source buffer into the destination buffer.
    /// </summary>
    /// <param name="dest">A pointer to the destination buffer.</param>
    /// <param name="destLen">The total size of the destination buffer upon entry and the actual size of the uncompressed data upon exit.</param>
    /// <param name="source">A pointer to the source buffer.</param>
    /// <param name="sourceLen">The byte length of the source buffer.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_BUF_ERROR"/> if there was not enough room in the output buffer, or <see cref="Z_DATA_ERROR"/> if the input data was corrupted or incomplete.</returns>
    /// <remarks>In the case where there is not enough room, the method will fill the destination buffer with the uncompressed data up to that point.</remarks>
    unsafe int Uncompress(byte* dest, uint* destLen, byte* source, uint sourceLen);

    /// <summary>
    /// Decompresses the source buffer into the destination buffer.
    /// </summary>
    /// <param name="dest">A pointer to the destination buffer.</param>
    /// <param name="destLen">The total size of the destination buffer upon entry and the actual size of the uncompressed data upon exit.</param>
    /// <param name="source">A pointer to the source buffer.</param>
    /// <param name="sourceLen">The byte length of the source buffer upon entry and the number of source bytes consumed upon exit.</param>
    /// <returns><see cref="Z_OK"/> if success, <see cref="Z_MEM_ERROR"/> if there was not enough memory, <see cref="Z_BUF_ERROR"/> if there was not enough room in the output buffer, or <see cref="Z_DATA_ERROR"/> if the input data was corrupted or incomplete.</returns>
    /// <remarks>In the case where there is not enough room, the method will fill the destination buffer with the uncompressed data up to that point.</remarks>
    unsafe int Uncompress(byte* dest, uint* destLen, byte* source, uint* sourceLen);
}