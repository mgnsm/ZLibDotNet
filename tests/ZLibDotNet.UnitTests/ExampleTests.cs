using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZLibDotNet.UnitTests;

[TestClass]
public class ExampleTests
{
    private static readonly byte[] s_inputData = Encoding.ASCII.GetBytes("hello, hello!");

    [TestMethod]
    public void CompressAndUncompress()
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
        _ = zlib.DeflateInit(ref zStream, Z_DEFAULT_COMPRESSION);
        _ = zlib.Deflate(ref zStream, Z_FULL_FLUSH);
        _ = zlib.DeflateEnd(ref zStream);

        // Uncompress
        byte[] uncomressedData = new byte[s_inputData.Length];
        zStream.Input = compressedData;
        zStream.Output = uncomressedData;
        _ = zlib.InflateInit(ref zStream);
        _ = zlib.Inflate(ref zStream, Z_SYNC_FLUSH);

        Assert.IsTrue(Enumerable.SequenceEqual(s_inputData, uncomressedData));
    }
}