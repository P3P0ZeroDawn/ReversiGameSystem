using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum MatchInformationRetrievalResult
{
    Success,
    NotFound,
    ServerConnectionFailed,
    RetrievalError
}

/// <summary>
/// Supplies temporary match records until the match-history server is connected.
/// </summary>
internal sealed class MatchInformationService
{
    private readonly string _currentPlayerName;
    private readonly List<MatchInformation> _matches;

    internal MatchInformationService(string currentPlayerName)
        : this(currentPlayerName, CreatePrototypeMatches(currentPlayerName))
    {
    }

    internal MatchInformationService(
        string currentPlayerName,
        IEnumerable<MatchInformation> matches)
    {
        _currentPlayerName = currentPlayerName;
        _matches = matches.ToList();
    }

    internal MatchInformationRetrievalResult TryGetHistory(
        out IReadOnlyList<MatchInformation> matches)
    {
        matches = _matches
            .Where(IsCurrentPlayerParticipant)
            .OrderByDescending(match => match.RecordedAtUtc)
            .ToArray();
        return MatchInformationRetrievalResult.Success;
    }

    internal IReadOnlyList<MatchInformation> GetSpectatableMatches()
    {
        return _matches.Where(match => match.State == MatchRecordState.InProgress).ToArray();
    }

    internal MatchInformationRetrievalResult TryGet(
        string matchId,
        out MatchInformation? match)
    {
        match = _matches.FirstOrDefault(
            candidate => string.Equals(candidate.Id, matchId, StringComparison.Ordinal));
        return match == null
            ? MatchInformationRetrievalResult.NotFound
            : MatchInformationRetrievalResult.Success;
    }

    private bool IsCurrentPlayerParticipant(MatchInformation match)
    {
        return string.Equals(
                match.BlackPlayerName,
                _currentPlayerName,
                StringComparison.Ordinal)
            || string.Equals(
                match.WhitePlayerName,
                _currentPlayerName,
                StringComparison.Ordinal);
    }

    private static IEnumerable<MatchInformation> CreatePrototypeMatches(
        string currentPlayerName)
    {
        IReadOnlyList<ReversiBoard> frames = CreateFrames();
        DateTime now = DateTime.UtcNow;
        return new[]
        {
            CreateMatch("live-1", "César", "Gael", MatchRecordState.InProgress,
                null, frames, 40, now.AddMinutes(-20), null),
            CreateMatch("history-1", currentPlayerName, "César", MatchRecordState.Finished,
                currentPlayerName, frames, 40, now.AddHours(-2),
                MatchEndReason.BoardCompleted),
            CreateMatch("history-2", currentPlayerName, "Gael", MatchRecordState.Finished,
                currentPlayerName, frames, 40, now.AddDays(-1),
                MatchEndReason.Abandonment),
            CreateMatch("history-3", currentPlayerName, "César", MatchRecordState.Finished,
                "César", frames, 30, now.AddDays(-3), MatchEndReason.Inactivity),
            CreateMatch("history-4", currentPlayerName, "Luna", MatchRecordState.Finished,
                currentPlayerName, frames, 60, now.AddDays(-7),
                MatchEndReason.BoardCompleted)
        };
    }

    private static MatchInformation CreateMatch(
        string id,
        string blackPlayerName,
        string whitePlayerName,
        MatchRecordState state,
        string? winnerName,
        IReadOnlyList<ReversiBoard> frames,
        int turnTimeSeconds,
        DateTime recordedAtUtc,
        MatchEndReason? endReason)
    {
        string[] players = { blackPlayerName, whitePlayerName };
        string[] coordinates = { "C4", "C3", "D3", "E3", "F3" };
        List<RecordedMove> moves = new List<RecordedMove>();
        for (int index = 0; index < coordinates.Length; index++)
        {
            moves.Add(new RecordedMove(
                index + 1,
                players[index % players.Length],
                coordinates[index],
                recordedAtUtc.AddMinutes(index - coordinates.Length),
                index % 3 + 1,
                index == 3 ? MatchMoveOrigin.Timeout : MatchMoveOrigin.Manual));
        }

        return new MatchInformation(
            id,
            blackPlayerName,
            whitePlayerName,
            turnTimeSeconds,
            state,
            winnerName,
            frames,
            moves,
            recordedAtUtc,
            endReason);
    }

    private static IReadOnlyList<ReversiBoard> CreateFrames()
    {
        return new[]
        {
            ReversiBoard.CreateInitial(),
            ReversiBoard.FromRows(
                "........", "........", "........", "..BBB...",
                "...BW...", "........", "........", "........"),
            ReversiBoard.FromRows(
                "........", "........", "..W.....", "..BWB...",
                "...BW...", "........", "........", "........"),
            ReversiBoard.FromRows(
                "........", "........", "..WB....", "..BBB...",
                "...BW...", "........", "........", "........"),
            ReversiBoard.FromRows(
                "........", "........", "..WBW...", "..BBW...",
                "...BW...", "........", "........", "........"),
            ReversiBoard.FromRows(
                "........", "........", "..WBWB..", "..BBB...",
                "...BW...", "........", "........", "........")
        };
    }
}
