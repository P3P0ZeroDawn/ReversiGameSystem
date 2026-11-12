namespace ReversiGameSystem.Services;

internal enum FriendshipStatus
{
    Success,
    InvalidSession,
    ServiceUnavailable
}

internal sealed class FriendshipResult
{
    internal FriendshipResult(FriendshipStatus status, FriendSummary[] friends)
    {
        Status = status;
        Friends = friends;
    }

    internal FriendshipStatus Status { get; }
    internal FriendSummary[] Friends { get; }
}

internal enum FriendChange
{
    Connected,
    Disconnected,
    Removed
}

internal sealed class FriendSummary
{
    internal FriendSummary(long accountId, string username, bool hasProfileImage, bool isOnline)
    {
        AccountId = accountId;
        Username = username;
        HasProfileImage = hasProfileImage;
        IsOnline = isOnline;
    }

    internal long AccountId { get; }
    internal string Username { get; }
    internal bool HasProfileImage { get; }
    internal bool IsOnline { get; set; }
}
