using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Friendships;

public sealed class FriendshipApplicationService
{
    private readonly SessionStore _sessions;
    private readonly IFriendshipRepository _friendships;

    public FriendshipApplicationService(
        SessionStore sessions,
        IFriendshipRepository friendships)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _friendships = friendships ?? throw new ArgumentNullException(nameof(friendships));
    }

    public FriendshipResult<AuthenticatedSession> EvaluateChannelAccess(string sessionToken)
    {
        SessionResult<AuthenticatedSession> sessionResult =
            _sessions.FindAuthenticated(sessionToken);
        if (sessionResult.Status == SessionStatus.MissingSessionToken)
        {
            return new FriendshipResult<AuthenticatedSession>(FriendshipStatus.MissingSessionToken);
        }

        if (sessionResult.Status != SessionStatus.Success || sessionResult.Value == null)
        {
            return new FriendshipResult<AuthenticatedSession>(FriendshipStatus.InvalidSession);
        }

        AuthenticatedSession session = sessionResult.Value;
        if (!_friendships.IsAccountActive(session.AccountId))
        {
            return new FriendshipResult<AuthenticatedSession>(FriendshipStatus.InactiveAccount);
        }

        return new FriendshipResult<AuthenticatedSession>(session);
    }

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

    public long[] ReadConfirmedFriendIds(long accountId)
    {
        if (accountId <= 0)
        {
            return Array.Empty<long>();
        }

        return _friendships.ReadConfirmedFriendIds(accountId);
    }

    public bool VerifyActiveFriendship(long accountId, long friendAccountId)
    {
        if (accountId <= 0 || friendAccountId <= 0 || accountId == friendAccountId)
        {
            return false;
        }

        return _friendships.AreFriends(accountId, friendAccountId);
    }

    public bool IsAccountActive(long accountId)
    {
        if (accountId <= 0)
        {
            return false;
        }

        return _friendships.IsAccountActive(accountId);
    }
}
