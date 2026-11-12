using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Validation;

/// <summary>
/// Applies the supported image rules to a board background preview.
/// </summary>
internal static class BoardBackgroundPolicy
{
    internal const long MaximumFileBytes = ProfilePicturePolicy.MaximumFileBytes;
    internal const int MaximumDimension = ProfilePicturePolicy.MaximumDimension;

    internal static BitmapSource? CreatePreview(string path)
    {
        return ProfilePicturePolicy.CreatePreview(path, 512);
    }
}
