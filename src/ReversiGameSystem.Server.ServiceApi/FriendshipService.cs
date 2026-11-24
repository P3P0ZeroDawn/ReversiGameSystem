using System;
using System.ServiceModel;
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
                Account = new AccountReference
                {
                    AccountId = friend.AccountId,
                    Username = friend.Username
                },
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
    /// <exception cref="FaultException{ServiceFault}">
    /// The operation is not currently available.
    /// </exception>
    public PlayerSummary[] FindPlayers(string sessionToken, string searchText)
    {
        throw ServiceFaultFactory.CreateUnexpectedFault(CorrelationContext.GetOrCreate());
    }

    /// <summary>
    /// Sends a friend request to the specified target account.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="recipientAccountId">The target player's account identifier.</param>
    /// <returns>The created friend request.</returns>
    /// <exception cref="FaultException{ServiceFault}">
    /// The operation is not currently available.
    /// </exception>
    public FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId)
    {
        throw ServiceFaultFactory.CreateUnexpectedFault(CorrelationContext.GetOrCreate());
    }

    /// <summary>
    /// Returns pending incoming and outgoing friend requests for the caller.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <returns>The pending friend requests.</returns>
    /// <exception cref="FaultException{ServiceFault}">
    /// The operation is not currently available.
    /// </exception>
    public FriendRequest[] GetPendingFriendRequests(string sessionToken)
    {
        throw ServiceFaultFactory.CreateUnexpectedFault(CorrelationContext.GetOrCreate());
    }

    /// <summary>
    /// Accepts or declines a pending friend request.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="requestId">The friend request identifier.</param>
    /// <param name="accept"><c>true</c> to accept; <c>false</c> to decline.</param>
    /// <exception cref="FaultException{ServiceFault}">
    /// The operation is not currently available.
    /// </exception>
    public void RespondToFriendRequest(string sessionToken, string requestId, bool accept)
    {
        throw ServiceFaultFactory.CreateUnexpectedFault(CorrelationContext.GetOrCreate());
    }

    /// <summary>
    /// Removes an existing friend relationship.
    /// </summary>
    /// <param name="sessionToken">The opaque session token identifying the caller.</param>
    /// <param name="friendAccountId">The friend's account identifier.</param>
    /// <exception cref="FaultException{ServiceFault}">
    /// The operation is not currently available.
    /// </exception>
    public void RemoveFriend(string sessionToken, long friendAccountId)
    {
        throw ServiceFaultFactory.CreateUnexpectedFault(CorrelationContext.GetOrCreate());
    }

    private static void ThrowFaultIfFailed(FriendshipStatus status)
    {
        switch (status)
        {
            case FriendshipStatus.Success:
                return;

            case FriendshipStatus.MissingSessionToken:
                throw ServiceFaultFactory.CreateSessionFault(SessionFailure.MissingSessionToken);

            case FriendshipStatus.InvalidSession:
                throw ServiceFaultFactory.CreateSessionFault(SessionFailure.InvalidSession);

            case FriendshipStatus.InactiveAccount:
                throw ServiceFaultFactory.CreateSessionFault(SessionFailure.InactiveAccount);

            case FriendshipStatus.InvalidAccountId:
                throw ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.InvalidAccountId);

            case FriendshipStatus.AccountNotFound:
                throw ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.AccountUnavailable);

            case FriendshipStatus.FriendshipNotFound:
                throw ServiceFaultFactory.CreateFriendshipFault(FriendshipFailure.FriendshipNotFound);

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unsupported friendship status.");
        }
    }
}
