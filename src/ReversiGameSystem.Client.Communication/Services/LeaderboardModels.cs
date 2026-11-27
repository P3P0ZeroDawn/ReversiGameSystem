namespace ReversiGameSystem.Services;

internal enum LeaderboardStatus
{
    Success,
    Inactive,
    Busy,
    InvalidMaximumEntries,
    ServiceUnavailable
}

internal sealed class LeaderboardEntry
{
    internal LeaderboardEntry(long accountId, int position, string username, int wins)
    {
        AccountId = accountId;
        Position = position;
        Username = username;
        Wins = wins;
    }

    internal long AccountId { get; }

    internal int Position { get; }

    internal string Username { get; }

    internal int Wins { get; }
}
