namespace ReversiGameSystem.Services;

internal enum ProfileStatus
{
    Success,
    InvalidAccountId,
    AccountNotFound,
    InvalidUsername,
    UsernameUnavailable,
    InvalidImage,
    InactiveAccount,
    InvalidSession,
    ServiceUnavailable,
    Unauthorized,
    SubmissionInProgress
}

internal sealed class PlayerProfile
{
    internal PlayerProfile(PlayerAccount account, ProfileStatistics statistics)
    {
        Account = account;
        Statistics = statistics;
    }

    public PlayerAccount Account { get; }
    public ProfileStatistics Statistics { get; }
}

internal sealed class ProfileStatistics
{
    internal ProfileStatistics(int matchesPlayed, int wins, int losses, int draws, double winRate)
    {
        MatchesPlayed = matchesPlayed;
        Wins = wins;
        Losses = losses;
        Draws = draws;
        WinRate = winRate;
    }

    internal int MatchesPlayed { get; }

    internal int Wins { get; }

    internal int Losses { get; }

    internal int Draws { get; }

    internal double WinRate { get; }
}

internal sealed class PlayerImageContent
{
    internal PlayerImageContent(byte[] content, string mediaType)
    {
        Content = content;
        MediaType = mediaType;
    }

    internal byte[] Content { get; }

    internal string MediaType { get; }
}
