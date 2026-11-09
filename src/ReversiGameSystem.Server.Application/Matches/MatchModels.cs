using System;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>
/// Identifies an expected result of a match operation or persistence query.
/// </summary>
public enum MatchStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success,

    /// <summary>The required session token is absent.</summary>
    MissingSessionToken,

    /// <summary>The session is absent, expired, revoked, or not a registered account.</summary>
    InvalidSession,

    /// <summary>The account is not active.</summary>
    InactiveAccount,

    /// <summary>The match identifier is not positive.</summary>
    InvalidMatchId,

    /// <summary>The proposed match configuration is unsupported.</summary>
    InvalidConfiguration,

    /// <summary>The proposed turn duration is outside the accepted range.</summary>
    InvalidTurnDuration,

    /// <summary>The requested match does not exist.</summary>
    MatchNotFound,

    /// <summary>The creator attempted to occupy the second seat.</summary>
    CannotJoinOwnMatch,

    /// <summary>The supplied match code has an invalid format.</summary>
    InvalidMatchCode,

    /// <summary>The match code no longer identifies a joinable match.</summary>
    MatchCodeUnavailable,

    /// <summary>The caller is not authorized for the requested match operation.</summary>
    Unauthorized,

    /// <summary>The requested operation requires a waiting match.</summary>
    MatchNotWaiting,

    /// <summary>The requested operation requires an active match.</summary>
    MatchNotInProgress,

    /// <summary>The friend account identifier is not positive.</summary>
    InvalidFriendAccountId,

    /// <summary>The destination friend account does not exist or is inactive.</summary>
    FriendAccountUnavailable,

    /// <summary>The accounts do not have an accepted active friendship.</summary>
    FriendshipNotFound
}

/// <summary>
/// Encapsulates the expected status and optional data payload of a match operation.
/// </summary>
/// <typeparam name="T">The type of the internal model payload.</typeparam>
public sealed class MatchResult<T>
{
    /// <summary>
    /// Initializes a successful match result containing the required payload.
    /// </summary>
    /// <param name="value">The confirmed internal model.</param>
    /// <exception cref="ArgumentNullException">The supplied value is null.</exception>
    public MatchResult(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Status = MatchStatus.Success;
    }

    /// <summary>
    /// Initializes a rejected match result with its actionable status.
    /// </summary>
    /// <param name="status">The failure status.</param>
    /// <exception cref="ArgumentOutOfRangeException">The status represents success.</exception>
    public MatchResult(MatchStatus status)
    {
        if (status == MatchStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A rejected match result cannot specify success status.");
        }

        Status = status;
        Value = default;
    }

    /// <summary>
    /// Gets the authoritative operation status.
    /// </summary>
    public MatchStatus Status { get; }

    /// <summary>
    /// Gets the internal model payload, or null when the operation failed.
    /// </summary>
    public T? Value { get; }
}

/// <summary>Identifies expected match failures in services awaiting result migration.</summary>
public enum MatchRejection
{
    /// <summary>The match identifier is not positive.</summary>
    InvalidMatchId,
    /// <summary>The proposed match configuration is unsupported.</summary>
    InvalidConfiguration,
    /// <summary>The proposed turn duration is outside the accepted range.</summary>
    InvalidTurnDuration,
    /// <summary>The requested match does not exist.</summary>
    MatchNotFound,
    /// <summary>The creator attempted to occupy the second seat.</summary>
    CannotJoinOwnMatch,
    /// <summary>The supplied match code has an invalid format.</summary>
    InvalidMatchCode,
    /// <summary>The match code no longer identifies a joinable match.</summary>
    MatchCodeUnavailable,
    /// <summary>The caller does not own or participate in the requested match.</summary>
    Unauthorized,
    /// <summary>The requested operation requires a waiting match.</summary>
    MatchNotWaiting,
    /// <summary>The account is not active.</summary>
    InactiveAccount,
    /// <summary>The requested operation requires an active match.</summary>
    MatchNotInProgress
}

/// <summary>Reports an expected match failure in services awaiting result migration.</summary>
public sealed class MatchRejectedException : Exception
{
    /// <summary>Creates an expected match rejection with its actionable reason.</summary>
    public MatchRejectedException(MatchRejection reason)
        : base("Match request rejected.")
    {
        Reason = reason;
    }

    /// <summary>Gets the actionable expected rejection reason.</summary>
    public MatchRejection Reason { get; }
}
