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
            try
            {
                Match match = _matches.Create(player, acceptedSettings, _clock.UtcNow);
                TrackCreatedMatch(token, player.AccountId, match.Id);
                return MatchOperationResult<Match>.CreateSuccess(match);
            }
            catch (MatchRejectedException exception)
            {
                return MatchOperationResult<Match>.CreateFailure(
                    GetStatus(exception.Reason));
            }
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
            try
            {
                preparation = _matches.PreparePrivateInvitation(
                    player,
                    friendAccountId,
                    acceptedSettings,
                    _clock.UtcNow);
                TrackCreatedMatch(token, player.AccountId, preparation.Match.Id);
            }
            catch (MatchRejectedException exception)
            {
                return MatchOperationResult<Match>.CreateFailure(
                    GetStatus(exception.Reason));
            }
            catch (FriendshipRejectedException exception)
            {
                return MatchOperationResult<Match>.CreateFailure(
                    GetStatus(exception.Reason));
            }
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

        try
        {
            _matches.Authorize(playerResult.GetValue().AccountId);
            return MatchOperationStatus.Success;
        }
        catch (MatchRejectedException exception)
        {
            return GetStatus(exception.Reason);
        }
    }

    /// <summary>Lists public waiting lobbies available to the caller.</summary>
    public MatchOperationResult<Match[]> Available(string token)
    {
        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match[]>.CreateFailure(playerResult.Status);
        }

        try
        {
            Match[] matches = _matches.Available(playerResult.GetValue().AccountId);
            return MatchOperationResult<Match[]>.CreateSuccess(matches);
        }
        catch (MatchRejectedException exception)
        {
            return MatchOperationResult<Match[]>.CreateFailure(
                GetStatus(exception.Reason));
        }
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
            try
            {
                _matches.ValidateJoinForGuest(normalized);
            }
            catch (MatchRejectedException exception)
            {
                return MatchOperationResult<Match>.CreateFailure(
                    GetStatus(exception.Reason));
            }

            return MatchOperationResult<Match>.CreateFailure(
                MatchOperationStatus.Unauthorized);
        }

        MatchOperationResult<AuthenticatedSession> playerResult = GetPlayer(token);
        if (playerResult.Status != MatchOperationStatus.Success)
        {
            return MatchOperationResult<Match>.CreateFailure(playerResult.Status);
        }

        try
        {
            Match match = _matches.Join(
                playerResult.GetValue(),
                normalized,
                _clock.UtcNow);
            return MatchOperationResult<Match>.CreateSuccess(match);
        }
        catch (MatchRejectedException exception)
        {
            return MatchOperationResult<Match>.CreateFailure(
                GetStatus(exception.Reason));
        }
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

        try
        {
            Match match = _matches.Get(playerResult.GetValue().AccountId, matchId);
            return MatchOperationResult<Match>.CreateSuccess(match);
        }
        catch (MatchRejectedException exception)
        {
            return MatchOperationResult<Match>.CreateFailure(
                GetStatus(exception.Reason));
        }
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

        try
        {
            Match match = _matches.ChangeTurnDuration(
                playerResult.GetValue().AccountId,
                matchId,
                seconds);
            return MatchOperationResult<Match>.CreateSuccess(match);
        }
        catch (MatchRejectedException exception)
        {
            return MatchOperationResult<Match>.CreateFailure(
                GetStatus(exception.Reason));
        }
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

        try
        {
            _matches.Cancel(playerResult.GetValue().AccountId, matchId);
            lock (_lobbySync)
            {
                RemoveTrackedMatch(token, matchId);
            }
            return MatchOperationStatus.Success;
        }
        catch (MatchRejectedException exception)
        {
            return GetStatus(exception.Reason);
        }
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
            string? matchCode = preparation.Match.Code;
            if (string.IsNullOrWhiteSpace(matchCode))
            {
                throw new InvalidOperationException(
                    "A prepared private match must contain a join code.");
            }
            string acceptedMatchCode = matchCode
                ?? throw new InvalidOperationException(
                    "A prepared private match must contain a join code.");

            MatchInvitationEmail email = new MatchInvitationEmail(
                preparation.FriendEmail,
                player.Username,
                acceptedMatchCode);
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
                _matches.Cancel(player.AccountId, preparation.Match.Id);
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

            try
            {
                Match match = _matches.MarkInvitationDelivered(
                    player.AccountId,
                    preparation.Match.Id,
                    _clock.UtcNow);
                return MatchOperationResult<Match>.CreateSuccess(match);
            }
            catch (MatchRejectedException exception)
            {
                return MatchOperationResult<Match>.CreateFailure(
                    GetStatus(exception.Reason));
            }
        }
    }

    private void CancelPreparedMatch(long accountId, long matchId)
    {
        try
        {
            _matches.Cancel(accountId, matchId);
        }
        catch (MatchRejectedException exception)
        {
            if (exception.Reason != MatchRejection.MatchNotWaiting)
            {
                throw;
            }
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
            try
            {
                _matches.Cancel(match.Value, match.Key);
                cancelled.Add(match.Key);
                matches.Remove(match.Key);
            }
            catch (MatchRejectedException exception)
            {
                if (exception.Reason == MatchRejection.MatchNotWaiting)
                {
                    matches.Remove(match.Key);
                }
                else
                {
                    throw;
                }
            }
        }

        if (matches.Count == 0)
        {
            _createdMatches.Remove(token);
            _sessionsPendingLobbyCleanup.Remove(token);
        }

        return cancelled.ToArray();
    }

    private static MatchOperationStatus GetStatus(MatchRejection reason)
    {
        MatchOperationStatus status = MatchOperationStatus.Unauthorized;
        if (reason == MatchRejection.InvalidMatchId)
        {
            status = MatchOperationStatus.InvalidMatchId;
        }
        else if (reason == MatchRejection.InvalidConfiguration)
        {
            status = MatchOperationStatus.InvalidConfiguration;
        }
        else if (reason == MatchRejection.InvalidTurnDuration)
        {
            status = MatchOperationStatus.InvalidTurnDuration;
        }
        else if (reason == MatchRejection.MatchNotFound)
        {
            status = MatchOperationStatus.MatchNotFound;
        }
        else if (reason == MatchRejection.CannotJoinOwnMatch)
        {
            status = MatchOperationStatus.CannotJoinOwnMatch;
        }
        else if (reason == MatchRejection.InvalidMatchCode)
        {
            status = MatchOperationStatus.InvalidMatchCode;
        }
        else if (reason == MatchRejection.MatchCodeUnavailable)
        {
            status = MatchOperationStatus.MatchCodeUnavailable;
        }
        else if (reason == MatchRejection.MatchNotWaiting)
        {
            status = MatchOperationStatus.MatchNotWaiting;
        }
        else if (reason == MatchRejection.InactiveAccount)
        {
            status = MatchOperationStatus.InactiveAccount;
        }
        else if (reason == MatchRejection.MatchNotInProgress)
        {
            status = MatchOperationStatus.MatchNotInProgress;
        }

        return status;
    }

    private static MatchOperationStatus GetStatus(FriendshipRejection reason)
    {
        MatchOperationStatus status = MatchOperationStatus.FriendshipNotFound;
        if (reason == FriendshipRejection.InvalidAccountId)
        {
            status = MatchOperationStatus.InvalidFriendAccountId;
        }
        else if (reason == FriendshipRejection.AccountUnavailable)
        {
            status = MatchOperationStatus.FriendAccountUnavailable;
        }
        else if (reason == FriendshipRejection.InactiveAccount)
        {
            status = MatchOperationStatus.InactiveAccount;
        }

        return status;
    }
}
