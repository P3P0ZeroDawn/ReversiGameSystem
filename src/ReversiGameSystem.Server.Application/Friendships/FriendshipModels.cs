using System;
using System.Globalization;

namespace ReversiGameSystem.Server.Application.Friendships;

public sealed class FriendAccount
{
    public FriendAccount(long accountId, string username, bool hasProfileImage)
    {
        AccountId = accountId;
        Username = username ?? throw new ArgumentNullException(nameof(username));
        HasProfileImage = hasProfileImage;
    }

    public long AccountId { get; }

    public string Username { get; }

    public bool HasProfileImage { get; }
}

public enum FriendshipStatus
{
    Success,

    MissingSessionToken,

    InvalidSession,

    InactiveAccount,

    InvalidAccountId,

    AccountNotFound,

    FriendshipNotFound,

    CannotSendRequestToSelf,

    FriendshipAlreadyExists,

    RequestAlreadyPending,

    MissingRequestId,

    RequestUnavailable,

    UnauthorizedRequest
}

public sealed class PendingFriendRequest
{
    private const string RequestIdFormat = "{0}:{1}:{2}";

    public PendingFriendRequest(
        FriendAccount sender, FriendAccount recipient, DateTime createdAtUtc)
    {
        Sender = sender ?? throw new ArgumentNullException(nameof(sender));
        Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        RequestId = string.Format(CultureInfo.InvariantCulture, RequestIdFormat,
            Math.Min(sender.AccountId, recipient.AccountId),
            Math.Max(sender.AccountId, recipient.AccountId), CreatedAtUtc.Ticks);
    }

    public string RequestId { get; }

    public FriendAccount Sender { get; }

    public FriendAccount Recipient { get; }

    public DateTime CreatedAtUtc { get; }
}

public sealed class FriendshipResult<T>
{
    public FriendshipResult(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Status = FriendshipStatus.Success;
    }

    public FriendshipResult(FriendshipStatus status)
    {
        if (status == FriendshipStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A rejected friendship result cannot specify success status.");
        }

        Status = status;
        Value = default;
    }

    public FriendshipStatus Status { get; }

    public T? Value { get; }
}
