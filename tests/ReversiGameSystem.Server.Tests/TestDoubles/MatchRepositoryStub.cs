using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class MatchRepositoryStub : IMatchRepository
{
    internal int StartSystemMatchCalls { get; private set; }

    internal int EvaluateLobbyAccessCalls { get; private set; }

    internal int CancelMatchCalls { get; private set; }

    internal int ValidateGuestJoinCalls { get; private set; }

    internal int CreateMatchCalls { get; private set; }

    internal int GetAvailableMatchesCalls { get; private set; }

    internal int JoinMatchCalls { get; private set; }

    internal int LoadParticipantMatchCalls { get; private set; }

    internal int ChangeTurnDurationCalls { get; private set; }

    internal int PreparePrivateInvitationCalls { get; private set; }

    internal int MarkInvitationDeliveredCalls { get; private set; }

    internal long LastAccountId { get; private set; }

    internal long LastMatchId { get; private set; }

    internal long LastFriendAccountId { get; private set; }

    internal string LastCode { get; private set; } = string.Empty;

    internal int LastTurnDurationSeconds { get; private set; }

    internal MatchSettings? LastSettings { get; private set; }

    internal DateTime LastNowUtc { get; private set; }

    internal MatchStatus LobbyAccessStatus { get; set; } = MatchStatus.Success;

    internal MatchStatus CancelMatchStatus { get; set; } = MatchStatus.Success;

    internal MatchStatus ValidateGuestJoinStatus { get; set; } = MatchStatus.Success;

    internal MatchResult<Match> StartSystemMatchResult { get; set; } =
        new MatchResult<Match>(MatchStatus.MatchNotFound);

    internal MatchResult<Match> CreateMatchResult { get; set; } =
        new MatchResult<Match>(MatchStatus.MatchNotFound);

    internal MatchResult<Match[]> AvailableMatchesResult { get; set; } =
        new MatchResult<Match[]>(Array.Empty<Match>());

    internal MatchResult<Match> JoinMatchResult { get; set; } =
        new MatchResult<Match>(MatchStatus.MatchNotFound);

    internal MatchResult<Match> LoadParticipantMatchResult { get; set; } =
        new MatchResult<Match>(MatchStatus.MatchNotFound);

    internal MatchResult<Match> ChangeTurnDurationResult { get; set; } =
        new MatchResult<Match>(MatchStatus.MatchNotFound);

    internal MatchResult<MatchInvitationPreparation> PreparePrivateInvitationResult { get; set; } =
        new MatchResult<MatchInvitationPreparation>(MatchStatus.MatchNotFound);

    internal MatchResult<Match> MarkInvitationDeliveredResult { get; set; } =
        new MatchResult<Match>(MatchStatus.MatchNotFound);

    public MatchResult<Match> StartSystemMatch(long accountId, long matchId, DateTime now)
    {
        StartSystemMatchCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        LastNowUtc = now;
        return StartSystemMatchResult;
    }

    public MatchStatus EvaluateLobbyAccess(long accountId)
    {
        EvaluateLobbyAccessCalls++;
        LastAccountId = accountId;
        return LobbyAccessStatus;
    }

    public MatchStatus CancelMatch(long accountId, long matchId)
    {
        CancelMatchCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        return CancelMatchStatus;
    }

    public MatchStatus ValidateGuestJoin(string code)
    {
        ValidateGuestJoinCalls++;
        LastCode = code;
        return ValidateGuestJoinStatus;
    }

    public MatchResult<Match> CreateMatch(
        AuthenticatedSession player,
        MatchSettings settings,
        DateTime now)
    {
        CreateMatchCalls++;
        LastAccountId = player.AccountId;
        LastSettings = settings;
        LastNowUtc = now;
        return CreateMatchResult;
    }

    public MatchResult<Match[]> GetAvailableMatches(long accountId)
    {
        GetAvailableMatchesCalls++;
        LastAccountId = accountId;
        return AvailableMatchesResult;
    }

    public MatchResult<Match> JoinMatch(AuthenticatedSession player, string code, DateTime now)
    {
        JoinMatchCalls++;
        LastAccountId = player.AccountId;
        LastCode = code;
        LastNowUtc = now;
        return JoinMatchResult;
    }

    public MatchResult<Match> LoadParticipantMatch(long accountId, long matchId)
    {
        LoadParticipantMatchCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        return LoadParticipantMatchResult;
    }

    public MatchResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds)
    {
        ChangeTurnDurationCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        LastTurnDurationSeconds = seconds;
        return ChangeTurnDurationResult;
    }

    public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
        AuthenticatedSession player,
        long friendAccountId,
        MatchSettings settings,
        DateTime now)
    {
        PreparePrivateInvitationCalls++;
        LastAccountId = player.AccountId;
        LastFriendAccountId = friendAccountId;
        LastSettings = settings;
        LastNowUtc = now;
        return PreparePrivateInvitationResult;
    }

    public MatchResult<Match> MarkInvitationDelivered(long accountId, long matchId, DateTime now)
    {
        MarkInvitationDeliveredCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        LastNowUtc = now;
        return MarkInvitationDeliveredResult;
    }
}
