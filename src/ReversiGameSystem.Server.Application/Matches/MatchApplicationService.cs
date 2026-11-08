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
    public MatchOperationResult<Match> Create(string token, MatchSettings? settings)
    {
        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(playerResult.Status);
        }

        MatchOperationStatus validation = ValidateSettings(settings);
        if (validation != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(validation);
        }

        AuthenticatedSession player = playerResult.GetValue();
        MatchSettings acceptedSettings = settings
            ?? throw new InvalidOperationException(
                "Successful settings validation requires a value.");
        lock (_lobbySync)
        {
            PrepareCreation(token);
            MatchRepositoryResult<Match> creationResult = _matches.Create(
                player,
                acceptedSettings,
                _clock.UtcNow);
            if (creationResult.Status != MatchRepositoryStatus.Success)
            {
                return MatchOperationResult<Match>.CreateFailure(
                    MapRepositoryStatus(creationResult.Status));
            }

            Match match = creationResult.GetValue();
            TrackCreatedMatch(token, player.AccountId, match.Id);
            return MatchOperationResult<Match>.CreateSuccess(match);
        }
    }

    /// <summary>Creates and emails a private waiting match for an accepted friend.</summary>
    public MatchOperationResult<Match> Invite(
        string token,
        long friendAccountId,
        MatchSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.MissingSessionToken);
        }

        if (_sessions.TryGetGuest(token, out GuestSession? guest)
            && guest != null
            && guest.ExpiresAtUtc > _clock.UtcNow)
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.Unauthorized);
        }

        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(playerResult.Status);
        }

        AuthenticatedSession player = playerResult.GetValue();
        if (friendAccountId <= 0)
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.InvalidFriendAccountId);
        }
        if (friendAccountId == player.AccountId)
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.Unauthorized);
        }

        MatchOperationStatus validation = ValidateSettings(settings);
        if (validation != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(validation);
        }

        MatchSettings acceptedSettings = settings
            ?? throw new InvalidOperationException(
                "Successful settings validation requires a value.");
        MatchInvitationPreparation preparation;
        lock (_lobbySync)
        {
            PrepareCreation(token);
            MatchRepositoryResult<MatchInvitationPreparation> prepareResult = _matches.PreparePrivateInvitation(
                player,
                friendAccountId,
                acceptedSettings,
                _clock.UtcNow);
            if (prepareResult.Status != MatchRepositoryStatus.Success)
            {
                return MatchOperationResult<Match>.CreateFailure(
                    MapRepositoryStatus(prepareResult.Status));
            }

            preparation = prepareResult.GetValue();
            TrackCreatedMatch(token, player.AccountId, preparation.Match.Id);
        }

        SendInvitationOrCancel(token, player, preparation);
        return FinalizeInvitationDelivery(token, player, preparation);
    }

    /// <summary>Verifies that a live registered session belongs to an active account.</summary>
    public MatchOperationStatus Authorize(string token)
    {
        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return playerResult.Status;
        }

        MatchRepositoryStatus status = _matches.Authorize(playerResult.GetValue().AccountId);
        return MapRepositoryStatus(status);
    }

    /// <summary>Lists public waiting lobbies available to the caller.</summary>
    public MatchOperationResult<Match[]> Available(string token)
    {
        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match[]>.CreateFailure(playerResult.Status);
        }

        MatchRepositoryResult<Match[]> availableResult = _matches.Available(
            playerResult.GetValue().AccountId);
        if (availableResult.Status != MatchRepositoryStatus.Success)
        {
            return MatchOperationResult<Match[]>.CreateFailure(
                MapRepositoryStatus(availableResult.Status));
        }

        return MatchOperationResult<Match[]>.CreateSuccess(availableResult.GetValue());
    }

    /// <summary>Occupies the second seat and starts a waiting match atomically.</summary>
    public MatchOperationResult<Match> Join(string token, string code)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.MissingSessionToken);
        }

        string normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length != 6
            || normalized.Any(character => !CodeAlphabet.Contains(character)))
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.InvalidMatchCode);
        }

        if (_sessions.TryGetGuest(token, out GuestSession? guest)
            && guest != null
            && guest.ExpiresAtUtc > _clock.UtcNow)
        {
            MatchRepositoryStatus guestStatus = _matches.ValidateJoinForGuest(normalized);
            if (guestStatus != MatchRepositoryStatus.Success)
            {
                return MatchOperationResult<Match>.CreateFailure(MapRepositoryStatus(guestStatus));
            }

            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.Unauthorized);
        }

        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(playerResult.Status);
        }

        MatchRepositoryResult<Match> joinResult = _matches.Join(
            playerResult.GetValue(),
            normalized,
            _clock.UtcNow);
        if (joinResult.Status != MatchRepositoryStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(MapRepositoryStatus(joinResult.Status));
        }

        return MatchOperationResult<Match>.CreateSuccess(joinResult.GetValue());
    }

    /// <summary>Loads confirmed match data for an authorized participant.</summary>
    public MatchOperationResult<Match> Get(string token, long matchId)
    {
        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.InvalidMatchId);
        }

        MatchRepositoryResult<Match> getResult = _matches.Get(
            playerResult.GetValue().AccountId,
            matchId);
        if (getResult.Status != MatchRepositoryStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(
                MapRepositoryStatus(getResult.Status));
        }

        return MatchOperationResult<Match>.CreateSuccess(getResult.GetValue());
    }

    /// <summary>Changes the turn duration of the caller's waiting match.</summary>
    public MatchOperationResult<Match> ChangeTurnDuration(
        string token,
        long matchId,
        int seconds)
    {
        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.InvalidMatchId);
        }
        if (seconds < 10 || seconds > 120)
        {
            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.InvalidTurnDuration);
        }

        MatchRepositoryResult<Match> changeResult = _matches.ChangeTurnDuration(
            playerResult.GetValue().AccountId,
            matchId,
            seconds);
        if (changeResult.Status != MatchRepositoryStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(
                MapRepositoryStatus(changeResult.Status));
        }

        return MatchOperationResult<Match>.CreateSuccess(changeResult.GetValue());
    }

    /// <summary>Cancels a waiting match owned by the caller.</summary>
    public MatchOperationStatus Cancel(string token, long matchId)
    {
        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return playerResult.Status;
        }
        if (matchId <= 0)
        {
            return MatchOperationStatus.InvalidMatchId;
        }

        MatchRepositoryStatus status = _matches.Cancel(playerResult.GetValue().AccountId, matchId);
        if (status == MatchRepositoryStatus.Success)
        {
            lock (_lobbySync)
            {
                RemoveTrackedMatch(token, matchId);
            }
        }

        return MapRepositoryStatus(status);
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

    private MatchOperationResult<AuthenticatedSession> GetPlayer(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return MatchOperationResult<AuthenticatedSession>.CreateFailure(
                MatchOperationStatus.MissingSessionToken);
        }
        if (!_sessions.TryGet(token, out AuthenticatedSession? player)
            || player == null
            || player.ExpiresAtUtc <= _clock.UtcNow)
        {
            return MatchOperationResult<AuthenticatedSession>.CreateFailure(
                MatchOperationStatus.InvalidSession);
        }

        return MatchOperationResult<AuthenticatedSession>.CreateSuccess(player);
    }

    private static MatchOperationStatus ValidateSettings(MatchSettings? settings)
    {
        MatchOperationStatus status = MatchOperationStatus.Success;
        if (settings == null
            || settings.OpponentKind != "Human"
            || settings.CountsTowardStatistics)
        {
            status = MatchOperationStatus.InvalidConfiguration;
        }
        else if (settings.TurnDurationSeconds < 10
            || settings.TurnDurationSeconds > 120)
        {
            status = MatchOperationStatus.InvalidTurnDuration;
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
                MatchRepositoryStatus cancellationStatus = _matches.Cancel(player.AccountId, preparation.Match.Id);
                if (cancellationStatus != MatchRepositoryStatus.Success && cancellationStatus != MatchRepositoryStatus.MatchNotWaiting)
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

    private MatchOperationResult<Match> FinalizeInvitationDelivery(
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
                return MatchOperationResult<Match>.CreateFailure(
                    MatchOperationStatus.MatchNotWaiting);
            }

            MatchRepositoryResult<Match> deliveredResult = _matches.MarkInvitationDelivered(
                player.AccountId,
                preparation.Match.Id,
                _clock.UtcNow);
            if (deliveredResult.Status != MatchRepositoryStatus.Success)
            {
                return MatchOperationResult<Match>.CreateFailure(
                    MapRepositoryStatus(deliveredResult.Status));
            }

            return MatchOperationResult<Match>.CreateSuccess(deliveredResult.GetValue());
        }
    }

    private void CancelPreparedMatch(long accountId, long matchId)
    {
        MatchRepositoryStatus status = _matches.Cancel(accountId, matchId);
        if (status != MatchRepositoryStatus.Success && status != MatchRepositoryStatus.MatchNotWaiting)
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
            MatchRepositoryStatus status = _matches.Cancel(match.Value, match.Key);
            if (status == MatchRepositoryStatus.Success)
            {
                cancelled.Add(match.Key);
                matches.Remove(match.Key);
            }
            else if (status == MatchRepositoryStatus.MatchNotWaiting || status == MatchRepositoryStatus.MatchNotFound)
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

    private static MatchOperationStatus MapRepositoryStatus(MatchRepositoryStatus status)
    {
        MatchOperationStatus operationStatus = MatchOperationStatus.Unauthorized;
        if (status == MatchRepositoryStatus.Success)
        {
            operationStatus = MatchOperationStatus.Success;
        }
        else if (status == MatchRepositoryStatus.InactiveAccount)
        {
            operationStatus = MatchOperationStatus.InactiveAccount;
        }
        else if (status == MatchRepositoryStatus.MatchNotFound)
        {
            operationStatus = MatchOperationStatus.MatchNotFound;
        }
        else if (status == MatchRepositoryStatus.MatchNotWaiting)
        {
            operationStatus = MatchOperationStatus.MatchNotWaiting;
        }
        else if (status == MatchRepositoryStatus.MatchNotInProgress)
        {
            operationStatus = MatchOperationStatus.MatchNotInProgress;
        }
        else if (status == MatchRepositoryStatus.CannotJoinOwnMatch)
        {
            operationStatus = MatchOperationStatus.CannotJoinOwnMatch;
        }
        else if (status == MatchRepositoryStatus.MatchCodeUnavailable)
        {
            operationStatus = MatchOperationStatus.MatchCodeUnavailable;
        }
        else if (status == MatchRepositoryStatus.Unauthorized)
        {
            operationStatus = MatchOperationStatus.Unauthorized;
        }
        else if (status == MatchRepositoryStatus.FriendAccountUnavailable)
        {
            operationStatus = MatchOperationStatus.FriendAccountUnavailable;
        }
        else if (status == MatchRepositoryStatus.FriendshipNotFound)
        {
            operationStatus = MatchOperationStatus.FriendshipNotFound;
        }

        return operationStatus;
    }
}
