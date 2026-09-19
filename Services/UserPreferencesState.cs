using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Services;

/// <summary>
/// Holds the signed-in player's appearance preferences for the current session.
/// </summary>
internal static class UserPreferencesState
{
    internal static bool UseProfilePicturesOnPieces { get; set; }

    internal static bool UseCustomBoardBackground { get; set; }

    internal static BitmapSource? BoardBackgroundPreview { get; private set; }

    internal static void SetBoardBackground(BitmapSource preview)
    {
        BoardBackgroundPreview = preview;
        UseCustomBoardBackground = true;
    }

    internal static void Reset()
    {
        UseProfilePicturesOnPieces = false;
        UseCustomBoardBackground = false;
        BoardBackgroundPreview = null;
    }
}
