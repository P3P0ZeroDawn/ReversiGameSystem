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

    public string MatchCode { get; }
    public string CreatorDisplayName { get; }
    public int TurnDurationSeconds { get; }
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

    public long MatchId { get; }
    public MatchParticipant[] Participants { get; }
    public int? AssignedSeatNumber { get; }
    public int TurnDurationSeconds { get; }
}

internal sealed class MatchParticipant
{
    internal MatchParticipant(int seatNumber, string displayName)
    {
        SeatNumber = seatNumber;
        DisplayName = displayName;
    }

    public int SeatNumber { get; }
    public long? AccountId { get; set; }
    public string DisplayName { get; }
    public bool IsSystem { get; set; }
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

internal sealed class MatchMove
{
    public int MoveNumber { get; set; }
    public int SeatNumber { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public DateTime PlayedAtUtc { get; set; }
    public int FlippedPieceCount { get; set; }
    public bool IsAutomatic { get; set; }
}

internal sealed class MatchRecord
{
    public long MatchId { get; set; }
    public MatchParticipant[] Participants { get; set; } = Array.Empty<MatchParticipant>();
    public int? AssignedSeatNumber { get; set; }
    public ConfirmedMatchState State { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public int BlackPieceCount { get; set; }
    public int WhitePieceCount { get; set; }
    public RemoteMatchConclusion? Conclusion { get; set; }
}

internal sealed class MatchRecording
{
    internal MatchRecording(MatchRecord match, MatchMove[] moves,
        ConfirmedBoardCell[][] boardFrames)
    {
        Match = match;
        Moves = moves;
        BoardFrames = boardFrames;
    }

    internal MatchRecord Match { get; }
    internal MatchMove[] Moves { get; }
    internal ConfirmedBoardCell[][] BoardFrames { get; }
    internal int TurnDurationSeconds { get; set; }
}
