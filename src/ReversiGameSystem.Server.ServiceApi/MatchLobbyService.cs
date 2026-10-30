using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
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
        _channels = new MatchChannels<IMatchLobbyServiceCallback>((token, _) =>
        {
            try
            {
                _handler.Available(token); return true;
            }
            catch (ReversiGameSystem.Server.Application.Accounts.SessionRejectedException)
            {
                return false;
            }
            catch (MatchRejectedException)
            {
                return false;
            }
        }, Cleanup, _boundary);
        _expiryTimer = new Timer(_ => SweepExpired(), null,
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    /// <inheritdoc />
    public void OpenLobbyChannel(string sessionToken) => _boundary.Execute(nameof(OpenLobbyChannel), () =>
    {
        _handler.Available(sessionToken);
        _channels.Register(sessionToken, 0);
        return true;
    });

    /// <inheritdoc />
    public LobbySnapshot CreateMatch(string sessionToken, MatchConfiguration configuration) =>
        _boundary.Execute(nameof(CreateMatch), () => MatchContractMapper.Lobby(_handler.Create(sessionToken,
            configuration == null ? null! : new MatchSettings
            {
                TurnDurationSeconds = configuration.TurnDurationSeconds,
                CountsTowardStatistics = configuration.CountsTowardStatistics,
                OpponentKind = configuration.OpponentKind.ToString()
            })));

    /// <inheritdoc />
    public AvailableMatch[] GetAvailableMatches(string sessionToken) =>
        _boundary.Execute(nameof(GetAvailableMatches), () => _handler.Available(sessionToken)
            .Select(match => new AvailableMatch
            {
                MatchCode = match.Code!, CreatorDisplayName = match.Participants[0].DisplayName,
                TurnDurationSeconds = match.TurnDurationSeconds
            }).ToArray());

    /// <inheritdoc />
    public LobbySnapshot JoinMatch(string sessionToken, string matchCode) =>
        _boundary.Execute(nameof(JoinMatch), () =>
        {
            MatchRecord match = _handler.Join(sessionToken, matchCode);
            _channels.Publish(match.Id, (token, callback) =>
                callback.OnMatchStarted(MatchContractMapper.InitialState(_handler.Get(token, match.Id))));
            return MatchContractMapper.Lobby(match);
        });

    /// <inheritdoc />
    public LobbySnapshot ChangeTurnDuration(string sessionToken, long matchId, int turnDurationSeconds) =>
        _boundary.Execute(nameof(ChangeTurnDuration), () => MatchContractMapper.Lobby(
            _handler.ChangeTurnDuration(sessionToken, matchId, turnDurationSeconds)));

    /// <inheritdoc />
    public MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId) =>
        throw MatchBoundary.Unsupported(nameof(StartSystemMatch));

    /// <inheritdoc />
    public void CancelMatch(string sessionToken, long matchId) => _boundary.Execute(nameof(CancelMatch), () =>
    {
        _handler.Cancel(sessionToken, matchId);
        PublishClosed(matchId);
        return true;
    });

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

    private void PublishClosed(long id) => _channels.Publish(id, (token, callback) =>
    {
        _handler.Get(token, id);
        callback.OnLobbyClosed(id);
    });
}
