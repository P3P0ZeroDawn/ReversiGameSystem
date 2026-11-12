using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Coordinates authorized lobby and initial match lifecycle operations.</summary>
public sealed class MatchApplicationService
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly ISessionRepository _sessions;
    private readonly IMatchRepository _matches;
    private readonly IUtcClock _clock;
    private readonly IMatchInvitationEmailSender? _emailSender;
    private readonly Dictionary<string, Dictionary<long, long>> _createdMatches =
        new Dictionary<string, Dictionary<long, long>>(StringComparer.Ordinal);
    private readonly HashSet<string> _sessionsPendingLobbyCleanup =
        new HashSet<string>(StringComparer.Ordinal);
    private readonly object _lobbySync = new object();

    /// <summary>Notifies observers when a waiting match transitions to active gameplay.</summary>
    public event Action<Match>? MatchStarted;

    /// <summary>Creates the match application service without invitation delivery.</summary>
    public MatchApplicationService(
        ISessionRepository sessions,
        IMatchRepository matches,
        IUtcClock clock)
        : this(sessions, matches, clock, null)
    {
    }

    /// <summary>Creates the match application service.</summary>
    public MatchApplicationService(
        ISessionRepository sessions,
        IMatchRepository matches,
        IUtcClock clock,
        IMatchInvitationEmailSender? emailSender)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _matches = matches ?? throw new ArgumentNullException(nameof(matches));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _emailSender = emailSender;
    }

    /// <summary>Creates a waiting lobby and its creator seat atomically.</summary>
    public MatchResult<Match> Create(string token, MatchSettings? settings)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        MatchStatus validation = ValidateSettings(settings);
        if (validation != MatchStatus.Success)
        {
            return new MatchResult<Match>(validation);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        MatchSettings acceptedSettings = settings
            ?? throw new InvalidOperationException(
                "Successful settings validation requires a value.");
        lock (_lobbySync)
        {
            PrepareCreation(token);
            MatchResult<Match> creationResult = _matches.Create(
                player,
                acceptedSettings,
                _clock.UtcNow);
            if (creationResult.Status != MatchStatus.Success)
            {
                return new MatchResult<Match>(creationResult.Status);
            }

            Match match = creationResult.Value
                ?? throw new InvalidOperationException(
                    "A successful match result must contain its value.");
            TrackCreatedMatch(token, player.AccountId, match.Id);
            return new MatchResult<Match>(match);
        }
    }

    /// <summary>Creates and emails a private waiting match for an accepted friend.</summary>
    public MatchResult<Match> Invite(
        string token,
        long friendAccountId,
        MatchSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new MatchResult<Match>(MatchStatus.MissingSessionToken);
        }

        if (_sessions.TryGetGuest(token, out GuestSession? guest)
            && guest != null
            && guest.ExpiresAtUtc > _clock.UtcNow)
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        MatchResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        if (friendAccountId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidFriendAccountId);
        }
        if (friendAccountId == player.AccountId)
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        MatchStatus validation = ValidateSettings(settings);
        if (validation != MatchStatus.Success)
        {
            return new MatchResult<Match>(validation);
        }

        MatchSettings acceptedSettings = settings
            ?? throw new InvalidOperationException(
                "Successful settings validation requires a value.");
        MatchInvitationPreparation preparation;
        lock (_lobbySync)
        {
            PrepareCreation(token);
            MatchResult<MatchInvitationPreparation> prepareResult = _matches.PreparePrivateInvitation(
                player,
                friendAccountId,
                acceptedSettings,
                _clock.UtcNow);
            if (prepareResult.Status != MatchStatus.Success)
            {
                return new MatchResult<Match>(prepareResult.Status);
            }

            preparation = prepareResult.Value
                ?? throw new InvalidOperationException(
                    "A successful match result must contain its value.");
            TrackCreatedMatch(token, player.AccountId, preparation.Match.Id);
        }

        SendInvitationOrCancel(token, player, preparation);
        return FinalizeInvitationDelivery(token, player, preparation);
    }

    /// <summary>Verifies that a live registered session belongs to an active account.</summary>
    public MatchStatus EvaluateLobbyAccess(string token)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchStatus.Success)
        {
            return playerResult.Status;
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        return _matches.EvaluateLobbyAccess(player.AccountId);
    }

    /// <summary>Lists public waiting lobbies available to the caller.</summary>
    public MatchResult<Match[]> Available(string token)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match[]>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        return _matches.Available(player.AccountId);
    }

    /// <summary>Occupies the second seat and starts a waiting match atomically.</summary>
    public MatchResult<Match> Join(string token, string code)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new MatchResult<Match>(MatchStatus.MissingSessionToken);
        }

        string normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length != 6
            || normalized.Any(character => !CodeAlphabet.Contains(character)))
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchCode);
        }

        if (_sessions.TryGetGuest(token, out GuestSession? guest)
            && guest != null
            && guest.ExpiresAtUtc > _clock.UtcNow)
        {
            MatchStatus guestStatus = _matches.ValidateGuestJoin(normalized);
            if (guestStatus != MatchStatus.Success)
            {
                return new MatchResult<Match>(guestStatus);
            }

            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        MatchResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        MatchResult<Match> joinResult = _matches.Join(player, normalized, _clock.UtcNow);
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

    /// <summary>Loads confirmed match data for an authorized participant.</summary>
    public MatchResult<Match> Get(string token, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchId);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        return _matches.Get(player.AccountId, matchId);
    }

    /// <summary>Changes the turn duration of the caller's waiting match.</summary>
    public MatchResult<Match> ChangeTurnDuration(
        string token,
        long matchId,
        int seconds)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchStatus.Success)
        {
            return new MatchResult<Match>(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return new MatchResult<Match>(MatchStatus.InvalidMatchId);
        }
        if (seconds < 10 || seconds > 120)
        {
            return new MatchResult<Match>(MatchStatus.InvalidTurnDuration);
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        return _matches.ChangeTurnDuration(player.AccountId, matchId, seconds);
    }

    /// <summary>Cancels a waiting match owned by the caller.</summary>
    public MatchStatus Cancel(string token, long matchId)
    {
        MatchResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchStatus.Success)
        {
            return playerResult.Status;
        }
        if (matchId <= 0)
        {
            return MatchStatus.InvalidMatchId;
        }

        AuthenticatedSession player = playerResult.Value
            ?? throw new InvalidOperationException(
                "A successful match result must contain its value.");
        MatchStatus status = _matches.Cancel(player.AccountId, matchId);
        if (status == MatchStatus.Success)
        {
            lock (_lobbySync)
            {
                RemoveTrackedMatch(token, matchId);
            }
        }

        return status;
    }

    /// <summary>Cancels waiting lobbies created by a closed session.</summary>
    public long[] CancelWaitingForSession(string token)
    {
        lock (_lobbySync)
        {
            _sessionsPendingLobbyCleanup.Add(token);
            return CancelTracked(token);
        }
    }

    /// <summary>Cancels tracked lobbies whose creating session expired or was revoked.</summary>
    public long[] CancelExpiredWaitingSessions()
    {
        lock (_lobbySync)
        {
            List<long> cancelled = new List<long>();
            string[] tokens = _createdMatches.Keys.ToArray();
            for (int index = 0; index < tokens.Length; index++)
            {
                string token = tokens[index];
                if (_sessionsPendingLobbyCleanup.Contains(token)
                    || !_sessions.TryGet(token, out AuthenticatedSession? session)
                    || session == null
                    || session.ExpiresAtUtc <= _clock.UtcNow)
                {
                    cancelled.AddRange(CancelTracked(token));
                }
            }

            return cancelled.ToArray();
        }
    }

    private MatchResult<AuthenticatedSession> GetPlayer(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new MatchResult<AuthenticatedSession>(
                MatchStatus.MissingSessionToken);
        }
        if (!_sessions.TryGet(token, out AuthenticatedSession? player)
            || player == null
            || player.ExpiresAtUtc <= _clock.UtcNow)
        {
            return new MatchResult<AuthenticatedSession>(
                MatchStatus.InvalidSession);
        }

        return new MatchResult<AuthenticatedSession>(player);
    }

    private static MatchStatus ValidateSettings(MatchSettings? settings)
    {
        MatchStatus status = MatchStatus.Success;
        if (settings == null
            || settings.OpponentKind != "Human"
            || settings.CountsTowardStatistics)
        {
            status = MatchStatus.InvalidConfiguration;
        }
        else if (settings.TurnDurationSeconds < 10
            || settings.TurnDurationSeconds > 120)
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
            MatchInvitationEmail email = new MatchInvitationEmail(
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

            try
            {
                MatchStatus cancellationStatus = _matches.Cancel(player.AccountId, preparation.Match.Id);
                if (cancellationStatus != MatchStatus.Success && cancellationStatus != MatchStatus.MatchNotWaiting)
                {
                    throw new InvalidOperationException("Prepared match cancellation rejected: " + cancellationStatus);
                }
            }
            catch (Exception cancellationException)
            {
                throw new AggregateException(
                    "Invitation delivery and prepared match cancellation failed.",
                    deliveryException,
                    cancellationException);
            }

            throw new InvalidOperationException(
                "Failed to deliver the match invitation email.",
                deliveryException);
        }
    }

    private MatchResult<Match> FinalizeInvitationDelivery(
        string token,
        AuthenticatedSession player,
        MatchInvitationPreparation preparation)
    {
        lock (_lobbySync)
        {
            bool sessionRemainsValid = _sessions.TryGet(
                token,
                out AuthenticatedSession? currentSession)
                && currentSession != null
                && currentSession.ExpiresAtUtc > _clock.UtcNow;
            bool preparationRemainsTracked = IsTracked(token, preparation.Match.Id);
            if (_sessionsPendingLobbyCleanup.Contains(token)
                || !preparationRemainsTracked
                || !sessionRemainsValid)
            {
                CancelPreparedMatch(player.AccountId, preparation.Match.Id);
                RemoveTrackedMatch(token, preparation.Match.Id);
                return new MatchResult<Match>(MatchStatus.MatchNotWaiting);
            }

            MatchResult<Match> deliveredResult = _matches.MarkInvitationDelivered(
                player.AccountId,
                preparation.Match.Id,
                _clock.UtcNow);
            return deliveredResult;
        }
    }

    private void CancelPreparedMatch(long accountId, long matchId)
    {
        MatchStatus status = _matches.Cancel(accountId, matchId);
        if (status != MatchStatus.Success && status != MatchStatus.MatchNotWaiting)
        {
            throw new InvalidOperationException("Failed to cancel prepared match: " + status);
        }
    }

    private void PrepareCreation(string token)
    {
        if (_sessionsPendingLobbyCleanup.Contains(token)
            || _createdMatches.ContainsKey(token))
        {
            CancelTracked(token);
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

    private bool IsTracked(string token, long matchId)
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

    private long[] CancelTracked(string token)
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
            MatchStatus status = _matches.Cancel(match.Value, match.Key);
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
}
