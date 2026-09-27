using System;
using System.IO;
using System.Windows.Media.Imaging;
using ReversiGameSystem.LocalRuntime;

namespace ReversiGameSystem.Services;

internal static class ClientRuntime
{
    private static readonly Lazy<LocalRuntimeServices> Runtime =
        new Lazy<LocalRuntimeServices>(LocalRuntimeFactory.CreateDefault);

    internal static LocalRuntimeServices Services => Runtime.Value;

    internal static BitmapSource? TryLoadProfileImage(string? profileImageUri)
    {
        string? profileImageDirectory = Services.ProfileImageDirectory;
        if (profileImageUri == null
            || string.IsNullOrWhiteSpace(profileImageUri)
            || profileImageDirectory == null
            || string.IsNullOrWhiteSpace(profileImageDirectory))
        {
            return null;
        }

        string normalizedUri = profileImageUri.Replace('\\', '/');
        const string expectedPrefix = "profile-images/";
        if (!normalizedUri.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || normalizedUri.IndexOf('/', expectedPrefix.Length) >= 0)
        {
            return null;
        }

        string path = Path.Combine(
            profileImageDirectory,
            normalizedUri.Substring(expectedPrefix.Length));
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is NotSupportedException
            || exception is ArgumentException)
        {
            return null;
        }
    }
}
