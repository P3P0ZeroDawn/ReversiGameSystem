using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Players;

namespace ReversiGameSystem.Server.Tests;

internal static class PlayerImageRepositoryTests
{
    internal static void SaveImage_DetailedBackgroundAboveSoftTarget_AcceptsImage()
    {
        string root = CreateRoot();
        try
        {
            using (Bitmap source = new Bitmap(1024, 1024, PixelFormat.Format24bppRgb))
            using (MemoryStream input = new MemoryStream())
            {
                BitmapData pixels = source.LockBits(
                    new Rectangle(0, 0, source.Width, source.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
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
                TestAssert.AreEqual(true, input.Length < 5 * 1024 * 1024);
                PlayerImageRepository repository = new PlayerImageRepository(root);
                ProfileResult<string> saved = repository.SaveImage(
                    new ProfileImageData(input.ToArray(), "image/png"),
                PlayerImageKind.BoardBackground);
                TestAssert.AreEqual(ProfileOperationStatus.Success, saved.Status);
                ProfileImageData stored = repository.ReadImage(
                TestAssert.IsNotNull(saved.Value));
                TestAssert.AreEqual("image/jpeg", stored.MediaType);
                TestAssert.AreEqual(true, stored.Content.Length > 512 * 1024);
            }
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    internal static void SaveImage_WideTransparentImage_CropsCenterAndCompositesWhite()
    {
        string root = CreateRoot();
        try
        {
            using (Bitmap source = new Bitmap(600, 300))
            using (Graphics graphics = Graphics.FromImage(source))
            using (MemoryStream input = new MemoryStream())
            {
                graphics.Clear(Color.Transparent);
                graphics.FillRectangle(Brushes.Red, 0, 0, 150, 300);
                graphics.FillRectangle(Brushes.Blue, 450, 0, 150, 300);
                graphics.FillRectangle(Brushes.Green, 250, 100, 100, 100);
                source.Save(input, ImageFormat.Png);
                PlayerImageRepository repository = new PlayerImageRepository(root);
                ProfileResult<string> saved = repository.SaveImage(
                    new ProfileImageData(input.ToArray(), "image/png"), PlayerImageKind.Profile);
                string reference = TestAssert.IsNotNull(saved.Value);
                ProfileImageData stored = repository.ReadImage(reference);
                using (MemoryStream output = new MemoryStream(stored.Content))
                using (Bitmap decoded = new Bitmap(output))
                {
                    TestAssert.AreEqual("image/jpeg", stored.MediaType);
                    TestAssert.AreEqual(true, reference.EndsWith(".jpg"));
                    TestAssert.AreEqual(256, decoded.Width);
                    TestAssert.AreEqual(256, decoded.Height);
                    Color corner = decoded.GetPixel(20, 20);
                    Color center = decoded.GetPixel(128, 128);
                    TestAssert.AreEqual(true, corner.R > 240 && corner.G > 240 && corner.B > 240);
                    TestAssert.AreEqual(true, center.G > center.R + 50 && center.G > center.B + 50);
                }
            }
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    internal static void SaveImage_Background_ResizesAndDoesNotUpscale()
    {
        string root = CreateRoot();
        try
        {
            PlayerImageRepository repository = new PlayerImageRepository(root);
            foreach (int dimension in new int[] { 40, 1200 })
            {
                using (Bitmap source = new Bitmap(dimension, dimension))
                using (MemoryStream input = new MemoryStream())
                {
                    source.Save(input, ImageFormat.Png);
                    ProfileResult<string> saved = repository.SaveImage(
                        new ProfileImageData(input.ToArray(), "image/png"),
                PlayerImageKind.BoardBackground);
                    ProfileImageData stored = repository.ReadImage(
                TestAssert.IsNotNull(saved.Value));
                    using (MemoryStream output = new MemoryStream(stored.Content))
                    using (Bitmap decoded = new Bitmap(output))
                    {
                        TestAssert.AreEqual(Math.Min(dimension, 1024), decoded.Width);
                        TestAssert.AreEqual(decoded.Width, decoded.Height);
                    }
                }
            }
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    internal static void ReadImage_LegacyFormats_PreservesMediaType()
    {
        string root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            PlayerImageRepository repository = new PlayerImageRepository(root);
            foreach (string extension in new string[] { ".png", ".jpg" })
            {
                string name = Guid.NewGuid().ToString("N") + extension;
                ImageFormat format = ImageFormat.Png;
                string mediaType = "image/png";
                if (extension == ".jpg")
                {
                    format = ImageFormat.Jpeg;
                    mediaType = "image/jpeg";
                }

                using (Bitmap image = new Bitmap(30, 20))
                {
                    image.Save(Path.Combine(root, name), format);
                }

                ProfileImageData stored = repository.ReadImage("profile-images/" + name);
                TestAssert.AreEqual(mediaType, stored.MediaType);
            }
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    internal static void DeleteImage_TraversalReference_RejectsAndPreservesFile()
    {
        string root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            string name = Guid.NewGuid().ToString("N") + ".jpg";
            string path = Path.Combine(root, name);
            File.WriteAllBytes(path, new byte[] { 1 });
            PlayerImageRepository repository = new PlayerImageRepository(root);
            bool rejected = false;
            try
            {
                repository.DeleteImage("profile-images/../" + name);
            }
            catch (IOException)
            {
                rejected = true;
            }

            TestAssert.AreEqual(true, rejected);
            TestAssert.AreEqual(true, File.Exists(path));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static string CreateRoot()
    {
        return Path.Combine(Path.GetTempPath(), "ReversiGameSystem.Tests",
            Guid.NewGuid().ToString("N"));
    }

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    internal static void SaveAndDelete_ValidImage_UsesRelativeUri()
    {
        // Arrange
        string root = Path.Combine(
            Path.GetTempPath(),
            "ReversiGameSystem.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            PlayerImageRepository store = new PlayerImageRepository(root);
            byte[] imageContent = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwC" +
                "AAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

            // Act
            ProfileResult<string> result = store.SaveImage(
                new ProfileImageData(imageContent, "image/png"), PlayerImageKind.Profile);

            // Assert
            TestAssert.AreEqual(ProfileOperationStatus.Success, result.Status);
            TestAssert.AreEqual(true, result.Value?.StartsWith("profile-images/"));
            TestAssert.AreEqual(1, Directory.GetFiles(root).Length);

            string imageUri = TestAssert.IsNotNull(result.Value);
            store.DeleteImage(imageUri);
            TestAssert.AreEqual(0, Directory.GetFiles(root).Length);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    internal static void SaveImage_ValidAndInvalidImages_ReturnsExpectedOutcome()
    {
        // Arrange
        string root = Path.Combine(
            Path.GetTempPath(),
            "ReversiGameSystem.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            PlayerImageRepository repository = new PlayerImageRepository(root);
            byte[] imageContent = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwC"
                + "AAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

            // Act
            ProfileResult<string> saved = repository.SaveImage(
                new ProfileImageData(imageContent, "image/png"), PlayerImageKind.Profile);
            ProfileResult<string> rejected = repository.SaveImage(
                new ProfileImageData(new byte[] { 1, 2 }, "image/png"), PlayerImageKind.Profile);

            // Assert
            TestAssert.AreEqual(ProfileOperationStatus.Success, saved.Status);
            string savedReference = TestAssert.IsNotNull(saved.Value);
            TestAssert.AreEqual(true, savedReference.StartsWith("profile-images/"));
            TestAssert.AreEqual(ProfileOperationStatus.InvalidImage, rejected.Status);
            TestAssert.AreEqual<string?>(null, rejected.Value);
            TestAssert.AreEqual(1, Directory.GetFiles(root).Length);

            repository.DeleteImage(savedReference);
            TestAssert.AreEqual(0, Directory.GetFiles(root).Length);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
