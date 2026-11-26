using System;
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Validation;

internal static class ProfilePicturePolicy
{
    private const double PreviewDpi = 96;
    internal const long MaximumFileBytes = PlayerImagePolicy.MaximumFileBytes;
    internal const int MaximumDimension = PlayerImagePolicy.MaximumDimension;
    internal const int ProfilePreviewSize = PlayerImagePolicy.MaximumProfileOutputDimension;

    internal static BitmapSource? CreatePreview(
        string path,
        int previewWidth = ProfilePreviewSize)
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
        using (FileStream stream = File.OpenRead(path))
        {
            BitmapDecoder decoder = BitmapDecoder.Create(
                stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return CreateSquarePreview(decoder.Frames[0], previewWidth);
        }
    }

    internal static BitmapSource CreateSquarePreview(BitmapSource source, int maximumSize)
    {
        int side = Math.Min(source.PixelWidth, source.PixelHeight);
        int targetSize = Math.Min(side, maximumSize);
        Int32Rect crop = new Int32Rect(
            (source.PixelWidth - side) / 2, (source.PixelHeight - side) / 2, side, side);
        CroppedBitmap square = new CroppedBitmap(source, crop);
        DrawingVisual visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (DrawingContext drawing = visual.RenderOpen())
        {
            Rect bounds = new Rect(0, 0, targetSize, targetSize);
            drawing.DrawRectangle(Brushes.White, null, bounds);
            drawing.DrawImage(square, bounds);
        }

        RenderTargetBitmap preview = new RenderTargetBitmap(
            targetSize, targetSize, PreviewDpi, PreviewDpi, PixelFormats.Pbgra32);
        preview.Render(visual);
        preview.Freeze();
        return preview;
    }
}
