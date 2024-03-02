# ZLibDotNet
![Build Status](https://github.com/mgnsm/ZLibDotNet/actions/workflows/ci.yml/badge.svg)
[![NuGet Badge](https://img.shields.io/nuget/v/ZLibDotNet.svg)](https://www.nuget.org/packages/ZLibDotNet/)

A fully managed, [performant](tests/ZLibDotNet.Benchmarks) and modern C# (.NET Standard 1.3 compatible) implementation of the [zlib compression library](https://www.zlib.net/) which provides in-memory compression, decompression, and integrity checks of uncompressed data in the zlib ([RFC (Request for Comments) 1950](https://datatracker.ietf.org/doc/html/rfc1950)) and raw deflate ([RFC 1951](https://datatracker.ietf.org/doc/html/rfc1951)) data formats (but not the gzip or zip formats).

## zlib support in .NET
If you simply want to compress or decompress data in the zlib format in a .NET application, there is a `ZLibStream` class for doing this in .NET 6 and later versions:
```cs
byte[] inputData = Encoding.ASCII.GetBytes("hello, hello!");

// Compress
using MemoryStream memoryStream = new();
using (ZLibStream deflateStream = new(memoryStream, CompressionMode.Compress, true))
    deflateStream.Write(inputData, 0, inputData.Length);
byte[] compressedData = memoryStream.ToArray();

// Uncompress
byte[] uncompressedData = new byte[inputData.Length];
memoryStream.Seek(0, SeekOrigin.Begin);
using (ZLibStream inflateStream = new(memoryStream, CompressionMode.Decompress))
    inflateStream.Read(uncompressedData, 0, uncompressedData.Length);

Debug.Assert(Enumerable.SequenceEqual(inputData, uncompressedData));
```
In earlier versions of .NET, there are the `DeflateStream` and `InflateStream` classes that can be used to compress and decompress data in the raw deflate (RFC 1951) format.
### Why yet another zlib implementation?
Besides being unsupported in .NET 5 and earlier versions, the `ZLibStream` class also doesn't provide any functionality to retrieve information such as the total number of bytes processed and output, or the Adler-32 checksum of the data. 

There are indeed other third-party and fully managed C#/.NET libraries that already provide this functionality but ZLibDotNet was implemented during the porting of a C/C++ library to C#/.NET where it was undesirable to rely on any existing third-party software. It has been designed to provide an API surface that is very similar to the one that the original ported C library provides and also adds support for `Span<byte>` buffers.
## Installation
ZLibDotNet is preferably installed using NuGet:

    PM> Install-Package ZLibDotNet
## Example
Below is an example of how to compress and uncompress some sample data using the `ZLib` and `ZStream` types in the `ZLibDotNet` namespace:
```cs
using static ZLibDotNet.ZLib;
using ZLibDotNet;

ReadOnlySpan<byte> inputData = Encoding.ASCII.GetBytes("hello, hello!");

// Compress
ZLib zlib = new();
uint sourceLen = zlib.CompressBound((uint)s_inputData.Length);
Span<byte> compressedData = new byte[sourceLen];
ZStream zStream = new()
{
    Input = inputData,
    Output = compressedData
};
_ = zlib.DeflateInit(ref zStream, Z_DEFAULT_COMPRESSION);
_ = zlib.Deflate(ref zStream, Z_FULL_FLUSH);
_ = zlib.DeflateEnd(ref zStream);

// Uncompress
Span<byte> uncomressedData = new byte[inputData.Length];
zStream.Input = compressedData;
zStream.Output = uncomressedData;
_ = zlib.InflateInit(ref zStream);
_ = zlib.Inflate(ref zStream, Z_SYNC_FLUSH);

Debug.Assert(MemoryExtensions.SequenceEqual(inputData, uncomressedData));
```
Check out the [unit tests](https://github.com/mgnsm/ZLibDotNet/tree/main/tests/ZLibDotNet.UnitTests) for more examples.
### Implementation
`ZStream` has been named after the equivalent `z_stream` type in the ported library. It intentionally breaks the [CA1711](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1711) naming rule as it's not a `System.IO.Stream` but a `ref struct`. 

`Input` is a `ReadOnlySpan<byte>` property that defines the input buffer of the data to be compressed or uncompressed. The optional `int` properties `NextIn` and `AvailableIn` can be used to specify a starting position in the buffer and the maximum number of bytes to consume from it respectively.

Similarly, there is a `Span<Byte>` property named `Output` for the output buffer with corresponding `NextOut` and `AvailableOut` properties.

Using the `NextIn`/`NextOut` and `AvailableIn`/`AvailableOut` properties is not required and can be avoided by [slicing](https://learn.microsoft.com/en-us/dotnet/api/system.span-1.slice) the input and output buffers.
## Licensing and Support
The software is provided 'as-is', without any kind of express or implied warranty whatsoever. See the [license file](LICENSE.md) for details.

Since the implementation of ZLibDotNet is heavily based on the C implementation of the ported zlib data compression library, any questions that are not directly related to the C#/.NET implementation in this repository are referred to the authors of the original zlib library. There is a documentation with [frequently asked questions](https://zlib.net/zlib_faq.html), [a manual](https://zlib.net/manual.html) and an email address available at [https://zlib.net](https://zlib.net/).

If you any questions or feedback about the managed code in this repository, please [open an issue](https://docs.github.com/en/issues/tracking-your-work-with-issues/creating-an-issue).
## Contributions
Contributions such as bug fixes, extended functionality within the scope of the zlib compression library and other kind of improvements are welcome.

Follow [these steps](CONTRIBUTING.md) when contributing code to this repository.
## Implemented/Ported APIs
Below is an exhaustive list of the functions and macros in the zlib compression library that have currently been ported, and their C#/.NET counterparts.
| C function/macro  | C# API(s) |
| --- | --- |
| `int deflateInit(z_streamp strm, int level)` | `int DeflateInit(ref ZStream strm, int level)` |
| `int deflateInit2(z_streamp strm, int level, int method, int windowBits, int memLevel, int strategy)` | `int DeflateInit(ref ZStream strm, int level, int method, int windowBits, int memLevel, int strategy)` |
| `int deflate(z_streamp strm, int flush)` | `int Deflate(ref ZStream strm, int flush)` |
| `int deflateEnd (z_streamp strm)` | `int DeflateEnd(ref ZStream strm)` |
| `int inflateInit (z_streamp strm)` | `int InflateInit(ref ZStream strm)` |
| `int inflateInit2 (z_streamp strm, int windowBits)` | `int InflateInit(ref ZStream strm, int windowBits)` |
| `int inflate (z_streamp strm, int flush)` | `int Inflate(ref ZStream strm, int flush)` |
| `int inflateEnd (z_streamp strm)` | `int InflateEnd(ref ZStream strm)` |
| `int deflateSetDictionary (z_streamp strm, const Bytef* dictionary, uInt dictLength)` | `int DeflateSetDictionary(ref ZStream strm, byte[] dictionary, int dictLength)`<br />`int DeflateSetDictionary(ref ZStream strm, byte[] dictionary)`<br />`int DeflateSetDictionary(ref ZStream strm, ReadOnlySpan<byte> dictionary)` |
| `int deflateParams (z_streamp strm, int level, int strategy)` | `int DeflateParams(ref ZStream strm, int level, int strategy)` |
| `int inflateSetDictionary (z_streamp strm, const Bytef* dictionary, uInt dictLength)` | `int InflateSetDictionary(ref ZStream strm, byte[] dictionary, int dictLength)`<br />`int InflateSetDictionary(ref ZStream strm, byte[] dictionary)`<br />`int InflateSetDictionary(ref ZStream strm, ReadOnlySpan<byte> dictionary)` |
| `int inflateSync (z_streamp strm)` | `int InflateSync(ref ZStream strm)` |
| `int inflateCopy (z_streamp dest, z_streamp source)` | `int InflateCopy(ref ZStream source, ref ZStream dest)` |
| `int inflateReset (z_streamp strm)` | `int InflateReset(ref ZStream strm)` |
| `int inflateReset2 (z_streamp strm, int windowBits)` | `int InflateReset(ref ZStream strm, int windowBits)` |
| `int inflatePrime (z_streamp strm, int bits, int value)` | `int InflatePrime (ref ZStream strm, int bits, int value)` |
| `int compress (Bytef *dest, uLongf *destLen, const Bytef *source, uLong sourceLen)` | `int Compress(byte[] dest, out int destLen, byte[] source, int sourceLen)`<br />`int Compress(Span<byte> dest, out int destLen, ReadOnlySpan<byte> source)` |
| `int compress2 (Bytef *dest, uLongf *destLen, const Bytef *source, uLong sourceLen, int level)` | `int Compress(byte[] dest, out int destLen, byte[] source, int sourceLen, int level)`<br />`int Compress(Span<byte> dest, out int destLen, ReadOnlySpan<byte> source, int level)` |
| `uLong compressBound (uLong sourceLen)` | `uint CompressBound(uint sourceLen)` |
| `int uncompress (Bytef *dest, uLongf *destLen, const Bytef *source, uLong sourceLen)` | `int Uncompress(byte[] dest, out int destLen, byte[] source, int sourceLen)`<br />`int Uncompress(Span<byte> dest, out int destLen, ReadOnlySpan<byte> source)` |
| `int uncompress2 (Bytef *dest, uLongf *destLen, const Bytef *source, uLong *sourceLen)` | `int Uncompress(byte[] dest, out int destLen, byte[] source, out int sourceLen)`<br />`int Uncompress(Span<byte> dest, out int destLen, ReadOnlySpan<byte> source, out int sourceLen)` |
| `uLong adler32 (uLong adler, const Bytef *buf, uInt len)` | `uint Adler32(uint adler, byte[] buf, int len)`<br />`uint Adler32(uint adler, ReadOnlySpan<byte> buf)` |