using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes authorized initial match information without simulating gameplay writes.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class GameplayMatchService : IGameplayMatchService, IDisposable
{
    private readonly MatchHandler _handler;
    private readonly IAuthenticatedSessionStore _sessions;
    private readonly MatchBoundary _boundary;
    private readonly MatchChannels<IGameplayMatchServiceCallback> _channels;

    /// <summary>Creates the initial match boundary.</summary>
    public GameplayMatchService(MatchHandler handler, IAuthenticatedSessionStore sessions,
        ILogger<GameplayMatchService> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _boundary = new MatchBoundary(logger);
        _channels = new MatchChannels<IGameplayMatchServiceCallback>((token, id) =>
        {
            try
            {
                MatchContractMapper.RequireStarted(_handler.Get(token, id)); return true;
            }
            catch (SessionRejectedException)
            {
                return false;
            }
            catch (MatchRejectedException)
            {
                return false;
            }
        }, _ => { }, _boundary);
    }

    /// <inheritdoc />
    public void OpenMatchChannel(string sessionToken, long matchId) =>
        _boundary.Execute(nameof(OpenMatchChannel), () =>
        {
            MatchContractMapper.RequireStarted(_handler.Get(sessionToken, matchId));
            _channels.Register(sessionToken, matchId);
            return true;
        });

    /// <inheritdoc />
    public MatchInformation GetMatchInformation(string sessionToken, long matchId) =>
        _boundary.Execute(nameof(GetMatchInformation), () =>
        {
            MatchRecord match = _handler.Get(sessionToken, matchId);
            MatchContractMapper.RequireStarted(match);
            if (!_sessions.TryGet(sessionToken, out AuthenticatedSessionRecord? session) || session == null)
            {
                throw new SessionRejectedException();
            }
            return new MatchInformation
            {
                MatchId = match.Id, Participants = MatchContractMapper.Participants(match),
                TurnDurationSeconds = match.TurnDurationSeconds,
                AssignedSeatNumber = match.Participants.Single(player => player.AccountId == session.AccountId).SeatNumber
            };
        });

    /// <inheritdoc />
    public MatchStateSnapshot GetMatchState(string sessionToken, long matchId) =>
        _boundary.Execute(nameof(GetMatchState), () =>
            MatchContractMapper.InitialState(_handler.Get(sessionToken, matchId)));
    /// <inheritdoc />
    public MatchStateSnapshot PlacePiece(string sessionToken, PiecePlacement placement) => throw MatchBoundary.Unsupported(nameof(PlacePiece));
    /// <inheritdoc />
    public MatchConclusion ForfeitMatch(string sessionToken, long matchId) => throw MatchBoundary.Unsupported(nameof(ForfeitMatch));
    /// <inheritdoc />
    public MatchStateSnapshot ObserveMatch(string sessionToken, long matchId) => throw MatchBoundary.Unsupported(nameof(ObserveMatch));
    /// <inheritdoc />
    public MatchSummary[] GetMatchHistory(string sessionToken) => throw MatchBoundary.Unsupported(nameof(GetMatchHistory));
    /// <inheritdoc />
    public MatchDetails GetMatchDetails(string sessionToken, long matchId) => throw MatchBoundary.Unsupported(nameof(GetMatchDetails));
    /// <inheritdoc />
    public MoveRecord[] GetMoveHistory(string sessionToken, long matchId) => throw MatchBoundary.Unsupported(nameof(GetMoveHistory));
    /// <summary>Removes all match subscriptions for a revoked session.</summary>
    public void RemoveSession(string sessionToken) => _channels.RemoveSession(sessionToken);
    /// <inheritdoc />
    public void Dispose() => _channels.Dispose();
}
