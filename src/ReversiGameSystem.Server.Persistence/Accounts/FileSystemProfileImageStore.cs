using System;
using System.Drawing;
using System.IO;
using System.Security;
using ReversiGameSystem.Domain.Accounts;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Persistence.Accounts;

/// <summary>
/// Represents file system profile image store.
/// </summary>
public sealed class FileSystemProfileImageStore : IProfileImageStore
{
    private const string UriDirectory = "profile-images";
    private readonly string _storageDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemProfileImageStore"/> class.
    /// </summary>
    /// <param name="storageDirectory">The storage directory.</param>
    public FileSystemProfileImageStore(string storageDirectory)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException(
                "A profile image directory is required.",
                nameof(storageDirectory));
        }

        _storageDirectory = Path.GetFullPath(storageDirectory);
    }

    /// <summary>
    /// Saves a profile image.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <returns>The result of the operation.</returns>
    public ProfileImageStorageResult Save(ProfileImageData image)
    {
        if (image == null
            || !ProfileImagePolicy.IsSupported(image.Content, image.MediaType)
            || !HasSupportedDimensions(image.Content))
        {
            return new ProfileImageStorageResult(ProfileImageStorageOutcome.InvalidImage);
        }

        string extension = string.Equals(
            image.MediaType,
            "image/png",
            StringComparison.OrdinalIgnoreCase)
                ? ".png"
                : ".jpg";
        string fileName = Guid.NewGuid().ToString("N") + extension;
        string finalPath = Path.Combine(_storageDirectory, fileName);
        string temporaryPath = finalPath + ".tmp";

        ProfileImageStorageResult result;
        try
        {
            Directory.CreateDirectory(_storageDirectory);
            File.WriteAllBytes(temporaryPath, image.Content);
            File.Move(temporaryPath, finalPath);

            result = new ProfileImageStorageResult(
                ProfileImageStorageOutcome.Success,
                UriDirectory + "/" + fileName);
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or SecurityException
                                            or NotSupportedException)
        {
            result = CreateUnavailableResult(temporaryPath);
        }

        return result;
    }

    /// <summary>
    /// Deletes a profile image.
    /// </summary>
    /// <param name="imageUri">The image uri.</param>
    /// <returns>The result of the operation.</returns>
    public void Delete(string imageUri)
    {
        if (string.IsNullOrWhiteSpace(imageUri))
        {
            return;
        }

        string normalizedUri = imageUri.Replace('\\', '/');
        string expectedPrefix = UriDirectory + "/";
        if (!normalizedUri.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || normalizedUri.IndexOf('/', expectedPrefix.Length) >= 0)
        {
            return;
        }

        string fileName = normalizedUri.Substring(expectedPrefix.Length);
        TryDeleteFile(Path.Combine(_storageDirectory, fileName));
    }

    private static bool HasSupportedDimensions(byte[] content)
    {
        bool isSupported;
        try
        {
            using (MemoryStream stream = new MemoryStream(content, writable: false))
            using (Image image = Image.FromStream(
                stream,
                useEmbeddedColorManagement: false,
                validateImageData: true))
            {
                isSupported = image.Width >= 1
                    && image.Height >= 1
                    && image.Width <= ProfileImagePolicy.MaximumDimension
                    && image.Height <= ProfileImagePolicy.MaximumDimension;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or OutOfMemoryException)
        {
            isSupported = false;
        }

        return isSupported;
    }

    private static ProfileImageStorageResult CreateUnavailableResult(
        string temporaryPath)
    {
        TryDeleteFile(temporaryPath);
        return new ProfileImageStorageResult(ProfileImageStorageOutcome.Unavailable);
    }

    private static bool TryDeleteFile(string path)
    {
        bool isDeleted;
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            isDeleted = true;
        }
        catch (Exception exception) when (exception is IOException
                                            or UnauthorizedAccessException
                                            or SecurityException
                                            or NotSupportedException)
        {
            isDeleted = false;
        }

        return isDeleted;
    }
}
