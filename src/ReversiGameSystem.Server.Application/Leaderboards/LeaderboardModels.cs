using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Application.Leaderboards;

public sealed class LeaderboardResult
{
    public LeaderboardResult(IReadOnlyList<LeaderboardEntry> entries)
    {
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        Status = LeaderboardStatus.Success;
    }

    public LeaderboardResult(LeaderboardStatus status)
    {
        if (status == LeaderboardStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A rejected leaderboard result cannot specify success status.");
        }

        Status = status;
        Entries = Array.Empty<LeaderboardEntry>();
    }

    public LeaderboardStatus Status { get; }

    public IReadOnlyList<LeaderboardEntry> Entries { get; }
}

public enum LeaderboardStatus
{
    Success,

    InvalidMaximumEntries
}

public sealed class LeaderboardPlayer
{
    public LeaderboardPlayer(
        long accountId,
        string username,
        string? profileImageUri,
        int matchesPlayed,
        int wins)
    {
        AccountId = accountId;
        Username = username;
        ProfileImageUri = profileImageUri;
        MatchesPlayed = matchesPlayed;
        Wins = wins;
    }

    public long AccountId { get; }

    public string Username { get; }

    public string? ProfileImageUri { get; }

    public int MatchesPlayed { get; }

    public int Wins { get; }
}

public sealed class LeaderboardEntry
{
    public LeaderboardEntry(
        int position,
        long accountId,
        string username,
        string? profileImageUri,
        int matchesPlayed,
        int wins,
        double winRate)
    {
        Position = position;
        AccountId = accountId;
        Username = username;
        ProfileImageUri = profileImageUri;
        MatchesPlayed = matchesPlayed;
        Wins = wins;
        WinRate = winRate;
    }

    public int Position { get; }

    public long AccountId { get; }

    public string Username { get; }

    public string? ProfileImageUri { get; }

    public int MatchesPlayed { get; }

    public int Wins { get; }

    public double WinRate { get; }
}
