using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZLibDotNet.UnitTests;

[TestClass]
public class ExampleTests
{
    private static readonly byte[] s_inputData = Encoding.ASCII.GetBytes("hello, hello!");

    [TestMethod]
    public void SafeCompressAndUncompress()
    {
        // Compress
        ZLib zlib = new();
        uint sourceLen = zlib.CompressBound((uint)s_inputData.Length);
        byte[] compressedData = new byte[sourceLen];
        ZStream zStream = new()
        {
            Input = s_inputData,
            Output = compressedData
        };
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
        ZLib zlib = new();
        uint sourceLen = zlib.CompressBound((uint)s_inputData.Length);
        byte[] compressedData = new byte[sourceLen];
        byte[] uncompressedData = new byte[s_inputData.Length];

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