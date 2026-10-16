using System;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.LocalRuntime;

namespace ReversiGameSystem.Services;

internal static class ClientRuntime
{
    private static readonly Lazy<LocalRuntimeServices> Runtime =
        new Lazy<LocalRuntimeServices>(LocalRuntimeFactory.CreateDefault);
    private static readonly Lazy<ILoggerFactory> Logging =
        new Lazy<ILoggerFactory>(CreateLoggerFactory);
    private static readonly Lazy<RemoteAccountAccessService> AccountAccess =
        new Lazy<RemoteAccountAccessService>(CreateAccountAccessService);

    internal static LocalRuntimeServices Services => Runtime.Value;

    internal static RemoteAccountAccessService AccountAccessService =>
        AccountAccess.Value;

    internal static void Dispose()
    {
        if (Logging.IsValueCreated)
        {
            Logging.Value.Dispose();
        }
    }

    internal static bool TryLoadProfileImage(
        string? profileImageUri,
        out BitmapSource? profileImage)
    {
        profileImage = null;
        if (!TryResolveProfileImagePath(profileImageUri, out string? path))
        {
            return false;
        }

        bool isLoaded = TryLoadBitmap(path, out BitmapImage? loadedBitmap);
        profileImage = loadedBitmap;
        return isLoaded;
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

    private static bool TryLoadBitmap(string? path, out BitmapImage? profileImage)
    {
        profileImage = null;
        if (path == null)
        {
            return false;
        }

        bool isLoaded;
        try
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            profileImage = image;
            isLoaded = true;
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or NotSupportedException
                                            or ArgumentException)
        {
            isLoaded = false;
        }

        return isLoaded;
    }

    private static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(
            builder => builder
                .SetMinimumLevel(LogLevel.Information)
                .AddDebug());
    }

    private static RemoteAccountAccessService CreateAccountAccessService()
    {
        ILogger<RemoteAccountAccessService> logger =
            Logging.Value.CreateLogger<RemoteAccountAccessService>();
        return new RemoteAccountAccessService(logger);
    }
}
