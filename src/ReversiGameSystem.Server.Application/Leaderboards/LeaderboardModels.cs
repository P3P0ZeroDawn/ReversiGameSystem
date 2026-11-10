using System;
using System.Collections.Generic;

namespace ReversiGameSystem.Server.Application.Leaderboards;

/// <summary>
/// Represents the result of retrieving the global leaderboard.
/// </summary>
public sealed class LeaderboardResult
{
    /// <summary>
    /// Initializes a successful leaderboard result with its ordered entries.
    /// </summary>
    /// <param name="entries">The ordered leaderboard entries.</param>
    /// <exception cref="ArgumentNullException">The entries list is null.</exception>
    public LeaderboardResult(IReadOnlyList<LeaderboardEntry> entries)
    {
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        Status = LeaderboardStatus.Success;
    }

    /// <summary>
    /// Initializes a rejected leaderboard result with its actionable status.
    /// </summary>
    /// <param name="status">The failure status.</param>
    /// <exception cref="ArgumentOutOfRangeException">The status represents success.</exception>
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

    /// <summary>Gets the application result status.</summary>
    public LeaderboardStatus Status { get; }

    /// <summary>Gets the ordered leaderboard entries.</summary>
    public IReadOnlyList<LeaderboardEntry> Entries { get; }
}

/// <summary>
/// Defines the expected outcomes of retrieving the leaderboard.
/// </summary>
public enum LeaderboardStatus
{
    /// <summary>The leaderboard was retrieved successfully.</summary>
    Success,

    /// <summary>The requested maximum entry count is outside the accepted range.</summary>
    InvalidMaximumEntries
}

/// <summary>
/// Represents the persisted statistics needed to rank one player.
/// </summary>
public sealed class LeaderboardPlayer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardPlayer"/> class.
    /// </summary>
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

    /// <summary>Gets the account identifier.</summary>
    public long AccountId { get; }

    /// <summary>Gets the public username.</summary>
    public string Username { get; }

    /// <summary>Gets the internal profile image URI, when one exists.</summary>
    public string? ProfileImageUri { get; }

    /// <summary>Gets the number of matches counted for statistics.</summary>
    public int MatchesPlayed { get; }

    /// <summary>Gets the number of wins counted for statistics.</summary>
    public int Wins { get; }
}

/// <summary>
/// Represents one ranked application-level leaderboard entry.
/// </summary>
public sealed class LeaderboardEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardEntry"/> class.
    /// </summary>
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

    /// <summary>Gets the consecutive one-based position.</summary>
    public int Position { get; }

    /// <summary>Gets the account identifier.</summary>
    public long AccountId { get; }

    /// <summary>Gets the public username.</summary>
    public string Username { get; }

    /// <summary>Gets the internal profile image URI, when one exists.</summary>
    public string? ProfileImageUri { get; }

    /// <summary>Gets the number of matches counted for statistics.</summary>
    public int MatchesPlayed { get; }

    /// <summary>Gets the number of wins counted for statistics.</summary>
    public int Wins { get; }

    /// <summary>Gets the proportion of won matches.</summary>
    public double WinRate { get; }
}
