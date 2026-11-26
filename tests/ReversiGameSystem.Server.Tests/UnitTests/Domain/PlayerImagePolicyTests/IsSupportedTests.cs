using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Domain.PlayerImagePolicyTests;

[TestClass]
[TestCategory("Unit")]
public sealed class IsSupportedTests
{
    private static readonly byte[] ValidPngBytes = { 137, 80, 78, 71, 13, 10, 26, 10, 0 };
    private static readonly byte[] ValidJpegBytes = { 0xFF, 0xD8, 0x01, 0x02, 0xFF, 0xD9 };

    [TestMethod]
    [DataRow(PlayerImagePolicy.PngMediaType)]
    [DataRow("IMAGE/PNG")]
    public void IsSupported_ValidPng_ReturnsTrue(string mediaType)
    {
        bool expectedSupported = true;

        bool actualSupported = PlayerImagePolicy.IsSupported(ValidPngBytes, mediaType);

        Assert.AreEqual(expectedSupported, actualSupported);
    }

    [TestMethod]
    [DataRow(PlayerImagePolicy.JpegMediaType)]
    [DataRow("IMAGE/JPEG")]
    public void IsSupported_ValidJpeg_ReturnsTrue(string mediaType)
    {
        bool expectedSupported = true;

        bool actualSupported = PlayerImagePolicy.IsSupported(ValidJpegBytes, mediaType);

        Assert.AreEqual(expectedSupported, actualSupported);
    }

    [TestMethod]
    public void IsSupported_NullContent_ReturnsFalse()
    {
        bool expectedSupported = false;

        bool actualSupported = PlayerImagePolicy.IsSupported(
            null!,
            PlayerImagePolicy.PngMediaType);

        Assert.AreEqual(expectedSupported, actualSupported);
    }

    [TestMethod]
    public void IsSupported_EmptyContent_ReturnsFalse()
    {
        byte[] content = Array.Empty<byte>();
        bool expectedSupported = false;

        bool actualSupported = PlayerImagePolicy.IsSupported(
            content,
            PlayerImagePolicy.PngMediaType);

        Assert.AreEqual(expectedSupported, actualSupported);
    }

    [TestMethod]
    public void IsSupported_ContentExceedingMaximumFileBytes_ReturnsFalse()
    {
        byte[] content = new byte[PlayerImagePolicy.MaximumFileBytes + 1];
        Array.Copy(ValidPngBytes, content, ValidPngBytes.Length);
        bool expectedSupported = false;

        bool actualSupported = PlayerImagePolicy.IsSupported(
            content,
            PlayerImagePolicy.PngMediaType);

        Assert.AreEqual(expectedSupported, actualSupported);
    }

    [TestMethod]
    [DataRow("image/gif")]
    [DataRow("")]
    [DataRow(null)]
    public void IsSupported_UnsupportedMediaType_ReturnsFalse(string mediaType)
    {
        bool expectedSupported = false;

        bool actualSupported = PlayerImagePolicy.IsSupported(ValidPngBytes, mediaType);

        Assert.AreEqual(expectedSupported, actualSupported);
    }

    [TestMethod]
    public void IsSupported_InvalidPngSignature_ReturnsFalse()
    {
        byte[] content = { 137, 80, 78, 71, 13, 10, 26 };
        bool expectedSupported = false;

        bool actualSupported = PlayerImagePolicy.IsSupported(
            content,
            PlayerImagePolicy.PngMediaType);

        Assert.AreEqual(expectedSupported, actualSupported);
    }

    [TestMethod]
    [DataRow(new byte[] { 0xFF, 0xD8, 0xD9 })]
    [DataRow(new byte[] { 0x00, 0xD8, 0x01, 0xFF, 0xD9 })]
    [DataRow(new byte[] { 0xFF, 0xD8, 0x01, 0xFF, 0x00 })]
    public void IsSupported_InvalidJpegSignature_ReturnsFalse(byte[] content)
    {
        bool expectedSupported = false;

        bool actualSupported = PlayerImagePolicy.IsSupported(
            content,
            PlayerImagePolicy.JpegMediaType);

        Assert.AreEqual(expectedSupported, actualSupported);
    }
}
