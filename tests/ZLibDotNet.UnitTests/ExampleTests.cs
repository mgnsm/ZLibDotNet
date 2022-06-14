using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZLibDotNet.UnitTests;

[TestClass]
public class ExampleTests
{
    private const int HeaderAndTrailerSize = 2 + 4;
    private static readonly byte[] s_inputData = Encoding.ASCII.GetBytes("hello, hello!");

    [TestMethod]
    public void SafeCompressAndUncompress()
    {
        // Compress
        byte[] compressedData = new byte[s_inputData.Length + HeaderAndTrailerSize];
        ZStream zStream = new()
        {
            Input = s_inputData,
            Output = compressedData
        };
        ZLib zlib = new();
        _ = zlib.DeflateInit(zStream, Z_DEFAULT_COMPRESSION);
        _ = zlib.Deflate(zStream, Z_FULL_FLUSH);
        _ = zlib.DeflateEnd(zStream);

        // Uncompress
        byte[] uncomressedData = new byte[s_inputData.Length];
        zStream.Input = compressedData;
        zStream.Output = uncomressedData;
        _ = zlib.InflateInit(zStream);
        _ = zlib.Inflate(zStream, Z_SYNC_FLUSH);

        Assert.IsTrue(Enumerable.SequenceEqual(s_inputData, uncomressedData));
    }

    [TestMethod]
    public void UnsafeCompressAndUncompress()
    {
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
                    zStream.AvailableOut = (uint)uncompressedData.Length;
                    _ = zlib.InflateInit(zStream);
                    _ = zlib.Inflate(zStream, Z_SYNC_FLUSH);
                }

                Assert.IsTrue(Enumerable.SequenceEqual(s_inputData, uncompressedData));
            }
        }
    }
}