using System;
using System.Collections.Generic;
using ReversiGameSystem.Server.Domain.Matches;

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

    /// <summary>Gets or sets the number of consecutive timeouts accumulated by this participant.</summary>
    public int ConsecutiveTimeoutCount { get; set; }
}

/// <summary>Records an in-memory piece placement performed during active gameplay.</summary>
public sealed class MatchMove
{
    /// <summary>Gets or sets the one-based sequential move number.</summary>
    public int MoveNumber { get; set; }

    /// <summary>Gets or sets the participant seat: one for black or two for white.</summary>
    public int SeatNumber { get; set; }

    /// <summary>Gets or sets the zero-based board row (0..7).</summary>
    public int Row { get; set; }

    /// <summary>Gets or sets the zero-based board column (0..7).</summary>
    public int Column { get; set; }

    /// <summary>Gets or sets the UTC timestamp confirmed by the server.</summary>
    public DateTime PlayedAtUtc { get; set; }

    /// <summary>Gets or sets the count of opposing pieces flipped by this placement.</summary>
    public int FlippedPieceCount { get; set; }

    /// <summary>Gets or sets whether the placement originated from a player, a timeout or the system.</summary>
    public string Origin { get; set; } = "Player";
}

/// <summary>Encapsulates the parameters required to execute a piece placement attempt.</summary>
public sealed class MatchPlacement
{
    /// <summary>Initializes a new match placement attempt.</summary>
    /// <param name="matchId">The target positive match identifier.</param>
    /// <param name="expectedVersion">The confirmed match state version expected by the client.</param>
    /// <param name="row">The zero-based board row (0..7).</param>
    /// <param name="column">The zero-based board column (0..7).</param>
    public MatchPlacement(long matchId, long expectedVersion, int row, int column)
    {
        MatchId = matchId;
        ExpectedVersion = expectedVersion;
        Row = row;
        Column = column;
    }

    /// <summary>Gets the target match identifier.</summary>
    public long MatchId { get; }

    /// <summary>Gets the expected confirmed match state version.</summary>
    public long ExpectedVersion { get; }

    /// <summary>Gets the zero-based board row index.</summary>
    public int Row { get; }

    /// <summary>Gets the zero-based board column index.</summary>
    public int Column { get; }
}

/// <summary>Contains confirmed match data independently of transport contracts.</summary>
public sealed class Match
{
    private int? _currentTurnSeatNumber;
    private DateTime? _turnStartedAtUtc;

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
            return ReversiEngine.CreateInitialBoard();
        }
    }

    /// <summary>Gets or sets the confirmed active board state in row-major order.</summary>
    public int[] Board { get; set; } = ReversiEngine.CreateInitialBoard();

    /// <summary>Gets or sets the confirmed version supported by this increment.</summary>
    public long MatchStateVersion { get; set; }

    /// <summary>Gets or sets the creator seat holding the initial turn, or no active seat.</summary>
    public int? CurrentTurnSeatNumber
    {
        get
        {
            if (_currentTurnSeatNumber.HasValue)
            {
                return _currentTurnSeatNumber.Value;
            }

            if (State == "InProgress")
            {
                return 1;
            }

            return null;
        }
        set
        {
            _currentTurnSeatNumber = value;
        }
    }

    /// <summary>Gets or sets the initial turn start persisted with the match start.</summary>
    public DateTime? TurnStartedAtUtc
    {
        get
        {
            return _turnStartedAtUtc ?? StartedAtUtc;
        }
        set
        {
            _turnStartedAtUtc = value;
        }
    }

    /// <summary>Gets or sets the placement made in the most recent state transition.</summary>
    public MatchMove? LastMove { get; set; }

    /// <summary>Gets or sets the seat whose turn was skipped in the most recent transition, if any.</summary>
    public int? SkippedSeatNumber { get; set; }

    /// <summary>Gets or sets the sequential list of confirmed placements played in this match.</summary>
    public List<MatchMove> Moves { get; set; } = new List<MatchMove>();

    /// <summary>Gets or sets the reason the match concluded, or null while in progress.</summary>
    public string? EndReason { get; set; }

    /// <summary>Gets or sets the winning participant seat (1 or 2), or null for a draw or in progress.</summary>
    public int? WinnerSeatNumber { get; set; }

    /// <summary>Gets or sets the participant seat that abandoned the match, if applicable.</summary>
    public int? AbandoningSeatNumber { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the match was concluded, or null while in progress.</summary>
    public DateTime? EndedAtUtc { get; set; }

    /// <summary>Gets the synchronization root used to serialize concurrent state mutations on this match.</summary>
    public object SyncRoot { get; } = new object();
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
    FriendshipNotFound,

    /// <summary>The piece placement object is absent.</summary>
    MissingPlacement,

    /// <summary>The supplied match state version is negative.</summary>
    InvalidMatchStateVersion,

    /// <summary>The supplied match state version differs from the confirmed version.</summary>
    StaleMatchStateVersion,

    /// <summary>The requesting participant does not hold the active turn.</summary>
    NotPlayersTurn,

    /// <summary>The target cell is outside the board, occupied, or yields no captures.</summary>
    InvalidPosition,

    /// <summary>Observation cannot begin in the requested match state.</summary>
    SpectatingUnavailable,

    /// <summary>The match has already concluded.</summary>
    MatchFinished
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
