using System;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class GameplayRepositoryStub : IGameplayRepository
{
    internal int EvaluateLobbyAccessCalls { get; private set; }

    internal int GetObservableMatchesCalls { get; private set; }

    internal int LoadMatchCalls { get; private set; }

    internal int SynchronizeMatchCalls { get; private set; }

    internal int LoadMatchHistoryCalls { get; private set; }

    internal int LoadMatchForObservationCalls { get; private set; }

    internal long LastAccountId { get; private set; }

    internal long LastMatchId { get; private set; }

    internal Match? LastSynchronizedMatch { get; private set; }

    internal MatchStatus LobbyAccessStatus { get; set; } = MatchStatus.Success;

    internal MatchResult<Match[]> ObservableMatchesResult { get; set; } =
        new MatchResult<Match[]>(Array.Empty<Match>());

    internal MatchResult<Match> LoadMatchResult { get; set; } =
        new MatchResult<Match>(MatchStatus.MatchNotFound);

    internal bool SynchronizeMatchResult { get; set; } = true;

    internal MatchResult<Match[]> MatchHistoryResult { get; set; } =
        new MatchResult<Match[]>(Array.Empty<Match>());

    internal MatchResult<Match> LoadMatchForObservationResult { get; set; } =
        new MatchResult<Match>(MatchStatus.MatchNotFound);

    internal bool ThrowOnLoadMatch { get; set; }

    public MatchStatus EvaluateLobbyAccess(long accountId)
    {
        EvaluateLobbyAccessCalls++;
        LastAccountId = accountId;
        return LobbyAccessStatus;
    }

    public MatchResult<Match[]> GetObservableMatches(long accountId)
    {
        GetObservableMatchesCalls++;
        LastAccountId = accountId;
        return ObservableMatchesResult;
    }

    public MatchResult<Match> LoadMatch(long matchId)
    {
        LoadMatchCalls++;
        LastMatchId = matchId;
        if (ThrowOnLoadMatch)
        {
            throw new InvalidOperationException("private-storage-failure");
        }

        return LoadMatchResult;
    }

    public bool SynchronizeMatch(Match match)
    {
        SynchronizeMatchCalls++;
        LastSynchronizedMatch = match;
        return SynchronizeMatchResult;
    }

    public MatchResult<Match[]> LoadMatchHistory(long accountId)
    {
        LoadMatchHistoryCalls++;
        LastAccountId = accountId;
        return MatchHistoryResult;
    }

    public MatchResult<Match> LoadMatchForObservation(long accountId, long matchId)
    {
        LoadMatchForObservationCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        return LoadMatchForObservationResult;
    }
}
