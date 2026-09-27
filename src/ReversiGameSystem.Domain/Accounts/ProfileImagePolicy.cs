using System;

namespace ReversiGameSystem.Domain.Accounts;

/// <summary>
/// Represents profile image policy.
/// </summary>
public static class ProfileImagePolicy
{
    /// <summary>
    /// Represents the maximum file bytes.
    /// </summary>
    public const int MaximumFileBytes = 5 * 1024 * 1024;
    /// <summary>
    /// Represents the maximum dimension.
    /// </summary>
    public const int MaximumDimension = 4096;

    /// <summary>
    /// Performs the is supported operation.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="mediaType">The media type.</param>
    /// <returns>The result of the operation.</returns>
    public static bool IsSupported(byte[] content, string mediaType)
    {
        if (content == null || content.Length == 0 || content.Length > MaximumFileBytes)
        {
            return false;
        }

        if (string.Equals(mediaType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            return HasPngSignature(content);
        }

        if (string.Equals(mediaType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return HasJpegSignature(content);
        }

        return false;
    }

    private static bool HasPngSignature(byte[] content)
    {
        byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (content.Length < signature.Length)
        {
            return false;
        }

        for (int index = 0; index < signature.Length; index++)
        {
            if (content[index] != signature[index])
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasJpegSignature(byte[] content)
    {
        return content.Length >= 4
            && content[0] == 0xFF
            && content[1] == 0xD8
            && content[content.Length - 2] == 0xFF
            && content[content.Length - 1] == 0xD9;
    }
}
