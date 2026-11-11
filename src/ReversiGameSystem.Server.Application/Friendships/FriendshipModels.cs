using System;

namespace ReversiGameSystem.Server.Application.Friendships;

/// <summary>Contains the public fields permitted in a friend listing.</summary>
public sealed class FriendAccount
{
    /// <summary>Initializes a new instance of the <see cref="FriendAccount"/> class.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="username">The public username.</param>
    /// <param name="hasProfileImage">Whether a custom image exists.</param>
    /// <exception cref="ArgumentNullException">The supplied username is null.</exception>
    public FriendAccount(long accountId, string username, bool hasProfileImage)
    {
        AccountId = accountId;
        Username = username ?? throw new ArgumentNullException(nameof(username));
        HasProfileImage = hasProfileImage;
    }

    /// <summary>Gets the account identifier.</summary>
    public long AccountId { get; }

    /// <summary>Gets the public username.</summary>
    public string Username { get; }

    /// <summary>Gets whether a custom image exists.</summary>
    public bool HasProfileImage { get; }
}

/// <summary>
/// Identifies the outcome of a friendship operation.
/// </summary>
public enum FriendshipStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success,

    /// <summary>The required session token was omitted.</summary>
    MissingSessionToken,

    /// <summary>The supplied session is unknown, expired, or revoked.</summary>
    InvalidSession,

    /// <summary>The authenticated account is no longer active.</summary>
    InactiveAccount,

    /// <summary>The target friend account identifier is not positive.</summary>
    InvalidAccountId,

    /// <summary>The destination friend account does not exist or is inactive.</summary>
    AccountNotFound,

    /// <summary>No active accepted friendship exists between the accounts.</summary>
    FriendshipNotFound
}

/// <summary>
/// Encapsulates the expected status and optional data payload of a friendship operation.
/// </summary>
/// <typeparam name="T">The type of the internal model payload.</typeparam>
public sealed class FriendshipResult<T>
{
    /// <summary>
    /// Initializes a successful friendship result containing the required payload.
    /// </summary>
    /// <param name="value">The confirmed internal model payload.</param>
    /// <exception cref="ArgumentNullException">The supplied value is null.</exception>
    public FriendshipResult(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Status = FriendshipStatus.Success;
    }

    /// <summary>
    /// Initializes a rejected friendship result with its actionable status.
    /// </summary>
    /// <param name="status">The failure status.</param>
    /// <exception cref="ArgumentOutOfRangeException">The status represents success.</exception>
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

    /// <summary>
    /// Gets the authoritative operation status.
    /// </summary>
    public FriendshipStatus Status { get; }

    /// <summary>
    /// Gets the internal model payload, or null when the operation failed.
    /// </summary>
    public T? Value { get; }
}
