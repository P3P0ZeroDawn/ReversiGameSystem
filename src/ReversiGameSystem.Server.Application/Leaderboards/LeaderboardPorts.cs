using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Leaderboards;

public interface ILeaderboardStore
{
    Task<LeaderboardStoreResult> GetAsync(int maximumEntries);
}

public sealed class LeaderboardStoreResult
{
    public LeaderboardStoreResult(
        LeaderboardStoreOutcome outcome,
        IReadOnlyList<LeaderboardEntry>? entries = null)
    {
        Outcome = outcome;
        Entries = entries ?? new LeaderboardEntry[0];
    }

    public LeaderboardStoreOutcome Outcome { get; }

    public IReadOnlyList<LeaderboardEntry> Entries { get; }
}

public enum LeaderboardStoreOutcome
{
    Success,
    Unavailable
}
