using System;

namespace ReversiGameSystem.Services;

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
    EmptyMessage,
    MessageTooLong,
    MessageBlocked,
    PlayerMuted,
    ServiceUnavailable
}

internal sealed class MatchResult<T> where T : class
{
    internal MatchResult(MatchStatus status, T? value)
    {
        if (status == MatchStatus.Success && value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        if (status != MatchStatus.Success && value != null)
        {
            throw new ArgumentException("A failed result cannot contain a value.", nameof(value));
        }
        Status = status;
        Value = value;
    }

    internal MatchStatus Status { get; }
    internal T? Value { get; }
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

internal sealed class RemoteAvailableMatch
{
    internal RemoteAvailableMatch(string matchCode, string creatorDisplayName, int turnDurationSeconds)
    {
        MatchCode = matchCode;
        CreatorDisplayName = creatorDisplayName;
        TurnDurationSeconds = turnDurationSeconds;
    }

    internal string MatchCode { get; }
    internal string CreatorDisplayName { get; }
    internal int TurnDurationSeconds { get; }
}

internal sealed class RemoteMatchInformation
{
    internal RemoteMatchInformation(
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
}

internal sealed class MatchChatMessage
{
    internal MatchChatMessage(long matchId, int number, int senderSeatNumber, string content)
    {
        MatchId = matchId;
        Number = number;
        SenderSeatNumber = senderSeatNumber;
        Content = content;
    }

    internal long MatchId { get; }
    internal int Number { get; }
    internal int SenderSeatNumber { get; }
    internal string Content { get; }
}

internal sealed class RemoteChatMute
{
    internal RemoteChatMute(long matchId, DateTime endsAtUtc)
    {
        MatchId = matchId;
        EndsAtUtc = endsAtUtc;
    }

    internal long MatchId { get; }
    internal DateTime EndsAtUtc { get; }
}

internal sealed class ChatSubmission
{
    internal ChatSubmission(MatchStatus status, DateTime? muteEndsAtUtc)
    {
        Status = status;
        MuteEndsAtUtc = muteEndsAtUtc;
    }

    internal MatchStatus Status { get; }
    internal DateTime? MuteEndsAtUtc { get; }
}
