namespace ReversiGameSystem.Services;

internal enum LeaderboardStatus
{
    Success,
    ServiceUnavailable
}

internal sealed class LeaderboardResult
{
    internal LeaderboardResult(LeaderboardStatus status, LeaderboardEntry[] entries)
    {
        Status = status;
        Entries = entries;
    }

    internal LeaderboardStatus Status { get; }
    internal LeaderboardEntry[] Entries { get; }
}

internal sealed class LeaderboardEntry
{
    internal LeaderboardEntry(int position, string username, int wins)
    {
        Position = position;
        Username = username;
        Wins = wins;
    }

    internal int Position { get; }

    internal string Username { get; }

    internal int Wins { get; }
}
