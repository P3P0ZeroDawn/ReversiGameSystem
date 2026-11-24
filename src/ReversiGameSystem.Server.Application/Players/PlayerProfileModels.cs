using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

public sealed class PlayerProfile
{
    public PlayerProfile(
        long accountId,
        string username,
        string? profileImageUri,
        int wins,
        int losses,
        int draws)
    {
        AccountId = accountId;
        Username = username;
        ProfileImageUri = profileImageUri;
        Wins = wins;
        Losses = losses;
        Draws = draws;
        MatchesPlayed = wins + losses + draws;
        WinRate = 0d;
        if (MatchesPlayed > 0)
        {
            WinRate = (double)wins / MatchesPlayed;
        }
    }

    public long AccountId { get; }

    public string Username { get; }

    public string? ProfileImageUri { get; }

    public int MatchesPlayed { get; }

    public int Wins { get; }

    public int Losses { get; }

    public int Draws { get; }

    public double WinRate { get; }
}

public enum ProfileStatus
{
    Success,
    MissingSessionToken,
    InvalidSession,
    InactiveAccount,
    InvalidAccountId,
    AccountNotFound,
    InvalidUsername,
    UsernameUnavailable,
    InvalidImage
}

public sealed class ProfileResult<T>
{
    public ProfileResult(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Status = ProfileStatus.Success;
    }

    public ProfileResult(ProfileStatus status)
    {
        if (status == ProfileStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A rejected profile result cannot specify success status.");
        }

        Status = status;
        Value = default;
    }

    public ProfileStatus Status { get; }

    public T? Value { get; }
}

public sealed class GetProfileImageResult
{
    public GetProfileImageResult()
    {
        Status = ProfileStatus.Success;
        Image = null;
    }

    public GetProfileImageResult(ProfileStatus status)
    {
        if (status == ProfileStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A rejected profile image result cannot specify success status.");
        }

        Status = status;
        Image = null;
    }

    public GetProfileImageResult(ProfileImageData? image)
    {
        Status = ProfileStatus.Success;
        Image = image;
    }

    public ProfileStatus Status { get; }

    public ProfileImageData? Image { get; }
}
