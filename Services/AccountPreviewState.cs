using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Services;

/// <summary>
/// Holds account details known to the client while server sessions are pending.
/// </summary>
internal static class AccountPreviewState
{
    internal static string? Username { get; private set; }

    internal static string? Email { get; private set; }

    internal static BitmapSource? ProfilePicture { get; private set; }

    internal static void Start(
        string email,
        string? username,
        BitmapSource? profilePicture = null)
    {
        Email = email;
        Username = username;
        ProfilePicture = profilePicture;
        UserPreferencesState.Reset();
    }

    internal static void UpdateUsername(string username)
    {
        Username = username;
    }

    internal static void UpdateProfilePicture(BitmapSource profilePicture)
    {
        ProfilePicture = profilePicture;
    }

    internal static void Clear()
    {
        Email = null;
        Username = null;
        ProfilePicture = null;
        UserPreferencesState.Reset();
        FriendshipPreviewState.Reset();
    }
}
