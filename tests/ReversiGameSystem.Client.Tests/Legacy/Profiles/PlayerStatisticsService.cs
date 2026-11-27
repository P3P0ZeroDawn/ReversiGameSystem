using System;
using System.Collections.Generic;
using ReversiGameSystem.Statistics;

namespace ReversiGameSystem.Services;

internal enum PlayerStatisticsQueryStatus
{
    Success,
    ServerConnectionFailed,
    RetrievalError
}

internal sealed class PlayerStatisticsQueryResult
{
    internal PlayerStatisticsQueryResult(
        PlayerStatisticsQueryStatus status,
        IReadOnlyList<CompletedMatchOutcome> completedMatches)
    {
        Status = status;
        CompletedMatches = completedMatches;
    }

    internal PlayerStatisticsQueryStatus Status { get; }

    internal IReadOnlyList<CompletedMatchOutcome> CompletedMatches { get; }
}

internal interface IPlayerStatisticsService
{
    public PlayerStatisticsQueryResult GetCompletedMatches(string? playerId);
}

/// <summary>
/// Holds the query boundary until finished matches can be retrieved from the server.
/// </summary>
internal sealed class PlayerStatisticsService : IPlayerStatisticsService
{
    public PlayerStatisticsQueryResult GetCompletedMatches(string? playerId)
    {
        // A null playerId means the authenticated player's own profile.
        // TODO: Request only finished matches for the selected account from the server.
        return new PlayerStatisticsQueryResult(
            PlayerStatisticsQueryStatus.ServerConnectionFailed,
            Array.Empty<CompletedMatchOutcome>());
    }
}
