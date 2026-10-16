using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Services;

/// <summary>
/// Holds the authenticated account details used by the client session.
/// </summary>
internal static class AccountPreviewState
{
    internal static string? Username { get; private set; }

    internal static string? Email { get; private set; }

    internal static long? AccountId { get; private set; }

    internal static string? SessionToken { get; private set; }

    internal static BitmapSource? ProfilePicture { get; private set; }

    internal static void Start(
        AuthenticatedAccountSession session,
        BitmapSource? profilePicture = null)
    {
        Email = session.EmailAddress;
        Username = session.Username;
        AccountId = session.AccountId;
        SessionToken = session.SessionToken;
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
        AccountId = null;
        SessionToken = null;
        ProfilePicture = null;
        UserPreferencesState.Reset();
        FriendshipPreviewState.Reset();
    }
}
