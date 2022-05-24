using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using System.Text;

namespace ZLibDotNet.UnitTests;

[TestClass]
public class InflateTests
{
    private static readonly byte[] s_hello = Encoding.ASCII.GetBytes("hello, hello!");
    private static readonly byte[] s_garbage = Encoding.ASCII.GetBytes("garbage");

    [TestMethod]
    public void Uncompress()
    {
        byte[] compr = new byte[19] { 120, 156, 203, 72, 205, 201, 201, 215, 81, 200, 0, 81, 138, 12, 0, 38, 6, 4, 150 };
        Array.Resize(ref compr, ComprLen);

        byte[] uncompr = new byte[ComprLen];
        Array.Copy(s_garbage, uncompr, s_garbage.Length);

        ZLib zlib = new();
        Assert.AreEqual(Z_OK, zlib.Uncompress(compr, uncompr, out uint sourceLen, out uint destLen));
        Assert.AreEqual(19U, sourceLen);
        Assert.AreEqual(14U, destLen);
        Assert.IsTrue(Enumerable.SequenceEqual(uncompr.TakeWhile(b => b != 0), s_hello), "bad uncompress");
    }

    [TestMethod]
    public void InflateWithSmallBuffers()
    {
        int uncomprLen = ComprLen;
        byte[] uncompr = new byte[uncomprLen];
        Array.Copy(s_garbage, uncompr, s_garbage.Length);

        ZStream zStream = new()
        {
            Input = new byte[20] { 120, 156, 203, 72, 205, 201, 201, 215, 81, 200, 0, 81, 138, 12, 0, 38, 6, 4, 150, 0 },
            AvailableIn = 0,
            Output = uncompr
        };

        ZLib zlib = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(zStream));
        Assert.AreEqual(1U, zStream.Adler);

        int count = 0;
        while (zStream.TotalOut < uncomprLen && zStream.TotalIn < ComprLen)
        {
            count++;
            zStream.AvailableIn = zStream.AvailableOut = 1; // force small buffers
            int err = zlib.Inflate(zStream, Z_NO_FLUSH);
            if (err == Z_STREAM_END)
                break;
            Assert.AreEqual(Z_OK, err);
        }
        Assert.AreEqual(21, count);
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(19, zStream.TotalIn);
        Assert.AreEqual(1, zStream.AvailableOut);
        Assert.AreEqual(14, zStream.TotalOut);
        Assert.AreEqual(637928598U, zStream.Adler);
        Assert.AreEqual(64, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(19, zStream.NextIn);
        Assert.AreEqual(14, zStream.NextOut);

        Assert.AreEqual(Z_OK, zlib.InflateEnd(zStream));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(19, zStream.TotalIn);
        Assert.AreEqual(1, zStream.AvailableOut);
        Assert.AreEqual(14, zStream.TotalOut);
        Assert.AreEqual(637928598U, zStream.Adler);
        Assert.AreEqual(64, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(19, zStream.NextIn);
        Assert.AreEqual(14, zStream.NextOut);
        Assert.IsTrue(Enumerable.SequenceEqual(uncompr.TakeWhile(b => b != 0), s_hello), "bad inflate");
    }

    [TestMethod]
    public void InflateWithLargeBuffers()
    {
        byte[] input = GetCompr();

        ZStream zStream = new()
        {
            Input = input,
            AvailableIn = input.Length
        };

        ZLib zlib = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(zStream));

        byte[] uncompr = new byte[ComprLen];
        Array.Copy(s_garbage, uncompr, s_garbage.Length);
        int count = 0;
        for (; ; )
        {
            count++;
            zStream.Output = uncompr; // discard the output
            zStream.AvailableOut = ComprLen;
            int err = zlib.Inflate(zStream, Z_NO_FLUSH);
            if (err == Z_STREAM_END)
                break;
            Assert.AreEqual(Z_OK, err, "large inflate");
        }
        Assert.AreEqual(3, count);
        Assert.AreEqual(19712, zStream.AvailableIn);
        Assert.AreEqual(20288, zStream.TotalIn);
        Assert.AreEqual(20000, zStream.AvailableOut);
        Assert.AreEqual(100000, zStream.TotalOut);
        Assert.AreEqual(1497247427U, zStream.Adler);
        Assert.AreEqual(64, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(20288, zStream.NextIn);

        Assert.AreEqual(Z_OK, zlib.InflateEnd(zStream));
        Assert.AreEqual(zStream.TotalOut, 2 * ComprLen + ComprLen / 2);
    }

    [TestMethod]
    public void InflateSync()
    {
        byte[] compr = new byte[28] { 120, 156, 202, 73, 205, 1, 0, 0, 0, 255, 255, 203, 201, 215, 81, 200, 72, 205, 201, 201, 87, 100, 0, 0, 38, 6, 4, 150 };
        Array.Resize(ref compr, ComprLen);

        ZStream zStream = new()
        {
            Input = compr,
            AvailableIn = 2 // just read the zlib header
        };

        ZLib zlib = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(zStream));

        const int UncomprLen = ComprLen;
        byte[] uncompr = new byte[UncomprLen];
        Array.Copy(s_garbage, uncompr, s_garbage.Length);

        zStream.Output = uncompr;
        zStream.AvailableOut = UncomprLen;

        Assert.AreEqual(Z_OK, zlib.Inflate(zStream, Z_NO_FLUSH));
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(2, zStream.TotalIn);
        Assert.AreEqual(40000, zStream.AvailableOut);
        Assert.AreEqual(0, zStream.TotalOut);
        Assert.AreEqual(1U, zStream.Adler);
        Assert.AreEqual(128, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(2, zStream.NextIn);
        Assert.AreEqual(0, zStream.NextOut);

        zStream.AvailableIn = 28 - 2; // read all compressed data
        Assert.AreEqual(Z_OK, zlib.InflateSync(zStream));  // but skip the damaged part
        Assert.AreEqual(17, zStream.AvailableIn);
        Assert.AreEqual(11, zStream.TotalIn);
        Assert.AreEqual(40000, zStream.AvailableOut);
        Assert.AreEqual(0, zStream.TotalOut);
        Assert.AreEqual(1U, zStream.Adler);
        Assert.AreEqual(128, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(11, zStream.NextIn);
        Assert.AreEqual(0, zStream.NextOut);

        Assert.AreEqual(Z_STREAM_END, zlib.Inflate(zStream, Z_FINISH), $"inflate should report {Z_STREAM_END}");
        Assert.AreEqual(0, zStream.AvailableIn);
        Assert.AreEqual(28, zStream.TotalIn);
        Assert.AreEqual(39989, zStream.AvailableOut);
        Assert.AreEqual(11, zStream.TotalOut);
        Assert.AreEqual(1U, zStream.Adler);
        Assert.AreEqual(64, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(28, zStream.NextIn);
        Assert.AreEqual(11, zStream.NextOut);
        Assert.IsTrue(Enumerable.SequenceEqual(uncompr.Take(11),
            new byte[11] { 108, 111, 44, 32, 104, 101, 108, 108, 111, 33, 0 }));

        Assert.AreEqual(Z_OK, zlib.InflateEnd(zStream));
    }

    [TestMethod]
    public void InflateWithPresetDictionary()
    {
        ZStream zStream = new();
        byte[] compr = new byte[ComprLen];
        Array.Copy(new byte[20] { 120, 249, 8, 65, 2, 21, 203, 0, 145, 58, 10, 96, 74, 145, 1, 0, 38, 6, 4, 150 },
            compr, 20);
        zStream.Input = compr;
        zStream.AvailableIn = compr.Length;

        ZLib zlib = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(zStream));

        byte[] uncompr = new byte[ComprLen];
        Array.Copy(s_garbage, uncompr, s_garbage.Length);

        zStream.Output = uncompr;
        zStream.AvailableOut = uncompr.Length;

        byte[] dictionary = Encoding.UTF8.GetBytes($"hello{char.MinValue}");
        for (; ; )
        {
            int err = zlib.Inflate(zStream, Z_NO_FLUSH);
            if (err == Z_STREAM_END)
                break;
            if (err == Z_NEED_DICT)
            {
                Assert.AreEqual(138478101U, zStream.Adler, "unexpected dictionary");
                err = zlib.InflateSetDictionary(zStream, dictionary);
            }
            Assert.AreEqual(Z_OK, err, "inflate with dict");
        }
        Assert.AreEqual(39980, zStream.AvailableIn);
        Assert.AreEqual(14, zStream.TotalIn);
        Assert.AreEqual(39986, zStream.AvailableOut);
        Assert.AreEqual(14, zStream.TotalOut);
        Assert.AreEqual(637928598U, zStream.Adler);
        Assert.AreEqual(64, zStream.DataType);
        Assert.IsNull(zStream.Message);

        Assert.AreEqual(Z_OK, zlib.InflateEnd(zStream));

        Assert.IsTrue(Enumerable.SequenceEqual(uncompr.TakeWhile(b => b != 0), s_hello), "bad inflate with dict");

        Assert.AreEqual(Z_STREAM_ERROR, zlib.InflateSetDictionary(zStream, default));
    }

    [TestMethod]
    public void InflateWithTooSmallOutputBuffer()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(zStream));

        zStream.Input = new byte[20] { 120, 156, 203, 72, 205, 201, 201, 215, 81, 200, 0, 81, 138, 12, 0, 38, 6, 4, 150, 0 };
        zStream.AvailableIn = zStream.Input.Length;

        byte[] ouputBuffer = new byte[14];
        zStream.Output = ouputBuffer;
        int outputBufferSize = ouputBuffer.Length - 1;
        zStream.AvailableOut = outputBufferSize;

        Assert.AreEqual(Z_BUF_ERROR, zlib.Inflate(zStream, Z_FINISH));
        Assert.AreEqual(outputBufferSize, zStream.TotalOut);
        Assert.AreEqual(0, zStream.AvailableOut);

        zStream.AvailableOut++;
        Assert.AreEqual(Z_STREAM_END, zlib.Inflate(zStream, Z_FINISH));
        Assert.AreEqual(Z_OK, zlib.InflateEnd(zStream));
    }

    [TestMethod]
    public void InflateWithOffset()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(zStream));

        byte[] inputBuffer = new byte[9] { 0, 0, 120, 156, 203, 72, 205, 201, 201 };
        const int InputOffset = 2;
        zStream.Input = inputBuffer;
        zStream.AvailableIn = inputBuffer.Length - InputOffset;
        zStream.NextIn = InputOffset;

        byte[] outputBuffer = new byte[10];
        const int OutputOffset = 5;
        zStream.Output = outputBuffer;
        zStream.AvailableOut = outputBuffer.Length - OutputOffset;
        zStream.NextOut = OutputOffset;

        Assert.AreEqual(Z_OK, zlib.Inflate(zStream, Z_NO_FLUSH));

        for (int i = 0; i < OutputOffset; i++)
            Assert.AreEqual(default, outputBuffer[i]);
        Assert.AreEqual(104, outputBuffer[OutputOffset]);
    }
}