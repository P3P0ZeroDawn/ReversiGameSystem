using System;
using System.ServiceModel;
using System.Collections.Generic;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Exposes public friendship and authorized presence notifications over WCF duplex channels.
/// </summary>
[ServiceBehavior(
    InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple,
    UseSynchronizationContext = false)]
public sealed class FriendshipService : IFriendshipService
{
    private readonly FriendshipApplicationService _applicationService;
    private readonly AccountPresenceRegistry _presenceRegistry;
    private readonly FriendshipSubscriptions _subscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendshipService"/> class.
    /// </summary>
    /// <param name="applicationService">The friendship application service.</param>
    /// <param name="presenceRegistry">The connected presence registry.</param>
    /// <param name="subscriptions">The active friendship callback subscriptions manager.</param>
    /// <exception cref="ArgumentNullException">A required dependency is null.</exception>
    public FriendshipService(
        FriendshipApplicationService applicationService,
        AccountPresenceRegistry presenceRegistry,
        FriendshipSubscriptions subscriptions)
    {
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _presenceRegistry = presenceRegistry
            ?? throw new ArgumentNullException(nameof(presenceRegistry));
        _subscriptions = subscriptions
            ?? throw new ArgumentNullException(nameof(subscriptions));
    }

    /// <summary>
    /// Authorizes the caller and opens a duplex callback channel for friendship presence updates.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public void OpenFriendshipChannel(string sessionToken)
    {
        FriendshipResult<AuthenticatedSession> accessResult =
            _applicationService.EvaluateChannelAccess(sessionToken);
        ThrowFaultIfFailed(accessResult.Status);

        AuthenticatedSession session = accessResult.Value
            ?? throw new InvalidOperationException(
                "A successful access evaluation must contain its session.");

        IFriendshipServiceCallback callback = OperationContext.Current
            .GetCallbackChannel<IFriendshipServiceCallback>();

        IDisposable presenceRegistration =
            _presenceRegistry.Register(sessionToken, session.AccountId);

        _subscriptions.Register(
            sessionToken,
            session.AccountId,
            callback,
            presenceRegistration);
    }

    /// <summary>
    /// Returns the confirmed friends of the authenticated caller along with online presence.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <returns>The caller's confirmed friend summaries.</returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    public FriendSummary[] GetFriends(string sessionToken)
    {
        FriendshipResult<FriendAccount[]> result =
            _applicationService.ReadConfirmedFriends(sessionToken);
        ThrowFaultIfFailed(result.Status);

        FriendAccount[] friends = result.Value
            ?? throw new InvalidOperationException(
                "A successful friends result must contain its value.");

        FriendSummary[] summaries = new FriendSummary[friends.Length];
        for (int index = 0; index < friends.Length; index++)
        {
            FriendAccount friend = friends[index];
            summaries[index] = new FriendSummary
            {
                Account = MapAccountReference(friend),
                HasProfileImage = friend.HasProfileImage,
                IsOnline = _presenceRegistry.IsOnline(friend.AccountId)
            };
        }

        return summaries;
    }

    /// <summary>
    /// Searches for players matching the provided search text.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="searchText">The username search text.</param>
    /// <returns>Matching player summaries.</returns>
    /// <exception cref="FaultException{SessionFault}">The session is not authorized.</exception>
    /// <exception cref="FaultException{FriendshipFault}">A friendship rule rejected the operation.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the operation from completing.
    /// </exception>
    public PlayerSummary[] FindPlayers(string sessionToken, string searchText)
    {
        FriendshipResult<FriendAccount[]> result =
            _applicationService.FindPlayers(sessionToken, searchText);
        ThrowFaultIfFailed(result.Status);
        FriendAccount[] accounts = result.Value
            ?? throw new InvalidOperationException("A successful search must contain its data.");
        PlayerSummary[] players = new PlayerSummary[accounts.Length];
        for (int index = 0; index < accounts.Length; index++)
        {
            FriendAccount account = accounts[index];
            players[index] = new PlayerSummary
            {
                Account = MapAccountReference(account),
                HasProfileImage = account.HasProfileImage
            };
        }
        return players;
    }

    /// <summary>
    /// Sends a friend request to the specified target account.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="recipientAccountId">The target player's account identifier.</param>
    /// <returns>The created friend request.</returns>
    /// <exception cref="FaultException{SessionFault}">The session is not authorized.</exception>
    /// <exception cref="FaultException{FriendshipFault}">A friendship rule rejected the operation.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the operation from completing.
    /// </exception>
    public FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId)
    {
        FriendshipResult<PendingFriendRequest> result =
            _applicationService.SendFriendRequest(sessionToken, recipientAccountId);
        ThrowFaultIfFailed(result.Status);
        PendingFriendRequest request = result.Value
            ?? throw new InvalidOperationException("A successful request must contain its data.");
        FriendRequest notification = MapRequest(request);
        _subscriptions.PublishToAccounts(new HashSet<long> { request.Recipient.AccountId },
            callback => callback.OnFriendRequestReceived(notification));
        return notification;
    }

    /// <summary>
    /// Returns pending incoming and outgoing friend requests for the caller.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <returns>The pending friend requests.</returns>
    /// <exception cref="FaultException{SessionFault}">The session is not authorized.</exception>
    /// <exception cref="FaultException{FriendshipFault}">A friendship rule rejected the operation.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the operation from completing.
    /// </exception>
    public FriendRequest[] GetPendingFriendRequests(string sessionToken)
    {
        FriendshipResult<PendingFriendRequest[]> result =
            _applicationService.ReadPendingRequests(sessionToken);
        ThrowFaultIfFailed(result.Status);
        PendingFriendRequest[] requests = result.Value
            ?? throw new InvalidOperationException("A successful query must contain its data.");
        FriendRequest[] pending = new FriendRequest[requests.Length];
        for (int index = 0; index < requests.Length; index++)
        {
            pending[index] = MapRequest(requests[index]);
        }
        return pending;
    }

    /// <summary>
    /// Accepts or declines a pending friend request.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="requestId">The friend request identifier.</param>
    /// <param name="accept"><c>true</c> to accept; <c>false</c> to decline.</param>
    /// <exception cref="FaultException{SessionFault}">The session is not authorized.</exception>
    /// <exception cref="FaultException{FriendshipFault}">A friendship rule rejected the operation.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the operation from completing.
    /// </exception>
    public void RespondToFriendRequest(string sessionToken, string requestId, bool accept)
    {
        FriendshipResult<PendingFriendRequest> result =
            _applicationService.RespondToFriendRequest(sessionToken, requestId, accept);
        ThrowFaultIfFailed(result.Status);
        PendingFriendRequest request = result.Value
            ?? throw new InvalidOperationException("A successful response must contain its data.");
        long senderId = request.Sender.AccountId;
        long recipientId = request.Recipient.AccountId;
        _subscriptions.PublishToAccounts(new HashSet<long> { senderId },
            callback => callback.OnFriendRequestResponded(request.RequestId, accept));
        if (accept)
        {
            FriendChange senderPresence = _presenceRegistry.IsOnline(senderId)
                ? FriendChange.Connected : FriendChange.Disconnected;
            FriendChange recipientPresence = _presenceRegistry.IsOnline(recipientId)
                ? FriendChange.Connected : FriendChange.Disconnected;
            _subscriptions.PublishToAccounts(new HashSet<long> { senderId },
                callback => callback.OnFriendChanged(recipientId, recipientPresence));
            _subscriptions.PublishToAccounts(new HashSet<long> { recipientId },
                callback => callback.OnFriendChanged(senderId, senderPresence));
        }
    }

    /// <summary>
    /// Removes an existing friend relationship.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="friendAccountId">The friend's account identifier.</param>
    /// <exception cref="FaultException{SessionFault}">The session is not authorized.</exception>
    /// <exception cref="FaultException{FriendshipFault}">A friendship rule rejected the operation.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the operation from completing.
    /// </exception>
    public void RemoveFriend(string sessionToken, long friendAccountId)
    {
        FriendshipResult<long> result =
            _applicationService.RemoveFriendship(sessionToken, friendAccountId);
        ThrowFaultIfFailed(result.Status);
        long callerAccountId = result.Value;
        _subscriptions.PublishToAccounts(new HashSet<long> { callerAccountId },
            callback => callback.OnFriendChanged(friendAccountId, FriendChange.Removed));
        _subscriptions.PublishToAccounts(new HashSet<long> { friendAccountId },
            callback => callback.OnFriendChanged(callerAccountId, FriendChange.Removed));
    }

    private static AccountReference MapAccountReference(FriendAccount account)
    {
        return new AccountReference { AccountId = account.AccountId, Username = account.Username };
    }

    private static FriendRequest MapRequest(PendingFriendRequest request)
    {
        return new FriendRequest
        {
            RequestId = request.RequestId,
            Sender = MapAccountReference(request.Sender),
            Recipient = MapAccountReference(request.Recipient),
            CreatedAtUtc = request.CreatedAtUtc
        };
    }

    private static void ThrowFaultIfFailed(FriendshipStatus status)
    {
        if (status == FriendshipStatus.Success)
        {
            return;
        }
        FaultException fault = status switch
        {
            FriendshipStatus.MissingSessionToken =>
                ServiceFaultFactory.CreateSessionFault(SessionFailure.MissingSessionToken),
            FriendshipStatus.InvalidSession =>
                ServiceFaultFactory.CreateSessionFault(SessionFailure.InvalidSession),
            FriendshipStatus.InactiveAccount =>
                ServiceFaultFactory.CreateSessionFault(SessionFailure.InactiveAccount),
            FriendshipStatus.InvalidAccountId =>
                ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.InvalidAccountId),
            FriendshipStatus.AccountNotFound =>
                ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.AccountUnavailable),
            FriendshipStatus.FriendshipNotFound =>
                ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.FriendshipNotFound),
            FriendshipStatus.CannotSendRequestToSelf =>
                ServiceFaultFactory.CreateFriendshipFault(
                    FriendshipFailure.CannotSendRequestToSelf),
            FriendshipStatus.FriendshipAlreadyExists =>
                ServiceFaultFactory.CreateFriendshipFault(
                    FriendshipFailure.FriendshipAlreadyExists),
            FriendshipStatus.RequestAlreadyPending =>
                ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.RequestAlreadyPending),
            FriendshipStatus.MissingRequestId =>
                ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.MissingRequestId),
            FriendshipStatus.RequestUnavailable =>
                ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.RequestUnavailable),
            FriendshipStatus.UnauthorizedRequest =>
                ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.UnauthorizedRequest),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        throw fault;
    }
}
