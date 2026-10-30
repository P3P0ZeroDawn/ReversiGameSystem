using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Publishes durable human lobby transitions over authenticated duplex channels.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class MatchLobbyService : IMatchLobbyService, IDisposable
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(5);
    private readonly MatchHandler _handler;
    private readonly MatchBoundary _boundary;
    private readonly MatchChannels<IMatchLobbyServiceCallback> _channels;
    private readonly Timer _expiryTimer;
    private int _sweeping;

    /// <summary>Creates the lobby boundary using the authoritative application handler.</summary>
    public MatchLobbyService(MatchHandler handler, ILogger<MatchLobbyService> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _boundary = new MatchBoundary(logger);
        _channels = new MatchChannels<IMatchLobbyServiceCallback>(
            AuthorizeChannel, Cleanup, _boundary);
        _expiryTimer = new Timer(_ => SweepExpired(), null, SweepInterval, SweepInterval);
    }

    /// <inheritdoc />
    public void OpenLobbyChannel(string sessionToken)
    {
        _boundary.Execute(nameof(OpenLobbyChannel), () => RegisterChannel(sessionToken));
    }

    /// <inheritdoc />
    public LobbySnapshot CreateMatch(string sessionToken, MatchConfiguration configuration)
    {
        return _boundary.Execute(nameof(CreateMatch),
            () => CreateLobby(sessionToken, configuration));
    }

    /// <inheritdoc />
    public LobbySnapshot InviteToMatch(
        string sessionToken,
        long friendAccountId,
        MatchConfiguration configuration)
    {
        return _boundary.Execute(nameof(InviteToMatch),
            () => InviteFriend(sessionToken, friendAccountId, configuration));
    }

    /// <inheritdoc />
    public AvailableMatch[] GetAvailableMatches(string sessionToken)
    {
        return _boundary.Execute(nameof(GetAvailableMatches),
            () => _handler.Available(sessionToken).Select(MapAvailableMatch).ToArray());
    }

    /// <inheritdoc />
    public LobbySnapshot JoinMatch(string sessionToken, string matchCode)
    {
        return _boundary.Execute(nameof(JoinMatch), () => JoinLobby(sessionToken, matchCode));
    }

    /// <inheritdoc />
    public LobbySnapshot ChangeTurnDuration(
        string sessionToken,
        long matchId,
        int turnDurationSeconds)
    {
        return _boundary.Execute(nameof(ChangeTurnDuration), () => MatchContractMapper.Lobby(
            _handler.ChangeTurnDuration(sessionToken, matchId, turnDurationSeconds)));
    }

    /// <inheritdoc />
    public MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId)
    {
        throw MatchBoundary.Unsupported(nameof(StartSystemMatch));
    }

    /// <inheritdoc />
    public void CancelMatch(string sessionToken, long matchId)
    {
        _boundary.Execute(nameof(CancelMatch), () => CancelLobby(sessionToken, matchId));
    }

    private bool AuthorizeChannel(string token, long matchId)
    {
        try
        {
            _handler.Authorize(token);
            return true;
        }
        catch (SessionRejectedException)
        {
            return false;
        }
        catch (MatchRejectedException)
        {
            return false;
        }
    }

    private void RegisterChannel(string token)
    {
        _handler.Authorize(token);
        _channels.Register(token, 0);
    }

    private LobbySnapshot CreateLobby(string token, MatchConfiguration configuration)
    {
        MatchSettings? settings = configuration == null ? null : MapSettings(configuration);
        MatchRecord match = _handler.Create(token, settings);
        return MatchContractMapper.Lobby(match);
    }

    private LobbySnapshot InviteFriend(
        string token,
        long friendAccountId,
        MatchConfiguration configuration)
    {
        MatchSettings? settings = configuration == null ? null : MapSettings(configuration);
        MatchRecord match = _handler.Invite(token, friendAccountId, settings);
        return MatchContractMapper.Lobby(match);
    }

    private LobbySnapshot JoinLobby(string token, string matchCode)
    {
        MatchRecord match = _handler.Join(token, matchCode);
        _channels.Publish(match.Id, (recipientToken, callback) =>
            PublishStarted(recipientToken, callback, match.Id));
        return MatchContractMapper.Lobby(match);
    }

    private void PublishStarted(string token, IMatchLobbyServiceCallback callback, long matchId)
    {
        MatchRecord match = _handler.Get(token, matchId);
        callback.OnMatchStarted(MatchContractMapper.InitialState(match));
    }

    private void CancelLobby(string token, long matchId)
    {
        _handler.Cancel(token, matchId);
        PublishClosed(matchId);
    }

    private static MatchSettings MapSettings(MatchConfiguration configuration)
    {
        return new MatchSettings
        {
            TurnDurationSeconds = configuration.TurnDurationSeconds,
            CountsTowardStatistics = configuration.CountsTowardStatistics,
            OpponentKind = configuration.OpponentKind.ToString()
        };
    }

    private static AvailableMatch MapAvailableMatch(MatchRecord match)
    {
        return new AvailableMatch
        {
            MatchCode = match.Code
                ?? throw new InvalidOperationException("An available match must have a join code."),
            CreatorDisplayName = match.Participants[0].DisplayName,
            TurnDurationSeconds = match.TurnDurationSeconds
        };
    }

    /// <summary>Removes a revoked session and cancels its waiting lobbies.</summary>
    public void RemoveSession(string sessionToken)
    {
        _channels.RemoveSession(sessionToken);
        Cleanup(sessionToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _expiryTimer.Dispose();
        _channels.Dispose();
    }

    private void SweepExpired()
    {
        if (Interlocked.Exchange(ref _sweeping, 1) != 0)
        {
            return;
        }
        try
        {
            foreach (long id in _handler.CancelExpiredWaitingSessions())
            {
                PublishClosed(id);
            }
        }
        catch (Exception exception)
        {
            _boundary.Log(nameof(SweepExpired), exception);
        }
        finally
        {
            Volatile.Write(ref _sweeping, 0);
        }
    }

    private void Cleanup(string token)
    {
        foreach (long id in _handler.CancelWaitingForSession(token))
        {
            PublishClosed(id);
        }
    }

    private void PublishClosed(long id)
    {
        _channels.Publish(id, (token, callback) => PublishClosedToParticipant(token, callback, id));
    }

    private void PublishClosedToParticipant(
        string token,
        IMatchLobbyServiceCallback callback,
        long matchId)
    {
        _handler.Get(token, matchId);
        callback.OnLobbyClosed(matchId);
    }
}
