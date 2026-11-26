using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Domain.Accounts;

namespace ReversiGameSystem.Server.Persistence.Players;

public sealed class PlayerImageRepository : IPlayerImageRepository
{
    private const string JpegExtension = ".jpg";
    private const string PngExtension = ".png";
    private const string JpegMediaType = "image/jpeg";
    private const string PngMediaType = "image/png";
    private const string ImageIdentifierFormat = "N";
    private const int ImageIdentifierLength = 32;
    private const int JpegEncoderParameterCount = 1;
    private const string DefaultReferencePrefix = "profile-images/";
    private static readonly object ImageProcessingLock = new object();
    private readonly string _referencePrefix;
    private readonly string _directory;

    public PlayerImageRepository(string storageDirectory)
        : this(storageDirectory, DefaultReferencePrefix)
    {
    }

    public PlayerImageRepository(string storageDirectory, string referencePrefix)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException(
                "An image directory is required.", nameof(storageDirectory));
        }

        if (string.IsNullOrWhiteSpace(referencePrefix)
            || !referencePrefix.EndsWith("/", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "An image reference prefix is required.", nameof(referencePrefix));
        }

        _directory = Path.GetFullPath(storageDirectory);
        _referencePrefix = referencePrefix;
    }

    public ProfileImageData ReadImage(string imageReference)
    {
        string path = ResolveImageFilePath(imageReference);
        FileInfo file = new FileInfo(path);
        if (!file.Exists || file.Length < 1 || file.Length > PlayerImagePolicy.MaximumFileBytes)
        {
            throw new IOException("The stored player image is unavailable.");
        }

        byte[] content = File.ReadAllBytes(path);
        string mediaType = JpegMediaType;
        if (Path.GetExtension(path) == PngExtension)
        {
            mediaType = PngMediaType;
        }

        if (!PlayerImagePolicy.IsSupported(content, mediaType))
        {
            throw new IOException("The stored player image content is invalid.");
        }

        return new ProfileImageData(content, mediaType);
    }

    public ProfileResult<string> SaveImage(ProfileImageData image, PlayerImageKind kind)
    {
        if (kind != PlayerImageKind.Profile && kind != PlayerImageKind.BoardBackground)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        ProfileResult<byte[]> recoded;
        lock (ImageProcessingLock)
        {
            recoded = RecodeImage(image, kind);
        }
        if (recoded.Status != ProfileStatus.Success)
        {
            return new ProfileResult<string>(recoded.Status);
        }

        byte[] content = recoded.Value
            ?? throw new InvalidOperationException("The encoded image is missing.");
        string reference = _referencePrefix
            + Guid.NewGuid().ToString(ImageIdentifierFormat) + JpegExtension;
        string path = ResolveImageFilePath(reference);
        string temporaryPath = path + ".tmp";
        Directory.CreateDirectory(_directory);
        try
        {
            File.WriteAllBytes(temporaryPath, content);
            File.Move(temporaryPath, path);
        }
        catch (Exception writeFailure)
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(writeFailure, cleanupFailure);
            }

            throw;
        }

        return new ProfileResult<string>(reference);
    }

    public void DeleteImage(string imageReference)
    {
        File.Delete(ResolveImageFilePath(imageReference));
    }

    private string ResolveImageFilePath(string reference)
    {
        if (reference == null || !reference.StartsWith(_referencePrefix, StringComparison.Ordinal))
        {
            throw new IOException("The stored player image reference is invalid.");
        }

        string fileName = reference.Substring(_referencePrefix.Length);
        string extension = Path.GetExtension(fileName);
        string identifier = Path.GetFileNameWithoutExtension(fileName);
        if ((extension != PngExtension && extension != JpegExtension)
            || identifier.Length != ImageIdentifierLength || fileName != Path.GetFileName(fileName)
            || !identifier.All(Uri.IsHexDigit))
        {
            throw new IOException("The stored player image reference is invalid.");
        }

        return Path.Combine(_directory, fileName);
    }

    private static ProfileResult<byte[]> RecodeImage(ProfileImageData image, PlayerImageKind kind)
    {
        if (image == null || !PlayerImagePolicy.IsSupported(image.Content, image.MediaType))
        {
            return new ProfileResult<byte[]>(ProfileStatus.InvalidImage);
        }

        try
        {
            using (MemoryStream input = new MemoryStream(image.Content, writable: false))
            using (Image source = Image.FromStream(input, false, true))
            {
                Guid expectedFormat = ImageFormat.Jpeg.Guid;
                if (image.MediaType.Equals(PngMediaType, StringComparison.OrdinalIgnoreCase))
                {
                    expectedFormat = ImageFormat.Png.Guid;
                }

                if (source.RawFormat.Guid != expectedFormat
                    || source.Width < 1 || source.Height < 1
                    || source.Width > PlayerImagePolicy.MaximumDimension
                    || source.Height > PlayerImagePolicy.MaximumDimension)
                {
                    return new ProfileResult<byte[]>(ProfileStatus.InvalidImage);
                }

                return EncodeImage(source, kind);
            }
        }
        catch (ArgumentException)
        {
            return new ProfileResult<byte[]>(ProfileStatus.InvalidImage);
        }
    }

    private static ProfileResult<byte[]> EncodeImage(Image source, PlayerImageKind kind)
    {
        int maximumDimension = PlayerImagePolicy.MaximumProfileOutputDimension;
        if (kind == PlayerImageKind.BoardBackground)
        {
            maximumDimension = PlayerImagePolicy.MaximumBackgroundOutputDimension;
        }

        int cropDimension = Math.Min(source.Width, source.Height);
        int outputDimension = Math.Min(cropDimension, maximumDimension);
        Rectangle crop = new Rectangle((source.Width - cropDimension) / 2,
            (source.Height - cropDimension) / 2, cropDimension, cropDimension);
        using (Bitmap target = new Bitmap(
            outputDimension, outputDimension, PixelFormat.Format24bppRgb))
        using (Graphics graphics = Graphics.FromImage(target))
        using (ImageAttributes attributes = new ImageAttributes())
        using (MemoryStream output = new MemoryStream())
        using (EncoderParameters parameters = new EncoderParameters(JpegEncoderParameterCount))
        {
            graphics.Clear(Color.White);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(source, new Rectangle(0, 0, outputDimension, outputDimension),
                crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, attributes);
            parameters.Param[0] = new EncoderParameter(
                Encoder.Quality, PlayerImagePolicy.JpegQuality);
            target.Save(output, FindJpegEncoder(), parameters);
            return new ProfileResult<byte[]>(output.ToArray());
        }
    }

    private static ImageCodecInfo FindJpegEncoder()
    {
        foreach (ImageCodecInfo encoder in ImageCodecInfo.GetImageEncoders())
        {
            if (encoder.FormatID == ImageFormat.Jpeg.Guid)
            {
                return encoder;
            }
        }

        throw new InvalidOperationException("The JPEG encoder is unavailable.");
    }
}
