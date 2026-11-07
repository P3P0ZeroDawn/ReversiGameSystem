using System;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>
/// Identifies an expected result of an authorized match operation.
/// </summary>
public enum MatchOperationStatus
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
/// Contains the expected status and successful value of a match operation.
/// </summary>
/// <typeparam name="T">The type of the operation payload.</typeparam>
public sealed class MatchOperationResult<T>
{
    private readonly T? _value;

    private MatchOperationResult(MatchOperationStatus status, T? value)
    {
        Status = status;
        _value = value;
    }

    /// <summary>
    /// Gets the expected operation status.
    /// </summary>
    public MatchOperationStatus Status { get; }

    /// <summary>
    /// Creates a successful operation result containing the required value.
    /// </summary>
    /// <param name="value">The operation value.</param>
    /// <returns>A successful operation result.</returns>
    /// <exception cref="ArgumentNullException">The supplied value is null.</exception>
    public static MatchOperationResult<T> CreateSuccess(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new MatchOperationResult<T>(MatchOperationStatus.Success, value);
    }

    /// <summary>
    /// Creates a failed operation result without a value.
    /// </summary>
    /// <param name="status">The failure status.</param>
    /// <returns>A failed operation result.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The status is Success.</exception>
    public static MatchOperationResult<T> CreateFailure(MatchOperationStatus status)
    {
        if (status == MatchOperationStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new MatchOperationResult<T>(status, default);
    }

    /// <summary>
    /// Gets the value of a successful operation.
    /// </summary>
    /// <returns>The operation value.</returns>
    /// <exception cref="InvalidOperationException">The operation was not successful or has no value.</exception>
    public T GetValue()
    {
        if (Status != MatchOperationStatus.Success || _value is null)
        {
            throw new InvalidOperationException("A successful match result must contain a value.");
        }

        return _value;
    }
}
