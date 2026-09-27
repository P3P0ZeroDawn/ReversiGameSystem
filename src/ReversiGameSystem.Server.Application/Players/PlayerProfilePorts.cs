using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>
/// Defines the player profile store contract.
/// </summary>
public interface IPlayerProfileStore
{
    /// <summary>
    /// Performs the get async operation.
    /// </summary>
    /// <param name="accountId">The account id.</param>
    /// <returns>The result of the operation.</returns>
    Task<PlayerProfileStoreResult> GetAsync(long accountId);
}

/// <summary>
/// Represents player profile store result.
/// </summary>
public sealed class PlayerProfileStoreResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerProfileStoreResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="profile">The profile.</param>
    public PlayerProfileStoreResult(
        PlayerProfileStoreOutcome outcome,
        PlayerProfileData? profile = null)
    {
        Outcome = outcome;
        Profile = profile;
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public PlayerProfileStoreOutcome Outcome { get; }

    /// <summary>
    /// Gets the profile.
    /// </summary>
    public PlayerProfileData? Profile { get; }
}

/// <summary>
/// Defines the supported player profile store outcome values.
/// </summary>
public enum PlayerProfileStoreOutcome
{
    /// <summary>
    /// Represents found.
    /// </summary>
    Found,
    /// <summary>
    /// Represents not found.
    /// </summary>
    NotFound,
    /// <summary>
    /// Represents unavailable.
    /// </summary>
    Unavailable
}

/// <summary>
/// Represents player profile data.
/// </summary>
public sealed class PlayerProfileData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerProfileData"/> class.
    /// </summary>
    /// <param name="accountId">The account id.</param>
    /// <param name="username">The username.</param>
    /// <param name="profileImageUri">The profile image uri.</param>
    /// <param name="wins">The wins.</param>
    /// <param name="losses">The losses.</param>
    /// <param name="draws">The draws.</param>
    public PlayerProfileData(
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
    }

    /// <summary>
    /// Gets the account id.
    /// </summary>
    public long AccountId { get; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the profile image uri.
    /// </summary>
    public string? ProfileImageUri { get; }

    /// <summary>
    /// Gets the wins.
    /// </summary>
    public int Wins { get; }

    /// <summary>
    /// Gets the losses.
    /// </summary>
    public int Losses { get; }

    /// <summary>
    /// Gets the draws.
    /// </summary>
    public int Draws { get; }
}
