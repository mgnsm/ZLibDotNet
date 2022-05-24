using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text;

namespace ZLibDotNet.UnitTests;

[TestClass]
public class DeflateTests
{
    private static readonly byte[] s_hello = Encoding.ASCII.GetBytes($"hello, hello!{char.MinValue}");

    [TestMethod]
    public void Compress()
    {
        byte[] source = s_hello.ToArray();
        byte[] dest = new byte[ComprLen];

        ZLib zlib = new();
        Assert.AreEqual(Z_OK, zlib.Compress(source, dest, out uint destlen));
        Assert.AreEqual(19U, destlen);
        Assert.IsTrue(Enumerable.SequenceEqual(dest.Take(19),
            new byte[19] { 120, 156, 203, 72, 205, 201, 201, 215, 81, 200, 0, 81, 138, 12, 0, 38, 6, 4, 150 }));
    }

    [TestMethod]
    public void DeflateWithSmallBuffers()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.DeflateInit(zStream, Z_DEFAULT_COMPRESSION));
        Assert.AreEqual(1U, zStream.Adler);
        Assert.AreEqual(2, zStream.DataType);

        int len = s_hello.Length;
        zStream.Input = s_hello;

        byte[] compr = new byte[ComprLen];
        zStream.Output = compr;

        Assert.AreEqual(0, zStream.TotalIn);
        Assert.AreEqual(0, zStream.TotalOut);

        int count = 0;
        while (zStream.TotalIn != len && zStream.TotalOut < ComprLen)
        {
            zStream.AvailableIn = zStream.AvailableOut = 1; // force small buffers
            Assert.AreEqual(Z_OK, zlib.Deflate(zStream, Z_NO_FLUSH));
            count++;
        }
        Assert.AreEqual(16, count);
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(14, zStream.TotalIn);
        Assert.AreEqual(1, zStream.AvailableOut);
        Assert.AreEqual(2, zStream.TotalOut);
        Assert.AreEqual(637928598U, zStream.Adler);
        Assert.AreEqual(2, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(14, zStream.NextIn);
        Assert.AreEqual(2, zStream.NextOut);

        count = 0;
        for (; ; )
        {
            zStream.AvailableOut = 1;
            int err = zlib.Deflate(zStream, Z_FINISH);
            count++;
            if (err == Z_STREAM_END)
                break;
            Assert.AreEqual(Z_OK, err);
        }
        Assert.AreEqual(18, count);
        Assert.AreEqual(Z_OK, zlib.DeflateEnd(zStream));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(14, zStream.TotalIn);
        Assert.AreEqual(1, zStream.AvailableOut);
        Assert.AreEqual(19, zStream.TotalOut);
        Assert.AreEqual(637928598U, zStream.Adler);
        Assert.AreEqual(0, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(14, zStream.NextIn);
        Assert.AreEqual(19, zStream.NextOut);
        Assert.IsTrue(Enumerable.SequenceEqual(compr.Take(20),
            new byte[20] { 120, 156, 203, 72, 205, 201, 201, 215, 81, 200, 0, 81, 138, 12, 0, 38, 6, 4, 150, 0 }));
    }

    [TestMethod]
    public void DeflateWithLargeBuffers()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.DeflateInit(zStream, Z_BEST_SPEED));
        Assert.AreEqual(1U, zStream.Adler);
        Assert.AreEqual(2, zStream.DataType);

        byte[] compr = new byte[ComprLen];
        zStream.Output = compr;

        const int UncomprLen = ComprLen;
        byte[] uncompr = new byte[UncomprLen];
        Array.Copy(s_hello, uncompr, s_hello.Length);
        zStream.Input = uncompr;

        Assert.AreEqual(40000, zStream.AvailableIn);
        Assert.AreEqual(0, zStream.TotalIn);
        Assert.AreEqual(40000, zStream.AvailableOut);
        Assert.AreEqual(0, zStream.TotalOut);

        // At this point, uncompr is still mostly zeroes, so it should compress very well.
        Assert.AreEqual(Z_OK, zlib.Deflate(zStream, Z_NO_FLUSH));
        Assert.AreEqual(0, zStream.AvailableIn, "deflate not greedy");
        Assert.AreEqual(40000, zStream.TotalIn);
        Assert.AreEqual(2, zStream.TotalOut);
        Assert.AreEqual(39998, zStream.AvailableOut);
        Assert.AreEqual(2638611606U, zStream.Adler);
        Assert.AreEqual(2, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(40000, zStream.NextIn);
        Assert.AreEqual(2, zStream.NextOut);
        VerifyOutput(compr, zStream.TotalOut, new byte[2] { 120, 1 });

        // Feed in already compressed data and switch to no compression.
        Assert.AreEqual(Z_OK, zlib.DeflateParams(zStream, Z_NO_COMPRESSION, Z_DEFAULT_STRATEGY));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(40000, zStream.TotalIn);
        Assert.AreEqual(208, zStream.TotalOut);
        Assert.AreEqual(40000 - zStream.TotalOut, zStream.AvailableOut);
        Assert.AreEqual(2638611606U, zStream.Adler);
        Assert.AreEqual(0, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(40000, zStream.NextIn);
        Assert.AreEqual(208, zStream.NextOut);
        VerifyOutput(compr, zStream.TotalOut,
            new byte[208] { 120, 1, 236, 208, 177, 13, 0, 32, 8, 0, 65, 70, 145, 222, 181, 76, 40, 72, 216, 191, 51, 206, 225, 125,
                243, 253, 213, 233, 158, 189, 234, 45, 67, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8,
                16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128,
                0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8,
                16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128,
                0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8,
                16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 8, 16, 32, 64, 128, 0, 1, 2, 4, 254, 22, 184 });
        zStream.Input = compr;
        zStream.AvailableIn = ComprLen / 2;
        Assert.AreEqual(Z_OK, zlib.Deflate(zStream, Z_NO_FLUSH));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(60000, zStream.TotalIn);
        Assert.AreEqual(208, zStream.TotalOut);
        Assert.AreEqual(40000 - zStream.TotalOut, zStream.AvailableOut);
        Assert.AreEqual(1115171374U, zStream.Adler);
        Assert.AreEqual(0, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(208, zStream.NextOut);

        // Switch back to compressing mode.
        Assert.AreEqual(Z_OK, zlib.DeflateParams(zStream, Z_BEST_COMPRESSION, Z_FILTERED));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(60000, zStream.TotalIn);
        Assert.AreEqual(20213, zStream.TotalOut);
        Assert.AreEqual(40000 - zStream.TotalOut, zStream.AvailableOut);
        Assert.AreEqual(1115171374U, zStream.Adler);
        Assert.AreEqual(0, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(20213, zStream.NextOut);
        byte[] testData = GetCompr();
        byte[] expectedOutput = testData.Take(421).ToArray();
        VerifyOutput(compr, expectedOutput.Length, expectedOutput);
        zStream.Input = uncompr;
        zStream.AvailableIn = UncomprLen;
        Assert.AreEqual(Z_OK, zlib.Deflate(zStream, Z_NO_FLUSH));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(100000, zStream.TotalIn);
        Assert.AreEqual(20213, zStream.TotalOut);
        Assert.AreEqual(40000 - zStream.TotalOut, zStream.AvailableOut);
        Assert.AreEqual(1497247427U, zStream.Adler);
        Assert.AreEqual(0, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(20213, zStream.NextOut);
        VerifyOutput(compr, expectedOutput.Length, expectedOutput);

        Assert.AreEqual(Z_STREAM_END, zlib.Deflate(zStream, Z_FINISH), $"deflate should report {Z_STREAM_END}");
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(100000, zStream.TotalIn);
        Assert.AreEqual(20288, zStream.TotalOut);
        Assert.AreEqual(40000 - zStream.TotalOut, zStream.AvailableOut);
        Assert.AreEqual(1497247427U, zStream.Adler);
        Assert.AreEqual(0, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(20288, zStream.NextOut);
        VerifyOutput(compr, zStream.TotalOut, testData);

        Assert.AreEqual(Z_OK, zlib.DeflateEnd(zStream));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(100000, zStream.TotalIn);
        Assert.AreEqual(20288, zStream.TotalOut);
        Assert.AreEqual(40000 - zStream.TotalOut, zStream.AvailableOut);
        Assert.AreEqual(1497247427U, zStream.Adler);
        Assert.AreEqual(0, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(20288, zStream.NextOut);

        static void VerifyOutput(byte[] actualOutput, int count, byte[] expectedOutput)
        {
            Assert.IsNotNull(actualOutput);
            Assert.IsNotNull(expectedOutput);

            for (int i = 0; i < count; i++)
                Assert.AreEqual(expectedOutput[i], actualOutput[i]);

            for (int i = count; i < actualOutput.Length; i++)
                Assert.AreEqual(default, actualOutput[i]);
        }
    }

    [TestMethod]
    public void Flush()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.DeflateInit(zStream, Z_DEFAULT_COMPRESSION));

        byte[] compr = GetCompr();
        zStream.Input = s_hello;
        zStream.Output = compr;
        zStream.AvailableIn = 3;
        zStream.AvailableOut = ComprLen;
        Assert.AreEqual(Z_OK, zlib.Deflate(zStream, Z_FULL_FLUSH));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(3, zStream.TotalIn);
        Assert.AreEqual(39989, zStream.AvailableOut);
        Assert.AreEqual(11, zStream.TotalOut);
        Assert.AreEqual(40960314U, zStream.Adler);
        Assert.AreEqual(1, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(3, zStream.NextIn);
        Assert.AreEqual(11, zStream.NextOut);
        Assert.IsTrue(Enumerable.SequenceEqual(compr.Take(11),
            new byte[11] { 120, 156, 202, 72, 205, 1, 0, 0, 0, 255, 255 }));

        int len = s_hello.Length;
        compr[3]++; // force an error in first compressed block
        zStream.AvailableIn = len - 3;

        int err = zlib.Deflate(zStream, Z_FINISH);
        if (err != Z_STREAM_END)
            Assert.AreEqual(Z_OK, err);
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(14, zStream.TotalIn);
        Assert.AreEqual(39972, zStream.AvailableOut);
        Assert.AreEqual(28, zStream.TotalOut);
        Assert.AreEqual(637928598U, zStream.Adler);
        Assert.AreEqual(1, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(14, zStream.NextIn);
        Assert.AreEqual(28, zStream.NextOut);
        Assert.IsTrue(Enumerable.SequenceEqual(compr.Take(28),
            new byte[28] { 120, 156, 202, 73, 205, 1, 0, 0, 0, 255, 255, 203, 201, 215, 81, 200, 72, 205, 201, 201, 87, 100, 0, 0, 38, 6, 4, 150 }));

        Assert.AreEqual(Z_OK, zlib.DeflateEnd(zStream));
    }

    [TestMethod]
    public void DeflateWithPresetDictionary()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.DeflateInit(zStream, Z_BEST_COMPRESSION));

        byte[] dictionary = Encoding.UTF8.GetBytes($"hello{char.MinValue}");
        Assert.AreEqual(Z_OK, zlib.DeflateSetDictionary(zStream, dictionary));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(6, zStream.TotalIn);
        Assert.AreEqual(0, zStream.AvailableOut);
        Assert.AreEqual(0, zStream.TotalOut);
        Assert.AreEqual(138478101U, zStream.Adler);
        Assert.AreEqual(2, zStream.DataType);
        Assert.IsNull(zStream.Message);

        byte[] compr = new byte[ComprLen];
        zStream.Output = compr;
        zStream.AvailableOut = compr.Length;

        zStream.Input = s_hello;
        zStream.AvailableIn = s_hello.Length;

        Assert.AreEqual(Z_STREAM_END, zlib.Deflate(zStream, Z_FINISH), "deflate should report Z_STREAM_END");
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(20, zStream.TotalIn);
        Assert.AreEqual(39980, zStream.AvailableOut);
        Assert.AreEqual(20, zStream.TotalOut);
        Assert.AreEqual(637928598U, zStream.Adler);
        Assert.AreEqual(0, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.IsTrue(Enumerable.SequenceEqual(compr.Take(20),
            new byte[20] { 120, 249, 8, 65, 2, 21, 203, 0, 145, 58, 10, 96, 74, 145, 1, 0, 38, 6, 4, 150 }));

        Assert.AreEqual(Z_OK, zlib.DeflateEnd(zStream));

        Assert.AreEqual(Z_STREAM_ERROR, zlib.DeflateSetDictionary(zStream, default));
    }
}