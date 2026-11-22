using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Matches;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

[CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
internal sealed class MatchLobbyClient : IDisposable, Proxy.IMatchLobbyServiceCallback
{
    private readonly string _token;
    private readonly object _lobbyDataLock = new object();
    private readonly DuplexChannel<Proxy.MatchLobbyServiceClient, Proxy.IMatchLobbyService>
        _channel;

    private LobbySnapshot? _currentLobby;
    private AvailableMatch[] _availableMatches = Array.Empty<AvailableMatch>();
    private bool _hasLobbySubscription;

    internal MatchLobbyClient(string token, ILogger<MatchLobbyClient> logger)
        : this(token, null, logger)
    {
    }

    internal MatchLobbyClient(string token, string? endpoint, ILogger<MatchLobbyClient> logger)
    {
        _token = token;
        Proxy.MatchLobbyServiceClient CreateClient()
        {
            Proxy.MatchLobbyServiceClient client;
            if (endpoint == null)
            {
                client = ServerConnectionFactory.CreateLobbyClient(new InstanceContext(this));
            }
            else
            {
                client = new Proxy.MatchLobbyServiceClient(new InstanceContext(this),
                    DuplexChannel<Proxy.MatchLobbyServiceClient, Proxy.IMatchLobbyService>
                        .CreateOverrideBinding(), new EndpointAddress(endpoint));
            }
            return client;
        }
        _channel = new DuplexChannel<Proxy.MatchLobbyServiceClient, Proxy.IMatchLobbyService>(
            CreateClient, logger);
        _channel.ChannelCreated += () => _hasLobbySubscription = false;
    }

    internal event Action<MatchStateSnapshot>? MatchStarted;
    internal event Action<long>? LobbyClosed;

    private async Task OpenLobbyChannel(Proxy.MatchLobbyServiceClient client)
    {
        if (!_hasLobbySubscription)
        {
            await client.OpenLobbyChannelAsync(_token).ConfigureAwait(false);
            _hasLobbySubscription = true;
        }
    }

    internal LobbySnapshot ReadCurrentLobby()
    {
        lock (_lobbyDataLock)
        {
            return _currentLobby
                ?? throw new InvalidOperationException("The lobby has not been loaded.");
        }
    }

    internal AvailableMatch[] ReadAvailableMatches()
    {
        lock (_lobbyDataLock)
        {
            return (AvailableMatch[])_availableMatches.Clone();
        }
    }

    internal Task<MatchStatus> CreateMatch(
        int turnDurationSeconds, CancellationToken cancellation = default)
    {
        async Task UpdateLobby(Proxy.MatchLobbyServiceClient client)
        {
            await OpenLobbyChannel(client).ConfigureAwait(false);
            Proxy.LobbySnapshot lobby = await client.CreateMatchAsync(
                _token, CreateConfiguration(turnDurationSeconds))
                .ConfigureAwait(false);
            lock (_lobbyDataLock)
            {
                _currentLobby = MapLobby(lobby);
            }
        }
        return SendMatchOperation(UpdateLobby, cancellation);
    }

    internal Task<MatchStatus> InviteToMatch(
        long friendAccountId, int turnDurationSeconds, CancellationToken cancellation = default)
    {
        async Task UpdateLobby(Proxy.MatchLobbyServiceClient client)
        {
            await OpenLobbyChannel(client).ConfigureAwait(false);
            Proxy.LobbySnapshot lobby = await client.InviteToMatchAsync(
                _token, friendAccountId, CreateConfiguration(turnDurationSeconds))
                .ConfigureAwait(false);
            lock (_lobbyDataLock)
            {
                _currentLobby = MapLobby(lobby);
            }
        }
        return SendMatchOperation(UpdateLobby, cancellation);
    }

    internal Task<MatchStatus> JoinMatch(
        string matchCode, CancellationToken cancellation = default)
    {
        async Task UpdateLobby(Proxy.MatchLobbyServiceClient client)
        {
            await OpenLobbyChannel(client).ConfigureAwait(false);
            Proxy.LobbySnapshot lobby = await client.JoinMatchAsync(_token, matchCode)
                .ConfigureAwait(false);
            lock (_lobbyDataLock)
            {
                _currentLobby = MapLobby(lobby);
            }
        }
        return SendMatchOperation(UpdateLobby, cancellation);
    }

    internal Task<MatchStatus> ChangeTurnDuration(
        long matchId, int turnDurationSeconds, CancellationToken cancellation = default)
    {
        async Task UpdateLobby(Proxy.MatchLobbyServiceClient client)
        {
            await OpenLobbyChannel(client).ConfigureAwait(false);
            Proxy.LobbySnapshot lobby = await client.ChangeTurnDurationAsync(
                _token, matchId, turnDurationSeconds)
                .ConfigureAwait(false);
            lock (_lobbyDataLock)
            {
                _currentLobby = MapLobby(lobby);
            }
        }
        return SendMatchOperation(UpdateLobby, cancellation);
    }

    internal Task<MatchStatus> LoadAvailableMatches(CancellationToken cancellation = default)
    {
        async Task LoadMatches(Proxy.MatchLobbyServiceClient client)
        {
            await OpenLobbyChannel(client).ConfigureAwait(false);
            Proxy.AvailableMatch[] matches = await client.GetAvailableMatchesAsync(_token)
                .ConfigureAwait(false);
            lock (_lobbyDataLock)
            {
                _availableMatches = matches.Select(MapAvailableMatch).ToArray();
            }
        }
        return SendMatchOperation(LoadMatches, cancellation);
    }

    internal Task<MatchStatus> CancelMatch(long matchId, CancellationToken cancellation = default)
    {
        return SendMatchOperation(
            client => client.CancelMatchAsync(_token, matchId), cancellation);
    }

    internal Task<MatchStatus> StartSystemMatch(
        long matchId, CancellationToken cancellation = default)
    {
        async Task StartMatch(Proxy.MatchLobbyServiceClient client)
        {
            Proxy.MatchStateSnapshot state = await client.StartSystemMatchAsync(_token, matchId)
                .ConfigureAwait(false);
            OnMatchStarted(state);
        }
        return SendMatchOperation(StartMatch, cancellation);
    }

    public void OnMatchStarted(Proxy.MatchStateSnapshot state)
    {
        MatchStarted?.Invoke(MatchContractMapper.MapState(state));
    }

    public void OnLobbyClosed(long matchId)
    {
        LobbyClosed?.Invoke(matchId);
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
    private static AvailableMatch MapAvailableMatch(Proxy.AvailableMatch match)
    {
        return new AvailableMatch(
            match.MatchCode,
            match.CreatorDisplayName,
            match.TurnDurationSeconds);
    }
    private async Task<MatchStatus> SendMatchOperation(
        Func<Proxy.MatchLobbyServiceClient, Task> operation, CancellationToken cancellation)
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
