using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.Persistence.Players;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[TestCategory("Integration")]
public sealed class PlayerImageRepositoryIntegrationTests
{
    private const string PngMediaType = "image/png";
    private const string JpegMediaType = "image/jpeg";
    private const string ProfilePrefix = "profile-images/";
    private const string MinimalPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwC"
        + "AAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private string _root = string.Empty;
    private PlayerImageRepository _repository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            "ReversiGameSystem.Tests",
            Guid.NewGuid().ToString("N"));
        _repository = new PlayerImageRepository(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [TestMethod]
    public void SaveImage_DetailedBackgroundAboveSoftTarget_AcceptsImage()
    {
        using Bitmap source = new Bitmap(1024, 1024, PixelFormat.Format24bppRgb);
        using MemoryStream input = new MemoryStream();
        BitmapData pixels = source.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format24bppRgb);
        try
        {
            byte[] noise = new byte[pixels.Stride * pixels.Height];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(noise);
            }

            Marshal.Copy(noise, 0, pixels.Scan0, noise.Length);
        }
        finally
        {
            source.UnlockBits(pixels);
        }

        source.Save(input, ImageFormat.Png);
        ProfileStatus expectedStatus = ProfileStatus.Success;
        int expectedMinimumStoredBytes = 512 * 1024;

        ProfileResult<string> saved = _repository.SaveImage(
            new ProfileImageData(input.ToArray(), PngMediaType),
            PlayerImageKind.BoardBackground);
        ProfileImageData stored = _repository.ReadImage(TestAssert.IsNotNull(saved.Value));
        ProfileStatus actualStatus = saved.Status;
        string actualMediaType = stored.MediaType;
        bool actualExceedsMinimum = stored.Content.Length > expectedMinimumStoredBytes;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(JpegMediaType, actualMediaType);
        Assert.IsTrue(actualExceedsMinimum);
    }

    [TestMethod]
    public void SaveImage_WideTransparentImage_CropsCenterAndCompositesWhite()
    {
        using Bitmap source = new Bitmap(600, 300);
        using Graphics graphics = Graphics.FromImage(source);
        using MemoryStream input = new MemoryStream();
        graphics.Clear(Color.Transparent);
        graphics.FillRectangle(Brushes.Red, 0, 0, 150, 300);
        graphics.FillRectangle(Brushes.Blue, 450, 0, 150, 300);
        graphics.FillRectangle(Brushes.Green, 250, 100, 100, 100);
        source.Save(input, ImageFormat.Png);
        int expectedSize = 256;

        ProfileResult<string> saved = _repository.SaveImage(
            new ProfileImageData(input.ToArray(), PngMediaType),
            PlayerImageKind.Profile);
        string reference = TestAssert.IsNotNull(saved.Value);
        ProfileImageData stored = _repository.ReadImage(reference);
        using MemoryStream output = new MemoryStream(stored.Content);
        using Bitmap decoded = new Bitmap(output);
        Color corner = decoded.GetPixel(20, 20);
        Color center = decoded.GetPixel(128, 128);
        bool actualIsWhiteCorner = corner.R > 240 && corner.G > 240 && corner.B > 240;
        bool actualIsGreenCenter = center.G > center.R + 50 && center.G > center.B + 50;

        Assert.AreEqual(JpegMediaType, stored.MediaType);
        Assert.IsTrue(reference.EndsWith(".jpg", StringComparison.Ordinal));
        Assert.AreEqual(expectedSize, decoded.Width);
        Assert.AreEqual(expectedSize, decoded.Height);
        Assert.IsTrue(actualIsWhiteCorner);
        Assert.IsTrue(actualIsGreenCenter);
    }

    [TestMethod]
    [DataRow(40, 40)]
    [DataRow(1200, 1024)]
    public void SaveImage_Background_ResizesAndDoesNotUpscale(
        int sourceDimension,
        int expectedDimension)
    {
        using Bitmap source = new Bitmap(sourceDimension, sourceDimension);
        using MemoryStream input = new MemoryStream();
        source.Save(input, ImageFormat.Png);

        ProfileResult<string> saved = _repository.SaveImage(
            new ProfileImageData(input.ToArray(), PngMediaType),
            PlayerImageKind.BoardBackground);
        ProfileImageData stored = _repository.ReadImage(TestAssert.IsNotNull(saved.Value));
        using MemoryStream output = new MemoryStream(stored.Content);
        using Bitmap decoded = new Bitmap(output);
        int actualWidth = decoded.Width;
        int actualHeight = decoded.Height;

        Assert.AreEqual(expectedDimension, actualWidth);
        Assert.AreEqual(expectedDimension, actualHeight);
    }

    [TestMethod]
    [DataRow(".png", PngMediaType)]
    [DataRow(".jpg", JpegMediaType)]
    public void ReadImage_LegacyFormats_PreservesMediaType(
        string extension,
        string expectedMediaType)
    {
        Directory.CreateDirectory(_root);
        string name = Guid.NewGuid().ToString("N") + extension;
        ImageFormat format = extension == ".jpg" ? ImageFormat.Jpeg : ImageFormat.Png;
        using (Bitmap image = new Bitmap(30, 20))
        {
            image.Save(Path.Combine(_root, name), format);
        }

        ProfileImageData stored = _repository.ReadImage(ProfilePrefix + name);
        string actualMediaType = stored.MediaType;

        Assert.AreEqual(expectedMediaType, actualMediaType);
    }

    [TestMethod]
    public void DeleteImage_TraversalReference_ThrowsIOExceptionAndPreservesFile()
    {
        Directory.CreateDirectory(_root);
        string name = Guid.NewGuid().ToString("N") + ".jpg";
        string path = Path.Combine(_root, name);
        File.WriteAllBytes(path, new byte[] { 1 });
        bool expectedExists = true;
        Action action = () => _repository.DeleteImage(ProfilePrefix + "../" + name);

        Assert.Throws<IOException>(action);
        bool actualExists = File.Exists(path);

        Assert.AreEqual(expectedExists, actualExists);
    }

    [TestMethod]
    public void SaveAndDelete_ValidImage_UsesRelativeUriAndRemovesFile()
    {
        byte[] imageContent = Convert.FromBase64String(MinimalPngBase64);
        ProfileStatus expectedStatus = ProfileStatus.Success;
        int expectedSavedCount = 1;
        int expectedDeletedCount = 0;

        ProfileResult<string> saved = _repository.SaveImage(
            new ProfileImageData(imageContent, PngMediaType),
            PlayerImageKind.Profile);
        string imageUri = TestAssert.IsNotNull(saved.Value);
        int actualSavedCount = Directory.GetFiles(_root).Length;
        _repository.DeleteImage(imageUri);
        int actualDeletedCount = Directory.GetFiles(_root).Length;

        Assert.AreEqual(expectedStatus, saved.Status);
        Assert.IsTrue(imageUri.StartsWith(ProfilePrefix, StringComparison.Ordinal));
        Assert.AreEqual(expectedSavedCount, actualSavedCount);
        Assert.AreEqual(expectedDeletedCount, actualDeletedCount);
    }

    [TestMethod]
    public void SaveImage_InvalidBytes_ReturnsInvalidImageWithoutSavingFile()
    {
        ProfileStatus expectedStatus = ProfileStatus.InvalidImage;
        bool expectedRootExists = false;

        ProfileResult<string> actualResult = _repository.SaveImage(
            new ProfileImageData(new byte[] { 1, 2 }, PngMediaType),
            PlayerImageKind.Profile);
        ProfileStatus actualStatus = actualResult.Status;
        string? actualValue = actualResult.Value;
        bool actualRootExists = Directory.Exists(_root);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNull(actualValue);
        Assert.AreEqual(expectedRootExists, actualRootExists);
    }
}
