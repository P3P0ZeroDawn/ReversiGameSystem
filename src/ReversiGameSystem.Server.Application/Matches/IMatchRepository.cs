using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Matches;

public interface IMatchRepository
{
    public MatchResult<Match> StartSystemMatch(long accountId, long matchId, DateTime now);

    public MatchStatus EvaluateLobbyAccess(long accountId);

    public MatchStatus CancelMatch(long accountId, long matchId);

    public MatchStatus ValidateGuestJoin(string code);

    public MatchResult<Match> CreateMatch(AuthenticatedSession player, MatchSettings settings, DateTime now);

    public MatchResult<Match[]> GetAvailableMatches(long accountId);

    public MatchResult<Match> JoinMatch(AuthenticatedSession player, string code, DateTime now);

    public MatchResult<Match> LoadParticipantMatch(long accountId, long matchId);

    public MatchResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds);

    public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
        AuthenticatedSession player,
        long friendAccountId,
        MatchSettings settings,
        DateTime now);

    public MatchResult<Match> MarkInvitationDelivered(long accountId, long matchId, DateTime now);

}
