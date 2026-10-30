using System;
using ReversiGameSystem.Server.Application.Accounts;

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
public sealed class ParticipantRecord
{
    /// <summary>Gets or sets the participant seat, either one or two.</summary>
    public int SeatNumber { get; set; }
    /// <summary>Gets or sets the stable server-resolved account identifier.</summary>
    public long AccountId { get; set; }
    /// <summary>Gets or sets the public name recorded when joining.</summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Confirmed match data, independent of transport contracts.</summary>
public sealed class MatchRecord
{
    /// <summary>Gets or sets the stable match identifier.</summary>
    public long Id { get; set; }
    /// <summary>Gets or sets the join code; absent once the lobby stops accepting joins.</summary>
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
    public ParticipantRecord[] Participants { get; set; } = Array.Empty<ParticipantRecord>();
    /// <summary>Gets a fresh initial board: zero is empty, one black, two white.</summary>
    public int[] InitialBoard => CreateInitialBoard();
    /// <summary>Gets the initial confirmed version supported by this increment.</summary>
    public long MatchStateVersion => 0;
    /// <summary>Gets the creator seat holding the initial turn, or no active seat.</summary>
    public int? CurrentTurnSeatNumber => State == "InProgress" ? 1 : (int?)null;
    /// <summary>Gets the initial turn start persisted with the match start.</summary>
    public DateTime? TurnStartedAtUtc => StartedAtUtc;

    private static int[] CreateInitialBoard()
    {
        int[] board = new int[64];
        board[27] = board[36] = 2;
        board[28] = board[35] = 1;
        return board;
    }
}

/// <summary>Expected authoritative match rejection.</summary>
public enum MatchRejection
{
    /// <summary>Identifies the InvalidMatchId rejection.</summary>
    InvalidMatchId,
    /// <summary>Identifies the InvalidConfiguration rejection.</summary>
    InvalidConfiguration,
    /// <summary>Identifies the InvalidTurnDuration rejection.</summary>
    InvalidTurnDuration,
    /// <summary>Identifies the MatchNotFound rejection.</summary>
    MatchNotFound,
    /// <summary>Identifies the CannotJoinOwnMatch rejection.</summary>
    CannotJoinOwnMatch,
    /// <summary>Identifies the InvalidMatchCode rejection.</summary>
    InvalidMatchCode,
    /// <summary>Identifies the MatchCodeUnavailable rejection.</summary>
    MatchCodeUnavailable,
    /// <summary>Identifies the Unauthorized rejection.</summary>
    Unauthorized,
    /// <summary>Identifies the MatchNotWaiting rejection.</summary>
    MatchNotWaiting,
    /// <summary>Identifies the InactiveAccount rejection.</summary>
    InactiveAccount,
    /// <summary>Identifies the MatchNotInProgress rejection.</summary>
    MatchNotInProgress,
}

/// <summary>Expected match failure, without a communication dependency.</summary>
public sealed class MatchRejectedException : Exception
{
    /// <summary>Creates an expected match rejection with its actionable reason.</summary>
    public MatchRejectedException(MatchRejection reason) : base("Match request rejected.")
    {
        Reason = reason;
    }
    /// <summary>Gets the actionable expected rejection reason.</summary>
    public MatchRejection Reason { get; }
}

/// <summary>Atomic durable match operations. Implementations enforce state transitions.</summary>
public interface IMatchStore
{
    /// <summary>Creates the waiting lobby and its creator seat atomically.</summary>
    MatchRecord Create(AuthenticatedSessionRecord player, MatchSettings settings, DateTime now);
    /// <summary>Lists joinable waiting lobbies belonging to other players.</summary>
    MatchRecord[] Available(long accountId);
    /// <summary>Occupies the second seat and starts the waiting match atomically.</summary>
    MatchRecord Join(AuthenticatedSessionRecord player, string code, DateTime now);
    /// <summary>Loads confirmed match data for an authorized participant.</summary>
    MatchRecord Get(long accountId, long matchId);
    /// <summary>Changes the duration only while the creator still owns a waiting lobby.</summary>
    MatchRecord ChangeTurnDuration(long accountId, long matchId, int seconds);
    /// <summary>Cancels a waiting lobby owned by the requesting account.</summary>
    void Cancel(long accountId, long matchId);
}
