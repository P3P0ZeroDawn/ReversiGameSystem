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

    public FriendshipResult<FriendAccount[]> FindPlayers(string sessionToken, string searchText)
    {
        FriendshipResult<AuthenticatedSession> access = EvaluateChannelAccess(sessionToken);
        if (access.Status != FriendshipStatus.Success)
        {
            return new FriendshipResult<FriendAccount[]>(access.Status);
        }
        AuthenticatedSession session = access.Value
            ?? throw new InvalidOperationException("Authorized access must contain its session.");
        string normalizedSearch = searchText?.Trim() ?? string.Empty;
        FriendAccount[] players = Array.Empty<FriendAccount>();
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            players = _friendships.FindPlayers(session.AccountId, normalizedSearch);
        }
        return new FriendshipResult<FriendAccount[]>(players);
    }

    public FriendshipResult<PendingFriendRequest> SendFriendRequest(
        string sessionToken, long recipientAccountId)
    {
        FriendshipResult<AuthenticatedSession> access = EvaluateChannelAccess(sessionToken);
        if (access.Status != FriendshipStatus.Success)
        {
            return new FriendshipResult<PendingFriendRequest>(access.Status);
        }
        AuthenticatedSession session = access.Value
            ?? throw new InvalidOperationException("Authorized access must contain its session.");
        if (recipientAccountId <= 0)
        {
            return new FriendshipResult<PendingFriendRequest>(FriendshipStatus.InvalidAccountId);
        }
        if (recipientAccountId == session.AccountId)
        {
            return new FriendshipResult<PendingFriendRequest>(
                FriendshipStatus.CannotSendRequestToSelf);
        }
        return _friendships.CreatePendingRequest(session.AccountId, recipientAccountId);
    }

    public FriendshipResult<PendingFriendRequest[]> ReadPendingRequests(string sessionToken)
    {
        FriendshipResult<AuthenticatedSession> access = EvaluateChannelAccess(sessionToken);
        if (access.Status != FriendshipStatus.Success)
        {
            return new FriendshipResult<PendingFriendRequest[]>(access.Status);
        }
        AuthenticatedSession session = access.Value
            ?? throw new InvalidOperationException("Authorized access must contain its session.");
        PendingFriendRequest[] requests = _friendships.ReadPendingRequests(session.AccountId);
        return new FriendshipResult<PendingFriendRequest[]>(requests);
    }

    public FriendshipResult<PendingFriendRequest> RespondToFriendRequest(
        string sessionToken, string requestId, bool accept)
    {
        FriendshipResult<AuthenticatedSession> access = EvaluateChannelAccess(sessionToken);
        if (access.Status != FriendshipStatus.Success)
        {
            return new FriendshipResult<PendingFriendRequest>(access.Status);
        }
        AuthenticatedSession session = access.Value
            ?? throw new InvalidOperationException("Authorized access must contain its session.");
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return new FriendshipResult<PendingFriendRequest>(FriendshipStatus.MissingRequestId);
        }
        PendingFriendRequest[] requests = _friendships.ReadPendingRequests(session.AccountId);
        foreach (PendingFriendRequest request in requests)
        {
            if (!string.Equals(request.RequestId, requestId, StringComparison.Ordinal))
            {
                continue;
            }
            if (request.Recipient.AccountId != session.AccountId)
            {
                return new FriendshipResult<PendingFriendRequest>(
                    FriendshipStatus.UnauthorizedRequest);
            }
            FriendshipStatus status = _friendships.RespondToRequest(
                session.AccountId, request, accept);
            FriendshipResult<PendingFriendRequest> response = status == FriendshipStatus.Success
                ? new FriendshipResult<PendingFriendRequest>(request)
                : new FriendshipResult<PendingFriendRequest>(status);
            return response;
        }
        return new FriendshipResult<PendingFriendRequest>(FriendshipStatus.RequestUnavailable);
    }

    public FriendshipResult<long> RemoveFriendship(string sessionToken, long friendAccountId)
    {
        FriendshipResult<AuthenticatedSession> access = EvaluateChannelAccess(sessionToken);
        if (access.Status != FriendshipStatus.Success)
        {
            return new FriendshipResult<long>(access.Status);
        }
        AuthenticatedSession session = access.Value
            ?? throw new InvalidOperationException("Authorized access must contain its session.");
        if (friendAccountId <= 0 || friendAccountId == session.AccountId)
        {
            return new FriendshipResult<long>(FriendshipStatus.InvalidAccountId);
        }
        FriendshipStatus status = _friendships.RemoveFriendship(session.AccountId, friendAccountId);
        FriendshipResult<long> removal = status == FriendshipStatus.Success
            ? new FriendshipResult<long>(session.AccountId)
            : new FriendshipResult<long>(status);
        return removal;
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
