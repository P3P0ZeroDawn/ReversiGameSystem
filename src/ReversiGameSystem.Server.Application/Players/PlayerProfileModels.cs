using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>Contains a player's public identity and finished-match statistics.</summary>
public sealed class PlayerProfile
{
    /// <summary>Initializes a public player profile from persisted match totals.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="username">The public username.</param>
    /// <param name="profileImageUri">The optional server-owned image reference.</param>
    /// <param name="wins">The number of counted wins.</param>
    /// <param name="losses">The number of counted losses.</param>
    /// <param name="draws">The number of counted draws.</param>
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

    /// <summary>Gets the account identifier.</summary>
    public long AccountId { get; }

    /// <summary>Gets the public username.</summary>
    public string Username { get; }

    /// <summary>Gets the optional server-owned image reference.</summary>
    public string? ProfileImageUri { get; }

    /// <summary>Gets the total number of counted finished matches.</summary>
    public int MatchesPlayed { get; }

    /// <summary>Gets the number of counted wins.</summary>
    public int Wins { get; }

    /// <summary>Gets the number of counted losses.</summary>
    public int Losses { get; }

    /// <summary>Gets the number of counted draws.</summary>
    public int Draws { get; }

    /// <summary>Gets wins divided by counted matches, or zero when none exist.</summary>
    public double WinRate { get; }
}

/// <summary>Identifies an expected outcome of a player profile operation.</summary>
public enum ProfileOperationStatus
{
    /// <summary>The requested public data is available.</summary>
    Success,
    /// <summary>The caller omitted the session token.</summary>
    MissingSessionToken,
    /// <summary>The session is unknown, expired or revoked.</summary>
    InvalidSession,
    /// <summary>The authenticated account is no longer active.</summary>
    InactiveAccount,
    /// <summary>The requested account identifier is not positive.</summary>
    InvalidAccountId,
    /// <summary>The requested active public account does not exist.</summary>
    AccountNotFound,
    /// <summary>The proposed username violates the domain policy.</summary>
    InvalidUsername,
    /// <summary>Another account already owns the proposed username.</summary>
    UsernameUnavailable,
    /// <summary>The proposed profile image is absent or invalid.</summary>
    InvalidImage
}

/// <summary>Contains the expected result of a public profile query.</summary>
public sealed class GetPlayerProfileResult
{
    /// <summary>Initializes a public profile result.</summary>
    /// <param name="status">The expected read status.</param>
    /// <param name="profile">The profile when the operation succeeds.</param>
    public GetPlayerProfileResult(
        ProfileOperationStatus status,
        PlayerProfile? profile = null)
    {
        Status = status;
        Profile = profile;
    }

    /// <summary>Gets the expected read status.</summary>
    public ProfileOperationStatus Status { get; }

    /// <summary>Gets the public profile when the operation succeeds.</summary>
    public PlayerProfile? Profile { get; }
}

/// <summary>Contains the expected result of a public profile image query.</summary>
public sealed class GetProfileImageResult
{
    /// <summary>Initializes a public profile image result.</summary>
    /// <param name="status">The expected read status.</param>
    /// <param name="image">The optional custom image.</param>
    public GetProfileImageResult(
        ProfileOperationStatus status,
        ProfileImageData? image = null)
    {
        Status = status;
        Image = image;
    }

    /// <summary>Gets the expected read status.</summary>
    public ProfileOperationStatus Status { get; }

    /// <summary>Gets the custom image, or null when the profile uses the default.</summary>
    public ProfileImageData? Image { get; }
}

/// <summary>Represents whether an active public profile was found.</summary>
public sealed class PlayerProfileLookupResult
{
    private readonly PlayerProfile? _profile;

    private PlayerProfileLookupResult(bool isFound, PlayerProfile? profile)
    {
        IsFound = isFound;
        _profile = profile;
    }

    /// <summary>Gets whether the active profile was found.</summary>
    public bool IsFound { get; }

    /// <summary>Creates a successful lookup containing the active profile.</summary>
    /// <param name="profile">The active profile that was found.</param>
    /// <returns>A successful lookup result.</returns>
    /// <exception cref="ArgumentNullException">The profile is absent.</exception>
    public static PlayerProfileLookupResult CreateFound(PlayerProfile profile)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        return new PlayerProfileLookupResult(true, profile);
    }

    /// <summary>Creates a lookup indicating that no active profile exists.</summary>
    /// <returns>A not-found lookup result.</returns>
    public static PlayerProfileLookupResult CreateNotFound()
    {
        return new PlayerProfileLookupResult(false, null);
    }

    /// <summary>Gets the profile contained in a successful lookup.</summary>
    /// <returns>The active profile.</returns>
    /// <exception cref="InvalidOperationException">The lookup did not find a profile.</exception>
    public PlayerProfile GetFoundProfile()
    {
        if (_profile == null)
        {
            throw new InvalidOperationException(
                "A successful profile lookup must contain a profile.");
        }

        return _profile;
    }
}
