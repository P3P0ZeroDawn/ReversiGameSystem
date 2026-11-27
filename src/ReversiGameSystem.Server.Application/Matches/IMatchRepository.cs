using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Matches;

public interface IMatchRepository
{
    public MatchResult<Match> StartSystemMatch(MatchPlayer player, long matchId, DateTime now);

    public MatchStatus EvaluateLobbyAccess(long accountId);

    public MatchStatus CancelMatch(MatchPlayer player, long matchId);

    public MatchResult<Match> CreateMatch(MatchPlayer player, MatchSettings settings, DateTime now);

    public MatchResult<Match[]> GetAvailableMatches(MatchPlayer player);

    public MatchResult<Match> JoinMatch(MatchPlayer player, string code, DateTime now);

    public MatchResult<Match> LoadParticipantMatch(MatchPlayer player, long matchId);

    public MatchResult<Match> ChangeTurnDuration(MatchPlayer player, long matchId, int seconds);

    public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
        AuthenticatedSession player,
        long friendAccountId,
        MatchSettings settings,
        DateTime now);

    public MatchResult<Match> MarkInvitationDelivered(long accountId, long matchId, DateTime now);

}
