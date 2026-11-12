using System;

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
    ServiceUnavailable
}

internal sealed class ProfileResult<T> where T : class
{
    internal ProfileResult(ProfileStatus status, T? value)
    {
        if (status == ProfileStatus.Success && value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        if (status != ProfileStatus.Success && value != null)
        {
            throw new ArgumentException("A failed result cannot contain a value.", nameof(value));
        }
        Status = status;
        Value = value;
    }

    internal ProfileStatus Status { get; }
    internal T? Value { get; }
}

internal sealed class PlayerProfile
{
    internal PlayerProfile(string username, bool hasProfileImage, ProfileStatistics statistics)
    {
        Username = username;
        HasProfileImage = hasProfileImage;
        Statistics = statistics;
    }

    internal string Username { get; }
    internal bool HasProfileImage { get; }
    internal ProfileStatistics Statistics { get; }
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

internal sealed class ProfileImageContent
{
    internal ProfileImageContent(byte[] content, string mediaType)
    {
        Content = content;
        MediaType = mediaType;
    }

    internal byte[] Content { get; }
    internal string MediaType { get; }
}
