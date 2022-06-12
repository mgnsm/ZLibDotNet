using System.Buffers;
using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using Elskom.Generic.Libs;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Ionic.Zlib;
using CompressionMode = System.IO.Compression.CompressionMode;

namespace ZLibDotNet.Benchmarks;

[MemoryDiagnoser]
[MValueColumn]
public class UncompressionBenchmarks
{
    private const int OutputBufferSize = 1500;
    private static readonly ZLib s_zlib = new();

    public IEnumerable<byte[]> Input { get; } = new[]
    {
        new byte[18] { 120, 156, 203, 72, 205, 201, 201, 215, 81, 200, 0, 81, 138, 0, 33, 112, 4, 150 },
        new byte[53] { 120, 156, 13, 201, 177, 1, 192, 48, 8, 4, 177, 85, 110, 4, 158, 7, 28, 239, 198, 240, 113, 161, 74, 177, 218, 90, 21, 106, 230, 54, 74, 100, 58, 11, 15, 254, 56, 98, 130, 17, 39, 49, 229, 126, 94, 4, 210, 253, 1, 126, 150, 11, 183 },
        new byte[89] { 120, 156, 21, 141, 129, 13, 192, 48, 8, 195, 94, 201, 9, 16, 186, 22, 126, 235, 241, 115, 37, 44, 64, 200, 33, 110, 222, 116, 223, 25, 149, 150, 50, 66, 171, 76, 127, 20, 44, 45, 191, 249, 131, 13, 7, 26, 70, 153, 1, 9, 220, 179, 52, 173, 225, 154, 24, 33, 147, 23, 218, 91, 78, 225, 187, 228, 165, 143, 34, 231, 224, 54, 110, 227, 54, 110, 31, 153, 127, 230, 95, 177, 87, 215, 15, 116, 198, 27, 219 },
        new byte[291] { 120, 156, 29, 145, 219, 149, 69, 49, 8, 66, 91, 161, 132, 40, 106, 142, 189, 221, 226, 103, 103, 62, 92, 121, 64, 12, 224, 249, 157, 95, 204, 207, 42, 101, 40, 83, 105, 37, 251, 86, 142, 242, 42, 63, 229, 202, 71, 14, 57, 101, 203, 37, 183, 186, 212, 173, 218, 213, 148, 166, 53, 163, 61, 218, 208, 166, 246, 83, 156, 82, 209, 45, 226, 104, 162, 89, 87, 49, 220, 15, 235, 61, 84, 112, 63, 212, 165, 62, 106, 85, 101, 238, 121, 243, 241, 39, 239, 243, 32, 226, 160, 226, 32, 227, 188, 187, 69, 227, 161, 16, 26, 40, 13, 164, 6, 188, 122, 146, 223, 10, 191, 224, 23, 252, 130, 223, 240, 26, 94, 131, 247, 243, 4, 222, 224, 13, 222, 15, 167, 223, 208, 111, 95, 193, 69, 122, 46, 220, 133, 187, 112, 119, 112, 13, 103, 225, 239, 203, 225, 5, 65, 18, 104, 51, 218, 140, 54, 163, 205, 104, 51, 218, 28, 47, 37, 2, 66, 147, 241, 236, 120, 239, 193, 241, 231, 4, 35, 15, 19, 175, 201, 215, 255, 89, 130, 27, 220, 228, 66, 224, 46, 56, 69, 204, 5, 134, 15, 227, 195, 248, 40, 112, 23, 253, 27, 28, 79, 198, 147, 241, 100, 180, 123, 56, 15, 231, 121, 103, 122, 48, 7, 95, 214, 143, 252, 209, 83, 100, 85, 100, 85, 252, 91, 249, 102, 194, 208, 152, 108, 49, 218, 98, 182, 197, 112, 139, 161, 22, 83, 45, 180, 20, 255, 15, 58, 7, 254, 124, 12, 151, 108, 134, 108, 134, 92, 134, 92, 46, 61, 47, 61, 47, 61, 175, 95, 249, 15, 140, 28, 106, 46 },
        new byte[627] { 120, 156, 77, 84, 209, 114, 227, 32, 12, 252, 21, 253, 74, 227, 78, 122, 157, 75, 174, 158, 58, 115, 157, 123, 84, 141, 18, 56, 99, 148, 98, 147, 140, 239, 235, 79, 32, 98, 247, 69, 172, 64, 72, 98, 181, 246, 147, 119, 61, 237, 124, 34, 120, 10, 179, 251, 74, 244, 97, 221, 44, 206, 87, 194, 98, 70, 140, 46, 136, 255, 47, 69, 130, 29, 185, 139, 88, 55, 73, 28, 236, 60, 246, 67, 182, 161, 183, 100, 158, 252, 200, 193, 64, 201, 148, 205, 111, 199, 158, 102, 216, 69, 190, 7, 216, 165, 232, 151, 15, 102, 3, 13, 26, 154, 75, 80, 99, 49, 206, 145, 210, 148, 33, 247, 236, 81, 202, 54, 28, 209, 103, 27, 206, 158, 239, 20, 53, 82, 220, 201, 249, 1, 154, 232, 198, 137, 3, 52, 11, 6, 120, 198, 56, 148, 227, 12, 214, 157, 23, 246, 134, 66, 148, 74, 197, 139, 184, 84, 64, 164, 1, 63, 45, 14, 174, 160, 35, 94, 40, 204, 88, 240, 155, 119, 55, 218, 130, 222, 34, 134, 11, 85, 216, 91, 167, 217, 222, 73, 215, 14, 243, 91, 21, 18, 110, 183, 186, 252, 132, 181, 165, 226, 173, 13, 156, 82, 252, 74, 236, 38, 61, 171, 228, 60, 19, 93, 91, 23, 134, 2, 186, 97, 209, 187, 110, 212, 91, 108, 46, 149, 128, 189, 139, 244, 25, 157, 176, 189, 247, 153, 32, 29, 209, 158, 35, 77, 179, 150, 223, 167, 222, 78, 14, 225, 5, 93, 152, 62, 57, 50, 188, 88, 158, 102, 13, 204, 156, 192, 70, 76, 201, 174, 215, 138, 253, 67, 94, 184, 134, 31, 28, 104, 49, 148, 193, 92, 154, 122, 13, 198, 97, 200, 143, 206, 232, 194, 240, 122, 227, 184, 128, 18, 120, 192, 27, 5, 67, 113, 5, 210, 232, 100, 197, 187, 7, 77, 125, 32, 225, 168, 177, 238, 124, 22, 170, 14, 238, 98, 117, 234, 5, 233, 148, 21, 230, 193, 21, 180, 54, 88, 251, 209, 205, 220, 107, 69, 37, 107, 134, 165, 187, 130, 234, 36, 20, 63, 70, 161, 94, 101, 83, 157, 117, 20, 234, 206, 68, 126, 59, 93, 235, 141, 84, 204, 35, 75, 16, 251, 208, 200, 17, 35, 75, 161, 35, 25, 151, 198, 111, 159, 133, 110, 148, 92, 10, 171, 92, 212, 105, 83, 188, 250, 199, 201, 218, 95, 117, 87, 177, 84, 255, 42, 9, 47, 223, 35, 54, 201, 168, 175, 162, 201, 243, 56, 58, 19, 86, 70, 143, 46, 204, 77, 36, 28, 5, 77, 243, 242, 206, 249, 2, 247, 61, 78, 46, 192, 47, 188, 225, 95, 86, 25, 8, 94, 224, 205, 155, 3, 246, 4, 69, 240, 106, 159, 35, 126, 66, 85, 188, 46, 185, 70, 125, 72, 139, 158, 54, 233, 20, 175, 180, 152, 209, 214, 96, 246, 182, 246, 90, 188, 226, 130, 82, 244, 10, 45, 97, 111, 219, 116, 62, 11, 138, 9, 202, 228, 90, 159, 70, 104, 249, 110, 170, 186, 43, 75, 249, 166, 52, 191, 232, 31, 227, 157, 23, 212, 25, 117, 104, 140, 39, 221, 173, 243, 238, 48, 152, 26, 183, 178, 42, 160, 179, 50, 73, 232, 28, 133, 128, 178, 248, 155, 200, 243, 161, 131, 141, 238, 77, 12, 93, 144, 177, 127, 231, 125, 19, 198, 73, 84, 121, 34, 17, 233, 201, 10, 169, 210, 221, 137, 71, 156, 25, 78, 194, 207, 116, 197, 40, 162, 128, 237, 249, 245, 115, 254, 176, 132, 217, 102, 178, 139, 237, 70, 30, 8, 170, 190, 116, 41, 133, 254, 3, 111, 114, 1, 96 }
    };

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
        _ = s_zlib.InflateInit(zStream);
        _ = s_zlib.Inflate(zStream, Z_NO_FLUSH);
        _ = s_zlib.InflateEnd(zStream);

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Input))]
    public unsafe void ZLibDotNetUnsafe(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        fixed (byte* nextIn = input, nextOut = outputBuffer)
        {
            Unsafe.ZStream zStream = new()
            {
                NextIn = nextIn,
                AvailableIn = (uint)input.Length,
                NextOut = nextOut,
                AvailableOut = OutputBufferSize
            };
            _ = s_zlib.InflateInit(zStream);
            _ = s_zlib.Inflate(zStream, Z_NO_FLUSH);
            _ = s_zlib.InflateEnd(zStream);
        }

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Input))]
    public void DotNet6(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        using ZLibStream zLibStream = new(new MemoryStream(input), CompressionMode.Decompress);
        _ = zLibStream.Read(outputBuffer, 0, OutputBufferSize);

        ArrayPool<byte>.Shared.Return(outputBuffer);
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
        _ = zlibCodec.InitializeInflate();
        _ = zlibCodec.Inflate(FlushType.None);
        _ = zlibCodec.EndInflate();

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Input))]
    public void SharpZipLib(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        Inflater inflater = new();
        inflater.SetInput(input, 0, input.Length);
        _ = inflater.Inflate(outputBuffer, 0, OutputBufferSize);

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Input))]
    public void ZlibManaged(byte[] input)
    {
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(OutputBufferSize);

        using ZInputStream zInputStream = new(new MemoryStream(input));
        _ = zInputStream.Read(outputBuffer, 0, OutputBufferSize);

        ArrayPool<byte>.Shared.Return(outputBuffer);
    }
#pragma warning restore CA1062
}