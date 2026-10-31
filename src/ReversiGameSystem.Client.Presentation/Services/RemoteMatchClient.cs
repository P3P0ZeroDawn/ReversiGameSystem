using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

/// <summary>Consumes confirmed lobby, board and chat state through the existing proxy.</summary>
[CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
internal sealed class RemoteMatchClient : IDisposable, Proxy.IMatchLobbyServiceCallback,
    Proxy.IGameplayMatchServiceCallback, Proxy.IMatchChatServiceCallback
{
    private readonly string _token;
    private readonly ILogger<RemoteMatchClient> _logger;
    private readonly SemaphoreSlim _requests = new(1, 1);
    private Proxy.MatchLobbyServiceClient _lobby;
    private Proxy.GameplayMatchServiceClient _gameplay;
    private Proxy.MatchChatServiceClient _chat;
    private readonly MatchEndpoints _endpoints;
    private bool _lobbyOpen;
    private readonly HashSet<long> _gameplayOpen = new();
    private bool _disposed;
    private readonly object _sync = new();
    private readonly Dictionary<long, Proxy.MatchStateSnapshot> _states = new();
    private readonly Dictionary<long, SortedDictionary<int, Proxy.MatchChatEntry>> _messages = new();

    internal RemoteMatchClient(string token, ILogger<RemoteMatchClient> logger)
        : this(token, ServerConnectionFactory.CreateMatchEndpoints(), logger)
    {
    }

    internal RemoteMatchClient(
        string token,
        MatchEndpoints endpoints,
        ILogger<RemoteMatchClient> logger)
    {
        _token = token;
        _logger = logger;
        _endpoints = endpoints;
        _lobby = CreateLobby();
        _gameplay = CreateGameplay();
        _chat = CreateChat();
    }

    private Proxy.MatchLobbyServiceClient CreateLobby() =>
        ServerConnectionFactory.CreateLobbyClient(new InstanceContext(this));

    private Proxy.GameplayMatchServiceClient CreateGameplay() =>
        ServerConnectionFactory.CreateGameplayClient(new InstanceContext(this));

    private Proxy.MatchChatServiceClient CreateChat() =>
        ServerConnectionFactory.CreateChatClient(new InstanceContext(this));

    private Proxy.MatchLobbyServiceClient PrepareLobby()
    {
        if (_lobby.State is CommunicationState.Faulted or CommunicationState.Closed)
        {
            _lobby.Abort();
            _lobby = CreateLobby();
            _lobbyOpen = false;
        }
        return _lobby;
    }

    private Proxy.GameplayMatchServiceClient PrepareGameplay()
    {
        if (_gameplay.State is CommunicationState.Faulted or CommunicationState.Closed)
        {
            _gameplay.Abort();
            _gameplay = CreateGameplay();
            _gameplayOpen.Clear();
        }
        return _gameplay;
    }

    private Proxy.MatchChatServiceClient PrepareChat()
    {
        if (_chat.State is CommunicationState.Faulted or CommunicationState.Closed)
        {
            _chat.Abort();
            _chat = CreateChat();
        }
        return _chat;
    }

    internal event Action<Proxy.MatchStateSnapshot>? StateChanged;
    internal event Action<long>? LobbyClosed;
    internal event Action<long>? MessagesChanged;
    internal event Action<Proxy.ChatMuteNotification>? Muted;

    private static NetTcpBinding CreateBinding()
    {
        NetTcpBinding binding = new NetTcpBinding(SecurityMode.Transport)
        {
            ReceiveTimeout = TimeSpan.FromHours(1),
            SendTimeout = TimeSpan.FromSeconds(5),
            MaxReceivedMessageSize = 6291456
        };
        binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
        binding.Security.Transport.ProtectionLevel =
            System.Net.Security.ProtectionLevel.EncryptAndSign;
        return binding;
    }

    internal Task<Proxy.LobbySnapshot> CreateAsync(int seconds) =>
        RequestAsync(() => PrepareLobby(), async () =>
    {
        await OpenLobbyAsync();
        return await _lobby.CreateMatchAsync(_token, new Proxy.MatchConfiguration
        {
            TurnDurationSeconds = seconds, OpponentKind = Proxy.OpponentKind.Human,
            CountsTowardStatistics = false
        });
    });

    internal Task<Proxy.LobbySnapshot> InviteAsync(long friendAccountId, int seconds) =>
        RequestAsync(() => PrepareLobby(), async () =>
    {
        await OpenLobbyAsync();
        return await _lobby.InviteToMatchAsync(_token, friendAccountId, new Proxy.MatchConfiguration
        {
            TurnDurationSeconds = seconds, OpponentKind = Proxy.OpponentKind.Human,
            CountsTowardStatistics = false
        });
    });

    internal Task<Proxy.AvailableMatch[]> ListAsync() =>
        RequestAsync(() => PrepareLobby(), async () =>
    {
        await OpenLobbyAsync();
        return await _lobby.GetAvailableMatchesAsync(_token);
    });

    internal Task<Proxy.LobbySnapshot> JoinAsync(string code) =>
        RequestAsync(() => PrepareLobby(), async () =>
    {
        await OpenLobbyAsync();
        return await _lobby.JoinMatchAsync(_token, code);
    });

    internal Task<bool> CancelAsync(long id) =>
        RequestAsync(() => PrepareLobby(), async () =>
    {
        await _lobby.CancelMatchAsync(_token, id);
        return true;
    });

    private async Task OpenLobbyAsync()
    {
        if (!_lobbyOpen)
        {
            await _lobby.OpenLobbyChannelAsync(_token);
            _lobbyOpen = true;
        }
    }

    private async Task OpenGameplayAsync(long id)
    {
        if (!_gameplayOpen.Contains(id))
        {
            await _gameplay.OpenMatchChannelAsync(_token, id);
            _gameplayOpen.Add(id);
        }
    }

    internal Task<Proxy.MatchInformation> LoadInformationAsync(long id) =>
        RequestAsync(() => PrepareGameplay(), async () =>
    {
        await OpenGameplayAsync(id);
        return await _gameplay.GetMatchInformationAsync(_token, id);
    });

    internal async Task<Proxy.MatchStateSnapshot> LoadStateAsync(long id)
    {
        Proxy.MatchStateSnapshot state = await RequestAsync(() => PrepareGameplay(), async () =>
        {
            await OpenGameplayAsync(id);
            return await _gameplay.GetMatchStateAsync(_token, id);
        });
        OnMatchStateChanged(state);
        lock (_sync)
        {
            return _states[id];
        }
    }

    internal Task<bool> OpenChatAsync(long id) => RequestAsync(() => PrepareChat(), async () =>
    {
        await _chat.OpenChatChannelAsync(_token, id);
        return true;
    });

    internal async Task RefreshChatAsync(long id)
    {
        Proxy.MatchChatEntry[] messages = await RequestAsync(() => PrepareChat(),
            () => _chat.GetChatHistoryAsync(_token, id));
        foreach (Proxy.MatchChatEntry message in messages)
        {
            OnChatMessageReceived(message);
        }
    }

    internal async Task SendAsync(long id, string content)
    {
        Proxy.MatchChatEntry message = await RequestAsync(() => PrepareChat(),
            () => _chat.SendChatMessageAsync(_token, id, content.Trim()));
        OnChatMessageReceived(message);
    }

    internal Proxy.MatchChatEntry[] ReadMessages(long id)
    {
        lock (_sync)
        {
            return _messages.TryGetValue(id, out SortedDictionary<int, Proxy.MatchChatEntry> messages)
                ? messages.Values.ToArray() : Array.Empty<Proxy.MatchChatEntry>();
        }
    }

    internal bool TryReadState(long id, out Proxy.MatchStateSnapshot? state)
    {
        lock (_sync)
        {
            return _states.TryGetValue(id, out state);
        }
    }

    private async Task<T> RequestAsync<T, TContract>(
        Func<ClientBase<TContract>> getClient, Func<Task<T>> action)
        where TContract : class
    {
        await _requests.WaitAsync().ConfigureAwait(false);
        ClientBase<TContract>? client = null;
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(RemoteMatchClient));
                }
                client = getClient();
            }
            Task<T> request;
            using (OperationContextScope scope = new(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                    "CorrelationId", "urn:reversi:diagnostics:v1", correlationId));
                request = action();
            }
            return await request.ConfigureAwait(false);
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            if (exception is CommunicationException and not FaultException or TimeoutException)
            {
                client?.Abort();
            }
            _logger.LogWarning(FailureDiagnostics.ForLogging(exception),
                "The match request failed. CorrelationId: {CorrelationId}", correlationId);
            throw;
        }
        finally
        {
            _requests.Release();
        }
    }

    public void OnMatchStarted(Proxy.MatchStateSnapshot state) => OnMatchStateChanged(state);
    public void OnLobbyClosed(long id) => LobbyClosed?.Invoke(id);
    public void OnMatchStateChanged(Proxy.MatchStateSnapshot state)
    {
        lock (_sync)
        {
            if (_states.TryGetValue(state.MatchId, out Proxy.MatchStateSnapshot previous)
                && previous.MatchStateVersion >= state.MatchStateVersion)
        {
            return;
        }
            _states[state.MatchId] = state;
        }
        StateChanged?.Invoke(state);
    }
    public void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected)
    {
        // The initial-state view does not expose connection indicators yet.
    }
    public void OnChatMessageReceived(Proxy.MatchChatEntry message)
    {
        lock (_sync)
        {
            if (!_messages.TryGetValue(message.MatchId,
                out SortedDictionary<int, Proxy.MatchChatEntry> messages))
            {
                messages = new();
                _messages.Add(message.MatchId, messages);
            }
            if (messages.ContainsKey(message.MessageNumber))
            {
                return;
            }
            messages.Add(message.MessageNumber, message);
        }
        MessagesChanged?.Invoke(message.MatchId);
    }
    public void OnPlayerMuted(Proxy.ChatMuteNotification mute) => Muted?.Invoke(mute);

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _lobby.Abort();
            _gameplay.Abort();
            _chat.Abort();
        }
    }
}

/// <summary>Optional endpoint overrides for a coherent remote match service deployment.</summary>
internal sealed class MatchEndpoints
{
    internal string? Lobby { get; set; }
    internal string? Gameplay { get; set; }
    internal string? Chat { get; set; }
}
