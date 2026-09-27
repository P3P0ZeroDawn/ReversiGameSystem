using System;
using System.Collections.Generic;
using System.Linq;

namespace ReversiGameSystem.Services;

internal enum FriendRequestResult
{
    Success,
    CannotAddYourself,
    FriendshipAlreadyExists,
    FriendRequestAlreadyPending,
    AccountNotAvailable,
    ServerConnectionFailed,
    PersistenceError
}

internal enum FriendSearchResult
{
    Success,
    ServerConnectionFailed,
    RetrievalError
}

internal enum FriendshipRequestState
{
    Pending,
    Accepted,
    Rejected
}

internal enum FriendRequestResponseResult
{
    Accepted,
    Rejected,
    AlreadyProcessed,
    Unauthorized,
    ServerConnectionFailed,
    PersistenceError
}

internal sealed class FriendshipRequest
{
    internal FriendshipRequest(
        string id,
        string senderUsername,
        string recipientUsername,
        DateTime createdAtUtc,
        FriendshipRequestState state = FriendshipRequestState.Pending)
    {
        Id = id;
        SenderUsername = senderUsername;
        RecipientUsername = recipientUsername;
        CreatedAtUtc = createdAtUtc;
        State = state;
    }

    internal string Id { get; }

    internal string SenderUsername { get; }

    internal string RecipientUsername { get; }

    internal DateTime CreatedAtUtc { get; }

    internal FriendshipRequestState State { get; private set; }

    internal void ChangeState(FriendshipRequestState state)
    {
        State = state;
    }
}

internal sealed class FriendCandidate
{
    internal FriendCandidate(string username, bool isActive = true)
    {
        Username = username;
        IsActive = isActive;
    }

    internal string Username { get; }

    internal bool IsActive { get; }
}

internal sealed class FriendshipStore
{
    private readonly object _syncRoot = new object();
    private readonly List<FriendCandidate> _accounts;
    private readonly HashSet<string> _friendships;
    private readonly List<FriendshipRequest> _friendRequests;

    internal FriendshipStore(
        IEnumerable<FriendCandidate> accounts,
        IEnumerable<string>? friendships = null,
        IEnumerable<FriendshipRequest>? friendRequests = null)
    {
        _accounts = accounts.ToList();
        _friendships = new HashSet<string>(
            friendships ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        _friendRequests = new List<FriendshipRequest>(
            friendRequests ?? Array.Empty<FriendshipRequest>());
    }

    internal FriendSearchResult Search(
        string requesterUsername,
        string query,
        out IReadOnlyList<FriendCandidate> candidates)
    {
        lock (_syncRoot)
        {
            candidates = _accounts
                .Where(account => account.IsActive)
                .Where(account => !string.Equals(
                    account.Username,
                    requesterUsername,
                    StringComparison.OrdinalIgnoreCase))
                .Where(account => account.Username.IndexOf(
                    query,
                    StringComparison.CurrentCultureIgnoreCase) >= 0)
                .OrderBy(account => account.Username, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            return FriendSearchResult.Success;
        }
    }

    internal FriendRequestResult TryCreateRequest(
        string requesterUsername,
        string recipientUsername)
    {
        lock (_syncRoot)
        {
            if (string.Equals(
                requesterUsername,
                recipientUsername,
                StringComparison.OrdinalIgnoreCase))
            {
                return FriendRequestResult.CannotAddYourself;
            }

            FriendCandidate? recipient = _accounts.FirstOrDefault(account => string.Equals(
                account.Username,
                recipientUsername,
                StringComparison.OrdinalIgnoreCase));
            if (recipient == null || !recipient.IsActive)
            {
                return FriendRequestResult.AccountNotAvailable;
            }

            string relationshipKey = CreateRelationshipKey(
                requesterUsername,
                recipient.Username);
            if (_friendships.Contains(relationshipKey))
            {
                return FriendRequestResult.FriendshipAlreadyExists;
            }

            if (_friendRequests.Any(request =>
                request.State == FriendshipRequestState.Pending
                && string.Equals(
                    CreateRelationshipKey(
                        request.SenderUsername,
                        request.RecipientUsername),
                    relationshipKey,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return FriendRequestResult.FriendRequestAlreadyPending;
            }

            _friendRequests.Add(new FriendshipRequest(
                Guid.NewGuid().ToString("N"),
                requesterUsername,
                recipient.Username,
                DateTime.UtcNow));
            return FriendRequestResult.Success;
        }
    }

    internal IReadOnlyList<FriendshipRequest> GetPendingReceived(string recipientUsername)
    {
        lock (_syncRoot)
        {
            return _friendRequests
                .Where(request => request.State == FriendshipRequestState.Pending)
                .Where(request => string.Equals(
                    request.RecipientUsername,
                    recipientUsername,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(request => request.CreatedAtUtc)
                .ToArray();
        }
    }

    internal IReadOnlyList<FriendCandidate> GetFriends(string username)
    {
        lock (_syncRoot)
        {
            return _accounts
                .Where(account => account.IsActive)
                .Where(account => !string.Equals(
                    account.Username,
                    username,
                    StringComparison.OrdinalIgnoreCase))
                .Where(account => _friendships.Contains(
                    CreateRelationshipKey(username, account.Username)))
                .OrderBy(
                    account => account.Username,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }
    }

    internal FriendRemovalResult TryRemoveFriendship(
        string username,
        string friendUsername)
    {
        lock (_syncRoot)
        {
            string relationshipKey = CreateRelationshipKey(
                username,
                friendUsername);
            if (!_friendships.Contains(relationshipKey))
            {
                return FriendRemovalResult.NotFound;
            }

            _friendships.Remove(relationshipKey);
            return FriendRemovalResult.Success;
        }
    }

    internal FriendRequestResponseResult TryRespondToRequest(
        string requestId,
        string recipientUsername,
        bool accept)
    {
        lock (_syncRoot)
        {
            FriendshipRequest? request = _friendRequests.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, requestId, StringComparison.Ordinal));
            if (request == null || request.State != FriendshipRequestState.Pending)
            {
                return FriendRequestResponseResult.AlreadyProcessed;
            }

            if (!string.Equals(
                request.RecipientUsername,
                recipientUsername,
                StringComparison.OrdinalIgnoreCase))
            {
                return FriendRequestResponseResult.Unauthorized;
            }

            if (!accept)
            {
                request.ChangeState(FriendshipRequestState.Rejected);
                return FriendRequestResponseResult.Rejected;
            }

            string relationshipKey = CreateRelationshipKey(
                request.SenderUsername,
                request.RecipientUsername);
            _friendships.Add(relationshipKey);
            request.ChangeState(FriendshipRequestState.Accepted);
            return FriendRequestResponseResult.Accepted;
        }
    }

    internal static string CreateRelationshipKey(string firstUsername, string secondUsername)
    {
        return string.Compare(
            firstUsername,
            secondUsername,
            StringComparison.OrdinalIgnoreCase) <= 0
                ? firstUsername + "|" + secondUsername
                : secondUsername + "|" + firstUsername;
    }
}

internal sealed class FriendRequestService
{
    private readonly string _requesterUsername;
    private readonly FriendshipStore _store;

    internal FriendRequestService(string requesterUsername, FriendshipStore store)
    {
        _requesterUsername = requesterUsername;
        _store = store;
    }

    internal FriendSearchResult Search(
        string query,
        out IReadOnlyList<FriendCandidate> candidates)
    {
        return _store.Search(_requesterUsername, query.Trim(), out candidates);
    }

    internal FriendRequestResult Send(string recipientUsername)
    {
        return _store.TryCreateRequest(_requesterUsername, recipientUsername);
    }
}

internal static class FriendshipPreviewState
{
    private static readonly object SyncRoot = new object();
    private static string? _username;
    private static FriendshipStore? _store;

    internal static FriendRequestService GetService(string requesterUsername)
    {
        lock (SyncRoot)
        {
            if (_store == null || !string.Equals(
                _username,
                requesterUsername,
                StringComparison.OrdinalIgnoreCase))
            {
                _username = requesterUsername;
                string friendship = FriendshipStore.CreateRelationshipKey(
                    requesterUsername,
                    "EmiPan");
                DateTime now = DateTime.UtcNow;
                _store = new FriendshipStore(
                    new[]
                    {
                        new FriendCandidate(requesterUsername),
                        new FriendCandidate("EmiMora"),
                        new FriendCandidate("EmiPan"),
                        new FriendCandidate("Emilio"),
                        new FriendCandidate("Gael"),
                        new FriendCandidate("Luna"),
                        new FriendCandidate("Emilia", false)
                    },
                    new[] { friendship },
                    new[]
                    {
                        new FriendshipRequest(
                            "outgoing-emilio",
                            requesterUsername,
                            "Emilio",
                            now.AddHours(-1)),
                        new FriendshipRequest(
                            "incoming-gael",
                            "Gael",
                            requesterUsername,
                            now.AddMinutes(-20)),
                        new FriendshipRequest(
                            "incoming-luna",
                            "Luna",
                            requesterUsername,
                            now.AddDays(-1)),
                        new FriendshipRequest(
                            "accepted-old",
                            "EmiMora",
                            requesterUsername,
                            now.AddDays(-2),
                            FriendshipRequestState.Accepted)
                    });
            }

            return new FriendRequestService(requesterUsername, _store);
        }
    }

    internal static void Reset()
    {
        lock (SyncRoot)
        {
            _username = null;
            _store = null;
        }
    }

    internal static FriendshipStore GetStore(string requesterUsername)
    {
        GetService(requesterUsername);
        lock (SyncRoot)
        {
            if (_store == null)
            {
                throw new InvalidOperationException(
                    "The friendship store was not initialized.");
            }

            return _store;
        }
    }
}
