using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Friendships;

/// <summary>
/// Coordinates friendship queries and access evaluations according to application rules.
/// </summary>
public sealed class FriendshipApplicationService
{
    private readonly ISessionRepository _sessions;
    private readonly IFriendshipRepository _friendships;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendshipApplicationService"/> class.
    /// </summary>
    /// <param name="sessions">The authoritative session repository.</param>
    /// <param name="friendships">The active friendships repository.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public FriendshipApplicationService(
        ISessionRepository sessions,
        IFriendshipRepository friendships)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _friendships = friendships ?? throw new ArgumentNullException(nameof(friendships));
    }

    /// <summary>
    /// Evaluates whether a session token is authorized to connect to the friendship presence channel.
    /// </summary>
    /// <param name="sessionToken">The session token to evaluate.</param>
    /// <returns>The result containing the authenticated session, or an expected rejection status.</returns>
    public FriendshipResult<AuthenticatedSession> EvaluateChannelAccess(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return new FriendshipResult<AuthenticatedSession>(FriendshipStatus.MissingSessionToken);
        }

        if (!_sessions.TryGet(sessionToken, out AuthenticatedSession? session) || session == null)
        {
            return new FriendshipResult<AuthenticatedSession>(FriendshipStatus.InvalidSession);
        }

        if (!_friendships.IsAccountActive(session.AccountId))
        {
            return new FriendshipResult<AuthenticatedSession>(FriendshipStatus.InactiveAccount);
        }

        return new FriendshipResult<AuthenticatedSession>(session);
    }

    /// <summary>
    /// Retrieves the confirmed friends of an authenticated and active account.
    /// </summary>
    /// <param name="sessionToken">The caller's session token.</param>
    /// <returns>The result containing the friend accounts, or an expected rejection status.</returns>
    public FriendshipResult<FriendAccount[]> ReadConfirmedFriends(string sessionToken)
    {
        FriendshipResult<AuthenticatedSession> access = EvaluateChannelAccess(sessionToken);
        if (access.Status != FriendshipStatus.Success)
        {
            return new FriendshipResult<FriendAccount[]>(access.Status);
        }

        AuthenticatedSession session = access.Value
            ?? throw new InvalidOperationException(
                "A successful channel access must contain its session.");

        FriendAccount[] friends = _friendships.GetFriends(session.AccountId);
        return new FriendshipResult<FriendAccount[]>(friends);
    }

    /// <summary>
    /// Reads the identifiers of all confirmed active friends for an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>The confirmed friend account identifiers.</returns>
    public long[] ReadConfirmedFriendIds(long accountId)
    {
        if (accountId <= 0)
        {
            return Array.Empty<long>();
        }

        return _friendships.ReadConfirmedFriendIds(accountId);
    }

    /// <summary>
    /// Verifies whether two distinct accounts hold an active, accepted friendship.
    /// </summary>
    /// <param name="accountId">The first account identifier.</param>
    /// <param name="friendAccountId">The second account identifier.</param>
    /// <returns>True if an active friendship exists; otherwise, false.</returns>
    public bool VerifyActiveFriendship(long accountId, long friendAccountId)
    {
        if (accountId <= 0 || friendAccountId <= 0 || accountId == friendAccountId)
        {
            return false;
        }

        return _friendships.AreFriends(accountId, friendAccountId);
    }

    /// <summary>
    /// Checks whether an account is currently active.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>True if the account is active; otherwise, false.</returns>
    public bool IsAccountActive(long accountId)
    {
        if (accountId <= 0)
        {
            return false;
        }

        return _friendships.IsAccountActive(accountId);
    }
}
