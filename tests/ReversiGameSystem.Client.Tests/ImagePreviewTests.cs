using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class ImagePreviewTests
{
    [TestMethod]
    public void CreatePreview_WideImage_CropsCentralSquare()
    {
        byte[] pixels = new byte[6 * 2 * 4];
        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 6; column++)
            {
                int offset = (row * 6 + column) * 4;
                pixels[offset + 1] = column >= 2 && column < 4 ? (byte)255 : (byte)0;
                pixels[offset + 2] = column >= 2 && column < 4 ? (byte)0 : (byte)255;
                pixels[offset + 3] = 255;
            }
        }
        string path = WriteImage(6, 2, pixels);
        try
        {
            BitmapSource? preview = ProfilePicturePolicy.CreatePreview(path);
            Assert.IsNotNull(preview);
            Assert.AreEqual(2, preview.PixelWidth);
            Assert.AreEqual(2, preview.PixelHeight);
            byte[] result = new byte[16];
            preview.CopyPixels(result, 8, 0);
            Assert.AreEqual((byte)0, result[2]);
            Assert.AreEqual((byte)255, result[1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void CreatePreview_TransparentImage_CompositesWhiteWithoutUpscaling()
    {
        string path = WriteImage(3, 5, new byte[3 * 5 * 4]);
        try
        {
            BitmapSource? preview = BoardBackgroundPolicy.CreatePreview(path);
            Assert.IsNotNull(preview);
            Assert.AreEqual(3, preview.PixelWidth);
            Assert.AreEqual(3, preview.PixelHeight);
            byte[] result = new byte[3 * 3 * 4];
            preview.CopyPixels(result, 12, 0);
            foreach (byte component in result)
            {
                Assert.AreEqual((byte)255, component);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void CreatePreview_LargeImage_PreservesPurposeResolution()
    {
        string path = WriteImage(1200, 1100, new byte[1200 * 1100 * 4]);
        try
        {
            BitmapSource? profile = ProfilePicturePolicy.CreatePreview(path);
            BitmapSource? background = BoardBackgroundPolicy.CreatePreview(path);
            Assert.IsNotNull(profile);
            Assert.IsNotNull(background);
            Assert.AreEqual(256, profile.PixelWidth);
            Assert.AreEqual(256, profile.PixelHeight);
            Assert.AreEqual(1024, background.PixelWidth);
            Assert.AreEqual(1024, background.PixelHeight);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task Decode_ReturnedProfile_Preserves256Pixels()
    {
        string path = WriteImage(256, 256, new byte[256 * 256 * 4]);
        try
        {
            PlayerImageContent content = await PlayerImageService.ReadImageFile(path);
            byte[] original = File.ReadAllBytes(path);
            Assert.AreEqual(Convert.ToBase64String(original),
                Convert.ToBase64String(content.Content));
            BitmapSource? preview = PlayerImageService.CreatePreview(content);
            Assert.IsNotNull(preview);
            Assert.AreEqual(256, preview.PixelWidth);
            Assert.AreEqual(256, preview.PixelHeight);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task CreatePreview_OversizedDimensions_RejectsImage()
    {
        string path = WriteImage(4097, 1, new byte[4097 * 4]);
        try
        {
            Assert.IsNull(ProfilePicturePolicy.CreatePreview(path));
            PlayerImageContent content = await PlayerImageService.ReadImageFile(path);
            Assert.ThrowsExactly<InvalidDataException>(() =>
                PlayerImageService.CreatePreview(content));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteImage(int width, int height, byte[] pixels)
    {
        BitmapSource image = BitmapSource.Create(
            width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        using (FileStream stream = File.Create(path))
        {
            encoder.Save(stream);
        }
        return path;
    }
}
