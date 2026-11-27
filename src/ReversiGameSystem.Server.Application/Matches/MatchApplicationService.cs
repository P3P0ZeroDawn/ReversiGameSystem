using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
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
    private readonly ITemplatedEmailSender? _emailSender;
    private readonly ILogger<MatchApplicationService> _logger;
    private readonly Dictionary<string, Dictionary<long, MatchPlayer>> _createdMatches =
        new Dictionary<string, Dictionary<long, MatchPlayer>>(StringComparer.Ordinal);
    private readonly HashSet<string> _sessionsPendingLobbyCleanup =
        new HashSet<string>(StringComparer.Ordinal);
    private readonly object _lobbySync = new object();

    public event Action<Match>? MatchStarted;

    public MatchApplicationService(
        SessionStore sessions,
        IMatchRepository matches,
        IUtcClock clock,
        ILogger<MatchApplicationService> logger)
        : this(sessions, matches, clock, null, logger)
    {
    }

    public MatchApplicationService(
        SessionStore sessions,
        IMatchRepository matches,
        IUtcClock clock,
        ITemplatedEmailSender? emailSender,
        ILogger<MatchApplicationService> logger)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _emailSender = emailSender;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MatchResult<Match> CreateMatch(string token, MatchSettings? settings)
    {
        MatchResult<MatchPlayer> playerResult = MatchPlayer.ResolveSession(_sessions, token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchStatus validation = ValidateSettings(settings);
        if (settings == null || validation != MatchStatus.Success)
        {
            return new MatchResult<Match>(validation);
        }

        MatchPlayer player = playerResult.Value;
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
                player.RegisterParticipation(creationResult.Value);
                TrackCreatedMatch(token, player, creationResult.Value.Id);
            }
        }

        if (creationResult.Value != null)
        {
            _logger.LogInformation(
                "Match {MatchId} created by {Actor} with turn duration {TurnDurationSeconds}s.",
                creationResult.Value.Id,
                FormatActor(player),
                settings.TurnDurationSeconds);
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
            _logger.LogWarning(
                "Private match invitation rejected for guest session: {Reason}.",
                MatchStatus.Unauthorized);
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
                TrackCreatedMatch(token, new MatchPlayer(player), prepareResult.Value.Match.Id);
            }
        }

        if (prepareResult.Value == null)
        {
            return new MatchResult<Match>(prepareResult.Status);
        }

        MatchInvitationPreparation preparation = prepareResult.Value;
        SendInvitationOrCancel(token, player, preparation);
        MatchResult<Match> invitationResult = FinalizeInvitationDelivery(token, player, preparation);
        if (invitationResult.Status == MatchStatus.Success && invitationResult.Value != null)
        {
            _logger.LogInformation(
                "Private match {MatchId} created by account {AccountId} for friend {FriendAccountId}.",
                invitationResult.Value.Id,
                player.AccountId,
                friendAccountId);
        }

        return invitationResult;
    }

    public MatchStatus EvaluateLobbyAccess(string token)
    {
        MatchResult<MatchPlayer> playerResult = MatchPlayer.ResolveSession(_sessions, token);
        if (playerResult.Value == null)
        {
            return playerResult.Status;
        }

        return playerResult.Value.IsGuest ? MatchStatus.Success
            : _matches.EvaluateLobbyAccess(playerResult.Value.AccountId!.Value);
    }

    public MatchResult<Match[]> GetAvailableMatches(string token)
    {
        MatchResult<MatchPlayer> playerResult = MatchPlayer.ResolveSession(_sessions, token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match[]>(playerResult.Status);
        }

        return _matches.GetAvailableMatches(playerResult.Value);
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

        MatchResult<MatchPlayer> playerResult = MatchPlayer.ResolveSession(_sessions, token);
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
            playerResult.Value.RegisterParticipation(joinResult.Value);
            _logger.LogInformation(
                "Match {MatchId} joined by {Actor} and started.",
                joinResult.Value.Id,
                FormatActor(playerResult.Value));
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
        MatchResult<MatchPlayer> session = MatchPlayer.ResolveSession(_sessions, token);
        if (session.Value == null)
        {
            return new MatchResult<Match>(session.Status);
        }

        MatchResult<Match> result = _matches.StartSystemMatch(
            session.Value, matchId, _clock.UtcNow);
        if (result.Value != null)
        {
            RemoveTrackedMatch(token, matchId);
            _logger.LogInformation(
                "System match {MatchId} started by {Actor}.",
                result.Value.Id,
                FormatActor(session.Value));
            MatchStarted?.Invoke(result.Value);
        }

        return result;
    }

    public MatchResult<Match> GetParticipantMatch(string token, long matchId)
    {
        MatchResult<MatchPlayer> playerResult = MatchPlayer.ResolveSession(_sessions, token);
        if (playerResult.Value == null)
        {
            return new MatchResult<Match>(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchId);
        }

        return _matches.LoadParticipantMatch(playerResult.Value, matchId);
    }

    public MatchResult<Match> ChangeTurnDuration(
        string token,
        long matchId,
        int seconds)
    {
        MatchResult<MatchPlayer> playerResult = MatchPlayer.ResolveSession(_sessions, token);
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

        return _matches.ChangeTurnDuration(playerResult.Value, matchId, seconds);
    }

    public MatchStatus CancelMatch(string token, long matchId)
    {
        MatchResult<MatchPlayer> playerResult = MatchPlayer.ResolveSession(_sessions, token);
        if (playerResult.Value == null)
        {
            return playerResult.Status;
        }
        if (matchId <= 0)
        {
            return MatchStatus.InvalidMatchId;
        }

        MatchStatus status = _matches.CancelMatch(playerResult.Value, matchId);
        if (status == MatchStatus.Success)
        {
            lock (_lobbySync)
            {
                RemoveTrackedMatch(token, matchId);
            }

            _logger.LogInformation(
                "Match {MatchId} cancelled by {Actor}.",
                matchId,
                FormatActor(playerResult.Value));
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

        if (cancelledIds.Length > 0)
        {
            _logger.LogInformation(
                "Cancelled {MatchCount} waiting lobby match(es) for closed session.",
                cancelledIds.Length);
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
                    || MatchPlayer.ResolveSession(_sessions, token).Status != MatchStatus.Success)
                {
                    cancelled.AddRange(CancelTrackedMatches(token));
                }
            }
        }

        if (cancelled.Count > 0)
        {
            _logger.LogInformation(
                "Cancelled {MatchCount} expired waiting lobby match(es).",
                cancelled.Count);
        }

        return cancelled.ToArray();
    }

    private static string FormatActor(MatchPlayer player)
    {
        return LogSanitizer.FormatMatchActor(
            player.AccountId,
            player.DisplayName,
            player.IsGuest);
    }

    private MatchResult<AuthenticatedSession> ResolveActiveSession(string token)
    {
        SessionResult<AuthenticatedSession> session =
            _sessions.FindAuthenticated(token);
        if (session.Status == SessionStatus.MissingSessionToken)
        {
            return new MatchResult<AuthenticatedSession>(
                MatchStatus.MissingSessionToken);
        }

        if (session.Status != SessionStatus.Success || session.Value == null)
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
            SendInvitationEmail(preparation.FriendEmail, player.Username, matchCode);
        }
        catch (Exception deliveryException)
        {
            lock (_lobbySync)
            {
                RemoveTrackedMatch(token, preparation.Match.Id);
            }

            CancelInvitationOnDeliveryFailure(
                new MatchPlayer(player),
                preparation.Match.Id,
                deliveryException);
            throw new InvalidOperationException(
                "Failed to deliver the match invitation email.",
                deliveryException);
        }
    }

    private void CancelInvitationOnDeliveryFailure(
        MatchPlayer player,
        long matchId,
        Exception deliveryException)
    {
        try
        {
            CancelPreparedMatch(player, matchId);
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
                _sessions.FindAuthenticated(token).Status == SessionStatus.Success;
            bool preparationRemainsTracked = IsMatchTracked(token, preparation.Match.Id);
            if (_sessionsPendingLobbyCleanup.Contains(token)
                || !preparationRemainsTracked
                || !sessionRemainsValid)
            {
                CancelPreparedMatch(new MatchPlayer(player), preparation.Match.Id);
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

    private void CancelPreparedMatch(MatchPlayer player, long matchId)
    {
        MatchStatus status = _matches.CancelMatch(player, matchId);
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

    private void TrackCreatedMatch(string token, MatchPlayer player, long matchId)
    {
        if (!_createdMatches.TryGetValue(token, out Dictionary<long, MatchPlayer>? matches))
        {
            matches = new Dictionary<long, MatchPlayer>();
            _createdMatches.Add(token, matches);
        }

        matches[matchId] = player;
    }

    private bool IsMatchTracked(string token, long matchId)
    {
        return _createdMatches.TryGetValue(token, out Dictionary<long, MatchPlayer>? matches)
            && matches.ContainsKey(matchId);
    }

    private void RemoveTrackedMatch(string token, long matchId)
    {
        if (_createdMatches.TryGetValue(token, out Dictionary<long, MatchPlayer>? matches))
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
        if (!_createdMatches.TryGetValue(token, out Dictionary<long, MatchPlayer>? matches))
        {
            _sessionsPendingLobbyCleanup.Remove(token);
            return Array.Empty<long>();
        }

        List<long> cancelled = new List<long>();
        KeyValuePair<long, MatchPlayer>[] tracked = matches.ToArray();
        for (int index = 0; index < tracked.Length; index++)
        {
            KeyValuePair<long, MatchPlayer> match = tracked[index];
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

    private void SendInvitationEmail(
        string recipientEmail,
        string creatorDisplayName,
        string matchCode)
    {
        Dictionary<string, string> templateValues = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "CreatorDisplayName", creatorDisplayName },
            { "MatchCode", matchCode }
        };

        _emailSender!.SendTemplatedEmail(
            recipientEmail,
            EmailTemplateNames.MatchInvitation,
            templateValues);
    }
}
