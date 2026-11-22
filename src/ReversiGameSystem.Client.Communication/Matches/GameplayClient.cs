using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Matches;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

[CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
internal sealed class GameplayClient : IDisposable, Proxy.IGameplayMatchServiceCallback
{
    private readonly string _token;
    private readonly object _matchDataLock = new object();
    private readonly DuplexChannel<Proxy.GameplayMatchServiceClient, Proxy.IGameplayMatchService>
        _channel;

    private readonly HashSet<long> _subscribedMatches = new HashSet<long>();
    private readonly Dictionary<long, MatchStateSnapshot> _states =
        new Dictionary<long, MatchStateSnapshot>();
    private readonly Dictionary<long, MatchInformation> _information =
        new Dictionary<long, MatchInformation>();
    private readonly Dictionary<long, Dictionary<int, ParticipantConnectionState>>
        _participantConnections =
            new Dictionary<long, Dictionary<int, ParticipantConnectionState>>();

    internal GameplayClient(string token, ILogger<GameplayClient> logger)
        : this(token, null, logger)
    {
    }

    internal GameplayClient(string token, string? endpoint, ILogger<GameplayClient> logger)
    {
        _token = token;
        Proxy.GameplayMatchServiceClient CreateClient()
        {
            Proxy.GameplayMatchServiceClient client;
            if (endpoint == null)
            {
                client = ServerConnectionFactory.CreateGameplayClient(new InstanceContext(this));
            }
            else
            {
                client = new Proxy.GameplayMatchServiceClient(new InstanceContext(this),
                    DuplexChannel<Proxy.GameplayMatchServiceClient, Proxy.IGameplayMatchService>
                        .CreateOverrideBinding(), new EndpointAddress(endpoint));
            }
            return client;
        }
        _channel = new DuplexChannel<Proxy.GameplayMatchServiceClient, Proxy.IGameplayMatchService>(
            CreateClient, logger);
        _channel.ChannelCreated += _subscribedMatches.Clear;
        _channel.ConnectionLost += () => ConnectionLost?.Invoke();
    }

    internal event Action<MatchStateSnapshot>? StateChanged;
    internal event Action<long>? ParticipantConnectionChanged;
    internal event Action? ConnectionLost;

    private async Task OpenGameplayChannel(Proxy.GameplayMatchServiceClient client, long matchId)
    {
        if (!_subscribedMatches.Contains(matchId))
        {
            await client.OpenMatchChannelAsync(_token, matchId).ConfigureAwait(false);
            _subscribedMatches.Add(matchId);
        }
    }

    internal Task<MatchStatus> LoadMatchInformation(
        long matchId, CancellationToken cancellation = default)
    {
        async Task LoadInformation(Proxy.GameplayMatchServiceClient client)
        {
            await OpenGameplayChannel(client, matchId).ConfigureAwait(false);
            Proxy.MatchInformation information = await client.GetMatchInformationAsync(
                _token, matchId)
                .ConfigureAwait(false);
            lock (_matchDataLock)
            {
                _information[matchId] = MapInformation(information);
            }
        }
        return SendMatchOperation(LoadInformation, cancellation);
    }

    internal MatchInformation ReadMatchInformation(long matchId)
    {
        lock (_matchDataLock)
        {
            if (!_information.ContainsKey(matchId))
            {
                throw new InvalidOperationException("The match information has not been loaded.");
            }
            return _information[matchId];
        }
    }

    internal Task<MatchStatus> LoadMatchState(
        long matchId, CancellationToken cancellation = default)
    {
        async Task LoadState(Proxy.GameplayMatchServiceClient client)
        {
            await OpenGameplayChannel(client, matchId).ConfigureAwait(false);
            Proxy.MatchStateSnapshot state = await client.GetMatchStateAsync(_token, matchId)
                .ConfigureAwait(false);
            StoreState(MatchContractMapper.MapState(state));
        }
        return SendMatchOperation(LoadState, cancellation);
    }

    internal Task<MatchStatus> PlacePiece(
        MatchPlacement placement, CancellationToken cancellation = default)
    {
        if (placement == null)
        {
            throw new ArgumentNullException(nameof(placement));
        }
        Proxy.PiecePlacement request = new Proxy.PiecePlacement
        {
            MatchId = placement.MatchId,
            ExpectedMatchStateVersion = placement.ExpectedVersion,
            Row = placement.Row,
            Column = placement.Column
        };
        async Task PlaceMatchPiece(Proxy.GameplayMatchServiceClient client)
        {
            await OpenGameplayChannel(client, request.MatchId).ConfigureAwait(false);
            Proxy.MatchStateSnapshot state = await client.PlacePieceAsync(_token, request)
                .ConfigureAwait(false);
            StoreState(MatchContractMapper.MapState(state));
        }
        return SendMatchOperation(PlaceMatchPiece, cancellation);
    }

    internal Task<MatchStatus> ForfeitMatch(
        long matchId, CancellationToken cancellation = default)
    {
        async Task ForfeitParticipant(Proxy.GameplayMatchServiceClient client)
        {
            await OpenGameplayChannel(client, matchId).ConfigureAwait(false);
            await client.ForfeitMatchAsync(_token, matchId).ConfigureAwait(false);
        }
        return SendMatchOperation(ForfeitParticipant, cancellation);
    }

    internal Task<MatchStatus> RestoreMatchConnection(long matchId, CancellationToken cancellation)
    {
        async Task RestoreState(Proxy.GameplayMatchServiceClient client)
        {
            try
            {
                await OpenGameplayChannel(client, matchId).ConfigureAwait(false);
            }
            catch (FaultException<Proxy.MatchFault> fault)
                when (fault.Detail.Reason == Proxy.MatchFailure.MatchNotInProgress)
            {
                Proxy.MatchStateSnapshot current = await client.GetMatchStateAsync(_token, matchId)
                    .ConfigureAwait(false);
                if ((ConfirmedMatchState)current.State != ConfirmedMatchState.Finished)
                {
                    throw;
                }
                StoreState(MatchContractMapper.MapState(current));
                return;
            }
            Proxy.MatchStateSnapshot state = await client.GetMatchStateAsync(_token, matchId)
                .ConfigureAwait(false);
            StoreState(MatchContractMapper.MapState(state));
        }
        return SendMatchOperation(RestoreState, cancellation);
    }
    internal bool HasMatchState(long id)
    {
        lock (_matchDataLock)
        {
            return _states.ContainsKey(id);
        }
    }
    internal MatchStateSnapshot ReadMatchState(long id)
    {
        lock (_matchDataLock)
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
        lock (_matchDataLock)
        {
            if (_participantConnections.ContainsKey(matchId)
                && _participantConnections[matchId].ContainsKey(seatNumber))
            {
                return _participantConnections[matchId][seatNumber];
            }

            return ParticipantConnectionState.Unknown;
        }
    }
    private void StoreState(MatchStateSnapshot state)
    {
        lock (_matchDataLock)
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
        lock (_matchDataLock)
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
    private static MatchInformation MapInformation(Proxy.MatchInformation information)
    {
        MatchParticipant[] participants = information.Participants
            .Select(participant => new MatchParticipant(
                participant.SeatNumber, participant.DisplayName)
            {
                IsSystem = participant.Kind == Proxy.ParticipantKind.System
            })
            .ToArray();
        return new MatchInformation(
            information.MatchId,
            participants,
            information.AssignedSeatNumber,
            information.TurnDurationSeconds);
    }
    public void OnMatchStateChanged(Proxy.MatchStateSnapshot state)
    {
        StoreState(MatchContractMapper.MapState(state));
    }
    private async Task<MatchStatus> SendMatchOperation(
        Func<Proxy.GameplayMatchServiceClient, Task> operation, CancellationToken cancellation)
    {
        MatchStatus status;
        try
        {
            await _channel.SendServiceCall(operation, cancellation).ConfigureAwait(false);
            status = MatchStatus.Success;
        }
        catch (CommunicationException exception)
            when (MatchContractMapper.CanMapFailure(exception))
        {
            status = MatchContractMapper.MapFailure(exception);
        }
        catch (CommunicationException exception) when (exception is not FaultException)
        {
            status = MatchStatus.ServiceUnavailable;
        }
        catch (TimeoutException)
        {
            status = MatchStatus.ServiceUnavailable;
        }
        return status;
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
