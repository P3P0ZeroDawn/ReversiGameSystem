using System;

namespace ReversiGameSystem.Matches;

internal enum MatchStatus
{
    Success,
    InvalidMatchId,
    InvalidConfiguration,
    InvalidTurnDuration,
    MatchNotFound,
    CannotJoinOwnMatch,
    InvalidMatchCode,
    MatchCodeUnavailable,
    Unauthorized,
    MatchNotWaiting,
    MatchNotInProgress,
    MissingPlacement,
    InvalidMatchStateVersion,
    StaleMatchStateVersion,
    NotPlayersTurn,
    InvalidPosition,
    SpectatingUnavailable,
    InactiveAccount,
    InvalidSession,
    AccountUnavailable,
    FriendshipNotFound,
    InvalidAccountId,
    ServiceUnavailable
}

internal sealed class LobbySnapshot
{
    internal LobbySnapshot(long matchId, string matchCode, int turnDurationSeconds)
    {
        MatchId = matchId;
        MatchCode = matchCode;
        TurnDurationSeconds = turnDurationSeconds;
    }

    internal long MatchId { get; }
    internal string MatchCode { get; }
    internal int TurnDurationSeconds { get; }
}

internal sealed class AvailableMatch
{
    internal AvailableMatch(string matchCode, string creatorDisplayName, int turnDurationSeconds)
    {
        MatchCode = matchCode;
        CreatorDisplayName = creatorDisplayName;
        TurnDurationSeconds = turnDurationSeconds;
    }

    internal string MatchCode { get; }
    internal string CreatorDisplayName { get; }
    internal int TurnDurationSeconds { get; }
}

internal sealed class MatchInformation
{
    internal MatchInformation(
        long matchId,
        MatchParticipant[] participants,
        int? assignedSeatNumber,
        int turnDurationSeconds)
    {
        MatchId = matchId;
        Participants = participants;
        AssignedSeatNumber = assignedSeatNumber;
        TurnDurationSeconds = turnDurationSeconds;
    }

    internal long MatchId { get; }
    internal MatchParticipant[] Participants { get; }
    internal int? AssignedSeatNumber { get; }
    internal int TurnDurationSeconds { get; }
}

internal sealed class MatchParticipant
{
    internal MatchParticipant(int seatNumber, string displayName)
    {
        SeatNumber = seatNumber;
        DisplayName = displayName;
    }

    internal int SeatNumber { get; }
    internal string DisplayName { get; }
    internal bool IsSystem { get; set; }
}

internal enum ConfirmedMatchState
{
    Waiting,
    InProgress,
    Finished,
    Cancelled
}

internal enum ConfirmedBoardCell
{
    Empty,
    Black,
    White
}

internal sealed class MatchStateSnapshot
{
    internal MatchStateSnapshot(
        long matchId,
        long version,
        ConfirmedMatchState state,
        ConfirmedBoardCell[] board,
        int blackPieceCount,
        int whitePieceCount,
        int? currentTurnSeatNumber)
    {
        MatchId = matchId;
        Version = version;
        State = state;
        Board = board;
        BlackPieceCount = blackPieceCount;
        WhitePieceCount = whitePieceCount;
        CurrentTurnSeatNumber = currentTurnSeatNumber;
    }

    internal long MatchId { get; }
    internal long Version { get; }
    internal ConfirmedMatchState State { get; }
    internal ConfirmedBoardCell[] Board { get; }
    internal int BlackPieceCount { get; }
    internal int WhitePieceCount { get; }
    internal int? CurrentTurnSeatNumber { get; }
    internal DateTime? TurnStartedAtUtc { get; set; }
    internal RemoteMatchConclusion? Conclusion { get; set; }
}

internal enum RemoteMatchEndReason
{
    NoLegalMoves,
    Inactivity,
    Abandonment
}

internal sealed class RemoteMatchConclusion
{
    internal RemoteMatchConclusion(
        RemoteMatchEndReason reason,
        int? winnerSeatNumber)
    {
        Reason = reason;
        WinnerSeatNumber = winnerSeatNumber;
    }

    internal RemoteMatchEndReason Reason { get; }
    internal int? WinnerSeatNumber { get; }
}

internal sealed class MatchPlacement
{
    internal long MatchId { get; set; }
    internal long ExpectedVersion { get; set; }
    internal int Row { get; set; }
    internal int Column { get; set; }
}

internal enum ParticipantConnectionState
{
    Unknown,
    Connected,
    Disconnected
}
