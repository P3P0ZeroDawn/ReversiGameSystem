using System;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Leaderboards;

public sealed class GetLeaderboardHandler : IGetLeaderboardHandler
{
    public const int MaximumAllowedEntries = 100;
    private readonly ILeaderboardStore leaderboardStore;

    public GetLeaderboardHandler(ILeaderboardStore leaderboardStore)
    {
        this.leaderboardStore = leaderboardStore
            ?? throw new ArgumentNullException(nameof(leaderboardStore));
    }

    public async Task<LeaderboardResult> ExecuteAsync(LeaderboardQuery query)
    {
        if (query == null
            || query.MaximumEntries < 1
            || query.MaximumEntries > MaximumAllowedEntries)
        {
            return new LeaderboardResult(LeaderboardOutcome.InvalidRequest);
        }

        try
        {
            LeaderboardStoreResult result = await leaderboardStore.GetAsync(
                query.MaximumEntries);
            return result.Outcome == LeaderboardStoreOutcome.Success
                ? new LeaderboardResult(LeaderboardOutcome.Success, result.Entries)
                : new LeaderboardResult(LeaderboardOutcome.ServiceUnavailable);
        }
        catch (Exception)
        {
            return new LeaderboardResult(LeaderboardOutcome.ServiceUnavailable);
        }
    }
}
