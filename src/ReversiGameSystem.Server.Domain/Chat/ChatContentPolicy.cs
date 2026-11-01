using System.Globalization;
using System.Text;

namespace ReversiGameSystem.Server.Domain.Chat;

/// <summary>Matches complete words and phrases without case or diacritic distinctions.</summary>
public static class ChatContentPolicy
{
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
