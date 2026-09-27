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

    internal static bool TryLoadProfileImage(
        string? profileImageUri,
        out BitmapSource? profileImage)
    {
        profileImage = null;
        if (!TryResolveProfileImagePath(profileImageUri, out string? path))
        {
            return false;
        }

        return TryLoadBitmap(path, out profileImage);
    }

    private static bool TryResolveProfileImagePath(
        string? profileImageUri,
        out string? path)
    {
        path = null;
        string? profileImageDirectory = Services.ProfileImageDirectory;
        if (profileImageUri == null
            || string.IsNullOrWhiteSpace(profileImageUri)
            || profileImageDirectory == null
            || string.IsNullOrWhiteSpace(profileImageDirectory))
        {
            return false;
        }

        string normalizedUri = profileImageUri.Replace('\\', '/');
        const string expectedPrefix = "profile-images/";
        if (!normalizedUri.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || normalizedUri.IndexOf('/', expectedPrefix.Length) >= 0)
        {
            return false;
        }

        path = Path.Combine(
            profileImageDirectory,
            normalizedUri.Substring(expectedPrefix.Length));
        return File.Exists(path);
    }

    private static bool TryLoadBitmap(string? path, out BitmapSource? profileImage)
    {
        profileImage = null;
        if (path == null)
        {
            return false;
        }

        try
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            profileImage = image;
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
    }
}
