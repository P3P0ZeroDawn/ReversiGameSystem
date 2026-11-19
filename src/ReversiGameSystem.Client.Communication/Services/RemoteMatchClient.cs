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

[CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
internal sealed class RemoteMatchClient : IDisposable, Proxy.IMatchLobbyServiceCallback,
    Proxy.IGameplayMatchServiceCallback, Proxy.IMatchChatServiceCallback
{
    private const int RequestConcurrencyLimit = 1;
    private static readonly TimeSpan RecoveryAttemptLimit = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RecoveryRetryDelay = TimeSpan.FromSeconds(1);
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

        client.InnerChannel.Faulted += OnGameplayChannelFaulted;
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
    internal event Action? GameplayConnectionLost;
    internal event Action<long>? LobbyClosed;
    internal event Action<long>? MessagesChanged;
    internal event Action<long>? ParticipantConnectionChanged;
    internal event Action<RemoteChatMute>? Muted;

    internal Task<MatchResult<LobbySnapshot>> CreateMatch(int seconds)
    {
        Proxy.MatchConfiguration configuration = CreateConfiguration(seconds);
        Task<MatchResult<LobbySnapshot>> result = SendMatchCall(
            PrepareLobby, async () =>
            {
                await OpenLobbyChannel();
                return MapLobby(await _lobby.CreateMatchAsync(_token, configuration));
            });
        return result;
    }

    internal Task<MatchResult<LobbySnapshot>> InviteToMatch(long friendAccountId, int seconds)
    {
        Proxy.MatchConfiguration configuration = CreateConfiguration(seconds);
        Task<MatchResult<LobbySnapshot>> result = SendMatchCall(
            PrepareLobby, async () =>
            {
                await OpenLobbyChannel();
                return MapLobby(await _lobby.InviteToMatchAsync(
                    _token, friendAccountId, configuration));
            });
        return result;
    }

    internal Task<MatchResult<RemoteAvailableMatch[]>> LoadAvailableMatches()
    {
        Task<MatchResult<RemoteAvailableMatch[]>> result = SendMatchCall(
            PrepareLobby, async () =>
            {
                await OpenLobbyChannel();
                return (await _lobby.GetAvailableMatchesAsync(_token))
                    .Select(MapAvailableMatch).ToArray();
            });
        return result;
    }

    internal Task<MatchResult<LobbySnapshot>> JoinMatch(string code)
    {
        Task<MatchResult<LobbySnapshot>> result = SendMatchCall(
            PrepareLobby, async () =>
            {
                await OpenLobbyChannel();
                return MapLobby(await _lobby.JoinMatchAsync(_token, code));
            });
        return result;
    }

    internal Task<MatchStatus> CancelMatch(long id)
    {
        return SendMatchCommand(PrepareLobby, () => _lobby.CancelMatchAsync(_token, id));
    }

    internal async Task<MatchStatus> StartSystemMatch(long id)
    {
        MatchResult<MatchStateSnapshot> result = await SendMatchCall(PrepareLobby, async () =>
        {
            Proxy.MatchStateSnapshot state = await _lobby.StartSystemMatchAsync(_token, id);
            return MapState(state);
        });
        if (result.Value != null)
        {
            StoreState(result.Value);
        }

        return result.Status;
    }

    internal Task<MatchResult<LobbySnapshot>> ChangeTurnDuration(long id, int seconds)
    {
        async Task<LobbySnapshot> RequestDurationChange()
        {
            await OpenLobbyChannel();
            Proxy.LobbySnapshot updatedLobby = await _lobby.ChangeTurnDurationAsync(
                _token, id, seconds);
            return MapLobby(updatedLobby);
        }

        return SendMatchCall(PrepareLobby, RequestDurationChange);
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
        Task<MatchResult<RemoteMatchInformation>> result = SendMatchCall(
            PrepareGameplay, async () =>
            {
                await OpenGameplayChannel(id);
                return MapInformation(await _gameplay.GetMatchInformationAsync(_token, id));
            });
        return result;
    }

    internal async Task<MatchResult<MatchStateSnapshot>> LoadMatchState(
        long id, CancellationToken cancellation = default)
    {
        MatchResult<MatchStateSnapshot> result = await SendMatchCall(
            PrepareGameplay,
            async () =>
            {
                await OpenGameplayChannel(id);
                return MapState(await _gameplay.GetMatchStateAsync(_token, id));
            }, cancellation);
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

        Proxy.PiecePlacement placement = new Proxy.PiecePlacement
        {
            MatchId = confirmedState.MatchId,
            Row = row,
            Column = column,
            ExpectedMatchStateVersion = confirmedState.Version
        };
        MatchResult<MatchStateSnapshot> result = await SendMatchCall(PrepareGameplay, async () =>
        {
            await OpenGameplayChannel(placement.MatchId);
            return MapState(await _gameplay.PlacePieceAsync(_token, placement));
        });
        if (result.Status == MatchStatus.Success && result.Value != null)
        {
            StoreState(result.Value);
        }
        return result;
    }

    internal Task<MatchStatus> ForfeitMatch(long id)
    {
        Task<MatchStatus> status = SendMatchCommand(PrepareGameplay, async () =>
        {
            await OpenGameplayChannel(id);
            await _gameplay.ForfeitMatchAsync(_token, id);
        });
        return status;
    }

    internal async Task<MatchStatus> RecoverMatch(long id, CancellationToken cancellation)
    {
        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            using CancellationTokenSource attempt = CancellationTokenSource
                .CreateLinkedTokenSource(cancellation);
            attempt.CancelAfter(RecoveryAttemptLimit);
            try
            {
                MatchStatus status = await RecoverMatchState(id, attempt.Token)
                    .ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();
                if (status != MatchStatus.ServiceUnavailable
                    && status != MatchStatus.MatchNotInProgress)
                {
                    return status;
                }
            }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
            {
            }

            await Task.Delay(RecoveryRetryDelay, cancellation).ConfigureAwait(false);
        }
    }

    private async Task<MatchStatus> RecoverMatchState(long id, CancellationToken cancellation)
    {
        MatchResult<MatchStateSnapshot> result = await SendMatchCall(
            PrepareGameplay,
            () => ReadStateAfterReopeningChannel(id), cancellation).ConfigureAwait(false);
        if (result.Value is MatchStateSnapshot confirmedState)
        {
            StoreState(confirmedState);
        }
        return result.Status;
    }

    internal Task<MatchStatus> OpenChatChannel(
        long id, CancellationToken cancellation = default)
    {
        return SendMatchCommand(
            PrepareChat, () => _chat.OpenChatChannelAsync(_token, id), cancellation);
    }

    internal async Task<MatchStatus> RefreshChat(
        long id, CancellationToken cancellation = default)
    {
        MatchResult<MatchChatMessage[]> result = await SendMatchCall(
            PrepareChat,
            async () =>
            {
                Proxy.MatchChatEntry[] messages = await _chat.GetChatHistoryAsync(_token, id);
                return messages.Select(MapMessage).ToArray();
            }, cancellation);
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
            Proxy.MatchChatEntry message = await SendDuplexCall(PrepareChat,
                () => _chat.SendChatMessageAsync(_token, id, content.Trim()));
            StoreMessage(MapMessage(message));
            submission = new ChatSubmission(MatchStatus.Success, null);
        }
        catch (FaultException<Proxy.ChatFault> fault)
        {
            submission = new ChatSubmission(
                MapChatFailure(fault.Detail.Reason), fault.Detail.MuteEndsAtUtc);
        }
        catch (CommunicationException exception)
        {
            submission = new ChatSubmission(MapFailure(exception), null);
        }
        catch (TimeoutException)
        {
            submission = new ChatSubmission(MatchStatus.ServiceUnavailable, null);
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

    private async Task<MatchStateSnapshot> ReadStateAfterReopeningChannel(long id)
    {
        try
        {
            await OpenGameplayChannel(id).ConfigureAwait(false);
        }
        catch (FaultException<Proxy.MatchFault> fault)
            when (fault.Detail.Reason == Proxy.MatchFailure.MatchNotInProgress)
        {
            Proxy.MatchStateSnapshot current = await _gameplay.GetMatchStateAsync(
                _token, id).ConfigureAwait(false);
            if ((ConfirmedMatchState)current.State != ConfirmedMatchState.Finished)
            {
                throw;
            }

            return MapState(current);
        }

        Proxy.MatchStateSnapshot state = await _gameplay.GetMatchStateAsync(_token, id)
            .ConfigureAwait(false);
        MatchStateSnapshot result = MapState(state);
        return result;
    }


    private async Task<MatchResult<T>> SendMatchCall<T, TContract>(
        Func<ClientBase<TContract>> prepareChannel,
        Func<Task<T>> callOperation,
        CancellationToken cancellation = default)
        where T : class
        where TContract : class
    {
        MatchResult<T> result;
        try
        {
            T value = await SendDuplexCall(prepareChannel, callOperation, cancellation);
            result = new MatchResult<T>(MatchStatus.Success, value);
        }
        catch (CommunicationException exception)
        {
            result = new MatchResult<T>(MapFailure(exception), null);
        }
        catch (TimeoutException)
        {
            result = new MatchResult<T>(MatchStatus.ServiceUnavailable, null);
        }
        return result;
    }

    private async Task<MatchStatus> SendMatchCommand<TContract>(
        Func<ClientBase<TContract>> prepareChannel,
        Func<Task> callOperation,
        CancellationToken cancellation = default)
        where TContract : class
    {
        MatchStatus status;
        try
        {
            await SendDuplexCall(prepareChannel, async () =>
            {
                await callOperation();
                return true;
            }, cancellation);
            status = MatchStatus.Success;
        }
        catch (CommunicationException exception)
        {
            status = MapFailure(exception);
        }
        catch (TimeoutException)
        {
            status = MatchStatus.ServiceUnavailable;
        }
        return status;
    }

    private async Task<T> SendDuplexCall<T, TContract>(
        Func<ClientBase<TContract>> prepareChannel,
        Func<Task<T>> callOperation,
        CancellationToken cancellation = default)
        where TContract : class
    {
        await _requests.WaitAsync(cancellation).ConfigureAwait(false);
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
                client = prepareChannel();
            }
            using CancellationTokenRegistration registration = cancellation.Register(client.Abort);
            cancellation.ThrowIfCancellationRequested();
            Task<T> request = StartCorrelatedCall(
                client.InnerChannel, callOperation, correlationId);
            T result = await request.ConfigureAwait(false);
            return result;
        }
        catch (CommunicationException exception) when (exception is not FaultException)
        {
            cancellation.ThrowIfCancellationRequested();
            AbortFailedChannel(client, exception, correlationId);
            throw;
        }
        catch (TimeoutException exception)
        {
            cancellation.ThrowIfCancellationRequested();
            AbortFailedChannel(client, exception, correlationId);
            throw;
        }
        finally
        {
            _requests.Release();
        }
    }

    private void AbortFailedChannel(
        ICommunicationObject? channel, Exception failure, string correlationId)
    {
        channel?.Abort();
        if (ReferenceEquals(channel, _gameplay))
        {
            NotifyGameplayConnectionLost();
        }

        _logger.LogWarning(FailureDiagnostics.ForLogging(failure),
            "The match request failed. CorrelationId: {CorrelationId}", correlationId);
    }

    private static Task<T> StartCorrelatedCall<T>(
        IContextChannel channel, Func<Task<T>> callOperation, string correlationId)
    {
        using OperationContextScope scope = new OperationContextScope(channel);
        OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
            "CorrelationId", "urn:reversi:diagnostics:v1", correlationId));
        Task<T> request = callOperation();
        return request;
    }

    private void OnGameplayChannelFaulted(object? sender, EventArgs eventArguments)
    {
        if (!_disposed && ReferenceEquals(sender, _gameplay.InnerChannel))
        {
            NotifyGameplayConnectionLost();
        }
    }

    private void NotifyGameplayConnectionLost()
    {
        if (!_disposed)
        {
            GameplayConnectionLost?.Invoke();
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
                participant.SeatNumber, participant.DisplayName)
            {
                IsSystem = participant.Kind == Proxy.ParticipantKind.System
            })
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
            TurnStartedAtUtc = state.TurnStartedAtUtc,
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
        else if (failure == Proxy.ChatFailure.RateLimitExceeded)
        {
            status = MatchStatus.RateLimitExceeded;
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
