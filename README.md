# ZLibDotNet
![Build Status](https://github.com/mgnsm/ZLibDotNet/actions/workflows/ci.yml/badge.svg)

A fully managed C#/.NET Standard 1.3 implementation of the compression (deflate) and decompression (inflate) functions in the [zlib compression library](https://www.zlib.net/), including both unsafe (operations involving pointers) and type-safe APIs for doing in-memory compression, decompression and integrity checks of uncompressed data in the zlib format.

### Data format
The zlib data format, documented in [RFC (Request for Comments) 1950](https://datatracker.ietf.org/doc/html/rfc1950), wraps a raw deflate stream of data, which is itself documented in [RFC 1951](https://datatracker.ietf.org/doc/html/rfc1951), in a compact header and trailer of just two and four bytes respectively. It was designed for in-memory and communication channel applications and is different from the gzip or zip formats, neither of which are supported by this implementation.

## zlib support in .NET
If you simply want to compress or decompress data in the zlib format in a .NET application, there is a `ZLibStream` class for doing this in .NET 6:
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
Besides being unsupported in .NET 5 and earlier versions, the `ZLibStream` class in .NET 6 also doesn't provide any functionality to retrieve information such as the total number of bytes processed and output or the Adler-32 checksum of the data. 

There are indeed other third-party and fully managed C#/.NET libraries that already provide this functionality but ZLibDotNet was implemented during the porting of a C/C++ library to C#/.NET where it was undesirable to rely on any existing third-party software. 

It has been designed to provide an API surface that is very similar to the one that the original ported C library provides. This includes the ability to use pointers to data to be compressed or uncompressed, which comes in handy when you for example consume data from a socket frequently and want to minimze the amount of memory heap allocations your application performs.
## Installation
ZLibDotNet is installed using NuGet:

    PM> Install-Package ZLibDotNet
## Example
Below is an example of how to compress and uncompress some sample data using the type-safe `ZLib` and `ZStream` classes in the `ZLibDotNet` namespace:
```cs
using static ZLibDotNet.ZLib;
using ZLibDotNet;

byte[] inputData = Encoding.ASCII.GetBytes("hello, hello!");

// Compress
const int HeaderAndTrailerSize = 2 + 4;
byte[] compressedData = new byte[inputData.Length + HeaderAndTrailerSize];
ZStream zStream = new()
{
    Input = inputData,
    Output = compressedData
};
ZLib zlib = new();
_ = zlib.DeflateInit(zStream, Z_DEFAULT_COMPRESSION);
_ = zlib.Deflate(zStream, Z_FULL_FLUSH);
_ = zlib.DeflateEnd(zStream);

// Uncompress
byte[] uncomressedData = new byte[inputData.Length];
zStream.Input = compressedData;
zStream.Output = uncomressedData;
_ = zlib.InflateInit(zStream);
_ = zlib.Inflate(zStream, Z_SYNC_FLUSH);

Debug.Assert(Enumerable.SequenceEqual(inputData, uncomressedData));
```
## Unsafe Example
There is also another `ZStream` class available in the `ZLibDotNet.Unsafe` namespace that lets you compress and uncompress data pointed to by pointers in an `unsafe` context:
```cs
byte[] compressedData = new byte[s_inputData.Length + HeaderAndTrailerSize];
byte[] uncompressedData = new byte[s_inputData.Length];

ZLib zlib = new();
unsafe
{
    fixed (byte* input = s_inputData, compr = compressedData)
    {
        // Compress
        Unsafe.ZStream zStream = new()
        {
            NextIn = input,
            AvailableIn = (uint)s_inputData.Length,
            NextOut = compr,
            AvailableOut = (uint)compressedData.Length
        };
        _ = zlib.DeflateInit(zStream, Z_DEFAULT_COMPRESSION);
        _ = zlib.Deflate(zStream, Z_FULL_FLUSH);
        _ = zlib.DeflateEnd(zStream);

        // Uncompress
        zStream.NextIn = compr;
        zStream.AvailableIn = zStream.TotalOut;
        fixed (byte* uncompr = uncompressedData)
        {
            zStream.NextOut = uncompr;
            zStream.AvailableOut = (uint) uncompressedData.Length;
            _ = zlib.InflateInit(zStream);
            _ = zlib.Inflate(zStream, Z_SYNC_FLUSH);
        }

        Debug.Assert(Enumerable.SequenceEqual(s_inputData, uncompressedData));
    }
}
```
Check out the [unit tests](https://github.com/mgnsm/ZLibDotNet/tree/main/tests/ZLibDotNet.UnitTests) for more examples.
## Licensing and Support
The software is provided 'as-is', without any kind of express or implied warranty whatsoever. See the [license file](LICENSE.md) for details.

Since the implementation of ZLibDotNet is heavily based on the C implementation of the ported zlib data compression library, any questions that are not directly related to the C#/.NET implementation in this repository are referred to the authors of the original zlib library. There is a documentation with [frequently asked questions](https://zlib.net/zlib_faq.html), [a manual](https://zlib.net/manual.html) and an email address available at [https://zlib.net](https://zlib.net/).

If you any questions or feedback about the managed code in this repository, please [open an issue](https://docs.github.com/en/issues/tracking-your-work-with-issues/creating-an-issue).
## Contributions
Contributions such as bug fixes, extended functionality within the scope of the zlib compression library and other kind of improvements are welcome.

Follow these steps when contributing code to this repository:

1. Open an issue where you describe the reason behind the code being contributed.

2. Clone the repository:

        git clone https://github.com/mgnsm/ZLibDotNet.git

3. Create a new feature branch:

        git branch -b feature_branch

4. Make and commit your changes to this feature branch:

        git commit -m "Some informational message"

    The [.editorconfig](.editorconfig) file defines the code style. Do not edit this one or the [project file](https://github.com/mgnsm/ZLibDotNet/blob/main/src/ZLibDotNet/ZLibDotNet.csproj) unless these files are directly related to your changes.

5. Build and test the code in both debug and release mode using the [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0):

        dotnet test tests/ZLibDotNet.UnitTests/ZLibDotNet.UnitTests.csproj -c release

    Make sure that the code builds without any warnings and that all existing and any new unit tests pass.

6. Push the feature branch to GitHub:

        git push -u origin feature_branch

7. Submit a [pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request), including a clear and concise description, against the `main` branch.

8. Wait for the pull request to become validated and approved.
## Implemented/Ported APIs
Below is an exhaustive list of the functions and macros in the zlib compression library that have currently been ported, and their C#/.NET counterparts.
| C function/macro  | C# API | Unsafe C# API |
| --- | --- | --- |
| `int deflateInit(z_streamp strm, int level)` | `int DeflateInit(ZStream strm, int level)` | `int DeflateInit(Unsafe.ZStream strm, int level)` |
| `int deflateInit2(z_streamp strm, int level, int method, int windowBits, int memLevel, int strategy)` | `int DeflateInit(ZStream strm, int level, int method, int windowBits, int memLevel, int strategy)` | `int DeflateInit(Unsafe.ZStream strm, int level, int method, int windowBits, int memLevel, int strategy)` |
| `int deflate(z_streamp strm, int flush)` | `int Deflate(ZStream strm, int flush)` | `int Deflate(Unsafe.ZStream strm, int flush)` |
| `int deflateEnd (z_streamp strm)` | `int DeflateEnd(ZStream strm)` | `int DeflateEnd(Unsafe.ZStream strm)` |
| `int inflateInit (z_streamp strm)` | `int InflateInit(ZStream strm)` | `int InflateInit(Unsafe.ZStream strm)` |
| `int inflateInit2 (z_streamp strm, int windowBits)` | `int InflateInit(ZStream strm, int windowBits)` | `int InflateInit(Unsafe.ZStream strm, int windowBits)` |
| `int inflate (z_streamp strm, int flush)` | `int Inflate(ZStream strm, int flush)` | `int Inflate(Unsafe.ZStream strm, int flush)` |
| `int inflateEnd (z_streamp strm)` | `int InflateEnd(ZStream strm)` | `int InflateEnd(ZStream strm)` |
| `int deflateSetDictionary (z_streamp strm, const Bytef* dictionary, uInt dictLength)` | `int DeflateSetDictionary(ZStream strm, byte[] dictionary)` | `int DeflateSetDictionary(Unsafe.ZStream strm, byte* dictionary, uint dictLength)` |
| `int deflateParams (z_streamp strm, int level, int strategy)` | `int DeflateParams(ZStream strm, int level, int strategy)` | `int DeflateParams(Unsafe.ZStream strm, int level, int strategy)` |
| `int inflateSetDictionary (z_streamp strm, const Bytef* dictionary, uInt dictLength)` | `int InflateSetDictionary(ZStream strm, byte[] dictionary) / int InflateSetDictionary(ZStream strm, byte[] dictionary, int length)` | `int InflateSetDictionary(Unsafe.ZStream strm, byte* dictionary, uint dictLength)` |
| `int inflateSync (z_streamp strm)` | `int InflateSync(ZStream strm)` | `int InflateSync(Unsafe.ZStream strm)` |
| `int inflateReset (z_streamp strm)` | `int InflateReset(ZStream strm)` | `int InflateReset(Unsafe.ZStream strm)` |
| `int inflateReset2 (z_streamp strm, int windowBits)` | `int InflateReset(ZStream strm, int windowBits)` | `int InflateReset(Unsafe.ZStream strm, int windowBits)` |
| `int inflatePrime (z_streamp strm, int bits, int value)` | `int InflatePrime (ZStream strm, int bits, int value)` | `int InflatePrime (Unsafe.ZStream strm, int bits, int value)|
| `int compress (Bytef *dest, uLongf *destLen, const Bytef *source, uLong sourceLen)` | `int Compress(ReadOnlySpan<byte> source, Span<byte> dest, out uint destLen)` | `int Compress(byte* dest, uint* destLen, byte* source, uint sourceLen)` |
| `int compress2 (Bytef *dest, uLongf *destLen, const Bytef *source, uLong sourceLen, int level)` | `int Compress(ReadOnlySpan<byte> source, Span<byte> dest, out uint destLen, int level)` | `int Compress(byte* dest, uint* destLen, byte* source, uint sourceLen, int level)` |
| `int uncompress (Bytef *dest, uLongf *destLen, const Bytef *source, uLong sourceLen)` | `int Uncompress(ReadOnlySpan<byte> source, Span<byte> dest, out uint destLen)` | `int Uncompress(byte* dest, uint* destLen, byte* source, uint sourceLen)` |
| `int uncompress2 (Bytef *dest, uLongf *destLen, const Bytef *source, uLong *sourceLen)` | `int Uncompress(ReadOnlySpan<byte> source, Span<byte> dest, out uint sourceLen, out uint destLen)` | `int Uncompress(byte* dest, uint* destLen, byte* source, uint* sourceLen)` |
| `uLong adler32 (uLong adler, const Bytef *buf, uInt len)` | `uint Adler32(uint adler, ReadOnlySpan<byte> buf)` | `uint Adler32(uint adler, byte* buf, uint len)` |