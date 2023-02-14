using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZLibDotNet.UnitTests;

[TestClass]
public class ExampleTests
{
    [TestMethod]
    public void CompressAndUncompress()
    {
        ReadOnlySpan<byte> inputData = Encoding.ASCII.GetBytes("hello, hello!");

        // Compress
        ZLib zlib = new();
        uint sourceLen = zlib.CompressBound((uint)inputData.Length);
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

        Assert.IsTrue(MemoryExtensions.SequenceEqual(inputData, uncomressedData));
    }
}