using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Players;

public interface IGetPlayerProfileHandler
{
    Task<PlayerProfileResult> ExecuteAsync(PlayerProfileQuery query);
}

public sealed class PlayerProfileQuery
{
    public PlayerProfileQuery(string sessionToken, long? accountId)
    {
        SessionToken = sessionToken;
        AccountId = accountId;
    }

    public string SessionToken { get; }

    public long? AccountId { get; }
}

public sealed class PlayerProfileResult
{
    public PlayerProfileResult(PlayerProfileOutcome outcome, PlayerProfile? profile = null)
    {
        Outcome = outcome;
        Profile = profile;
    }

    public PlayerProfileOutcome Outcome { get; }

    public PlayerProfile? Profile { get; }
}

public sealed class PlayerProfile
{
    public PlayerProfile(
        long accountId,
        string username,
        string? profileImageUri,
        PlayerStatistics statistics)
    {
        AccountId = accountId;
        Username = username;
        ProfileImageUri = profileImageUri;
        Statistics = statistics;
    }

    public long AccountId { get; }

    public string Username { get; }

    public string? ProfileImageUri { get; }

    public PlayerStatistics Statistics { get; }
}

public sealed class PlayerStatistics
{
    public PlayerStatistics(int matchesPlayed, int wins, int losses, int draws, double winRate)
    {
        MatchesPlayed = matchesPlayed;
        Wins = wins;
        Losses = losses;
        Draws = draws;
        WinRate = winRate;
    }

    public int MatchesPlayed { get; }

    public int Wins { get; }

    public int Losses { get; }

    public int Draws { get; }

    public double WinRate { get; }
}

public enum PlayerProfileOutcome
{
    Success,
    InvalidRequest,
    InvalidSession,
    AccountNotFound,
    ServiceUnavailable
}
