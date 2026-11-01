using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;

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
    public ProfileImageData Read(string imageReference)
    {
        string path = ResolvePath(imageReference);
        FileInfo file = new FileInfo(path);
        if (!file.Exists || file.Length < 1 || file.Length > ProfileImagePolicy.MaximumFileBytes)
        {
            throw new IOException("The stored profile image is unavailable.");
        }

        byte[] content = File.ReadAllBytes(path);
        string mediaType = string.Equals(Path.GetExtension(path), ".png",
            StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        ProfileImageData data = new ProfileImageData(content, mediaType);
        try
        {
            Validate(data);
        }
        catch (ProfileRejectedException exception)
        {
            throw new IOException("The stored profile image content is invalid.", exception);
        }

        return data;
    }

    /// <inheritdoc />
    public string Save(ProfileImageData image)
    {
        byte[] recoded = Recode(image);
        string reference = ReferencePrefix + Guid.NewGuid().ToString("N")
            + (image.MediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
                ? ".png" : ".jpg");
        string path = ResolvePath(reference);
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

        return reference;
    }

    /// <inheritdoc />
    public void Delete(string imageReference)
    {
        File.Delete(ResolvePath(imageReference));
    }

    private string ResolvePath(string reference)
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

    private static byte[] Recode(ProfileImageData image)
    {
        Validate(image);
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
                    throw new ProfileRejectedException(ProfileRejection.InvalidImage);
                }

                return output.ToArray();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or OutOfMemoryException)
        {
            throw new ProfileRejectedException(ProfileRejection.InvalidImage);
        }
    }

    private static void Validate(ProfileImageData image)
    {
        if (image == null || !ProfileImagePolicy.IsSupported(image.Content, image.MediaType))
        {
            throw new ProfileRejectedException(ProfileRejection.InvalidImage);
        }

        try
        {
            using (MemoryStream input = new MemoryStream(image.Content, writable: false))
            using (Image decoded = Image.FromStream(input, false, true))
            {
                Guid expectedFormat = image.MediaType.Equals(
                    "image/png", StringComparison.OrdinalIgnoreCase)
                        ? ImageFormat.Png.Guid : ImageFormat.Jpeg.Guid;
                if (decoded.RawFormat.Guid != expectedFormat
                    || decoded.Width < 1 || decoded.Height < 1
                    || decoded.Width > ProfileImagePolicy.MaximumDimension
                    || decoded.Height > ProfileImagePolicy.MaximumDimension)
                {
                    throw new ProfileRejectedException(ProfileRejection.InvalidImage);
                }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or OutOfMemoryException)
        {
            throw new ProfileRejectedException(ProfileRejection.InvalidImage);
        }
    }
}
