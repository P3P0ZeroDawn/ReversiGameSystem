using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Application.Matches;

public sealed class MatchApplicationService
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int MatchCodeLength = 6;
    private const int MinimumTurnDurationSeconds = 10;
    private const int MaximumTurnDurationSeconds = 120;
    private const string HumanOpponentKind = "Human";
    private readonly SessionStore _sessions;
    private readonly IMatchRepository _matches;
    private readonly IUtcClock _clock;
    private readonly IEmailSender? _emailSender;
    private readonly Dictionary<string, Dictionary<long, long>> _createdMatches =
        new Dictionary<string, Dictionary<long, long>>(StringComparer.Ordinal);
    private readonly HashSet<string> _sessionsPendingLobbyCleanup =
        new HashSet<string>(StringComparer.Ordinal);
    private readonly object _lobbySync = new object();

    public event Action<Match>? MatchStarted;

    public MatchApplicationService(
        SessionStore sessions,
        IMatchRepository matches,
        IUtcClock clock)
        : this(sessions, matches, clock, null)
    {
    }

    public MatchApplicationService(
        SessionStore sessions,
        IMatchRepository matches,
        IUtcClock clock,
        IEmailSender? emailSender)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _emailSender = emailSender;
    }

    public MatchResult<Match> CreateMatch(string token, MatchSettings? settings)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchStatus validation = ValidateSettings(settings);
        if (settings == null || validation != MatchStatus.Success)
        {
            return new MatchResult<Match>(validation);
        }

        AuthenticatedSession player = playerResult.Value;
        MatchResult<Match> creationResult;
        lock (_lobbySync)
        {
            CancelPriorLobbiesForToken(token);
            creationResult = _matches.CreateMatch(
                player,
                settings,
                _clock.UtcNow);
            if (creationResult.Value != null)
            {
                TrackCreatedMatch(token, player.AccountId, creationResult.Value.Id);
            }
        }

        return creationResult;
    }

    public MatchResult<Match> InviteToMatch(
        string token,
        long friendAccountId,
        MatchSettings? settings)
    {
        if (_sessions.IsGuestActive(token))
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value;
        if (friendAccountId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidFriendAccountId);
        }
        if (friendAccountId == player.AccountId)
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        MatchStatus validation = ValidateSettings(settings);
        if (settings == null || validation != MatchStatus.Success)
        {
            return new MatchResult<Match>(validation);
        }

        MatchResult<MatchInvitationPreparation> prepareResult;
        lock (_lobbySync)
        {
            CancelPriorLobbiesForToken(token);
            prepareResult = _matches.PreparePrivateInvitation(
                player,
                friendAccountId,
                settings,
                _clock.UtcNow);
            if (prepareResult.Value != null)
            {
                TrackCreatedMatch(token, player.AccountId, prepareResult.Value.Match.Id);
            }
        }

        if (prepareResult.Value == null)
        {
            return new MatchResult<Match>(prepareResult.Status);
        }

        MatchInvitationPreparation preparation = prepareResult.Value;
        SendInvitationOrCancel(token, player, preparation);
        return FinalizeInvitationDelivery(token, player, preparation);
    }

    public MatchStatus EvaluateLobbyAccess(string token)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(token);
        if (playerResult.Value == null)
        {
            return playerResult.Status;
        }

        return _matches.EvaluateLobbyAccess(playerResult.Value.AccountId);
    }

    public MatchResult<Match[]> GetAvailableMatches(string token)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match[]>(playerResult.Status);
        }

        return _matches.GetAvailableMatches(playerResult.Value.AccountId);
    }

    public MatchResult<Match> JoinMatch(string token, string code)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new MatchResult<Match>(MatchStatus.MissingSessionToken);
        }

        string normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length != MatchCodeLength
            || normalized.Any(character => !CodeAlphabet.Contains(character)))
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchCode);
        }

        if (_sessions.IsGuestActive(token))
        {
            MatchStatus guestStatus = _matches.ValidateGuestJoin(normalized);
            if (guestStatus != MatchStatus.Success)
            {
                return new MatchResult<Match>(guestStatus);
            }

            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchResult<Match> joinResult = _matches.JoinMatch(
            playerResult.Value,
            normalized,
            _clock.UtcNow);
        if (joinResult.Status == MatchStatus.Success && joinResult.Value != null)
        {
            Action<Match>? handler = MatchStarted;
            if (handler != null)
            {
                handler(joinResult.Value);
            }
        }

        return joinResult;
    }

    public MatchResult<Match> StartSystemMatch(string token, long matchId)
    {
        MatchResult<AuthenticatedSession> session = ResolveActiveSession(token);
        if (session.Value == null)
        {
            return new MatchResult<Match>(session.Status);
        }

        MatchResult<Match> result = _matches.StartSystemMatch(
            session.Value.AccountId, matchId, _clock.UtcNow);
        if (result.Value != null)
        {
            RemoveTrackedMatch(token, matchId);
            MatchStarted?.Invoke(result.Value);
        }

        return result;
    }

    public MatchResult<Match> GetParticipantMatch(string token, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchId);
        }

        return _matches.LoadParticipantMatch(playerResult.Value.AccountId, matchId);
    }

    public MatchResult<Match> ChangeTurnDuration(
        string token,
        long matchId,
        int seconds)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchId);
        }
        if (seconds < MinimumTurnDurationSeconds || seconds > MaximumTurnDurationSeconds)
        {
            return new MatchResult<Match>(MatchStatus.InvalidTurnDuration);
        }

        return _matches.ChangeTurnDuration(playerResult.Value.AccountId, matchId, seconds);
    }

    public MatchStatus CancelMatch(string token, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = ResolveActiveSession(token);
        if (playerResult.Value == null)
        {
            return playerResult.Status;
        }
        if (matchId <= 0)
        {
            return MatchStatus.InvalidMatchId;
        }

        MatchStatus status = _matches.CancelMatch(playerResult.Value.AccountId, matchId);
        if (status == MatchStatus.Success)
        {
            lock (_lobbySync)
            {
                RemoveTrackedMatch(token, matchId);
            }
        }

        return status;
    }

    public long[] CancelWaitingForSession(string token)
    {
        long[] cancelledIds;
        lock (_lobbySync)
        {
            _sessionsPendingLobbyCleanup.Add(token);
            cancelledIds = CancelTrackedMatches(token);
        }

        return cancelledIds;
    }

    public long[] CancelExpiredWaitingSessions()
    {
        List<long> cancelled = new List<long>();
        lock (_lobbySync)
        {
            string[] tokens = _createdMatches.Keys.ToArray();
            for (int index = 0; index < tokens.Length; index++)
            {
                string token = tokens[index];
                if (_sessionsPendingLobbyCleanup.Contains(token)
                    || _sessions.FindAuthenticated(token).Status != SessionOperationStatus.Success)
                {
                    cancelled.AddRange(CancelTrackedMatches(token));
                }
            }
        }

        return cancelled.ToArray();
    }

    private MatchResult<AuthenticatedSession> ResolveActiveSession(string token)
    {
        SessionResult<AuthenticatedSession> session =
            _sessions.FindAuthenticated(token);
        if (session.Status == SessionOperationStatus.MissingSessionToken)
        {
            return new MatchResult<AuthenticatedSession>(
                MatchStatus.MissingSessionToken);
        }

        if (session.Status != SessionOperationStatus.Success || session.Value == null)
        {
            return new MatchResult<AuthenticatedSession>(
                MatchStatus.InvalidSession);
        }

        return new MatchResult<AuthenticatedSession>(session.Value);
    }

    private static MatchStatus ValidateSettings(MatchSettings? settings)
    {
        MatchStatus status = MatchStatus.Success;
        if (settings == null
            || settings.OpponentKind != HumanOpponentKind
            || settings.CountsTowardStatistics)
        {
            status = MatchStatus.InvalidConfiguration;
        }
        else if (settings.TurnDurationSeconds < MinimumTurnDurationSeconds
            || settings.TurnDurationSeconds > MaximumTurnDurationSeconds)
        {
            status = MatchStatus.InvalidTurnDuration;
        }

        return status;
    }

    private void SendInvitationOrCancel(
        string token,
        AuthenticatedSession player,
        MatchInvitationPreparation preparation)
    {
        try
        {
            if (_emailSender == null)
            {
                throw new InvalidOperationException(
                    "No email sender is configured for match invitations.");
            }
            string matchCode = preparation.Match.Code ?? string.Empty;
            if (string.IsNullOrWhiteSpace(matchCode))
            {
                throw new InvalidOperationException(
                    "A prepared private match must contain a join code.");
            }
            EmailMessage email = CreateInvitationEmail(
                preparation.FriendEmail,
                player.Username,
                matchCode);
            _emailSender.Send(email);
        }
        catch (Exception deliveryException)
        {
            lock (_lobbySync)
            {
                RemoveTrackedMatch(token, preparation.Match.Id);
            }

            CancelInvitationOnDeliveryFailure(
                player.AccountId,
                preparation.Match.Id,
                deliveryException);
            throw new InvalidOperationException(
                "Failed to deliver the match invitation email.",
                deliveryException);
        }
    }

    private void CancelInvitationOnDeliveryFailure(
        long accountId,
        long matchId,
        Exception deliveryException)
    {
        try
        {
            CancelPreparedMatch(accountId, matchId);
        }
        catch (Exception cancellationException)
        {
            throw new AggregateException(
                "Invitation delivery and prepared match cancellation failed.",
                deliveryException,
                cancellationException);
        }
    }

    private MatchResult<Match> FinalizeInvitationDelivery(
        string token,
        AuthenticatedSession player,
        MatchInvitationPreparation preparation)
    {
        MatchResult<Match> deliveryResult;
        lock (_lobbySync)
        {
            bool sessionRemainsValid =
                _sessions.FindAuthenticated(token).Status == SessionOperationStatus.Success;
            bool preparationRemainsTracked = IsMatchTracked(token, preparation.Match.Id);
            if (_sessionsPendingLobbyCleanup.Contains(token)
                || !preparationRemainsTracked
                || !sessionRemainsValid)
            {
                CancelPreparedMatch(player.AccountId, preparation.Match.Id);
                RemoveTrackedMatch(token, preparation.Match.Id);
                deliveryResult = new MatchResult<Match>(MatchStatus.MatchNotWaiting);
            }
            else
            {
                deliveryResult = _matches.MarkInvitationDelivered(
                    player.AccountId,
                    preparation.Match.Id,
                    _clock.UtcNow);
            }
        }

        return deliveryResult;
    }

    private void CancelPreparedMatch(long accountId, long matchId)
    {
        MatchStatus status = _matches.CancelMatch(accountId, matchId);
        if (status != MatchStatus.Success && status != MatchStatus.MatchNotWaiting)
        {
            throw new InvalidOperationException("Failed to cancel prepared match: " + status);
        }
    }

    private void CancelPriorLobbiesForToken(string token)
    {
        if (_sessionsPendingLobbyCleanup.Contains(token)
            || _createdMatches.ContainsKey(token))
        {
            CancelTrackedMatches(token);
        }
    }

    private void TrackCreatedMatch(string token, long accountId, long matchId)
    {
        if (!_createdMatches.TryGetValue(token, out Dictionary<long, long>? matches))
        {
            matches = new Dictionary<long, long>();
            _createdMatches.Add(token, matches);
        }

        matches[matchId] = accountId;
    }

    private bool IsMatchTracked(string token, long matchId)
    {
        return _createdMatches.TryGetValue(token, out Dictionary<long, long>? matches)
            && matches.ContainsKey(matchId);
    }

    private void RemoveTrackedMatch(string token, long matchId)
    {
        if (_createdMatches.TryGetValue(token, out Dictionary<long, long>? matches))
        {
            matches.Remove(matchId);
            if (matches.Count == 0)
            {
                _createdMatches.Remove(token);
                _sessionsPendingLobbyCleanup.Remove(token);
            }
        }
    }

    private long[] CancelTrackedMatches(string token)
    {
        if (!_createdMatches.TryGetValue(token, out Dictionary<long, long>? matches))
        {
            _sessionsPendingLobbyCleanup.Remove(token);
            return Array.Empty<long>();
        }

        List<long> cancelled = new List<long>();
        KeyValuePair<long, long>[] tracked = matches.ToArray();
        for (int index = 0; index < tracked.Length; index++)
        {
            KeyValuePair<long, long> match = tracked[index];
            MatchStatus status = _matches.CancelMatch(match.Value, match.Key);
            if (status == MatchStatus.Success)
            {
                cancelled.Add(match.Key);
                matches.Remove(match.Key);
            }
            else if (status == MatchStatus.MatchNotWaiting || status == MatchStatus.MatchNotFound)
            {
                matches.Remove(match.Key);
            }
            else
            {
                throw new InvalidOperationException("Failed to cancel tracked match: " + status);
            }
        }

        if (matches.Count == 0)
        {
            _createdMatches.Remove(token);
            _sessionsPendingLobbyCleanup.Remove(token);
        }

        return cancelled.ToArray();
    }

    private static EmailMessage CreateInvitationEmail(
        string recipientEmail,
        string creatorDisplayName,
        string matchCode)
    {
        const string Subject = "Reversi: Invitación a partida / Match invitation";
        string body = "Hello / Hola,\n\n"
            + creatorDisplayName + " has invited you to play a match of Reversi. / "
            + creatorDisplayName + " te ha invitado a jugar una partida de Reversi.\n\n"
            + "Match code / Código de partida: " + matchCode + "\n\n"
            + "Instructions / Instrucciones:\n"
            + "1. Sign in to your Reversi account. / Inicia sesión en tu cuenta de Reversi.\n"
            + "2. Go to 'Join with code' in Available Matches and enter this code. / "
            + "Ve a 'Unirse con código' en Partidas Disponibles e ingresa este código.\n\n"
            + "«Puedes unirte con este código mientras el creador siga esperando» / "
            + "«You can join with this code while the creator is still waiting»\n";

        EmailMessage message = new EmailMessage(recipientEmail, Subject, body);
        return message;
    }
}
