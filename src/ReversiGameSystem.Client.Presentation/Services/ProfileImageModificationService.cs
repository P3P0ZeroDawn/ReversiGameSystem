using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Validation;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class ProfileImageModificationService
{
    internal async Task SaveAsync(string sessionToken, string imagePath)
    {
        Proxy.ImageContent image = await Task.Run(() => ReadUpload(imagePath));
        await ClientRuntime.ProfileService.UpdateProfileImageAsync(sessionToken, image);
    }

    internal static Proxy.ImageContent ReadUpload(string imagePath)
    {
        FileInfo file = new FileInfo(imagePath);
        if (!file.Exists || file.Length > ProfilePicturePolicy.MaximumFileBytes)
        {
            throw new IOException("The selected image exceeds the upload limit or is unavailable.");
        }
        return new Proxy.ImageContent
        {
            Content = File.ReadAllBytes(imagePath),
            MediaType = Path.GetExtension(imagePath).Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? "image/png" : "image/jpeg"
        };
    }

    internal static BitmapSource? Decode(Proxy.ImageContent? content)
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
