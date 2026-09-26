using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Game;

internal enum MatchRecordState
{
    InProgress,
    Finished
}

/// <summary>
/// Describes one registered move and the board produced by it.
/// </summary>
internal sealed class RecordedMove
{
    internal RecordedMove(
        int number,
        string playerName,
        string coordinate,
        DateTime recordedAtUtc,
        int affectedPieces,
        MatchMoveOrigin origin)
    {
        Number = number;
        PlayerName = playerName;
        Coordinate = coordinate;
        RecordedAtUtc = recordedAtUtc;
        AffectedPieces = affectedPieces;
        Origin = origin;
    }

    internal int Number { get; }

    internal string PlayerName { get; }

    internal string Coordinate { get; }

    internal DateTime RecordedAtUtc { get; }

    internal int AffectedPieces { get; }

    internal MatchMoveOrigin Origin { get; }
}

/// <summary>
/// Contains the information needed to inspect or replay a match.
/// </summary>
internal sealed class MatchInformation
{
    internal MatchInformation(
        string id,
        string blackPlayerName,
        string whitePlayerName,
        int turnTimeSeconds,
        MatchRecordState state,
        string? winnerName,
        IReadOnlyList<ReversiBoard> boardFrames,
        IReadOnlyList<RecordedMove> moves,
        DateTime recordedAtUtc,
        MatchEndReason? endReason)
    {
        Id = id;
        BlackPlayerName = blackPlayerName;
        WhitePlayerName = whitePlayerName;
        TurnTimeSeconds = turnTimeSeconds;
        State = state;
        WinnerName = winnerName;
        BoardFrames = boardFrames;
        Moves = moves;
        RecordedAtUtc = recordedAtUtc;
        EndReason = endReason;
    }

    internal string Id { get; }

    internal string BlackPlayerName { get; }

    internal string WhitePlayerName { get; }

    internal int TurnTimeSeconds { get; }

    internal MatchRecordState State { get; }

    internal string? WinnerName { get; }

    internal IReadOnlyList<ReversiBoard> BoardFrames { get; }

    internal IReadOnlyList<RecordedMove> Moves { get; }

    internal DateTime RecordedAtUtc { get; }

    internal MatchEndReason? EndReason { get; }

    internal ReversiBoard LatestBoard => BoardFrames[BoardFrames.Count - 1];
}
