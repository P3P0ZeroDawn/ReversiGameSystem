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
    private const int RequestConcurrencyLimit = 1;
    private static readonly IReadOnlyDictionary<Proxy.MatchFailure, MatchStatus>
        s_matchStatuses = new Dictionary<Proxy.MatchFailure, MatchStatus>
        {
            { Proxy.MatchFailure.InvalidMatchId, MatchStatus.InvalidMatchId },
            { Proxy.MatchFailure.InvalidConfiguration, MatchStatus.InvalidConfiguration },
            { Proxy.MatchFailure.InvalidTurnDuration, MatchStatus.InvalidTurnDuration },
            { Proxy.MatchFailure.MatchNotFound, MatchStatus.MatchNotFound },
            { Proxy.MatchFailure.CannotJoinOwnMatch, MatchStatus.CannotJoinOwnMatch },
            { Proxy.MatchFailure.InvalidMatchCode, MatchStatus.InvalidMatchCode },
            { Proxy.MatchFailure.MatchCodeUnavailable, MatchStatus.MatchCodeUnavailable },
            { Proxy.MatchFailure.Unauthorized, MatchStatus.Unauthorized },
            { Proxy.MatchFailure.MatchNotWaiting, MatchStatus.MatchNotWaiting },
            { Proxy.MatchFailure.MatchNotInProgress, MatchStatus.MatchNotInProgress },
            { Proxy.MatchFailure.MissingPlacement, MatchStatus.MissingPlacement },
            { Proxy.MatchFailure.InvalidMatchStateVersion, MatchStatus.InvalidMatchStateVersion },
            { Proxy.MatchFailure.StaleMatchStateVersion, MatchStatus.StaleMatchStateVersion },
            { Proxy.MatchFailure.NotPlayersTurn, MatchStatus.NotPlayersTurn },
            { Proxy.MatchFailure.InvalidPosition, MatchStatus.InvalidPosition },
            { Proxy.MatchFailure.SpectatingUnavailable, MatchStatus.SpectatingUnavailable },
            { Proxy.MatchFailure.InactiveAccount, MatchStatus.InactiveAccount }
        };
    private readonly string _token;
    private readonly ILogger<RemoteMatchClient> _logger;
    private readonly SemaphoreSlim _requests = new SemaphoreSlim(
        RequestConcurrencyLimit,
        RequestConcurrencyLimit);
    private Proxy.MatchLobbyServiceClient _lobby;
    private Proxy.GameplayMatchServiceClient _gameplay;
    private Proxy.MatchChatServiceClient _chat;
    private readonly MatchEndpoints _endpoints;
    private readonly bool _usesEndpointOverrides;
    private bool _lobbyOpen;
    private readonly HashSet<long> _gameplayOpen = new HashSet<long>();
    private bool _disposed;
    private readonly object _sync = new object();
    private readonly Dictionary<long, MatchStateSnapshot> _states =
        new Dictionary<long, MatchStateSnapshot>();
    private readonly Dictionary<long, SortedDictionary<int, MatchChatMessage>> _messages =
        new Dictionary<long, SortedDictionary<int, MatchChatMessage>>();
    private readonly Dictionary<long, Dictionary<int, ParticipantConnectionState>>
        _participantConnections =
            new Dictionary<long, Dictionary<int, ParticipantConnectionState>>();

    internal RemoteMatchClient(string token, ILogger<RemoteMatchClient> logger)
        : this(token, ServerConnectionFactory.CreateMatchEndpoints(), logger, false)
    {
    }

    internal RemoteMatchClient(
        string token,
        MatchEndpoints endpoints,
        ILogger<RemoteMatchClient> logger)
        : this(token, endpoints, logger, true)
    {
    }

    private RemoteMatchClient(
        string token,
        MatchEndpoints endpoints,
        ILogger<RemoteMatchClient> logger,
        bool usesEndpointOverrides)
    {
        _token = token;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        _usesEndpointOverrides = usesEndpointOverrides;
        _lobby = CreateLobby();
        _gameplay = CreateGameplay();
        _chat = CreateChat();
    }

    private Proxy.MatchLobbyServiceClient CreateLobby()
    {
        Proxy.MatchLobbyServiceClient client;
        if (_usesEndpointOverrides)
        {
            client = new Proxy.MatchLobbyServiceClient(
                new InstanceContext(this),
                CreateOverrideBinding(),
                new EndpointAddress(_endpoints.Lobby));
        }
        else
        {
            client = ServerConnectionFactory.CreateLobbyClient(new InstanceContext(this));
        }

        return client;
    }

    private Proxy.GameplayMatchServiceClient CreateGameplay()
    {
        Proxy.GameplayMatchServiceClient client;
        if (_usesEndpointOverrides)
        {
            client = new Proxy.GameplayMatchServiceClient(
                new InstanceContext(this),
                CreateOverrideBinding(),
                new EndpointAddress(_endpoints.Gameplay));
        }
        else
        {
            client = ServerConnectionFactory.CreateGameplayClient(new InstanceContext(this));
        }

        return client;
    }

    private Proxy.MatchChatServiceClient CreateChat()
    {
        Proxy.MatchChatServiceClient client;
        if (_usesEndpointOverrides)
        {
            client = new Proxy.MatchChatServiceClient(
                new InstanceContext(this),
                CreateOverrideBinding(),
                new EndpointAddress(_endpoints.Chat));
        }
        else
        {
            client = ServerConnectionFactory.CreateChatClient(new InstanceContext(this));
        }

        return client;
    }

    private static NetTcpBinding CreateOverrideBinding()
    {
        NetTcpBinding binding = new NetTcpBinding(SecurityMode.None)
        {
            ReceiveTimeout = TimeSpan.FromHours(1),
            SendTimeout = TimeSpan.FromSeconds(5),
            MaxReceivedMessageSize = 6291456
        };
        return binding;
    }

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

    internal event Action<MatchStateSnapshot>? StateChanged;
    internal event Action<long>? LobbyClosed;
    internal event Action<long>? MessagesChanged;
    internal event Action<long>? ParticipantConnectionChanged;
    internal event Action<RemoteChatMute>? Muted;

    internal Task<MatchResult<LobbySnapshot>> CreateMatch(int seconds)
    {
        return Request(async () =>
        {
            Proxy.LobbySnapshot lobby = await Execute(() => PrepareLobby(), async () =>
            {
                await OpenLobbyChannel();
                return await _lobby.CreateMatchAsync(_token, CreateConfiguration(seconds));
            });
            return MapLobby(lobby);
        });
    }

    internal Task<MatchResult<LobbySnapshot>> InviteToMatch(long friendAccountId, int seconds)
    {
        return Request(async () =>
        {
            Proxy.LobbySnapshot lobby = await Execute(() => PrepareLobby(), async () =>
            {
                await OpenLobbyChannel();
                return await _lobby.InviteToMatchAsync(
                    _token, friendAccountId, CreateConfiguration(seconds));
            });
            return MapLobby(lobby);
        });
    }

    internal Task<MatchResult<RemoteAvailableMatch[]>> LoadAvailableMatches()
    {
        return Request(async () =>
        {
            Proxy.AvailableMatch[] matches = await Execute(() => PrepareLobby(), async () =>
            {
                await OpenLobbyChannel();
                return await _lobby.GetAvailableMatchesAsync(_token);
            });
            return matches.Select(MapAvailableMatch).ToArray();
        });
    }

    internal Task<MatchResult<LobbySnapshot>> JoinMatch(string code)
    {
        return Request(async () =>
        {
            Proxy.LobbySnapshot lobby = await Execute(() => PrepareLobby(), async () =>
            {
                await OpenLobbyChannel();
                return await _lobby.JoinMatchAsync(_token, code);
            });
            return MapLobby(lobby);
        });
    }

    internal Task<MatchStatus> CancelMatch(long id)
    {
        return RequestStatus(async () =>
        {
            await Execute(() => PrepareLobby(), async () =>
            {
                await _lobby.CancelMatchAsync(_token, id);
                return true;
            });
        });
    }

    private async Task OpenLobbyChannel()
    {
        if (!_lobbyOpen)
        {
            await _lobby.OpenLobbyChannelAsync(_token);
            _lobbyOpen = true;
        }
    }

    private async Task OpenGameplayChannel(long id)
    {
        if (!_gameplayOpen.Contains(id))
        {
            await _gameplay.OpenMatchChannelAsync(_token, id);
            _gameplayOpen.Add(id);
        }
    }

    internal Task<MatchResult<RemoteMatchInformation>> LoadRemoteMatchInformation(long id)
    {
        return Request(async () =>
        {
            Proxy.MatchInformation information = await Execute(() => PrepareGameplay(), async () =>
            {
                await OpenGameplayChannel(id);
                return await _gameplay.GetMatchInformationAsync(_token, id);
            });
            return MapInformation(information);
        });
    }

    internal async Task<MatchResult<MatchStateSnapshot>> LoadMatchState(long id)
    {
        MatchResult<MatchStateSnapshot> result = await Request(async () =>
        {
            Proxy.MatchStateSnapshot state = await Execute(() => PrepareGameplay(), async () =>
            {
                await OpenGameplayChannel(id);
                return await _gameplay.GetMatchStateAsync(_token, id);
            });
            return MapState(state);
        });
        if (result.Status == MatchStatus.Success && result.Value != null)
        {
            StoreState(result.Value);
        }
        return result;
    }

    internal async Task<MatchResult<MatchStateSnapshot>> PlacePiece(
        MatchStateSnapshot confirmedState,
        int row,
        int column)
    {
        if (confirmedState == null)
        {
            throw new ArgumentNullException(nameof(confirmedState));
        }

        MatchResult<MatchStateSnapshot> result = await Request(async () =>
        {
            Proxy.MatchStateSnapshot state = await Execute(() => PrepareGameplay(), async () =>
            {
                await OpenGameplayChannel(confirmedState.MatchId);
                return await _gameplay.PlacePieceAsync(_token, new Proxy.PiecePlacement
                {
                    MatchId = confirmedState.MatchId,
                    Row = row,
                    Column = column,
                    ExpectedMatchStateVersion = confirmedState.Version
                });
            });
            return MapState(state);
        });
        if (result.Status == MatchStatus.Success && result.Value != null)
        {
            StoreState(result.Value);
        }
        return result;
    }

    internal Task<MatchStatus> ForfeitMatch(long id)
    {
        return RequestStatus(async () =>
        {
            _ = await Execute(() => PrepareGameplay(), async () =>
            {
                await OpenGameplayChannel(id);
                return await _gameplay.ForfeitMatchAsync(_token, id);
            });
        });
    }

    internal Task<MatchStatus> OpenChatChannel(long id)
    {
        return RequestStatus(async () =>
        {
            await Execute(() => PrepareChat(), async () =>
            {
                await _chat.OpenChatChannelAsync(_token, id);
                return true;
            });
        });
    }

    internal async Task<MatchStatus> RefreshChat(long id)
    {
        MatchResult<MatchChatMessage[]> result = await Request(async () =>
        {
            Proxy.MatchChatEntry[] messages = await Execute(() => PrepareChat(),
                () => _chat.GetChatHistoryAsync(_token, id));
            return messages.Select(MapMessage).ToArray();
        });
        if (result.Status == MatchStatus.Success && result.Value != null)
        {
            foreach (MatchChatMessage message in result.Value)
            {
                StoreMessage(message);
            }
        }
        return result.Status;
    }

    internal async Task<ChatSubmission> SendChatMessage(long id, string content)
    {
        ChatSubmission submission;
        try
        {
            Proxy.MatchChatEntry message = await Execute(() => PrepareChat(),
                () => _chat.SendChatMessageAsync(_token, id, content.Trim()));
            StoreMessage(MapMessage(message));
            submission = new ChatSubmission(MatchStatus.Success, null);
        }
        catch (FaultException<Proxy.ChatFault> fault)
        {
            submission = new ChatSubmission(
                MapChatFailure(fault.Detail.Reason), fault.Detail.MuteEndsAtUtc);
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            submission = new ChatSubmission(MapFailure(exception), null);
        }
        return submission;
    }

    internal MatchChatMessage[] ReadMessages(long id)
    {
        lock (_sync)
        {
            if (!_messages.ContainsKey(id))
            {
                return Array.Empty<MatchChatMessage>();
            }
            return _messages[id].Values.ToArray();
        }
    }

    internal bool HasMatchState(long id)
    {
        lock (_sync)
        {
            return _states.ContainsKey(id);
        }
    }

    internal MatchStateSnapshot ReadMatchState(long id)
    {
        lock (_sync)
        {
            if (!_states.ContainsKey(id))
            {
                throw new InvalidOperationException("The match state is not available.");
            }
            return _states[id];
        }
    }

    internal ParticipantConnectionState ReadParticipantConnection(long matchId, int seatNumber)
    {
        lock (_sync)
        {
            if (_participantConnections.ContainsKey(matchId)
                && _participantConnections[matchId].ContainsKey(seatNumber))
            {
                return _participantConnections[matchId][seatNumber];
            }

            return ParticipantConnectionState.Unknown;
        }
    }

    private async Task<MatchResult<T>> Request<T>(Func<Task<T>> operation) where T : class
    {
        MatchResult<T> result;
        try
        {
            result = new MatchResult<T>(MatchStatus.Success, await operation());
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            result = new MatchResult<T>(MapFailure(exception), null);
        }
        return result;
    }

    private async Task<MatchStatus> RequestStatus(Func<Task> operation)
    {
        MatchStatus status;
        try
        {
            await operation();
            status = MatchStatus.Success;
        }
        catch (Exception exception) when (RemoteCall.IsExpectedFailure(exception))
        {
            status = MapFailure(exception);
        }
        return status;
    }

    private async Task<T> Execute<T, TContract>(
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
            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
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

    public void OnMatchStarted(Proxy.MatchStateSnapshot state)
    {
        StoreState(MapState(state));
    }

    public void OnLobbyClosed(long matchId)
    {
        LobbyClosed?.Invoke(matchId);
    }

    public void OnMatchStateChanged(Proxy.MatchStateSnapshot state)
    {
        StoreState(MapState(state));
    }

    private void StoreState(MatchStateSnapshot state)
    {
        lock (_sync)
        {
            if (_states.ContainsKey(state.MatchId)
                && _states[state.MatchId].Version >= state.Version)
            {
                return;
            }

            _states[state.MatchId] = state;
        }

        StateChanged?.Invoke(state);
    }

    public void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected)
    {
        lock (_sync)
        {
            if (!_participantConnections.ContainsKey(matchId))
            {
                _participantConnections.Add(
                    matchId, new Dictionary<int, ParticipantConnectionState>());
            }

            _participantConnections[matchId][seatNumber] = isConnected
                ? ParticipantConnectionState.Connected
                : ParticipantConnectionState.Disconnected;
        }

        ParticipantConnectionChanged?.Invoke(matchId);
    }

    public void OnChatMessageReceived(Proxy.MatchChatEntry message)
    {
        StoreMessage(MapMessage(message));
    }

    private void StoreMessage(MatchChatMessage message)
    {
        lock (_sync)
        {
            if (!_messages.ContainsKey(message.MatchId))
            {
                _messages.Add(
                    message.MatchId, new SortedDictionary<int, MatchChatMessage>());
            }
            if (_messages[message.MatchId].ContainsKey(message.Number))
            {
                return;
            }
            _messages[message.MatchId].Add(message.Number, message);
        }

        MessagesChanged?.Invoke(message.MatchId);
    }

    public void OnPlayerMuted(Proxy.ChatMuteNotification mute)
    {
        Muted?.Invoke(new RemoteChatMute(mute.MatchId, mute.EndsAtUtc));
    }

    private static Proxy.MatchConfiguration CreateConfiguration(int seconds)
    {
        return new Proxy.MatchConfiguration
        {
            TurnDurationSeconds = seconds,
            OpponentKind = Proxy.OpponentKind.Human,
            CountsTowardStatistics = false
        };
    }

    private static LobbySnapshot MapLobby(Proxy.LobbySnapshot lobby)
    {
        return new LobbySnapshot(
            lobby.MatchId,
            lobby.MatchCode,
            lobby.Configuration.TurnDurationSeconds);
    }

    private static RemoteAvailableMatch MapAvailableMatch(Proxy.AvailableMatch match)
    {
        return new RemoteAvailableMatch(
            match.MatchCode,
            match.CreatorDisplayName,
            match.TurnDurationSeconds);
    }

    private static RemoteMatchInformation MapInformation(Proxy.MatchInformation information)
    {
        MatchParticipant[] participants = information.Participants
            .Select(participant => new MatchParticipant(
                participant.SeatNumber, participant.DisplayName))
            .ToArray();
        return new RemoteMatchInformation(
            information.MatchId,
            participants,
            information.AssignedSeatNumber,
            information.TurnDurationSeconds);
    }

    private static MatchStateSnapshot MapState(Proxy.MatchStateSnapshot state)
    {
        ConfirmedBoardCell[] board = state.Board.Select(MapBoardCell).ToArray();
        RemoteMatchConclusion? conclusion = state.Conclusion == null
            ? null
            : MapConclusion(state.Conclusion);
        return new MatchStateSnapshot(
            state.MatchId,
            state.MatchStateVersion,
            (ConfirmedMatchState)state.State,
            board,
            state.BlackPieceCount,
            state.WhitePieceCount,
            state.CurrentTurnSeatNumber)
        {
            Conclusion = conclusion
        };
    }

    private static ConfirmedBoardCell MapBoardCell(Proxy.BoardCellState cell)
    {
        return cell switch
        {
            Proxy.BoardCellState.Black => ConfirmedBoardCell.Black,
            Proxy.BoardCellState.White => ConfirmedBoardCell.White,
            _ => ConfirmedBoardCell.Empty
        };
    }

    private static RemoteMatchConclusion MapConclusion(Proxy.MatchConclusion conclusion)
    {
        return new RemoteMatchConclusion(
            (RemoteMatchEndReason)conclusion.Reason,
            conclusion.WinnerSeatNumber);
    }

    private static MatchChatMessage MapMessage(Proxy.MatchChatEntry message)
    {
        return new MatchChatMessage(
            message.MatchId,
            message.MessageNumber,
            message.SenderSeatNumber,
            message.Content);
    }

    private static MatchStatus MapFailure(Exception exception)
    {
        MatchStatus status = MatchStatus.ServiceUnavailable;
        if (exception is FaultException<Proxy.MatchFault> matchFault)
        {
            status = MapMatchFailure(matchFault.Detail.Reason);
        }
        else if (exception is FaultException<Proxy.SessionFault>)
        {
            status = MatchStatus.InvalidSession;
        }
        else if (exception is FaultException<Proxy.FriendshipFault> friendshipFault)
        {
            status = MapFriendshipFailure(friendshipFault.Detail.Reason);
        }
        return status;
    }

    private static MatchStatus MapFriendshipFailure(Proxy.FriendshipFailure reason)
    {
        return reason switch
        {
            Proxy.FriendshipFailure.AccountUnavailable => MatchStatus.AccountUnavailable,
            Proxy.FriendshipFailure.FriendshipNotFound => MatchStatus.FriendshipNotFound,
            Proxy.FriendshipFailure.InvalidAccountId => MatchStatus.InvalidAccountId,
            _ => MatchStatus.InactiveAccount
        };
    }

    private static MatchStatus MapMatchFailure(Proxy.MatchFailure failure)
    {
        MatchStatus status = MatchStatus.InactiveAccount;
        if (s_matchStatuses.ContainsKey(failure))
        {
            status = s_matchStatuses[failure];
        }
        return status;
    }

    private static MatchStatus MapChatFailure(Proxy.ChatFailure failure)
    {
        MatchStatus status = MatchStatus.MessageBlocked;
        if (failure == Proxy.ChatFailure.EmptyMessage)
        {
            status = MatchStatus.EmptyMessage;
        }
        else if (failure == Proxy.ChatFailure.MessageTooLong)
        {
            status = MatchStatus.MessageTooLong;
        }
        else if (failure == Proxy.ChatFailure.PlayerMuted)
        {
            status = MatchStatus.PlayerMuted;
        }
        return status;
    }

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

internal enum ParticipantConnectionState
{
    Unknown,
    Connected,
    Disconnected
}

/// <summary>Optional endpoint overrides for a coherent remote match service deployment.</summary>
internal sealed class MatchEndpoints
{
    internal string Lobby { get; set; } = string.Empty;
    internal string Gameplay { get; set; } = string.Empty;
    internal string Chat { get; set; } = string.Empty;
}
