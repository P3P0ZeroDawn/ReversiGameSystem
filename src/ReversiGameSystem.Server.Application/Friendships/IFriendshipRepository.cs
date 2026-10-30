namespace ReversiGameSystem.Server.Application.Friendships;

/// <summary>Reads active accounts and accepted, current friendships.</summary>
public interface IFriendshipRepository
{
    /// <summary>Checks whether the account is currently active.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>Whether the account is active.</returns>
    public bool IsAccountActive(long accountId);

    /// <summary>Reads only the caller's current, active friends.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <returns>Public friend identities ordered by username.</returns>
    public FriendAccount[] GetFriends(long accountId);

    /// <summary>Checks the current accepted relationship between active accounts.</summary>
    /// <param name="accountId">One account identifier.</param>
    /// <param name="friendAccountId">The other account identifier.</param>
    /// <returns>Whether an active friendship exists.</returns>
    public bool AreFriends(long accountId, long friendAccountId);
}

/// <summary>Contains the public fields permitted in a friend listing.</summary>
public sealed class FriendAccount
{
    /// <summary>Creates a public friend identity.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="username">The public username.</param>
    /// <param name="hasProfileImage">Whether a custom image exists.</param>
    public FriendAccount(long accountId, string username, bool hasProfileImage)
    {
        AccountId = accountId;
        Username = username;
        HasProfileImage = hasProfileImage;
    }

    /// <summary>Gets the account identifier.</summary>
    public long AccountId { get; }
    /// <summary>Gets the public username.</summary>
    public string Username { get; }
    /// <summary>Gets whether a custom image exists.</summary>
    public bool HasProfileImage { get; }
}
