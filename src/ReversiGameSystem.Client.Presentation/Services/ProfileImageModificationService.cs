using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Services;

internal sealed class ProfileImageModificationService
{
    internal async Task<ProfileStatus> Save(string sessionToken, string imagePath)
    {
        ProfileImageContent image = await Task.Run(() => ReadUpload(imagePath));
        return await ClientRuntime.ProfileService.UpdateProfileImage(
            sessionToken, image.Content, image.MediaType);
    }

    internal static ProfileImageContent ReadUpload(string imagePath)
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

    internal static BitmapSource? Decode(ProfileImageContent? content)
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
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.DecodePixelWidth = 128;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
