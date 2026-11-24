using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Client.Tests;

internal static class ImagePreviewTests
{
    internal static void CreatePreview_WideImage_CropsCentralSquare()
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
            BitmapSource preview = TestAssert.IsNotNull(ProfilePicturePolicy.CreatePreview(path));
            TestAssert.AreEqual(2, preview.PixelWidth);
            TestAssert.AreEqual(2, preview.PixelHeight);
            byte[] result = new byte[16];
            preview.CopyPixels(result, 8, 0);
            TestAssert.AreEqual((byte)0, result[2]);
            TestAssert.AreEqual((byte)255, result[1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    internal static void CreatePreview_TransparentImage_CompositesWhiteWithoutUpscaling()
    {
        string path = WriteImage(3, 5, new byte[3 * 5 * 4]);
        try
        {
            BitmapSource preview = TestAssert.IsNotNull(BoardBackgroundPolicy.CreatePreview(path));
            TestAssert.AreEqual(3, preview.PixelWidth);
            TestAssert.AreEqual(3, preview.PixelHeight);
            byte[] result = new byte[3 * 3 * 4];
            preview.CopyPixels(result, 12, 0);
            foreach (byte component in result)
            {
                TestAssert.AreEqual((byte)255, component);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    internal static void CreatePreview_LargeImage_PreservesPurposeResolution()
    {
        string path = WriteImage(1200, 1100, new byte[1200 * 1100 * 4]);
        try
        {
            BitmapSource profile = TestAssert.IsNotNull(ProfilePicturePolicy.CreatePreview(path));
            BitmapSource background = TestAssert.IsNotNull(
                BoardBackgroundPolicy.CreatePreview(path));
            TestAssert.AreEqual(256, profile.PixelWidth);
            TestAssert.AreEqual(256, profile.PixelHeight);
            TestAssert.AreEqual(1024, background.PixelWidth);
            TestAssert.AreEqual(1024, background.PixelHeight);
        }
        finally
        {
            File.Delete(path);
        }
    }

    internal static void Decode_ReturnedProfile_Preserves256Pixels()
    {
        string path = WriteImage(256, 256, new byte[256 * 256 * 4]);
        try
        {
            ProfileImageContent content = ProfileImageModificationService.ReadImageFile(path);
            byte[] original = File.ReadAllBytes(path);
            TestAssert.AreEqual(Convert.ToBase64String(original),
                Convert.ToBase64String(content.Content));
            BitmapSource preview = TestAssert.IsNotNull(
                ProfileImageModificationService.CreateProfilePreview(content));
            TestAssert.AreEqual(256, preview.PixelWidth);
            TestAssert.AreEqual(256, preview.PixelHeight);
        }
        finally
        {
            File.Delete(path);
        }
    }

    internal static void CreatePreview_OversizedDimensions_RejectsImage()
    {
        string path = WriteImage(4097, 1, new byte[4097 * 4]);
        try
        {
            TestAssert.AreEqual<BitmapSource?>(null, ProfilePicturePolicy.CreatePreview(path));
            ProfileImageContent content = ProfileImageModificationService.ReadImageFile(path);
            TestAssert.Throws<InvalidOperationException>(() =>
                ProfileImageModificationService.CreateProfilePreview(content));
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
