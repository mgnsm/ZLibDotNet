using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZLibDotNet.UnitTests;

[TestClass]
public class Adler32Tests
{
    [TestMethod]
    public void Adler32()
    {
        ZLib zlib = new();
        Assert.AreEqual(1U, zlib.Adler32(default, default));
        Assert.AreEqual(1U, zlib.Adler32(default, default, default));
        _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => zlib.Adler32(default, default, -1));
        _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => zlib.Adler32(default, default, 1));
    }
}