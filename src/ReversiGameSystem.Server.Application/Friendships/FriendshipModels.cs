using System;

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

    FriendshipNotFound
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
