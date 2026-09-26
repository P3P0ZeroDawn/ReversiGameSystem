using System;
using System.IO;
using System.Security;
using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Validation;

/// <summary>
/// Checks the selected profile picture and creates a small preview.
/// </summary>
internal static class ProfilePicturePolicy
{
    internal const long MaximumFileBytes = 5 * 1024 * 1024;
    internal const int MaximumDimension = 4096;

    internal static bool TryCreatePreview(
        string path,
        out BitmapSource? preview,
        int previewWidth = 128)
    {
        preview = null;

        string extension = Path.GetExtension(path);
        if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            FileInfo file = new FileInfo(path);
            if (file.Length == 0 || file.Length > MaximumFileBytes)
            {
                return false;
            }

            using (FileStream stream = file.OpenRead())
            {
                BitmapDecoder decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.None,
                    BitmapCacheOption.OnDemand);

                if (!(decoder is PngBitmapDecoder || decoder is JpegBitmapDecoder)
                    || decoder.Frames.Count == 0)
                {
                    return false;
                }

                BitmapFrame frame = decoder.Frames[0];
                if (frame.PixelWidth < 1 || frame.PixelHeight < 1
                    || frame.PixelWidth > MaximumDimension
                    || frame.PixelHeight > MaximumDimension)
                {
                    return false;
                }
            }

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(file.FullName);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = previewWidth;
            image.EndInit();
            image.Freeze();
            preview = image;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is SecurityException
            || exception is NotSupportedException
            || exception is ArgumentException
            || exception is FormatException
            || exception is InvalidOperationException)
        {
            return false;
        }
    }
}
