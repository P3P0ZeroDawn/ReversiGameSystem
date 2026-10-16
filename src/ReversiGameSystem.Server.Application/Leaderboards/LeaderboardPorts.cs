using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Leaderboards;

/// <summary>
/// Defines the leaderboard store contract.
/// </summary>
public interface ILeaderboardStore
{
    /// <summary>
    /// Performs the get async operation.
    /// </summary>
    /// <param name="maximumEntries">The maximum entries.</param>
    /// <returns>The result of the operation.</returns>
    public Task<LeaderboardStoreResult> GetAsync(int maximumEntries);
}

/// <summary>
/// Represents leaderboard store result.
/// </summary>
public sealed class LeaderboardStoreResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardStoreResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="entries">The entries.</param>
    public LeaderboardStoreResult(
        LeaderboardStoreOutcome outcome,
        IReadOnlyList<LeaderboardEntry>? entries = null)
    {
        Outcome = outcome;
        Entries = entries ?? new LeaderboardEntry[0];
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public LeaderboardStoreOutcome Outcome { get; }

    /// <summary>
    /// Gets the entries.
    /// </summary>
    public IReadOnlyList<LeaderboardEntry> Entries { get; }
}

/// <summary>
/// Defines the supported leaderboard store outcome values.
/// </summary>
public enum LeaderboardStoreOutcome
{
    /// <summary>
    /// Represents success.
    /// </summary>
    Success,
    /// <summary>
    /// Represents unavailable.
    /// </summary>
    Unavailable
}
