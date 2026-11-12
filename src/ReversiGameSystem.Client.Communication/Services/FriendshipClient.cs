using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal sealed class FriendshipClient : IDisposable
{
    private const int RequestConcurrencyLimit = 1;
    private readonly ILogger<FriendshipClient> _logger;
    private readonly Func<InstanceContext, Proxy.FriendshipServiceClient> _createClient;
    private readonly SemaphoreSlim _requests = new SemaphoreSlim(
        RequestConcurrencyLimit,
        RequestConcurrencyLimit);
    private readonly object _sync = new object();
    private readonly FriendshipSnapshot _snapshot = new FriendshipSnapshot();
    private Proxy.FriendshipServiceClient? _client;
    private string? _token;
    private int _generation;
    private bool _connected;
    private bool _disposed;

    internal FriendshipClient(ILogger<FriendshipClient> logger)
        : this(logger, context => ServerConnectionFactory.CreateFriendshipClient(context))
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

    internal bool IsConnected
    {
        get
        {
            lock (_sync)
            {
                return _connected && _client?.State == CommunicationState.Opened;
            }
        }
    }

    internal FriendSummary[] GetSnapshot()
    {
        return _snapshot.Read();
    }

    internal async Task<FriendshipResult> GetFriends(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new FriendshipResult(
                FriendshipStatus.InvalidSession, Array.Empty<FriendSummary>());
        }
        await _requests.WaitAsync().ConfigureAwait(false);
        try
        {
            FriendSummary[] friends = await Refresh(token).ConfigureAwait(false);
            return new FriendshipResult(FriendshipStatus.Success, friends);
        }
        catch (FaultException<Proxy.SessionFault>)
        {
            return new FriendshipResult(
                FriendshipStatus.InvalidSession, Array.Empty<FriendSummary>());
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            return new FriendshipResult(
                FriendshipStatus.ServiceUnavailable, Array.Empty<FriendSummary>());
        }
        finally
        {
            _requests.Release();
        }
    }

    private async Task<FriendSummary[]> Refresh(string token)
    {
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            Proxy.FriendshipServiceClient client = PrepareClient(token);
            _snapshot.BeginRefresh();
            if (!IsConnected)
            {
                await Invoke(client, correlationId, async () =>
                {
                    await client.OpenFriendshipChannelAsync(token).ConfigureAwait(false);
                    return true;
                }).ConfigureAwait(false);
            }
            Proxy.FriendSummary[] friends = await Invoke(client, correlationId,
                () => client.GetFriendsAsync(token)).ConfigureAwait(false);
            lock (_sync)
            {
                if (!ReferenceEquals(_client, client) || client.State != CommunicationState.Opened)
                {
                    throw new CommunicationException("The friendship channel was lost during refresh.");
                }
                _snapshot.CompleteRefresh(friends);
                _connected = true;
            }
            FriendsChanged?.Invoke();
            return _snapshot.Read();
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            _snapshot.CancelRefresh();
            _logger.LogWarning(FailureDiagnostics.ForLogging(exception),
                "The friendship presence request failed. CorrelationId: {CorrelationId}", correlationId);
            lock (_sync)
            {
                ReleaseChannel();
            }
            ConnectionLost?.Invoke();
            throw;
        }
    }

    private static Task<TResult> Invoke<TResult>(Proxy.FriendshipServiceClient client,
        string correlationId, Func<Task<TResult>> operation)
    {
        using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
        {
            OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                "CorrelationId", "urn:reversi:diagnostics:v1", correlationId));
            return operation();
        }
    }

    internal void Close()
    {
        Close(false);
    }

    private void Close(bool dispose)
    {
        _requests.Wait();
        try
        {
            lock (_sync)
            {
                _disposed |= dispose;
                ReleaseChannel();
                _snapshot.Clear();
            }
        }
        finally
        {
            _requests.Release();
        }
    }

    public void Dispose()
    {
        Close(true);
    }

    private Proxy.FriendshipServiceClient PrepareClient(string token)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FriendshipClient));
            }
            if (_token != token || _client?.State != CommunicationState.Opened)
            {
                bool changedSession = _token != token;
                ReleaseChannel();
                if (changedSession)
                {
                    _snapshot.Clear();
                }
                int generation = _generation;
                FriendshipCallback callback = new FriendshipCallback(
                    (id, change) => OnChanged(generation, id, change));
                _client = _createClient(new InstanceContext(callback));
                _client.InnerChannel.Faulted += OnChannelLost;
                _client.InnerChannel.Closed += OnChannelLost;
                _token = token;
            }
            return _client ?? throw new InvalidOperationException(
                "The friendship channel was not created.");
        }
    }

    private void OnChanged(int generation, long accountId, FriendChange change)
    {
        lock (_sync)
        {
            if (generation != _generation)
            {
                return;
            }
            _snapshot.Change(accountId, change);
        }
        FriendsChanged?.Invoke();
    }

    private void OnChannelLost(object sender, EventArgs args)
    {
        lock (_sync)
        {
            if (_client == null || !ReferenceEquals(sender, _client.InnerChannel))
            {
                return;
            }
            ReleaseChannel();
        }
        ConnectionLost?.Invoke();
    }

    private void ReleaseChannel()
    {
        _generation++;
        _connected = false;
        _token = null;
        if (_client != null)
        {
            _client.InnerChannel.Faulted -= OnChannelLost;
            _client.InnerChannel.Closed -= OnChannelLost;
            _client.Abort();
            _client = null;
        }
    }
}
