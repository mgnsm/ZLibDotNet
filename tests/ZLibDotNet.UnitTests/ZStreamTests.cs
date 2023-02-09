using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ZLibDotNet.UnitTests;

[TestClass]
public class ZStreamTests
{
    [TestMethod]
    public void InitializeZStream()
    {
        ZStream zStream = new();
        Assert.IsTrue(zStream.Input == default);
        Assert.AreEqual(default, zStream.NextIn);
        Assert.AreEqual(default, zStream.AvailableIn);
        Assert.AreEqual(default, zStream.TotalIn);
        Assert.IsTrue(zStream.Output == default);
        Assert.AreEqual(default, zStream.NextOut);
        Assert.AreEqual(default, zStream.AvailableOut);
        Assert.AreEqual(default, zStream.TotalOut);
        Assert.AreEqual(default, zStream.Message);
        Assert.AreEqual(default, zStream.DataType);
        Assert.AreEqual(default, zStream.Adler);
    }

    [TestMethod]
    public void InitializeZStreamInputBuffer()
    {
        ZStream zStream = new();
        byte[] buffer = new byte[10];
        zStream.Input = buffer;
        Assert.IsTrue(zStream.Input == buffer); // Verify that the property was set.
        Assert.IsTrue(zStream.Output == default);
        Assert.AreEqual(default, zStream.NextIn);
        Assert.AreEqual(buffer.Length, zStream.AvailableIn); // Verify that the AvailableIn property was set to the length of the buffer.

        try
        {
            zStream.AvailableIn++;
            Assert.Fail($"{nameof(zStream.AvailableIn)} should throw when set to a value that is greater than the length of the buffer.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(buffer.Length, zStream.AvailableIn);
        }

        // Decreasing the AvailableIn property should be fine as long as the value > 0.
        for (int i = 0; i < buffer.Length; i++)
            Assert.AreEqual(buffer.Length - i, zStream.AvailableIn--);
        Assert.AreEqual(0, zStream.AvailableIn);

        try
        {
            zStream.AvailableIn--;
            Assert.Fail($"{nameof(zStream.AvailableIn)} should throw when set to a negative value.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(0, zStream.AvailableIn);
        }

        // NextIn can be set to a value between 0 and buffer.Length - 1.
        try
        {
            zStream.NextIn--;
            Assert.Fail($"{nameof(zStream.NextIn)} should throw when set to a negative value.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(default, zStream.NextIn);
        }

        for (int i = 1; i < buffer.Length; i++)
            Assert.AreEqual(i, ++zStream.NextIn);
        Assert.AreEqual(buffer.Length - 1, zStream.NextIn);

        try
        {
            zStream.NextIn++;
            Assert.Fail($"{nameof(zStream.NextIn)} should throw when set to a value that is equal to or greater than the length of the buffer.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(buffer.Length - 1, zStream.NextIn);
        }

        Assert.AreEqual(1, ++zStream.AvailableIn);
        try
        {
            zStream.AvailableIn++;
            Assert.Fail($"{nameof(zStream.AvailableIn)} should throw when set to a value greater than the length of the buffer minus the value of the {nameof(ZStream.NextIn)} property.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(1, zStream.AvailableIn);
        }

        Assert.AreEqual(8, --zStream.NextIn);
        Assert.AreEqual(2, ++zStream.AvailableIn);

        try
        {
            zStream.NextIn++;
            Assert.Fail($"{nameof(zStream.NextIn)} should throw when set to a value that is not within the range of available bytes.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(8, zStream.NextIn);
        }

        // Ensure that the NextIn and AvailableIn properties are reset when re-setting the Input property.
        buffer = new byte[11];
        zStream.Input = buffer;
        Assert.IsTrue(zStream.Input == buffer);
        Assert.AreEqual(default, zStream.NextIn);
        Assert.AreEqual(buffer.Length, zStream.AvailableIn);
    }

    [TestMethod]
    public void InitializeZStreamOutputBuffer()
    {
        ZStream zStream = new();
        byte[] buffer = new byte[11];
        zStream.Output = buffer;
        Assert.IsTrue(zStream.Output == buffer); // Verify that the property was set.
        Assert.IsTrue(zStream.Input == default);
        Assert.AreEqual(default, zStream.NextOut);
        Assert.AreEqual(buffer.Length, zStream.AvailableOut); // Verify that the AvailableOut property was set to the length of the buffer.

        try
        {
            zStream.AvailableOut++;
            Assert.Fail($"{nameof(zStream.AvailableOut)} should throw when set to a value that is greater than the length of the buffer.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(buffer.Length, zStream.AvailableOut);
        }

        // Decreasing the AvailableOut property should be fine as long as the value > 0.
        for (int i = 0; i < buffer.Length; i++)
            Assert.AreEqual(buffer.Length - i, zStream.AvailableOut--);
        Assert.AreEqual(0, zStream.AvailableOut);

        try
        {
            zStream.AvailableOut--;
            Assert.Fail($"{nameof(zStream.AvailableOut)} should throw when set to a negative value.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(0, zStream.AvailableOut);
        }

        // NextOut can be set to a value between 0 and buffer.Length - 1.
        try
        {
            zStream.NextOut--;
            Assert.Fail($"{nameof(zStream.NextOut)} should throw when set to a negative value.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(default, zStream.NextOut);
        }

        for (int i = 1; i < buffer.Length; i++)
            Assert.AreEqual(i, ++zStream.NextOut);
        Assert.AreEqual(buffer.Length - 1, zStream.NextOut);

        try
        {
            zStream.NextOut++;
            Assert.Fail($"{nameof(zStream.NextOut)} should throw when set to a value that is equal to or greater than the length of the buffer.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(buffer.Length - 1, zStream.NextOut);
        }

        Assert.AreEqual(1, ++zStream.AvailableOut);
        try
        {
            zStream.AvailableOut++;
            Assert.Fail($"{nameof(zStream.AvailableOut)} should throw when set to a value greater than the length of the buffer minus the value of the {nameof(ZStream.NextOut)} property.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(1, zStream.AvailableOut);
        }

        Assert.AreEqual(9, --zStream.NextOut);
        Assert.AreEqual(2, ++zStream.AvailableOut);

        try
        {
            zStream.NextOut++;
            Assert.Fail($"{nameof(zStream.NextOut)} should throw when set to a value that is not within the range of available bytes.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Assert.AreEqual(9, zStream.NextOut);
        }

        // Ensure that the NextOut and AvailableOut properties are reset when re-setting the Output property.
        buffer = new byte[12];
        zStream.Output = buffer;
        Assert.IsTrue(zStream.Output == buffer);
        Assert.AreEqual(default, zStream.NextOut);
        Assert.AreEqual(buffer.Length, zStream.AvailableOut);
    }
}