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

/// <summary>Identifies an expected result of a match operation.</summary>
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

/// <summary>Contains the expected status and successful value of a match operation.</summary>
public sealed class MatchOperationResult<T>
{
    private readonly T? _value;

    private MatchOperationResult(MatchOperationStatus status, T? value)
    {
        Status = status;
        _value = value;
    }

    /// <summary>Gets the expected operation status.</summary>
    public MatchOperationStatus Status { get; }

    /// <summary>Creates a successful operation result.</summary>
    public static MatchOperationResult<T> CreateSuccess(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new MatchOperationResult<T>(MatchOperationStatus.Success, value);
    }

    /// <summary>Creates a failed operation result without a value.</summary>
    public static MatchOperationResult<T> CreateFailure(MatchOperationStatus status)
    {
        if (status == MatchOperationStatus.Success)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new MatchOperationResult<T>(status, default);
    }

    /// <summary>Gets the value of a successful operation.</summary>
    public T GetValue()
    {
        if (Status != MatchOperationStatus.Success || _value is null)
        {
            throw new InvalidOperationException(
                "A successful match result must contain a value.");
        }

        return _value;
    }
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

/// <summary>Identifies friendship failures in match invitation operations.</summary>
public enum FriendshipRejection
{
    /// <summary>The friend account identifier is not positive.</summary>
    InvalidAccountId,
    /// <summary>The destination account does not exist or is inactive.</summary>
    AccountUnavailable,
    /// <summary>The accounts do not have an accepted active friendship.</summary>
    FriendshipNotFound,
    /// <summary>The requesting account is inactive.</summary>
    InactiveAccount
}

/// <summary>Reports a friendship failure in services awaiting result migration.</summary>
public sealed class FriendshipRejectedException : Exception
{
    /// <summary>Creates an expected friendship rejection with its actionable reason.</summary>
    public FriendshipRejectedException(FriendshipRejection reason)
        : base("Friendship request rejected.")
    {
        Reason = reason;
    }

    /// <summary>Gets the actionable expected rejection reason.</summary>
    public FriendshipRejection Reason { get; }
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
