using System.Globalization;

namespace ReversiGameSystem.Game;

/// <summary>
/// Defines the supported turn-duration range for creating or modifying a match.
/// </summary>
internal static class TurnTimePolicy
{
    internal const int DefaultSeconds = 40;
    internal const int MinimumSeconds = 10;
    internal const int MaximumSeconds = 120;

    internal static bool TryParse(string? text, out int seconds)
    {
        return int.TryParse(
                text,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out seconds)
            && seconds >= MinimumSeconds
            && seconds <= MaximumSeconds;
    }
}
