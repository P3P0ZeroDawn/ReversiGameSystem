using System;
using System.IO;
using System.Security;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Domain.Accounts;

namespace ReversiGameSystem.Validation;

/// <summary>
/// Checks the selected profile picture and creates a small preview.
/// </summary>
internal static class ProfilePicturePolicy
{
    internal const long MaximumFileBytes = ProfileImagePolicy.MaximumFileBytes;
    internal const int MaximumDimension = ProfileImagePolicy.MaximumDimension;

    internal static bool TryCreatePreview(
        string path,
        out BitmapSource? preview,
        int previewWidth = 128)
    {
        preview = null;
        if (!HasSupportedExtension(path))
        {
            return false;
        }

        if (!TryGetSupportedFile(path, out FileInfo? file)
            || file == null
            || !TryValidateImageContent(file))
        {
            return false;
        }

        return TryLoadPreview(file.FullName, previewWidth, out preview);
    }

    private static bool TryGetSupportedFile(string path, out FileInfo? file)
    {
        file = null;
        try
        {
            FileInfo candidate = new FileInfo(path);
            file = HasSupportedFileSize(candidate) ? candidate : null;
            return file != null;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryValidateImageContent(FileInfo file)
    {
        try
        {
            return HasSupportedImageContent(file);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryLoadPreview(
        string path,
        int previewWidth,
        out BitmapSource? preview)
    {
        preview = null;
        try
        {
            preview = CreatePreview(path, previewWidth);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
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

    private static BitmapSource CreatePreview(string path, int previewWidth)
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
