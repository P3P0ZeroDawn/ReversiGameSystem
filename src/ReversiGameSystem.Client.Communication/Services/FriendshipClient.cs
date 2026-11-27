using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class FriendshipClient : IFriendshipClient, IDisposable
{
    private const int RequestConcurrencyLimit = 1;
    private const string CorrelationHeaderName = "CorrelationId";
    private const string CorrelationHeaderNamespace = "urn:reversi:diagnostics:v1";
    private readonly ILogger<FriendshipClient> _logger;
    private readonly Func<InstanceContext, Proxy.FriendshipServiceClient> _createClient;
    private readonly SemaphoreSlim _operationSemaphore = new SemaphoreSlim(
        RequestConcurrencyLimit,
        RequestConcurrencyLimit);
    private readonly object _friendshipChannelLock = new object();
    private readonly FriendshipSnapshot _friendSnapshot = new FriendshipSnapshot();
    private Proxy.FriendshipServiceClient? _serviceClient;
    private string? _sessionToken;
    private int _callbackGeneration;
    private bool _channelRegistered;
    private bool _disposed;
    private PlayerAccount[] _searchPlayers = Array.Empty<PlayerAccount>();
    private PendingFriendRequest[] _pendingRequests = Array.Empty<PendingFriendRequest>();

    internal FriendshipClient(ILogger<FriendshipClient> logger)
        : this(logger, ServerConnectionFactory.CreateFriendshipClient)
    {
    }

    internal FriendshipClient(ILogger<FriendshipClient> logger,
        Func<InstanceContext, Proxy.FriendshipServiceClient> createClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
    }

    internal event Action? FriendsChanged;
    internal event Action? ConnectionLost;
    internal event Action? RequestsChanged;
    internal event Action? MembershipChanged;

    public bool IsConnected
    {
        get
        {
            lock (_friendshipChannelLock)
            {
                return _channelRegistered && _serviceClient?.State == CommunicationState.Opened;
            }
        }
    }

    public FriendSummary[] ReadFriends()
    {
        return _friendSnapshot.Read();
    }

    public async Task<FriendshipStatus> LoadFriends(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return FriendshipStatus.InvalidSession;
        }
        string correlationId = Guid.NewGuid().ToString("D");
        await _operationSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await RefreshFriends(sessionToken, correlationId).ConfigureAwait(false);
            return FriendshipStatus.Success;
        }
        catch (FaultException<Proxy.SessionFault> fault) when (
            fault.Detail.Reason is Proxy.SessionFailure.MissingSessionToken
                or Proxy.SessionFailure.InvalidSession or Proxy.SessionFailure.InactiveAccount)
        {
            lock (_friendshipChannelLock)
            {
                ReleaseChannel();
                ClearConfirmedData();
            }
            return MapSessionFailure(fault.Detail.Reason);
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            _logger.LogWarning(exception,
                "The friendship presence request failed. CorrelationId: {CorrelationId}",
                correlationId);
            return FriendshipStatus.ServiceUnavailable;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public PlayerAccount[] ReadPlayers()
    {
        lock (_friendshipChannelLock)
        {
            return _searchPlayers.ToArray();
        }
    }

    public PendingFriendRequest[] ReadPendingRequests()
    {
        lock (_friendshipChannelLock)
        {
            return _pendingRequests.ToArray();
        }
    }

    public Task<FriendshipStatus> FindPlayers(string sessionToken, string searchText)
    {
        async Task SearchPlayers(Proxy.FriendshipServiceClient client)
        {
            Proxy.PlayerSummary[] players = await client.FindPlayersAsync(sessionToken, searchText)
                .ConfigureAwait(false);
            PlayerAccount[] mapped = players.Select(player => MapPlayer(
                player.Account, player.HasProfileImage)).ToArray();
            lock (_friendshipChannelLock)
            {
                if (!ReferenceEquals(client, _serviceClient))
                {
                    throw new CommunicationException(
                        "The friendship channel changed during search.");
                }
                _searchPlayers = mapped;
            }
        }
        return SendFriendshipOperation(sessionToken, SearchPlayers, _ => false);
    }

    public Task<FriendshipStatus> LoadPendingRequests(string sessionToken)
    {
        async Task ReadRequests(Proxy.FriendshipServiceClient client)
        {
            Proxy.FriendRequest[] requests = await client.GetPendingFriendRequestsAsync(
                sessionToken).ConfigureAwait(false);
            PendingFriendRequest[] mapped = requests.Select(MapRequest).ToArray();
            lock (_friendshipChannelLock)
            {
                if (!ReferenceEquals(client, _serviceClient))
                {
                    throw new CommunicationException(
                        "The friendship channel changed during refresh.");
                }
                _pendingRequests = mapped;
            }
        }
        return SendFriendshipOperation(sessionToken, ReadRequests, _ => false);
    }

    public async Task<FriendshipStatus> SendFriendRequest(
        string sessionToken, long recipientAccountId)
    {
        async Task SendRequest(Proxy.FriendshipServiceClient client)
        {
            Proxy.FriendRequest request = await client.SendFriendRequestAsync(
                sessionToken, recipientAccountId).ConfigureAwait(false);
            if (request == null || string.IsNullOrWhiteSpace(request.RequestId)
                || request.Recipient == null || request.Recipient.AccountId != recipientAccountId)
            {
                throw new InvalidOperationException(
                    "The server returned an invalid friend request.");
            }
        }
        FriendshipStatus status = await SendFriendshipOperation(sessionToken, SendRequest,
            failure => failure is Proxy.FriendshipFailure.InvalidAccountId
                or Proxy.FriendshipFailure.CannotSendRequestToSelf
                or Proxy.FriendshipFailure.AccountUnavailable
                or Proxy.FriendshipFailure.FriendshipAlreadyExists
                or Proxy.FriendshipFailure.RequestAlreadyPending
                or Proxy.FriendshipFailure.InactiveAccount)
            .ConfigureAwait(false);
        if (status == FriendshipStatus.Success)
        {
            RequestsChanged?.Invoke();
        }
        return status;
    }

    public async Task<FriendshipStatus> RespondToFriendRequest(
        string sessionToken, string requestId, bool accept)
    {
        async Task RespondToRequest(Proxy.FriendshipServiceClient client)
        {
            await client.RespondToFriendRequestAsync(sessionToken, requestId, accept)
                .ConfigureAwait(false);
        }
        FriendshipStatus status = await SendFriendshipOperation(sessionToken, RespondToRequest,
            failure => failure is Proxy.FriendshipFailure.MissingRequestId
                or Proxy.FriendshipFailure.RequestUnavailable
                or Proxy.FriendshipFailure.UnauthorizedRequest
                or Proxy.FriendshipFailure.AccountUnavailable
                or Proxy.FriendshipFailure.InactiveAccount)
            .ConfigureAwait(false);
        if (status == FriendshipStatus.Success)
        {
            RequestsChanged?.Invoke();
            if (accept)
            {
                MembershipChanged?.Invoke();
            }
        }
        return status;
    }

    public async Task<FriendshipStatus> RemoveFriend(string sessionToken, long friendAccountId)
    {
        async Task RemoveRelationship(Proxy.FriendshipServiceClient client)
        {
            await client.RemoveFriendAsync(sessionToken, friendAccountId).ConfigureAwait(false);
        }
        FriendshipStatus status = await SendFriendshipOperation(sessionToken, RemoveRelationship,
            failure => failure is Proxy.FriendshipFailure.InvalidAccountId
                or Proxy.FriendshipFailure.FriendshipNotFound
                or Proxy.FriendshipFailure.InactiveAccount)
            .ConfigureAwait(false);
        if (status == FriendshipStatus.Success)
        {
            lock (_friendshipChannelLock)
            {
                if (_sessionToken == sessionToken)
                {
                    _friendSnapshot.Change(friendAccountId, FriendChange.Removed);
                }
            }
            FriendsChanged?.Invoke();
        }
        return status;
    }

    private async Task<FriendshipStatus> SendFriendshipOperation(string sessionToken,
        Func<Proxy.FriendshipServiceClient, Task> operation,
        Func<Proxy.FriendshipFailure, bool> canMapFailure)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return FriendshipStatus.InvalidSession;
        }
        string correlationId = Guid.NewGuid().ToString("D");
        await _operationSemaphore.WaitAsync().ConfigureAwait(false);
        FriendshipStatus status;
        try
        {
            Proxy.FriendshipServiceClient client = PrepareClient(sessionToken);
            if (!IsConnected)
            {
                await StartCorrelatedRequest(client, correlationId,
                    () => client.OpenFriendshipChannelAsync(sessionToken)).ConfigureAwait(false);
                lock (_friendshipChannelLock)
                {
                    _channelRegistered = ReferenceEquals(client, _serviceClient);
                }
            }
            await StartCorrelatedRequest(client, correlationId, () => operation(client))
                .ConfigureAwait(false);
            status = FriendshipStatus.Success;
        }
        catch (FaultException<Proxy.FriendshipFault> fault) when (
            canMapFailure(fault.Detail.Reason))
        {
            status = MapFriendshipFailure(fault.Detail.Reason);
        }
        catch (FaultException<Proxy.SessionFault> fault) when (
            fault.Detail.Reason is Proxy.SessionFailure.MissingSessionToken
                or Proxy.SessionFailure.InvalidSession or Proxy.SessionFailure.InactiveAccount)
        {
            lock (_friendshipChannelLock)
            {
                ReleaseChannel();
                ClearConfirmedData();
            }
            status = MapSessionFailure(fault.Detail.Reason);
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            lock (_friendshipChannelLock)
            {
                ReleaseChannel();
            }
            _logger.LogWarning(exception,
                "The friendship operation failed. CorrelationId: {CorrelationId}", correlationId);
            ConnectionLost?.Invoke();
            status = FriendshipStatus.ServiceUnavailable;
        }
        finally
        {
            _operationSemaphore.Release();
        }
        return status;
    }

    private static Task StartCorrelatedRequest(Proxy.FriendshipServiceClient client,
        string correlationId, Func<Task> operation)
    {
        using OperationContextScope scope = new OperationContextScope(client.InnerChannel);
        OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
            CorrelationHeaderName, CorrelationHeaderNamespace, correlationId));
        return operation();
    }

    private void OnRequestsChanged(int generation)
    {
        lock (_friendshipChannelLock)
        {
            if (generation != _callbackGeneration)
            {
                return;
            }
        }
        RequestsChanged?.Invoke();
    }

    private void OnRequestResponded(int generation, bool accepted)
    {
        lock (_friendshipChannelLock)
        {
            if (generation != _callbackGeneration)
            {
                return;
            }
        }
        RequestsChanged?.Invoke();
        if (accepted)
        {
            MembershipChanged?.Invoke();
        }
    }

    private void ClearConfirmedData()
    {
        _friendSnapshot.Clear();
        _searchPlayers = Array.Empty<PlayerAccount>();
        _pendingRequests = Array.Empty<PendingFriendRequest>();
    }

    private static PlayerAccount MapPlayer(Proxy.AccountReference account, bool hasProfileImage)
    {
        if (account == null || account.AccountId <= 0
            || string.IsNullOrWhiteSpace(account.Username))
        {
            throw new InvalidOperationException("The server returned an invalid player identity.");
        }
        return new PlayerAccount(account.AccountId, account.Username, hasProfileImage);
    }

    private static PendingFriendRequest MapRequest(Proxy.FriendRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RequestId))
        {
            throw new InvalidOperationException("The server returned an invalid pending request.");
        }
        return new PendingFriendRequest(request.RequestId,
            MapPlayer(request.Sender, false), MapPlayer(request.Recipient, false),
            request.CreatedAtUtc);
    }

    private static FriendshipStatus MapSessionFailure(Proxy.SessionFailure failure)
    {
        FriendshipStatus status = failure switch
        {
            Proxy.SessionFailure.InvalidSession => FriendshipStatus.InvalidSession,
            Proxy.SessionFailure.MissingSessionToken => FriendshipStatus.MissingSessionToken,
            Proxy.SessionFailure.InactiveAccount => FriendshipStatus.InactiveAccount,
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        return status;
    }

    private static FriendshipStatus MapFriendshipFailure(Proxy.FriendshipFailure failure)
    {
        FriendshipStatus status = failure switch
        {
            Proxy.FriendshipFailure.InvalidAccountId => FriendshipStatus.InvalidAccountId,
            Proxy.FriendshipFailure.CannotSendRequestToSelf =>
                FriendshipStatus.CannotSendRequestToSelf,
            Proxy.FriendshipFailure.AccountUnavailable => FriendshipStatus.AccountUnavailable,
            Proxy.FriendshipFailure.FriendshipAlreadyExists =>
                FriendshipStatus.FriendshipAlreadyExists,
            Proxy.FriendshipFailure.RequestAlreadyPending => FriendshipStatus.RequestAlreadyPending,
            Proxy.FriendshipFailure.MissingRequestId => FriendshipStatus.MissingRequestId,
            Proxy.FriendshipFailure.RequestUnavailable => FriendshipStatus.RequestUnavailable,
            Proxy.FriendshipFailure.UnauthorizedRequest => FriendshipStatus.UnauthorizedRequest,
            Proxy.FriendshipFailure.FriendshipNotFound => FriendshipStatus.FriendshipNotFound,
            Proxy.FriendshipFailure.InactiveAccount => FriendshipStatus.InactiveAccount,
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        return status;
    }

    private async Task<FriendSummary[]> RefreshFriends(string sessionToken, string correlationId)
    {
        try
        {
            Proxy.FriendshipServiceClient client = PrepareClient(sessionToken);
            _friendSnapshot.BeginRefresh();
            if (!IsConnected)
            {
                await StartCorrelatedRequest(client, correlationId,
                    () => client.OpenFriendshipChannelAsync(sessionToken)).ConfigureAwait(false);
            }
            Proxy.FriendSummary[] friends = await StartCorrelatedRequest(client, correlationId,
                () => client.GetFriendsAsync(sessionToken)).ConfigureAwait(false);
            lock (_friendshipChannelLock)
            {
                if (!ReferenceEquals(_serviceClient, client)
                    || client.State != CommunicationState.Opened)
                {
                    throw new CommunicationException(
                        "The friendship channel was lost during refresh.");
                }
                _friendSnapshot.CompleteRefresh(friends);
                _channelRegistered = true;
            }
            FriendsChanged?.Invoke();
            return _friendSnapshot.Read();
        }
        catch (Exception exception) when (RemoteCall.IsConnectionFailure(exception))
        {
            _friendSnapshot.CancelRefresh();
            lock (_friendshipChannelLock)
            {
                ReleaseChannel();
            }
            ConnectionLost?.Invoke();
            throw;
        }
    }

    private static Task<TResult> StartCorrelatedRequest<TResult>(
        Proxy.FriendshipServiceClient client,
        string correlationId, Func<Task<TResult>> operation)
    {
        using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
        {
            OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                CorrelationHeaderName, CorrelationHeaderNamespace, correlationId));
            return operation();
        }
    }

    internal void Close()
    {
        Close(false);
    }

    private void Close(bool dispose)
    {
        _operationSemaphore.Wait();
        try
        {
            lock (_friendshipChannelLock)
            {
                _disposed |= dispose;
                ReleaseChannel();
                _sessionToken = null;
                ClearConfirmedData();
            }
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    public void Dispose()
    {
        Close(true);
    }

    private Proxy.FriendshipServiceClient PrepareClient(string sessionToken)
    {
        lock (_friendshipChannelLock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FriendshipClient));
            }
            if (_sessionToken != sessionToken || _serviceClient?.State != CommunicationState.Opened)
            {
                bool changedSession = _sessionToken != sessionToken;
                ReleaseChannel();
                if (changedSession)
                {
                    ClearConfirmedData();
                }
                int generation = _callbackGeneration;
                FriendshipCallback callback = new FriendshipCallback(
                    (accountId, change) => OnFriendChanged(generation, accountId, change),
                    () => OnRequestsChanged(generation),
                    accepted => OnRequestResponded(generation, accepted));
                _serviceClient = _createClient(new InstanceContext(callback));
                _serviceClient.Endpoint.EndpointBehaviors.Add(new CallbackErrorBehavior());
                _serviceClient.InnerChannel.Faulted += OnChannelLost;
                _serviceClient.InnerChannel.Closed += OnChannelLost;
                _sessionToken = sessionToken;
            }
            return _serviceClient ?? throw new InvalidOperationException(
                "The friendship channel was not created.");
        }
    }

    private void OnFriendChanged(int generation, long accountId, FriendChange change)
    {
        bool unknownFriend;
        lock (_friendshipChannelLock)
        {
            if (generation != _callbackGeneration)
            {
                return;
            }
            unknownFriend = !_friendSnapshot.Read().Any(friend => friend.AccountId == accountId);
            _friendSnapshot.Change(accountId, change);
        }
        FriendsChanged?.Invoke();
        if (unknownFriend && change != FriendChange.Removed)
        {
            MembershipChanged?.Invoke();
        }
    }

    private void OnChannelLost(object sender, EventArgs args)
    {
        lock (_friendshipChannelLock)
        {
            if (_serviceClient == null || !ReferenceEquals(sender, _serviceClient.InnerChannel))
            {
                return;
            }
            ReleaseChannel();
        }
        ConnectionLost?.Invoke();
    }

    private void ReleaseChannel()
    {
        _callbackGeneration++;
        _channelRegistered = false;
        if (_serviceClient != null)
        {
            _serviceClient.InnerChannel.Faulted -= OnChannelLost;
            _serviceClient.InnerChannel.Closed -= OnChannelLost;
            _serviceClient.Abort();
            _serviceClient = null;
        }
    }
}
