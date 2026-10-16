using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>
/// Defines the player-profile query use-case contract.
/// </summary>
public interface IGetPlayerProfileHandler
{
    /// <summary>
    /// Performs the execute async operation.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The result of the operation.</returns>
    public Task<PlayerProfileResult> ExecuteAsync(PlayerProfileQuery query);
}

/// <summary>
/// Represents player profile query.
/// </summary>
public sealed class PlayerProfileQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerProfileQuery"/> class.
    /// </summary>
    /// <param name="sessionToken">The session token.</param>
    /// <param name="accountId">The account id.</param>
    public PlayerProfileQuery(string sessionToken, long? accountId)
    {
        SessionToken = sessionToken;
        AccountId = accountId;
    }

    /// <summary>
    /// Gets the session token.
    /// </summary>
    public string SessionToken { get; }

    /// <summary>
    /// Gets the account id.
    /// </summary>
    public long? AccountId { get; }
}

/// <summary>
/// Represents player profile result.
/// </summary>
public sealed class PlayerProfileResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerProfileResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="profile">The profile.</param>
    public PlayerProfileResult(PlayerProfileOutcome outcome, PlayerProfile? profile = null)
    {
        Outcome = outcome;
        Profile = profile;
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public PlayerProfileOutcome Outcome { get; }

    /// <summary>
    /// Gets the profile.
    /// </summary>
    public PlayerProfile? Profile { get; }
}

/// <summary>
/// Represents player profile.
/// </summary>
public sealed class PlayerProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerProfile"/> class.
    /// </summary>
    /// <param name="accountId">The account id.</param>
    /// <param name="username">The username.</param>
    /// <param name="profileImageUri">The profile image uri.</param>
    /// <param name="statistics">The statistics.</param>
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

    /// <summary>
    /// Gets the account id.
    /// </summary>
    public long AccountId { get; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the profile image uri.
    /// </summary>
    public string? ProfileImageUri { get; }

    /// <summary>
    /// Gets the statistics.
    /// </summary>
    public PlayerStatistics Statistics { get; }
}

/// <summary>
/// Represents player statistics.
/// </summary>
public sealed class PlayerStatistics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerStatistics"/> class.
    /// </summary>
    /// <param name="matchesPlayed">The matches played.</param>
    /// <param name="wins">The wins.</param>
    /// <param name="losses">The losses.</param>
    /// <param name="draws">The draws.</param>
    /// <param name="winRate">The win rate.</param>
    public PlayerStatistics(int matchesPlayed, int wins, int losses, int draws, double winRate)
    {
        MatchesPlayed = matchesPlayed;
        Wins = wins;
        Losses = losses;
        Draws = draws;
        WinRate = winRate;
    }

    /// <summary>
    /// Gets the matches played.
    /// </summary>
    public int MatchesPlayed { get; }

    /// <summary>
    /// Gets the wins.
    /// </summary>
    public int Wins { get; }

    /// <summary>
    /// Gets the losses.
    /// </summary>
    public int Losses { get; }

    /// <summary>
    /// Gets the draws.
    /// </summary>
    public int Draws { get; }

    /// <summary>
    /// Gets the win rate.
    /// </summary>
    public double WinRate { get; }
}

/// <summary>
/// Defines the supported player profile outcome values.
/// </summary>
public enum PlayerProfileOutcome
{
    /// <summary>
    /// Represents success.
    /// </summary>
    Success,
    /// <summary>
    /// Represents invalid request.
    /// </summary>
    InvalidRequest,
    /// <summary>
    /// Represents invalid session.
    /// </summary>
    InvalidSession,
    /// <summary>
    /// Represents account not found.
    /// </summary>
    AccountNotFound,
    /// <summary>
    /// Represents service unavailable.
    /// </summary>
    ServiceUnavailable
}
