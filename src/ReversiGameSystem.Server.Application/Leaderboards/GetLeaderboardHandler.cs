using System;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Leaderboards;

/// <summary>
/// Represents get leaderboard handler.
/// </summary>
public sealed class GetLeaderboardHandler : IGetLeaderboardHandler
{
    /// <summary>
    /// Represents the maximum allowed entries.
    /// </summary>
    public const int MaximumAllowedEntries = 100;
    private readonly ILeaderboardStore _leaderboardStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetLeaderboardHandler"/> class.
    /// </summary>
    /// <param name="leaderboardStore">The leaderboard store.</param>
    public GetLeaderboardHandler(ILeaderboardStore leaderboardStore)
    {
        _leaderboardStore = leaderboardStore
            ?? throw new ArgumentNullException(nameof(leaderboardStore));
    }

    /// <summary>
    /// Performs the execute async operation.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<LeaderboardResult> ExecuteAsync(LeaderboardQuery query)
    {
        if (query == null
            || query.MaximumEntries < 1
            || query.MaximumEntries > MaximumAllowedEntries)
        {
            return new LeaderboardResult(LeaderboardOutcome.InvalidRequest);
        }

        LeaderboardStoreResult result = await _leaderboardStore.GetAsync(
            query.MaximumEntries);
        return result.Outcome == LeaderboardStoreOutcome.Success
            ? new LeaderboardResult(LeaderboardOutcome.Success, result.Entries)
            : new LeaderboardResult(LeaderboardOutcome.ServiceUnavailable);
    }
}
