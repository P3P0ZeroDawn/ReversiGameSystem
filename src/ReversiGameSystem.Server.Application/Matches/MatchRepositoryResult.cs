using System;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>
/// Identifies the outcome of a durable match persistence query or command.
/// </summary>
public enum MatchRepositoryStatus
{
    /// <summary>The persistence operation completed successfully.</summary>
    Success,

    /// <summary>An account required by the match is not active.</summary>
    InactiveAccount,

    /// <summary>The requested match was not found in persistence.</summary>
    MatchNotFound,

    /// <summary>The match is not in waiting status.</summary>
    MatchNotWaiting,

    /// <summary>The match is not in progress.</summary>
    MatchNotInProgress,

    /// <summary>The match code no longer identifies an open waiting match.</summary>
    MatchCodeUnavailable,

    /// <summary>The creator attempted to join their own match.</summary>
    CannotJoinOwnMatch,

    /// <summary>The account is not authorized for the requested match operation.</summary>
    Unauthorized,

    /// <summary>The destination friend account does not exist or is inactive.</summary>
    FriendAccountUnavailable,

    /// <summary>The accounts do not have an accepted active friendship.</summary>
    FriendshipNotFound
}

/// <summary>
/// Encapsulates the explicit outcome and optional payload of a match persistence query.
/// </summary>
/// <typeparam name="T">The type of the persisted entity or data payload.</typeparam>
public sealed class MatchRepositoryResult<T>
{
    private readonly T? _value;

    private MatchRepositoryResult(MatchRepositoryStatus status, T? value)
    {
        Status = status;
        _value = value;
    }

    /// <summary>
    /// Gets the persistence status outcome.
    /// </summary>
    public MatchRepositoryStatus Status { get; }

    /// <summary>
    /// Creates a successful persistence result containing the required payload.
    /// </summary>
    /// <param name="value">The persisted entity or value.</param>
    /// <returns>A successful persistence result.</returns>
    /// <exception cref="ArgumentNullException">The supplied value is null.</exception>
    public static MatchRepositoryResult<T> CreateSuccess(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new MatchRepositoryResult<T>(MatchRepositoryStatus.Success, value);
    }

    /// <summary>
    /// Creates a failed persistence result without a payload.
    /// </summary>
    /// <param name="status">The failure status.</param>
    /// <returns>A failed persistence result.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The status is Success.</exception>
    public static MatchRepositoryResult<T> CreateFailure(MatchRepositoryStatus status)
    {
        if (status == MatchRepositoryStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new MatchRepositoryResult<T>(status, default);
    }

    /// <summary>
    /// Gets the payload of a successful persistence operation.
    /// </summary>
    /// <returns>The persisted value.</returns>
    /// <exception cref="InvalidOperationException">The operation was not successful or has no value.</exception>
    public T GetValue()
    {
        if (Status != MatchRepositoryStatus.Success || _value is null)
        {
            throw new InvalidOperationException("A successful match persistence result must contain a value.");
        }

        return _value;
    }
}
