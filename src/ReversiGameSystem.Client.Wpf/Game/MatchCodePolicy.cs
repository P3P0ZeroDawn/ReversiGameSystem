namespace ReversiGameSystem.Game;

/// <summary>
/// Defines the format used by temporary match codes.
/// </summary>
internal static class MatchCodePolicy
{
    internal const int Length = 6;
    internal const string AllowedCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    internal static bool TryNormalize(string? text, out string code)
    {
        code = (text ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length != Length)
        {
            return false;
        }

        foreach (char character in code)
        {
            if (AllowedCharacters.IndexOf(character) < 0)
            {
                return false;
            }
        }

        return true;
    }
}
