namespace ReversiGameSystem.Server.Application.Matches;

public interface IGameplayRepository
{
    public MatchResult<Match> LoadMatch(long matchId);

    public bool SynchronizeMatch(Match match);

    public MatchResult<Match[]> LoadMatchHistory(long accountId);

    public MatchResult<Match> LoadMatchForObservation(long accountId, long matchId);
}
