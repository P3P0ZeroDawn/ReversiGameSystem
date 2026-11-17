namespace ReversiGameSystem.Server.Application.Players;

/// <summary>Loads active public profiles and their calculated match totals.</summary>
public interface IPlayerProfileRepository
{
    /// <summary>Gets the expected lookup result for an active public profile.</summary>
    /// <param name="accountId">The account to query.</param>
    /// <returns>The active profile or an account-not-found status.</returns>
    public ProfileResult<PlayerProfile> GetProfile(long accountId);

    /// <summary>Gets the stored image reference for an active account without loading statistics.</summary>
    /// <param name="accountId">The account to query.</param>
    /// <returns>The image reference (or empty string when absent), or AccountNotFound.</returns>
    public ProfileResult<string> GetProfileImageReference(long accountId);

    /// <summary>Checks whether an account remains active.</summary>
    /// <param name="accountId">The account to check.</param>
    /// <returns>Whether the account exists and remains active.</returns>
    public bool IsActive(long accountId);

    /// <summary>Changes the username of an active account when it remains available.</summary>
    /// <param name="accountId">The account to modify.</param>
    /// <param name="username">The normalized and validated username.</param>
    /// <returns>The expected operation status.</returns>
    public ProfileOperationStatus ChangeUsername(
        long accountId,
        string username);

    /// <summary>Associates a server-owned image reference with an active account.</summary>
    /// <param name="accountId">The account to modify.</param>
    /// <param name="imageReference">The server-owned image reference.</param>
    /// <returns>The expected operation status.</returns>
    public ProfileOperationStatus SetProfileImage(
        long accountId,
        string imageReference);
}
