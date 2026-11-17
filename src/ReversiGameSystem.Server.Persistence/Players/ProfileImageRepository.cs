using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Persistence.Players;

/// <summary>Reads and recodes images within the server-owned profile image directory.</summary>
public sealed class ProfileImageRepository : IPlayerImageRepository
{
    private const string ReferencePrefix = "profile-images/";
    private readonly string _directory;

    /// <summary>Initializes storage using the directory shared with account registration.</summary>
    /// <param name="storageDirectory">The server-owned profile image directory.</param>
    /// <exception cref="ArgumentException">The directory is absent or invalid.</exception>
    public ProfileImageRepository(string storageDirectory)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException(
                "An image directory is required.", nameof(storageDirectory));
        }

        _directory = Path.GetFullPath(storageDirectory);
    }

    /// <inheritdoc />
    public ProfileImageData ReadImage(string imageReference)
    {
        string path = ResolveImageFilePath(imageReference);
        FileInfo file = new FileInfo(path);
        if (!file.Exists || file.Length < 1 || file.Length > ProfileImagePolicy.MaximumFileBytes)
        {
            throw new IOException("The stored profile image is unavailable.");
        }

        byte[] content = File.ReadAllBytes(path);
        string mediaType = string.Equals(Path.GetExtension(path), ".png",
            StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        ProfileImageData data = new ProfileImageData(content, mediaType);
        if (!ProfileImagePolicy.IsSupported(data.Content, data.MediaType))
        {
            throw new IOException("The stored profile image content is invalid.");
        }

        return data;
    }

    /// <inheritdoc />
    public ProfileResult<string> SaveImage(ProfileImageData image)
    {
        byte[]? recoded = RecodeImage(image);
        if (recoded == null)
        {
            return new ProfileResult<string>(ProfileOperationStatus.InvalidImage);
        }

        string reference = ReferencePrefix + Guid.NewGuid().ToString("N")
            + (image.MediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
                ? ".png" : ".jpg");
        string path = ResolveImageFilePath(reference);
        string temporaryPath = path + ".tmp";
        Directory.CreateDirectory(_directory);
        try
        {
            File.WriteAllBytes(temporaryPath, recoded);
            File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return new ProfileResult<string>(reference);
    }

    /// <inheritdoc />
    public void DeleteImage(string imageReference)
    {
        File.Delete(ResolveImageFilePath(imageReference));
    }

    private string ResolveImageFilePath(string reference)
    {
        if (reference == null || !reference.StartsWith(ReferencePrefix, StringComparison.Ordinal))
        {
            throw new IOException("The stored profile image reference is invalid.");
        }

        string fileName = reference.Substring(ReferencePrefix.Length);
        string extension = Path.GetExtension(fileName);
        if ((extension != ".png" && extension != ".jpg")
            || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _)
            || fileName != Path.GetFileName(fileName))
        {
            throw new IOException("The stored profile image reference is invalid.");
        }

        return Path.Combine(_directory, fileName);
    }

    private static byte[]? RecodeImage(ProfileImageData image)
    {
        if (!IsValidProfileImage(image))
        {
            return null;
        }

        byte[]? recoded;
        try
        {
            using (MemoryStream input = new MemoryStream(image.Content, writable: false))
            using (Image source = Image.FromStream(input, false, true))
            using (Bitmap bitmap = new Bitmap(source))
            using (MemoryStream output = new MemoryStream())
            {
                bitmap.Save(output,
                    image.MediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
                        ? ImageFormat.Png : ImageFormat.Jpeg);
                if (output.Length > ProfileImagePolicy.MaximumFileBytes)
                {
                    recoded = null;
                }
                else
                {
                    recoded = output.ToArray();
                }
            }
        }
        catch (Exception exception) when (IsInvalidImageException(exception))
        {
            recoded = null;
        }

        return recoded;
    }

    private static bool IsValidProfileImage(ProfileImageData image)
    {
        if (image == null || !ProfileImagePolicy.IsSupported(image.Content, image.MediaType))
        {
            return false;
        }

        bool isValid;
        try
        {
            using (MemoryStream input = new MemoryStream(image.Content, writable: false))
            using (Image decoded = Image.FromStream(input, false, true))
            {
                Guid expectedFormat = image.MediaType.Equals(
                    "image/png", StringComparison.OrdinalIgnoreCase)
                        ? ImageFormat.Png.Guid : ImageFormat.Jpeg.Guid;
                isValid = decoded.RawFormat.Guid == expectedFormat
                    && decoded.Width >= 1 && decoded.Height >= 1
                    && decoded.Width <= ProfileImagePolicy.MaximumDimension
                    && decoded.Height <= ProfileImagePolicy.MaximumDimension;
            }
        }
        catch (Exception exception) when (IsInvalidImageException(exception))
        {
            isValid = false;
        }

        return isValid;
    }

    private static bool IsInvalidImageException(Exception exception)
    {
        return exception is ArgumentException || exception is OutOfMemoryException;
    }
}
