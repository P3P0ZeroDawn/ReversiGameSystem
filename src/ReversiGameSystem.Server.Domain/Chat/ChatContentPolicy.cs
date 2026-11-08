using System;
using System.Globalization;
using System.Text;

namespace ReversiGameSystem.Server.Domain.Chat;

/// <summary>Matches complete words and phrases without case or diacritic distinctions.</summary>
public static class ChatContentPolicy
{
    /// <summary>Maximum accepted message length in UTF-16 code units.</summary>
    public const int MaximumMessageLength = 200;

    /// <summary>Number of violations that activates an account mute.</summary>
    public const int ViolationsRequiredForMute = 3;

    /// <summary>Stable reason stored and reported for repeated violations.</summary>
    public const string RepeatedViolationsReasonCode = "REPEATED_VIOLATIONS";

    /// <summary>Rolling period used to count previous violations.</summary>
    public static readonly TimeSpan ViolationWindow = TimeSpan.FromHours(24);

    /// <summary>Duration of a mute created by repeated violations.</summary>
    public static readonly TimeSpan MuteDuration = TimeSpan.FromMinutes(10);

    /// <summary>Compares complete words and phrases after case and diacritic normalization.</summary>
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
