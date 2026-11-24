using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Validation;

internal static class BoardBackgroundPolicy
{
    internal const long MaximumFileBytes = ProfilePicturePolicy.MaximumFileBytes;
    internal const int MaximumDimension = ProfilePicturePolicy.MaximumDimension;
    internal const int PreviewSize = PlayerImagePolicy.MaximumBackgroundOutputDimension;

    internal static BitmapSource? CreatePreview(string path)
    {
        return ProfilePicturePolicy.CreatePreview(path, PreviewSize);
    }
}
