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
    private readonly ILogger<FriendshipClient> _logger;
    private readonly Func<InstanceContext, Proxy.FriendshipServiceClient> _createClient;
    private readonly SemaphoreSlim _requests = new(1, 1);
    private readonly object _sync = new();
    private readonly FriendshipSnapshot _snapshot = new();
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

    internal Proxy.FriendSummary[] GetSnapshot() => _snapshot.Read();

    internal async Task<Proxy.FriendSummary[]> GetFriendsAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("An authenticated session is required.", nameof(token));
        }
        await _requests.WaitAsync().ConfigureAwait(false);
        try
        {
            return await RefreshAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _requests.Release();
        }
    }

    private async Task<Proxy.FriendSummary[]> RefreshAsync(string token)
    {
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            Proxy.FriendshipServiceClient client = PrepareClient(token);
            _snapshot.BeginRefresh();
            if (!IsConnected)
            {
                await InvokeAsync(client, correlationId, async () =>
                {
                    await client.OpenFriendshipChannelAsync(token).ConfigureAwait(false);
                    return true;
                }).ConfigureAwait(false);
            }
            Proxy.FriendSummary[] friends = await InvokeAsync(client, correlationId,
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

    private static Task<TResult> InvokeAsync<TResult>(Proxy.FriendshipServiceClient client,
        string correlationId, Func<Task<TResult>> operation)
    {
        using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
        {
            OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                "CorrelationId", "urn:reversi:diagnostics:v1", correlationId));
            return operation();
        }
    }

    internal void Close() => Close(false);

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

    public void Dispose() => Close(true);

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
                FriendshipCallback callback = new((id, change) => OnChanged(generation, id, change));
                _client = _createClient(new InstanceContext(callback));
                _client.InnerChannel.Faulted += OnChannelLost;
                _client.InnerChannel.Closed += OnChannelLost;
                _token = token;
            }
            return _client!;
        }
    }

    private void OnChanged(int generation, long accountId, Proxy.FriendChange change)
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
