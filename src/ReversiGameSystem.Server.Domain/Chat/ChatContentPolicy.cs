using System;
using System.Globalization;
using System.Text;

namespace ReversiGameSystem.Server.Domain.Chat;

public static class ChatContentPolicy
{
    public const int MaximumMessageLength = 200;

    public const int ViolationsRequiredForMute = 3;

    public const string RepeatedViolationsReasonCode = "REPEATED_VIOLATIONS";

    public static readonly TimeSpan ViolationWindow = TimeSpan.FromHours(24);

    public static readonly TimeSpan MuteDuration = TimeSpan.FromMinutes(10);

    public static bool Matches(string content, string pattern)
    {
        string normalizedPattern = Normalize(pattern);
        return normalizedPattern.Length > 0
            && (" " + Normalize(content) + " ").Contains(" " + normalizedPattern + " ");
    }

    private static string Normalize(string value)
    {
        StringBuilder result = new StringBuilder();
        bool separator = true;
        foreach (char character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            if (char.IsLetterOrDigit(character))
            {
                result.Append(char.ToLowerInvariant(character));
                separator = false;
            }
            else if (!separator)
            {
                result.Append(' ');
                separator = true;
            }
        }
        return result.ToString().Trim();
    }
}
