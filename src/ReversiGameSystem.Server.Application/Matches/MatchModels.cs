using System;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Accepted human match configuration.</summary>
public sealed class MatchSettings
{
    /// <summary>Gets or sets the accepted duration per turn in seconds.</summary>
    public int TurnDurationSeconds { get; set; } = 40;

    /// <summary>Gets or sets whether completed results are eligible for statistics.</summary>
    public bool CountsTowardStatistics { get; set; }

    /// <summary>Gets or sets the proposed opponent; this increment accepts Human only.</summary>
    public string OpponentKind { get; set; } = "Human";
}

/// <summary>Durable identity of a participant.</summary>
public sealed class MatchParticipant
{
    /// <summary>Gets or sets the participant seat, either one or two.</summary>
    public int SeatNumber { get; set; }

    /// <summary>Gets or sets the stable server-resolved account identifier.</summary>
    public long AccountId { get; set; }

    /// <summary>Gets or sets the public name recorded when joining.</summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Contains confirmed match data independently of transport contracts.</summary>
public sealed class Match
{
    /// <summary>Gets or sets the stable match identifier.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the join code while the lobby accepts joins.</summary>
    public string? Code { get; set; }

    /// <summary>Gets or sets the confirmed lifecycle state.</summary>
    public string State { get; set; } = "Waiting";

    /// <summary>Gets or sets the accepted duration per turn in seconds.</summary>
    public int TurnDurationSeconds { get; set; }

    /// <summary>Gets or sets whether completed results are eligible for statistics.</summary>
    public bool CountsTowardStatistics { get; set; }

    /// <summary>Gets or sets the durable UTC match start, absent while waiting.</summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>Gets or sets the confirmed participants in seat order.</summary>
    public MatchParticipant[] Participants { get; set; } = Array.Empty<MatchParticipant>();

    /// <summary>Gets or sets whether the match is a private friend invitation.</summary>
    public bool IsPrivate { get; set; }

    /// <summary>Gets a fresh initial board: zero is empty, one black, two white.</summary>
    public int[] InitialBoard
    {
        get
        {
            return CreateInitialBoard();
        }
    }

    /// <summary>Gets the initial confirmed version supported by this increment.</summary>
    public long MatchStateVersion
    {
        get
        {
            return 0;
        }
    }

    /// <summary>Gets the creator seat holding the initial turn, or no active seat.</summary>
    public int? CurrentTurnSeatNumber
    {
        get
        {
            int? seatNumber = null;
            if (State == "InProgress")
            {
                seatNumber = 1;
            }

            return seatNumber;
        }
    }

    /// <summary>Gets the initial turn start persisted with the match start.</summary>
    public DateTime? TurnStartedAtUtc
    {
        get
        {
            return StartedAtUtc;
        }
    }

    private static int[] CreateInitialBoard()
    {
        int[] board = new int[64];
        board[27] = 2;
        board[36] = 2;
        board[28] = 1;
        board[35] = 1;
        return board;
    }
}

/// <summary>Contains one prepared private match and its server-resolved destination.</summary>
public sealed class MatchInvitationPreparation
{
    /// <summary>Creates the private match preparation details.</summary>
    public MatchInvitationPreparation(Match match, string friendEmail)
    {
        Match = match ?? throw new ArgumentNullException(nameof(match));
        FriendEmail = friendEmail ?? throw new ArgumentNullException(nameof(friendEmail));
    }

    /// <summary>Gets the prepared match.</summary>
    public Match Match { get; }

    /// <summary>Gets the destination email address.</summary>
    public string FriendEmail { get; }
}

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
