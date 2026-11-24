using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Services;

internal static class ProfileImageModificationService
{
    internal static async Task<ProfileStatus> ChangeProfileImage(string sessionToken, string imagePath)
    {
        ProfileImageContent image = await Task.Run(() => ReadImageFile(imagePath));
        return await ClientRuntime.ProfileService.UpdateProfileImage(
            sessionToken, image.Content, image.MediaType);
    }

    internal static ProfileImageContent ReadImageFile(string imagePath)
    {
        FileInfo file = new FileInfo(imagePath);
        if (!file.Exists || file.Length > ProfilePicturePolicy.MaximumFileBytes)
        {
            throw new IOException("The selected image exceeds the upload limit or is unavailable.");
        }
        string mediaType = Path.GetExtension(imagePath).Equals(
            ".png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";
        return new ProfileImageContent(File.ReadAllBytes(imagePath), mediaType);
    }

    internal static BitmapSource? CreateProfilePreview(ProfileImageContent? content)
    {
        if (content == null)
        {
            return null;
        }
        if (content.Content == null || content.Content.Length == 0
            || content.Content.Length > ProfilePicturePolicy.MaximumFileBytes)
        {
            throw new InvalidOperationException("The service returned invalid image content.");
        }
        using (MemoryStream stream = new MemoryStream(content.Content, false))
        {
            BitmapDecoder decoder = BitmapDecoder.Create(
                stream, BitmapCreateOptions.None, BitmapCacheOption.OnDemand);
            if (!(decoder is PngBitmapDecoder || decoder is JpegBitmapDecoder)
                || decoder.Frames.Count == 0)
            {
                throw new InvalidOperationException("The service returned an unsupported image.");
            }

            BitmapFrame frame = decoder.Frames[0];
            if (frame.PixelWidth < 1 || frame.PixelHeight < 1
                || frame.PixelWidth > ProfilePicturePolicy.MaximumDimension
                || frame.PixelHeight > ProfilePicturePolicy.MaximumDimension)
            {
                throw new InvalidOperationException(
                    "The service returned invalid image dimensions.");
            }

            return ProfilePicturePolicy.CreateSquarePreview(
                frame, ProfilePicturePolicy.ProfilePreviewSize);
        }
    }
}
