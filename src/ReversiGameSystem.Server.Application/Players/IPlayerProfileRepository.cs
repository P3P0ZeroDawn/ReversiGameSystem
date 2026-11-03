using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>Loads active public profiles and their calculated match totals.</summary>
public interface IPlayerProfileRepository
{
    /// <summary>Gets the expected lookup result for an active public profile.</summary>
    /// <param name="accountId">The account to query.</param>
    /// <returns>The explicit found or not-found result.</returns>
    public Task<PlayerProfileLookupResult> GetAsync(long accountId);

    /// <summary>Checks whether an account remains active.</summary>
    /// <param name="accountId">The account to check.</param>
    /// <returns>Whether the account exists and remains active.</returns>
    public Task<bool> IsActiveAsync(long accountId);

    /// <summary>Changes the username of an active account when it remains available.</summary>
    /// <param name="accountId">The account to modify.</param>
    /// <param name="username">The normalized and validated username.</param>
    /// <returns>The expected persistence outcome.</returns>
    public Task<ProfileRepositoryUpdateStatus> ChangeUsernameAsync(
        long accountId,
        string username);

    /// <summary>Associates a server-owned image reference with an active account.</summary>
    /// <param name="accountId">The account to modify.</param>
    /// <param name="imageReference">The server-owned image reference.</param>
    /// <returns>The expected persistence outcome.</returns>
    public Task<ProfileRepositoryUpdateStatus> SetProfileImageAsync(
        long accountId,
        string imageReference);
}

/// <summary>Identifies an expected outcome of a profile persistence update.</summary>
public enum ProfileRepositoryUpdateStatus
{
    /// <summary>The profile update was committed.</summary>
    Success,
    /// <summary>The account no longer exists as an active account.</summary>
    InactiveAccount,
    /// <summary>Another account already owns the proposed username.</summary>
    UsernameUnavailable
}
