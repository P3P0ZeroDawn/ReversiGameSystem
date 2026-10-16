using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Leaderboards;

/// <summary>
/// Defines the leaderboard query use-case contract.
/// </summary>
public interface IGetLeaderboardHandler
{
    /// <summary>
    /// Performs the execute async operation.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The result of the operation.</returns>
    public Task<LeaderboardResult> ExecuteAsync(LeaderboardQuery query);
}

/// <summary>
/// Represents leaderboard query.
/// </summary>
public sealed class LeaderboardQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardQuery"/> class.
    /// </summary>
    /// <param name="maximumEntries">The maximum entries.</param>
    public LeaderboardQuery(int maximumEntries)
    {
        MaximumEntries = maximumEntries;
    }

    /// <summary>
    /// Gets the maximum entries.
    /// </summary>
    public int MaximumEntries { get; }
}

/// <summary>
/// Represents leaderboard result.
/// </summary>
public sealed class LeaderboardResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="entries">The entries.</param>
    public LeaderboardResult(
        LeaderboardOutcome outcome,
        IReadOnlyList<LeaderboardEntry>? entries = null)
    {
        Outcome = outcome;
        Entries = entries ?? Array.Empty<LeaderboardEntry>();
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public LeaderboardOutcome Outcome { get; }

    /// <summary>
    /// Gets the entries.
    /// </summary>
    public IReadOnlyList<LeaderboardEntry> Entries { get; }
}

/// <summary>
/// Represents leaderboard entry.
/// </summary>
public sealed class LeaderboardEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardEntry"/> class.
    /// </summary>
    /// <param name="rank">The rank.</param>
    /// <param name="accountId">The account id.</param>
    /// <param name="username">The username.</param>
    /// <param name="profileImageUri">The profile image uri.</param>
    /// <param name="matchesPlayed">The matches played.</param>
    /// <param name="wins">The wins.</param>
    /// <param name="winRate">The win rate.</param>
    public LeaderboardEntry(
        int rank,
        long accountId,
        string username,
        string? profileImageUri,
        int matchesPlayed,
        int wins,
        double winRate)
    {
        Rank = rank;
        AccountId = accountId;
        Username = username;
        ProfileImageUri = profileImageUri;
        MatchesPlayed = matchesPlayed;
        Wins = wins;
        WinRate = winRate;
    }

    /// <summary>
    /// Gets the rank.
    /// </summary>
    public int Rank { get; }

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
    /// Gets the matches played.
    /// </summary>
    public int MatchesPlayed { get; }

    /// <summary>
    /// Gets the wins.
    /// </summary>
    public int Wins { get; }

    /// <summary>
    /// Gets the win rate.
    /// </summary>
    public double WinRate { get; }
}

/// <summary>
/// Defines the supported leaderboard outcome values.
/// </summary>
public enum LeaderboardOutcome
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
    /// Represents service unavailable.
    /// </summary>
    ServiceUnavailable
}
