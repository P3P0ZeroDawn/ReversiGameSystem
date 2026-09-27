using System;
using System.Drawing;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using ReversiGameSystem.Domain.Accounts;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Persistence.Accounts;

public sealed class FileSystemProfileImageStore : IProfileImageStore
{
    private const string UriDirectory = "profile-images";
    private readonly string storageDirectory;

    public FileSystemProfileImageStore(string storageDirectory)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException("A profile image directory is required.", nameof(storageDirectory));
        }

        this.storageDirectory = Path.GetFullPath(storageDirectory);
    }

    public Task<ProfileImageStorageResult> SaveAsync(ProfileImageData image)
    {
        if (image == null
            || !ProfileImagePolicy.IsSupported(image.Content, image.MediaType)
            || !HasSupportedDimensions(image.Content))
        {
            return Task.FromResult(
                new ProfileImageStorageResult(ProfileImageStorageOutcome.InvalidImage));
        }

        string extension = string.Equals(
            image.MediaType,
            "image/png",
            StringComparison.OrdinalIgnoreCase)
                ? ".png"
                : ".jpg";
        string fileName = Guid.NewGuid().ToString("N") + extension;
        string finalPath = Path.Combine(storageDirectory, fileName);
        string temporaryPath = finalPath + ".tmp";

        try
        {
            Directory.CreateDirectory(storageDirectory);
            File.WriteAllBytes(temporaryPath, image.Content);
            File.Move(temporaryPath, finalPath);

            return Task.FromResult(
                new ProfileImageStorageResult(
                    ProfileImageStorageOutcome.Success,
                    UriDirectory + "/" + fileName));
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is SecurityException
            || exception is NotSupportedException)
        {
            TryDeleteFile(temporaryPath);
            return Task.FromResult(
                new ProfileImageStorageResult(ProfileImageStorageOutcome.Unavailable));
        }
    }

    public Task DeleteAsync(string imageUri)
    {
        if (string.IsNullOrWhiteSpace(imageUri))
        {
            return Task.CompletedTask;
        }

        string normalizedUri = imageUri.Replace('\\', '/');
        string expectedPrefix = UriDirectory + "/";
        if (!normalizedUri.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || normalizedUri.IndexOf('/', expectedPrefix.Length) >= 0)
        {
            return Task.CompletedTask;
        }

        string fileName = normalizedUri.Substring(expectedPrefix.Length);
        TryDeleteFile(Path.Combine(storageDirectory, fileName));
        return Task.CompletedTask;
    }

    private static bool HasSupportedDimensions(byte[] content)
    {
        try
        {
            using (MemoryStream stream = new MemoryStream(content, writable: false))
            using (Image image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true))
            {
                return image.Width >= 1
                    && image.Height >= 1
                    && image.Width <= ProfileImagePolicy.MaximumDimension
                    && image.Height <= ProfileImagePolicy.MaximumDimension;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (OutOfMemoryException)
        {
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is SecurityException
            || exception is NotSupportedException)
        {
        }
    }
}
