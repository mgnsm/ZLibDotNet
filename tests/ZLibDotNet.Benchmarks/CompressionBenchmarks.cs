using System.Buffers;
using System.IO.Compression;
using System.Text;
using BenchmarkDotNet.Attributes;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Ionic.Zlib;
using Elskom.Generic.Libs;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace ZLibDotNet.Benchmarks;

[MemoryDiagnoser]
[MValueColumn]
public class CompressionBenchmarks
{
    private const int OutputBufferSize = 1000;
    private static readonly ZLib s_zlib = new();

    public IEnumerable<byte[]> Input { get; } = new[]
    {
        Encoding.ASCII.GetBytes("hello, hello!"),
        Encoding.ASCII.GetBytes("0|1|4|14 15 695 12 13 524 36 38 71 60 61 72 3 435 436 30 119"),
        Encoding.ASCII.GetBytes("0|1|128|99 3 4 100 432 102 103 104 422 105 106 107 108 109 110 111 112 113 98 97 114 40 29 30 66 21 22 23 24 54 55 179 180 181 182 187 202 203 382 383"),
        Encoding.ASCII.GetBytes("0|0|16|3 4 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35 54 55 499 64 65 66 90 91 92 98 104 422 110 615 119 168 169 170 171 616 617 618 619 443 172 188 204 205 206 207 208 209 210 211 212 213 214 243 244 245 246 247 248 251 252 253 254 255 256 257 258 259 260 290 291 292 293 294 295 296 319 297 298 299 300 304 305 306 307 308 309 310 313 314 315 316 317 318 320 322 323 324 332 336 337 338 125 340 341 342 346 347 348 438 349 350 351 352 353 360 361 362 363 365 366 375 384 410 411 412 423 424 425 426 427 428 429 433 434 437 441 622 623 689 690 691 693 694 710 711 712 732 733"),
        Encoding.ASCII.GetBytes("AliceBlue AntiqueWhite Aqua Aquamarine Azure Beige Bisque Black BlanchedAlmond Blue BlueViolet Brown BurlyWood CadetBlue Chartreuse Chocolate Coral CornflowerBlue Cornsilk Crimson Cyan DarkBlue DarkCyan DarkGoldenrod DarkGray DarkGreen DarkKhaki DarkMagenta DarkOliveGreen DarkOrange DarkOrchid DarkRed DarkSalmon DarkSeaGreen DarkSlateBlue DarkSlateGray DarkTurquoise DarkViolet DeepPink DeepSkyBlue DimGray DodgerBlue Firebrick FloralWhite ForestGreen Fuchsia Gainsboro GhostWhite Gold Goldenrod Gray Green GreenYellow Honeydew HotPink IndianRed Indigo Ivory Khaki Lavender LavenderBlush LawnGreen LemonChiffon LightBlue LightCoral LightCyan LightGoldenrodYellow LightGray LightGreen LightPink LightSalmon LightSeaGreen LightSkyBlue LightSlateGray LightSteelBlue LightYellow Lime LimeGreen Linen Magenta Maroon MediumAquamarine MediumBlue MediumOrchid MediumPurple MediumSeaGreen MediumSlateBlue MediumSpringGreen MediumTurquoise MediumVioletRed MidnightBlue MintCream MistyRose Moccasin NavajoWhite Navy OldLace Olive OliveDrab Orange OrangeRed Orchid PaleGoldenrod PaleGreen PaleTurquoise PaleVioletRed PapayaWhip PeachPuff Peru Pink Plum PowderBlue Purple Red RosyBrown RoyalBlue SaddleBrown Salmon SandyBrown SeaGreen SeaShell Sienna Silver SkyBlue SlateBlue SlateGray Snow SpringGreen SteelBlue Tan Teal Thistle Tomato Transparent Turquoise Violet Wheat White WhiteSmoke Yellow YellowGreen")
    };

    [Params(1, 6, 9)]
    public int Level { get; set; }

#pragma warning disable CA1062
    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(Input))]
    public void ZLibDotNet(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        ZStream zStream = new()
        {
            Input = input,
            Output = outputBuffer
        };
        _ = s_zlib.DeflateInit(ref zStream, Level);
        _ = s_zlib.Deflate(ref zStream, Z_FINISH);
        _ = s_zlib.DeflateEnd(ref zStream);

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Input))]
    public void DotNet6(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        using MemoryStream dest = new();
        using (ZLibStream zLibStream = new(dest, GetCompressionLevel(Level), true))
            zLibStream.Write(input, 0, input.Length);
        dest.Position = 0;
        _ = dest.Read(outputBuffer, 0, OutputBufferSize);

        ArrayPool<byte>.Shared.Return(outputBuffer);

        static CompressionLevel GetCompressionLevel(int level) => level switch
        {
            1 => CompressionLevel.Fastest,
            9 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal,
        };
    }

    [Benchmark]
    [ArgumentsSource(nameof(Input))]
    public void DotNetZip(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        ZlibCodec zlibCodec = new()
        {
            InputBuffer = input,
            AvailableBytesIn = input.Length,
            OutputBuffer = outputBuffer,
            AvailableBytesOut = OutputBufferSize
        };
        _ = zlibCodec.InitializeDeflate((Ionic.Zlib.CompressionLevel)Level);
        _ = zlibCodec.Deflate(FlushType.Finish);
        _ = zlibCodec.EndDeflate();

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Input))]
    public void SharpZipLib(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        Deflater deflater = new();
        deflater.SetLevel(Level);
        deflater.SetInput(input, 0, input.Length);
        deflater.Finish();
        _ = deflater.Deflate(outputBuffer, 0, OutputBufferSize);

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Input))]
    public void ZlibManaged(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        using MemoryStream dest = new();
        using (ZOutputStream zOutputStream = new(dest, Level == 6 ? ZlibCompression.ZDEFAULTCOMPRESSION
            : (ZlibCompression)Level))
        {
            zOutputStream.Write(input, 0, input.Length);
            zOutputStream.Finish();
            dest.Position = 0;
            _ = dest.Read(outputBuffer, 0, OutputBufferSize);
        }

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }
#pragma warning restore CA1062
}