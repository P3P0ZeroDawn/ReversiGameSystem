using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Leaderboards;

public interface IGetLeaderboardHandler
{
    Task<LeaderboardResult> ExecuteAsync(LeaderboardQuery query);
}

public sealed class LeaderboardQuery
{
    public LeaderboardQuery(int maximumEntries)
    {
        MaximumEntries = maximumEntries;
    }

    public int MaximumEntries { get; }
}

public sealed class LeaderboardResult
{
    public LeaderboardResult(
        LeaderboardOutcome outcome,
        IReadOnlyList<LeaderboardEntry>? entries = null)
    {
        Outcome = outcome;
        Entries = entries ?? Array.Empty<LeaderboardEntry>();
    }

    public LeaderboardOutcome Outcome { get; }

    public IReadOnlyList<LeaderboardEntry> Entries { get; }
}

public sealed class LeaderboardEntry
{
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

    public int Rank { get; }

    public long AccountId { get; }

    public string Username { get; }

    public string? ProfileImageUri { get; }

    public int MatchesPlayed { get; }

    public int Wins { get; }

    public double WinRate { get; }
}

public enum LeaderboardOutcome
{
    Success,
    InvalidRequest,
    ServiceUnavailable
}
