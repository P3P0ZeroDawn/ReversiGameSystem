using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ReversiGameSystem.Services;

internal sealed class DuplexChannel<TClient, TContract> : IDisposable
    where TClient : ClientBase<TContract>
    where TContract : class
{
    private const int RequestConcurrencyLimit = 1;
    private const long MaximumMessageBytes = 6291456;
    private const string CorrelationHeader = "CorrelationId";
    private const string DiagnosticsNamespace = "urn:reversi:diagnostics:v1";
    private const int ChannelReceiveTimeoutHours = 1;
    private const int ChannelSendTimeoutSeconds = 5;
    private readonly object _channelLock = new object();
    private readonly SemaphoreSlim _requests = new SemaphoreSlim(
        RequestConcurrencyLimit, RequestConcurrencyLimit);
    private readonly Func<TClient> _createClient;
    private readonly ILogger _logger;
    private TClient? _client;
    private bool _disposed;
    private int _pendingCalls;

    internal DuplexChannel(Func<TClient> createClient, ILogger logger)
    {
        _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal event Action? ConnectionLost;
    internal event Action? ChannelCreated;

    internal static NetTcpBinding CreateOverrideBinding()
    {
        return new NetTcpBinding(SecurityMode.None)
        {
            ReceiveTimeout = TimeSpan.FromHours(ChannelReceiveTimeoutHours),
            SendTimeout = TimeSpan.FromSeconds(ChannelSendTimeoutSeconds),
            MaxReceivedMessageSize = MaximumMessageBytes
        };
    }

    internal async Task SendServiceCall(
        Func<TClient, Task> operation, CancellationToken cancellation)
    {
        lock (_channelLock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DuplexChannel<TClient, TContract>));
            }
            _pendingCalls++;
        }

        bool acquired = false;
        TClient? client = null;
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            await _requests.WaitAsync(cancellation).ConfigureAwait(false);
            acquired = true;
            lock (_channelLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DuplexChannel<TClient, TContract>));
                }
                if (_client == null
                    || _client.State is CommunicationState.Faulted or CommunicationState.Closed)
                {
                    if (_client != null)
                    {
                        _client.InnerChannel.Faulted -= OnChannelFaulted;
                        _client.Abort();
                    }
                    _client = _createClient();
                    _client.Endpoint.EndpointBehaviors.Add(new CallbackErrorBehavior());
                    _client.InnerChannel.Faulted += OnChannelFaulted;
                    ChannelCreated?.Invoke();
                }
                client = _client;
            }
            using CancellationTokenRegistration registration = cancellation.Register(client.Abort);
            cancellation.ThrowIfCancellationRequested();
            Task request;
            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                    CorrelationHeader, DiagnosticsNamespace, correlationId));
                request = operation(client);
            }
            await request.ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
        }
        catch (CommunicationException exception) when (exception is not FaultException)
        {
            client?.Abort();
            cancellation.ThrowIfCancellationRequested();
            LogConnectionFailure(exception, correlationId);
            throw;
        }
        catch (TimeoutException exception)
        {
            client?.Abort();
            cancellation.ThrowIfCancellationRequested();
            LogConnectionFailure(exception, correlationId);
            throw;
        }
        finally
        {
            if (acquired)
            {
                _requests.Release();
            }
            lock (_channelLock)
            {
                _pendingCalls--;
                if (_disposed && _pendingCalls == 0)
                {
                    _requests.Dispose();
                }
            }
        }
    }

    private void LogConnectionFailure(Exception failure, string correlationId)
    {
        _logger.LogWarning(failure,
            "The service channel failed. CorrelationId: {CorrelationId}", correlationId);
        ConnectionLost?.Invoke();
    }

    private void OnChannelFaulted(object? sender, EventArgs arguments)
    {
        if (!_disposed)
        {
            ConnectionLost?.Invoke();
        }
    }

    public void Dispose()
    {
        TClient? client;
        lock (_channelLock)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            client = _client;
            if (client != null)
            {
                client.InnerChannel.Faulted -= OnChannelFaulted;
            }
            if (_pendingCalls == 0)
            {
                _requests.Dispose();
            }
        }
        client?.Abort();
    }
}
