using System;

namespace ReversiGameSystem.Services;

internal enum FriendshipStatus
{
    Success,
    InvalidSession,
    ServiceUnavailable,
    MissingSessionToken,
    InactiveAccount,
    InvalidAccountId,
    CannotSendRequestToSelf,
    AccountUnavailable,
    FriendshipAlreadyExists,
    RequestAlreadyPending,
    MissingRequestId,
    RequestUnavailable,
    UnauthorizedRequest,
    FriendshipNotFound,
    SubmissionInProgress
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

internal sealed class PlayerAccount
{
    internal PlayerAccount(long accountId, string username, bool hasProfileImage)
    {
        AccountId = accountId;
        Username = username;
        HasProfileImage = hasProfileImage;
    }

    public long AccountId { get; }
    public string Username { get; }
    public bool HasProfileImage { get; }
}

internal sealed class PendingFriendRequest
{
    internal PendingFriendRequest(string requestId, PlayerAccount sender,
        PlayerAccount recipient, DateTime createdAtUtc)
    {
        RequestId = requestId;
        Sender = sender;
        Recipient = recipient;
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
    }

    public string RequestId { get; }
    public PlayerAccount Sender { get; }
    public PlayerAccount Recipient { get; }
    public DateTime CreatedAtUtc { get; }
}
