using System;

namespace ReversiGameSystem.Validation;

public static class PlayerImagePolicy
{
    public const string PngMediaType = "image/png";
    public const string JpegMediaType = "image/jpeg";
    public const int MaximumFileBytes = 5 * 1024 * 1024;
    public const int MaximumDimension = 4096;
    public const int MaximumProfileOutputDimension = 256;
    public const int MaximumBackgroundOutputDimension = 1024;

    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private static readonly byte[] JpegStartMarker = { 0xFF, 0xD8 };
    private static readonly byte[] JpegEndMarker = { 0xFF, 0xD9 };

    public static bool IsSupported(byte[] content, string mediaType)
    {
        if (content == null || content.Length == 0 || content.Length > MaximumFileBytes)
        {
            return false;
        }

        if (string.Equals(mediaType, PngMediaType, StringComparison.OrdinalIgnoreCase))
        {
            return HasSignature(content, PngSignature, 0);
        }

        if (string.Equals(mediaType, JpegMediaType, StringComparison.OrdinalIgnoreCase))
        {
            return HasJpegSignature(content);
        }

        return false;
    }

    private static bool HasSignature(byte[] content, byte[] signature, int offset)
    {
        if (offset < 0 || content.Length - offset < signature.Length)
        {
            return false;
        }
        for (int index = 0; index < signature.Length; index++)
        {
            if (content[offset + index] != signature[index])
            {
                return false;
            }
        }
        return true;
    }

    private static bool HasJpegSignature(byte[] content)
    {
        return content.Length >= JpegStartMarker.Length + JpegEndMarker.Length
            && HasSignature(content, JpegStartMarker, 0)
            && HasSignature(content, JpegEndMarker, content.Length - JpegEndMarker.Length);
    }

}
