using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Services;

internal static class UserPreferencesState
{
    internal static bool UseProfilePicturesOnPieces { get; private set; }

    internal static bool UseCustomBoardBackground { get; private set; }

    internal static BitmapSource? BoardBackgroundPreview { get; private set; }

    internal static void SetBoardBackground(BitmapSource preview)
    {
        BoardBackgroundPreview = preview;
    }

    internal static void ApplyPreferences(PlayerPreferences preferences)
    {
        UseProfilePicturesOnPieces =
            preferences.PieceStyleCode == PlayerPreferences.ProfilePicturesPieceStyle;
        UseCustomBoardBackground = preferences.UseCustomBoardBackground;
    }

    internal static void ClearBoardBackground()
    {
        BoardBackgroundPreview = null;
    }

    internal static void Reset()
    {
        UseProfilePicturesOnPieces = false;
        UseCustomBoardBackground = false;
        BoardBackgroundPreview = null;
    }
}
