using System;
using System.Drawing;
using System.IO;
using System.Security;
using System.Threading.Tasks;
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
    /// Performs the save async operation.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <returns>The result of the operation.</returns>
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
        string finalPath = Path.Combine(_storageDirectory, fileName);
        string temporaryPath = finalPath + ".tmp";

        try
        {
            Directory.CreateDirectory(_storageDirectory);
            File.WriteAllBytes(temporaryPath, image.Content);
            File.Move(temporaryPath, finalPath);

            return Task.FromResult(
                new ProfileImageStorageResult(
                    ProfileImageStorageOutcome.Success,
                    UriDirectory + "/" + fileName));
        }
        catch (IOException)
        {
            return CreateUnavailableResult(temporaryPath);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateUnavailableResult(temporaryPath);
        }
        catch (SecurityException)
        {
            return CreateUnavailableResult(temporaryPath);
        }
        catch (NotSupportedException)
        {
            return CreateUnavailableResult(temporaryPath);
        }
    }

    /// <summary>
    /// Performs the delete async operation.
    /// </summary>
    /// <param name="imageUri">The image uri.</param>
    /// <returns>The result of the operation.</returns>
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
        TryDeleteFile(Path.Combine(_storageDirectory, fileName));
        return Task.CompletedTask;
    }

    private static bool HasSupportedDimensions(byte[] content)
    {
        try
        {
            using (MemoryStream stream = new MemoryStream(content, writable: false))
            using (Image image = Image.FromStream(
                stream,
                useEmbeddedColorManagement: false,
                validateImageData: true))
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

    private static Task<ProfileImageStorageResult> CreateUnavailableResult(
        string temporaryPath)
    {
        TryDeleteFile(temporaryPath);
        return Task.FromResult(
            new ProfileImageStorageResult(ProfileImageStorageOutcome.Unavailable));
    }

    private static bool TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

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
        catch (SecurityException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
