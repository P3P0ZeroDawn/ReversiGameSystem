using System;
using System.Text;

namespace ReversiGameSystem.Server.Application.Common;

/// <summary>
/// Sanitizes and masks user-supplied values before writing them to structured server logs.
/// </summary>
public static class LogSanitizer
{
    private const int DefaultMaxLength = 64;
    private const string EmptyPlaceholder = "[empty]";
    private const string InvalidEmailPlaceholder = "[invalid-email]";

    /// <summary>
    /// Removes control characters and bounds length to prevent log injection.
    /// </summary>
    /// <param name="value">The raw text value.</param>
    /// <param name="maxLength">The maximum permitted output length.</param>
    /// <returns>A single-line sanitized value, or a placeholder when empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxLength"/> is not positive.
    /// </exception>
    public static string SanitizeText(string? value, int maxLength = DefaultMaxLength)
    {
        if (maxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLength),
                "Maximum log value length must be positive.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return EmptyPlaceholder;
        }

        string trimmed = value!.Trim();
        StringBuilder builder = new StringBuilder(Math.Min(trimmed.Length, maxLength));
        for (int index = 0; index < trimmed.Length && builder.Length < maxLength; index++)
        {
            char character = trimmed[index];
            builder.Append(char.IsControl(character) ? ' ' : character);
        }

        string sanitized = builder.ToString().Trim();
        return sanitized.Length == 0 ? EmptyPlaceholder : sanitized;
    }

    /// <summary>
    /// Masks the local part of an email address while preserving its domain for auditing.
    /// </summary>
    /// <param name="emailAddress">The raw email address.</param>
    /// <returns>A redacted email representation, or a placeholder when invalid or empty.</returns>
    public static string MaskEmail(string? emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return EmptyPlaceholder;
        }

        string sanitized = SanitizeText(emailAddress, 254);
        int atIndex = sanitized.IndexOf('@');
        if (atIndex <= 0 || atIndex == sanitized.Length - 1)
        {
            return InvalidEmailPlaceholder;
        }

        string domain = sanitized.Substring(atIndex + 1);
        return sanitized[0] + "***@" + domain;
    }

    /// <summary>
    /// Masks an email login identifier or sanitizes a username login identifier.
    /// </summary>
    /// <param name="loginIdentifier">The raw username or email login identifier.</param>
    /// <returns>A safe representation suitable for audit logs.</returns>
    public static string MaskIdentifier(string? loginIdentifier)
    {
        if (string.IsNullOrWhiteSpace(loginIdentifier))
        {
            return EmptyPlaceholder;
        }

        return loginIdentifier!.IndexOf('@') >= 0
            ? MaskEmail(loginIdentifier)
            : SanitizeText(loginIdentifier);
    }
}
