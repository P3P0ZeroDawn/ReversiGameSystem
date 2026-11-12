using System.Windows.Media.Imaging;

namespace ReversiGameSystem.Services;

internal sealed class ClientSession
{
    internal string? Username { get; private set; }

    internal string? Email { get; private set; }

    internal long? AccountId { get; private set; }

    internal string? SessionToken { get; private set; }

    internal string? GuestName { get; private set; }

    internal BitmapSource? ProfilePicture { get; private set; }

    internal bool IsGuest
    {
        get { return GuestName != null; }
    }

    internal void StartAuthenticated(
        AuthenticatedAccountSession session,
        BitmapSource? profilePicture = null)
    {
        Username = session.Username;
        Email = session.EmailAddress;
        AccountId = session.AccountId;
        SessionToken = session.SessionToken;
        GuestName = null;
        ProfilePicture = profilePicture;
        UserPreferencesState.Reset();
    }

    internal void StartGuest(GuestAccountSession session)
    {
        Username = null;
        Email = null;
        AccountId = null;
        SessionToken = session.SessionToken;
        GuestName = session.DisplayName;
        ProfilePicture = null;
        UserPreferencesState.Reset();
        FriendshipPreviewState.Reset();
    }

    internal void UpdateUsername(string username)
    {
        Username = username;
    }

    internal void UpdateProfilePicture(BitmapSource profilePicture)
    {
        ProfilePicture = profilePicture;
    }

    internal void Clear()
    {
        Username = null;
        Email = null;
        AccountId = null;
        SessionToken = null;
        GuestName = null;
        ProfilePicture = null;
        UserPreferencesState.Reset();
        FriendshipPreviewState.Reset();
    }
}
