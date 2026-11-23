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

/// <summary>
/// Encapsulates the expected status and optional payload of a profile operation.
/// </summary>
/// <typeparam name="T">The type of the profile payload.</typeparam>
public sealed class ProfileResult<T>
{
    /// <summary>Initializes a successful profile result containing its required payload.</summary>
    /// <param name="value">The confirmed profile payload.</param>
    /// <exception cref="ArgumentNullException">The supplied value is null.</exception>
    public ProfileResult(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Status = ProfileOperationStatus.Success;
    }

    /// <summary>Initializes a rejected profile result with its expected status.</summary>
    /// <param name="status">The failure status.</param>
    /// <exception cref="ArgumentOutOfRangeException">The status represents success.</exception>
    public ProfileResult(ProfileOperationStatus status)
    {
        if (status == ProfileOperationStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A rejected profile result cannot specify success status.");
        }

        Status = status;
        Value = default;
    }

    /// <summary>Gets the authoritative operation status.</summary>
    public ProfileOperationStatus Status { get; }

    /// <summary>Gets the payload when successful, or default when rejected.</summary>
    public T? Value { get; }
}

/// <summary>Contains the expected result of a public profile image query.</summary>
public sealed class GetProfileImageResult
{
    /// <summary>
    /// Creates a successful profile image query result indicating no custom image.
    /// </summary>
    public GetProfileImageResult()
    {
        Status = ProfileOperationStatus.Success;
        Image = null;
    }

    /// <summary>Creates a rejected profile image query result.</summary>
    /// <param name="status">The failure status.</param>
    /// <exception cref="ArgumentOutOfRangeException">The status represents success.</exception>
    public GetProfileImageResult(ProfileOperationStatus status)
    {
        if (status == ProfileOperationStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A rejected profile image result cannot specify success status.");
        }

        Status = status;
        Image = null;
    }

    /// <summary>
    /// Creates a successful profile image query result with an optional custom image.
    /// </summary>
    /// <param name="image">The custom image, or null when using the default image.</param>
    public GetProfileImageResult(ProfileImageData? image)
    {
        Status = ProfileOperationStatus.Success;
        Image = image;
    }

    /// <summary>Gets the expected read status.</summary>
    public ProfileOperationStatus Status { get; }

    /// <summary>Gets the custom image, or null when the profile uses the default.</summary>
    public ProfileImageData? Image { get; }
}
