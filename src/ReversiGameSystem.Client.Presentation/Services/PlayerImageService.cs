using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Services;

internal static class PlayerImageService
{
    private const int FileBufferSize = 81920;

    internal static async Task<PlayerImageContent> ReadImageFile(string imagePath)
    {
        using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read,
            FileShare.Read, FileBufferSize, true))
        {
            if (stream.Length == 0 || stream.Length > ProfilePicturePolicy.MaximumFileBytes)
            {
                throw new IOException("The selected image exceeds the upload limit or is empty.");
            }
            byte[] bytes = new byte[(int)stream.Length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int bytesRead = await stream.ReadAsync(bytes, offset, bytes.Length - offset);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException(
                        "The selected image could not be read completely.");
                }
                offset += bytesRead;
            }
            string mediaType = Path.GetExtension(imagePath).Equals(
                ".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            return new PlayerImageContent(bytes, mediaType);
        }
    }

    internal static BitmapSource? CreatePreview(
        PlayerImageContent? content, int maximumSize = ProfilePicturePolicy.ProfilePreviewSize)
    {
        if (content == null || content.Content.Length == 0)
        {
            return null;
        }
        if (content.Content.Length > ProfilePicturePolicy.MaximumFileBytes)
        {
            throw new InvalidDataException("The service returned invalid image content.");
        }
        using (MemoryStream stream = new MemoryStream(content.Content, false))
        {
            BitmapDecoder decoder = BitmapDecoder.Create(
                stream, BitmapCreateOptions.None, BitmapCacheOption.OnDemand);
            if (!(decoder is PngBitmapDecoder || decoder is JpegBitmapDecoder)
                || decoder.Frames.Count == 0)
            {
                throw new InvalidDataException("The service returned an unsupported image.");
            }

            BitmapFrame frame = decoder.Frames[0];
            if (frame.PixelWidth < 1 || frame.PixelHeight < 1
                || frame.PixelWidth > ProfilePicturePolicy.MaximumDimension
                || frame.PixelHeight > ProfilePicturePolicy.MaximumDimension)
            {
                throw new InvalidDataException(
                    "The service returned invalid image dimensions.");
            }

            return ProfilePicturePolicy.CreateSquarePreview(
                frame, maximumSize);
        }
    }
}
