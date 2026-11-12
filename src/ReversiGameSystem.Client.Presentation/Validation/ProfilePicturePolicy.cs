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
    internal const long MaximumFileBytes = ProfileImagePolicy.MaximumFileBytes;
    internal const int MaximumDimension = ProfileImagePolicy.MaximumDimension;

    internal static BitmapSource? CreatePreview(
        string path,
        int previewWidth = 128)
    {
        BitmapSource? preview = null;
        if (!HasSupportedExtension(path))
        {
            return preview;
        }

        FileInfo? file = GetSupportedFile(path);
        if (file == null || !HasValidImageContent(file))
        {
            return preview;
        }

        return LoadPreview(file.FullName, previewWidth);
    }

    private static FileInfo? GetSupportedFile(string path)
    {
        FileInfo? file = null;
        try
        {
            FileInfo candidate = new FileInfo(path);
            file = HasSupportedFileSize(candidate) ? candidate : null;
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or SecurityException
                                            or NotSupportedException
                                            or ArgumentException)
        {
            file = null;
        }

        return file;
    }

    private static bool HasValidImageContent(FileInfo file)
    {
        bool isValid;
        try
        {
            isValid = HasSupportedImageContent(file);
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or NotSupportedException
                                            or ArgumentException)
        {
            isValid = false;
        }

        return isValid;
    }

    private static BitmapSource? LoadPreview(string path, int previewWidth)
    {
        BitmapSource? preview = null;
        try
        {
            preview = DecodePreview(path, previewWidth);
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or NotSupportedException
                                            or ArgumentException
                                            or FormatException
                                            or InvalidOperationException)
        {
            preview = null;
        }

        return preview;
    }

    private static bool HasSupportedExtension(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSupportedFileSize(FileInfo file)
    {
        return file.Length > 0 && file.Length <= MaximumFileBytes;
    }

    private static bool HasSupportedImageContent(FileInfo file)
    {
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
            return frame.PixelWidth >= 1
                && frame.PixelHeight >= 1
                && frame.PixelWidth <= MaximumDimension
                && frame.PixelHeight <= MaximumDimension;
        }
    }

    private static BitmapSource DecodePreview(string path, int previewWidth)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(path);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = previewWidth;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
