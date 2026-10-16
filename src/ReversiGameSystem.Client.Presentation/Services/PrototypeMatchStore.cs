using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum PrototypeMatchJoinResult
{
    Success,
    NotFound,
    NoLongerAvailable,
    OwnMatch
}

/// <summary>
/// Keeps deterministic match records shared by the prototype list and join flows.
/// </summary>
internal static class PrototypeMatchStore
{
    private static readonly object SyncRoot = new object();
    private static readonly List<AvailableMatch> Matches = new List<AvailableMatch>
    {
        new AvailableMatch(
            "waiting-1", "AN4B2C", "Ana", AvailableMatchState.Waiting, 1, false, 40),
        new AvailableMatch(
            "waiting-2", "LU5N7P", "Luis", AvailableMatchState.Waiting, 1, false, 60),
        new AvailableMatch(
            "own-1", "PE2D4R", "Pedro", AvailableMatchState.Waiting, 1, false, 45),
        new AvailableMatch(
            "full-1", "CE6S8R", "César", AvailableMatchState.Waiting, 2, false, 40),
        new AvailableMatch(
            "system-1", "GA4L6X", "Gael", AvailableMatchState.Waiting, 1, true, 30),
        new AvailableMatch(
            "playing-1", "LU7N9A", "Luna", AvailableMatchState.InProgress, 2, false, 40)
    };

    internal static IReadOnlyList<AvailableMatch> GetAll()
    {
        lock (SyncRoot)
        {
            return Matches.ToArray();
        }
    }

    internal static PrototypeMatchJoinResult TryJoinById(
        string matchId,
        string playerName,
        string? guestName,
        out MatchSession? session)
    {
        return TryJoin(
            match => string.Equals(match.Id, matchId, StringComparison.Ordinal),
            playerName,
            guestName,
            out session);
    }

    internal static PrototypeMatchJoinResult TryJoinByCode(
        string code,
        string playerName,
        string? guestName,
        out MatchSession? session)
    {
        return TryJoin(
            match => string.Equals(match.Code, code, StringComparison.Ordinal),
            playerName,
            guestName,
            out session);
    }

    private static PrototypeMatchJoinResult TryJoin(
        Func<AvailableMatch, bool> predicate,
        string playerName,
        string? guestName,
        out MatchSession? session)
    {
        lock (SyncRoot)
        {
            session = null;
            int matchIndex = Matches.FindIndex(match => predicate(match));
            if (matchIndex < 0)
            {
                return PrototypeMatchJoinResult.NotFound;
            }

            AvailableMatch match = Matches[matchIndex];
            if (string.Equals(match.HostName, playerName, StringComparison.Ordinal))
            {
                return PrototypeMatchJoinResult.OwnMatch;
            }

            if (match.State != AvailableMatchState.Waiting
                || match.ParticipantCount != 1
                || match.UsesSystemOpponent)
            {
                return PrototypeMatchJoinResult.NoLongerAvailable;
            }

            MatchSession joinedSession = new MatchSession(
                match.Code,
                match.TurnTimeSeconds,
                match.HostName,
                guestName,
                DiscColor.White);
            joinedSession.JoinAsSecondPlayer(playerName);

            Matches[matchIndex] = new AvailableMatch(
                match.Id,
                match.Code,
                match.HostName,
                AvailableMatchState.InProgress,
                2,
                match.UsesSystemOpponent,
                match.TurnTimeSeconds);
            session = joinedSession;
            return PrototypeMatchJoinResult.Success;
        }
    }
}
