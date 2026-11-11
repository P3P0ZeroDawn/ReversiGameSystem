namespace ReversiGameSystem.Server.Application.Friendships;

/// <summary>Reads active accounts and accepted, current friendships.</summary>
public interface IFriendshipRepository
{
    /// <summary>Checks whether the account is currently active.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>Whether the account is active.</returns>
    bool IsAccountActive(long accountId);

    /// <summary>Reads only the caller's current, active friends.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <returns>Public friend identities ordered by username.</returns>
    FriendAccount[] GetFriends(long accountId);

    /// <summary>Checks the current accepted relationship between active accounts.</summary>
    /// <param name="accountId">One account identifier.</param>
    /// <param name="friendAccountId">The other account identifier.</param>
    /// <returns>Whether an active friendship exists.</returns>
    bool AreFriends(long accountId, long friendAccountId);

    /// <summary>Reads the identifiers of all confirmed active friends for an account.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>The confirmed friend account identifiers.</returns>
    long[] ReadConfirmedFriendIds(long accountId);
}
