using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using System.Text;
using System.IO;
using ZLibDotNet.Inflate;

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
        Assert.AreEqual(Z_OK, zlib.Uncompress(uncompr, out int destLen, compr, out int sourceLen));
        Assert.AreEqual(19, sourceLen);
        Assert.AreEqual(14, destLen);
        Assert.IsTrue(Enumerable.SequenceEqual(uncompr.TakeWhile(b => b != 0), s_hello), "bad uncompress");

        Assert.AreEqual(Z_STREAM_ERROR, zlib.Uncompress(uncompr, out _, compr, -1));
        Assert.AreEqual(Z_STREAM_ERROR, zlib.Uncompress(uncompr, out _, compr, compr.Length + 1));
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
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream));
        Assert.AreEqual(1U, zStream.Adler);

        int count = 0;
        while (zStream.TotalOut < uncomprLen && zStream.TotalIn < ComprLen)
        {
            count++;
            zStream.AvailableIn = zStream.AvailableOut = 1; // force small buffers
            int err = zlib.Inflate(ref zStream, Z_NO_FLUSH);
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

        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref zStream));
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
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream));

        byte[] uncompr = new byte[ComprLen];
        Array.Copy(s_garbage, uncompr, s_garbage.Length);
        int count = 0;
        for (; ; )
        {
            count++;
            zStream.Output = uncompr; // discard the output
            zStream.AvailableOut = ComprLen;
            int err = zlib.Inflate(ref zStream, Z_NO_FLUSH);
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

        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref zStream));
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
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream));

        const int UncomprLen = ComprLen;
        byte[] uncompr = new byte[UncomprLen];
        Array.Copy(s_garbage, uncompr, s_garbage.Length);

        zStream.Output = uncompr;
        zStream.AvailableOut = UncomprLen;

        Assert.AreEqual(Z_OK, zlib.Inflate(ref zStream, Z_NO_FLUSH));
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
        Assert.AreEqual(Z_OK, zlib.InflateSync(ref zStream));  // but skip the damaged part
        Assert.AreEqual(17, zStream.AvailableIn);
        Assert.AreEqual(11, zStream.TotalIn);
        Assert.AreEqual(40000, zStream.AvailableOut);
        Assert.AreEqual(0, zStream.TotalOut);
        Assert.AreEqual(1U, zStream.Adler);
        Assert.AreEqual(128, zStream.DataType);
        Assert.IsNull(zStream.Message);
        Assert.AreEqual(11, zStream.NextIn);
        Assert.AreEqual(0, zStream.NextOut);

        Assert.AreEqual(Z_STREAM_END, zlib.Inflate(ref zStream, Z_FINISH), $"inflate should report {Z_STREAM_END}");
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

        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref zStream));
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
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream));

        byte[] uncompr = new byte[ComprLen];
        Array.Copy(s_garbage, uncompr, s_garbage.Length);

        zStream.Output = uncompr;
        zStream.AvailableOut = uncompr.Length;

        byte[] dictionary = Encoding.UTF8.GetBytes($"hello{char.MinValue}");
        for (; ; )
        {
            int err = zlib.Inflate(ref zStream, Z_NO_FLUSH);
            if (err == Z_STREAM_END)
                break;
            if (err == Z_NEED_DICT)
            {
                Assert.AreEqual(138478101U, zStream.Adler, "unexpected dictionary");
                err = zlib.InflateSetDictionary(ref zStream, dictionary);
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

        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref zStream));

        Assert.IsTrue(Enumerable.SequenceEqual(uncompr.TakeWhile(b => b != 0), s_hello), "bad inflate with dict");

        Assert.AreEqual(Z_STREAM_ERROR, zlib.InflateSetDictionary(ref zStream, default));
    }

    [TestMethod]
    public void InflateWithTooSmallOutputBuffer()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream));

        zStream.Input = new byte[20] { 120, 156, 203, 72, 205, 201, 201, 215, 81, 200, 0, 81, 138, 12, 0, 38, 6, 4, 150, 0 };
        zStream.AvailableIn = zStream.Input.Length;

        byte[] ouputBuffer = new byte[14];
        zStream.Output = ouputBuffer;
        int outputBufferSize = ouputBuffer.Length - 1;
        zStream.AvailableOut = outputBufferSize;

        Assert.AreEqual(Z_BUF_ERROR, zlib.Inflate(ref zStream, Z_FINISH));
        Assert.AreEqual(outputBufferSize, zStream.TotalOut);
        Assert.AreEqual(0, zStream.AvailableOut);

        zStream.AvailableOut++;
        Assert.AreEqual(Z_STREAM_END, zlib.Inflate(ref zStream, Z_FINISH));
        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref zStream));
    }

    [TestMethod]
    public void InflateWithOffset()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream));

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

        Assert.AreEqual(Z_OK, zlib.Inflate(ref zStream, Z_NO_FLUSH));

        for (int i = 0; i < OutputOffset; i++)
            Assert.AreEqual(default, outputBuffer[i]);
        Assert.AreEqual(104, outputBuffer[OutputOffset]);
    }

    [TestMethod]
    public void InflateInChunks()
    {
        const ushort ChunkSize = 16384;
        byte[] @in = new byte[ChunkSize];
        byte[] @out = new byte[ChunkSize];

        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream));

        const int SourceSize = 60_000;
        byte[] sourceBuffer = new byte[SourceSize];

        using MemoryStream source = new(DeflateTests.DeflateBytesInChunks(sourceBuffer));
        using MemoryStream dest = new();

        int ret = Z_OK, have;
        // decompress until deflate stream ends or end of stream
        do
        {
            zStream.Input = @in;
            zStream.AvailableIn = source.Read(@in, 0, ChunkSize);
            if (zStream.AvailableIn == 0)
                break;

            // run Inflate on input until output buffer not full
            do
            {
                zStream.Output = @out;
                zStream.AvailableOut = ChunkSize;
                ret = zlib.Inflate(ref zStream, Z_NO_FLUSH);
                Assert.AreNotEqual(Z_STREAM_ERROR, ret); // state not clobbered
                switch (ret)
                {
                    case Z_NEED_DICT:
                        ret = Z_DATA_ERROR; // and fall through
                        goto case Z_DATA_ERROR;
                    case Z_DATA_ERROR:
                    case Z_MEM_ERROR:
                        _ = zlib.InflateEnd(ref zStream);
                        Assert.Fail($"{ret}");
                        return;
                }
                have = ChunkSize - zStream.AvailableOut;
                dest.Write(@out, 0, have);
            } while (zStream.AvailableOut == 0);
            // done when Inflate says it's done
        } while (ret != Z_STREAM_END);

        // clean up
        _ = zlib.InflateEnd(ref zStream);
        Assert.AreEqual(Z_STREAM_END, ret);
        Assert.IsTrue(Enumerable.SequenceEqual(sourceBuffer, dest.ToArray()));
    }

    [TestMethod]
    public void RawInflateWithoutHeader()
    {
        ZLib zlib = new();
        byte[] compr = new byte[12] { 203, 72, 205, 201, 201, 215, 81, 200, 0, 81, 138, 0 };
        byte[] uncompr = new byte[13];
        ZStream zStream = new()
        {
            Input = compr,
            Output = uncompr
        };
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream, -15));
        Assert.AreEqual(Z_STREAM_END, zlib.Inflate(ref zStream, Z_FINISH));
        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref zStream));
        Assert.AreEqual(13, zStream.TotalOut);

        Assert.IsTrue(Enumerable.SequenceEqual(uncompr, s_hello));
    }

    [TestMethod]
    public void CoverSupport()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream));
        Assert.AreEqual(Z_OK, zlib.InflatePrime(ref zStream, 5, 31));
        Assert.AreEqual(Z_OK, zlib.InflatePrime(ref zStream, -1, 0));
        Assert.AreEqual(Z_STREAM_ERROR, zlib.InflateSetDictionary(ref zStream, null));
        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref zStream));

        Inf("63 0\0", 0, -15, 1, Z_OK); // force window allocation
        Inf("63 18 5\0", 0, -8, 259, Z_OK); // force window replacement
        Inf("63 18 68 30 d0 0 0\0", 4, -8, 259, Z_OK); // force split window update
        Inf("3 0\0", 0, -15, 1, Z_STREAM_END); // use fixed blocks
        Assert.AreEqual(Z_STREAM_ERROR, zlib.InflateInit(ref zStream, 1)); // bad window size
    }

    [TestMethod]
    public void CoverWrap()
    {
        ZLib zlib = new();
        ZStream zStream = new();
        Assert.AreEqual(Z_STREAM_ERROR, zlib.Inflate(ref zStream, default));
        Assert.AreEqual(Z_STREAM_ERROR, zlib.InflateEnd(ref zStream));
        Assert.AreEqual(Z_STREAM_ERROR, zlib.InflateCopy(ref zStream, ref zStream));
        Assert.AreEqual(Z_STREAM_ERROR, zlib.InflateInit(ref zStream, 31));

        // A pointer to an empty array == null:
        // len = 0 => @out == new byte[0] in Inf => strm.next_out == null in Inflater.Inflate => Z_STREAM_ERROR
        Inf("77 85\0", 0, 15, 1, Z_DATA_ERROR); // bad zlib method
        Inf("8 99\0", 0, 0, 1, Z_OK); // set window size from header
        Inf("78 9c", 0, 8, 1, Z_DATA_ERROR); //bad zlib window size
        Inf("78 9c 63 0 0 0 1 0 1\0", 0, 15, 1, Z_STREAM_END); // check adler32
        Inf("8 b8 0 0 0 1\0", 0, 8, 1, Z_NEED_DICT); // need dictionary
        Inf("78 9c 63 0\0", 0, 15, 1, Z_OK); // compute adler32

        Assert.AreEqual(Z_OK, zlib.InflateInit(ref zStream, -8));

        Assert.AreEqual(Z_OK, zlib.InflateSetDictionary(ref zStream, new byte[257]));

        zStream.Input = new byte[2] { 99, 0 };
        zStream.Output = new byte[1];
        Assert.AreEqual(Z_OK, zlib.InflatePrime(ref zStream, 16, 0));

        zStream.Input = new byte[2] { 128, 0 };
        Assert.AreEqual(Z_DATA_ERROR, zlib.InflateSync(ref zStream));
        Assert.AreEqual(Z_STREAM_ERROR, zlib.Inflate(ref zStream, Z_NO_FLUSH));

        zStream.Input = new byte[4] { 0, 0, 255, 255 };
        Assert.AreEqual(Z_OK, zlib.InflateSync(ref zStream));

        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref zStream));
    }

    [TestMethod]
    public void CoverInflate()
    {
        Try("0 0 0 0 0\0", "invalid stored block lengths", true);
        Try("3 0\0", "fixed", false);
        Try("6\0", "invalid block type", true);
        Try("1 1 0 fe ff 0\0", "stored", false);
        Try("fc 0 0\0", "too many length or distance symbols", true);
        Try("4 0 fe ff\0", "invalid code lengths set", true);
        Try("4 0 24 49 0\0", "invalid bit length repeat", true);
        Try("4 0 24 e9 ff ff\0", "invalid bit length repeat", true);
        Try("4 0 24 e9 ff 6d\0", "invalid code -- missing end-of-block", true);
        Try("4 80 49 92 24 49 92 24 71 ff ff 93 11 0\0", "invalid literal/lengths set", true);
        Try("4 80 49 92 24 49 92 24 f b4 ff ff c3 84\0", "invalid distances set", true);
        Try("4 c0 81 8 0 0 0 0 20 7f eb b 0 0\0", "invalid literal/length code", true);
        Try("2 7e ff ff\0", "invalid distance code", true);
        Try("c c0 81 0 0 0 0 0 90 ff 6b 4 0\0", "invalid distance too far back", true);
        Try("5 c0 21 d 0 0 0 80 b0 fe 6d 2f 91 6c\0", "pull 17", false);
        Try("5 e0 81 91 24 cb b2 2c 49 e2 f 2e 8b 9a 47 56 9f fb fe ec d2 ff 1f\0", "long code", false);
        Try("ed c0 1 1 0 0 0 40 20 ff 57 1b 42 2c 4f\0", "length extra", false);
        Try("ed cf c1 b1 2c 47 10 c4 30 fa 6f 35 1d 1 82 59 3d fb be 2e 2a fc f c\0", "long distance and extra", false);
        Try("ed c0 81 0 0 0 0 80 a0 fd a9 17 a9 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 6\0", "window end", false);
        Inf("2 8 20 80 0 3 0\0", 0, -15, 258, Z_STREAM_END); // inflate_fast TYPE return
        Inf("63 18 5 40 c 0\0", 3, -8, 300, Z_OK); // window wrap
    }

    [TestMethod]
    public void CoverTrees()
    {
        ushort[] lens = new ushort[16];
        ushort[] work = new ushort[16];
        Code[] table = new Code[Inflater.EnoughDists];

        int bits;
        for (bits = 0; bits < 15; bits++)
            lens[bits] = (ushort)(bits + 1);
        lens[15] = 15;
        bits = 15;

        ref ushort ptrToLens = ref lens[0];
        ref ushort ptrToWork = ref work[0];
        ref Code ptrToTable = ref table[0];

        // Call InflateTable() directly in order to manifest not-enough errors, since zlib ensures that enough is always enough.
        ref Code next = ref ptrToTable;
        uint offset = 0;
        Assert.AreEqual(1, Inflater.InflateTable(CodeType.Dists, ref ptrToLens, 16, ref next, ref bits, ref ptrToWork, ref offset));

        next = ptrToTable;
        bits = 1;
        Assert.AreEqual(1, Inflater.InflateTable(CodeType.Dists, ref ptrToLens, 16, ref next, ref bits, ref ptrToWork, ref offset));
    }

    [TestMethod]
    public void CoverFast()
    {
        Inf("e5 e0 81 ad 6d cb b2 2c c9 01 1e 59 63 ae 7d ee fb 4d fd b5 35 41 68 ff 7f 0f 0 0 0\0", 0, -8, 258, Z_DATA_ERROR); // fast length extra bits
        Inf("25 fd 81 b5 6d 59 b6 6a 49 ea af 35 6 34 eb 8c b9 f6 b9 1e ef 67 49 50 fe ff ff 3f 0 0\0", 0, -8, 258, Z_DATA_ERROR); // fast distance extra bits
        Inf("3 7e 0 0 0 0 0\0", 0, -8, 258, Z_DATA_ERROR); // fast invalid distance code
        Inf("1b 7 0 0 0 0 0\0", 0, -8, 258, Z_DATA_ERROR); // fast invalid literal/length code
        Inf("d c7 1 ae eb 38 c 4 41 a0 87 72 de df fb 1f b8 36 b1 38 5d ff ff 0\0", 0, -8, 258, Z_DATA_ERROR); // fast 2nd level codes and too far back
        Inf("63 18 5 8c 10 8 0 0 0 0\0", 0, -8, 259, Z_OK); // very common case
        Inf("63 60 60 18 c9 0 8 18 18 18 26 c0 28 0 29 0 0 0\0", 6, -8, 259, Z_OK); // contiguous and wrap around window
        Inf("63 0 3 0 0 0 0 0", 0, -8, 259, Z_STREAM_END); // copy direct from output
    }

    private static void Inf(string hex, int step, int win, uint len, int err)
    {
        ZLib zlib = new();
        ZStream strm = new();
        ZStream copy = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref strm, win));

        byte[] @out = new byte[len];
        int have = default;
        byte[] @in = H2B(hex, ref have);
        Assert.IsNotNull(@in);
        if (step == 0 || step > have)
            step = have;
        have -= step;
        strm.Input = @in;
        strm.AvailableIn = step;
        do
        {
            strm.Output = @out;
            strm.AvailableOut = (int)len;
            int ret = zlib.Inflate(ref strm, Z_NO_FLUSH);
            Assert.IsTrue(err == 9 || ret == err);
            if (ret != Z_OK && ret != Z_BUF_ERROR && ret != Z_NEED_DICT)
                break;
            if (ret == Z_NEED_DICT)
            {
                Assert.AreEqual(Z_DATA_ERROR, zlib.InflateSetDictionary(ref strm, @in, 1));
                strm.inflateState.mode = InflateMode.Dict;
                Assert.AreEqual(Z_OK, zlib.InflateSetDictionary(ref strm, @out, 0));
                Assert.AreEqual(Z_BUF_ERROR, zlib.Inflate(ref strm, Z_NO_FLUSH));
            }
            Assert.AreEqual(Z_OK, zlib.InflateCopy(ref strm, ref copy));
            Assert.AreEqual(Z_OK, zlib.InflateEnd(ref copy));
            err = 9; // don't care next time around
            have += strm.AvailableIn;
            strm.AvailableIn = step > have ? have : step;
            have -= strm.AvailableIn;
        } while (strm.AvailableIn > 0);
        Assert.AreEqual(Z_OK, zlib.InflateReset(ref strm, -8));
        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref strm));
    }

    private static byte[] H2B(string hex, ref int len)
    {
        byte[] @in = new byte[(hex.Length + 1) >> 1];
        int next = 0;
        uint val = 1;
        for (int i = 0; i < hex.Length; i++) // go through the characters in the string
        {
            char c = hex[i];
            if (c >= '0' && c <= '9')
                val = (val << 4) + c - '0';
            else if (c >= 'A' && c <= 'F')
                val = (val << 4) + c - 'A' + 10;
            else if (c >= 'a' && c <= 'f')
                val = (val << 4) + c - 'a' + 10;
            else if (val != 1 && val < 32) // one digit followed by delimiter
                val += 240; // make it look like two digits
            if (val > 255) // have two digits
            {
                @in[next++] = (byte)(val & 0xff); // save the decoded byte
                val = 1; // start over
            }
        }
        len = next;

        Array.Resize(ref @in, next);
        return @in;
    }

    private static void Try(string hex, string id, bool err)
    {
        // convert to hex
        int len = default;
        byte[] @in = H2B(hex, ref len);
        Assert.IsNotNull(@in);

        int size = len << 3;
        byte[] @out = new byte[size];
        Assert.IsNotNull(@out);

        // inflate
        ZLib zlib = new();
        ZStream strm = new();
        Assert.AreEqual(Z_OK, zlib.InflateInit(ref strm, -15));
        strm.Input = @in;
        strm.AvailableIn = len;
        int ret;
        do
        {
            strm.Output = @out;
            strm.AvailableOut = size;
            ret = zlib.Inflate(ref strm, Z_TREES);
            Assert.IsTrue(ret != Z_STREAM_ERROR && ret != Z_MEM_ERROR);
            if (ret == Z_DATA_ERROR || ret == Z_NEED_DICT)
                break;
        } while (strm.AvailableIn > 0 || strm.AvailableOut == 0);
        if (err)
        {
            Assert.AreEqual(Z_DATA_ERROR, ret);
            Assert.AreEqual(id, strm.Message);
        }
        Assert.AreEqual(Z_OK, zlib.InflateEnd(ref strm));
    }
}