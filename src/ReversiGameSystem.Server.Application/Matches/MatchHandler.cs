using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Authorizes registered players and validates lobby operations.</summary>
public sealed class MatchHandler
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly IAuthenticatedSessionStore _sessions;
    private readonly IMatchStore _store;
    private readonly IUtcClock _clock;
    private readonly SessionRegistry? _sessionRegistry;
    private readonly IMatchInvitationEmailSender? _emailSender;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, long>> _created = new();
    private readonly HashSet<string> _pendingCleanup = new HashSet<string>(StringComparer.Ordinal);
    private readonly object _creationSync = new object();

    /// <summary>Creates a match handler with server sessions, durable storage, and a clock.</summary>
    public MatchHandler(IAuthenticatedSessionStore sessions, IMatchStore store, IUtcClock clock)
        : this(sessions, store, clock, sessionRegistry: null, emailSender: null)
    {
    }

    /// <summary>Creates a match handler with guest registry and invitation email sender.</summary>
    public MatchHandler(
        IAuthenticatedSessionStore sessions,
        IMatchStore store,
        IUtcClock clock,
        SessionRegistry? sessionRegistry,
        IMatchInvitationEmailSender? emailSender)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _sessionRegistry = sessionRegistry;
        _emailSender = emailSender;
    }

    /// <summary>Creates the waiting lobby and its creator seat atomically.</summary>
    public MatchRecord Create(string token, MatchSettings? settings)
    {
        lock (_creationSync)
        {
            return CreateTracked(token, settings);
        }
    }

    private MatchRecord CreateTracked(string token, MatchSettings? settings)
    {
        AuthenticatedSessionRecord player = RequirePlayer(token);
        PrepareCreation(token);
        settings = ValidateSettings(settings);
        MatchRecord result = _store.Create(player, settings, _clock.UtcNow);
        TrackCreatedMatch(token, player.AccountId, result.Id);
        return result;
    }

    /// <summary>Creates a private waiting match and delivers the join code to an accepted friend.</summary>
    public MatchRecord Invite(string token, long friendAccountId, MatchSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SessionRejectedException(SessionRejection.MissingSessionToken);
        }

        if (_sessionRegistry != null && _sessionRegistry.TryGetGuest(token, out GuestSessionRecord? guest)
            && guest != null && guest.ExpiresAtUtc > _clock.UtcNow)
        {
            throw new MatchRejectedException(MatchRejection.Unauthorized);
        }

        AuthenticatedSessionRecord player = RequirePlayer(token);

        if (friendAccountId <= 0)
        {
            throw new FriendshipRejectedException(FriendshipRejection.InvalidAccountId);
        }

        if (friendAccountId == player.AccountId)
        {
            throw new MatchRejectedException(MatchRejection.Unauthorized);
        }

        settings = ValidateSettings(settings);

        PrivateMatchInvitationPreparation preparation;
        lock (_creationSync)
        {
            PrepareCreation(token);
            preparation = _store.PreparePrivate(player, friendAccountId, settings, _clock.UtcNow);
            TrackCreatedMatch(token, player.AccountId, preparation.Match.Id);
        }

        try
        {
            if (_emailSender == null)
            {
                throw new InvalidOperationException("No email sender is configured for match invitations.");
            }

            MatchInvitationEmail email = new MatchInvitationEmail(
                preparation.FriendEmail,
                player.Username,
                preparation.Match.Code!);
            _emailSender.Send(email);
        }
        catch (Exception emailException)
        {
            lock (_creationSync)
            {
                if (_created.TryGetValue(token, out ConcurrentDictionary<long, long>? matches))
                {
                    matches.TryRemove(preparation.Match.Id, out _);
                }
            }

            try
            {
                _store.Cancel(player.AccountId, preparation.Match.Id);
            }
            catch (Exception)
            {
                // Unconfirmed invitation protected by invitationConfirmedAtUtc IS NULL
            }

            throw new InvalidOperationException("Failed to deliver match invitation email.", emailException);
        }

        lock (_creationSync)
        {
            if (_pendingCleanup.Contains(token)
                || !_created.TryGetValue(token, out ConcurrentDictionary<long, long>? matches)
                || !matches.ContainsKey(preparation.Match.Id)
                || !_sessions.TryGet(token, out AuthenticatedSessionRecord? currentSession)
                || currentSession == null
                || currentSession.ExpiresAtUtc <= _clock.UtcNow)
            {
                try
                {
                    _store.Cancel(player.AccountId, preparation.Match.Id);
                }
                catch (MatchRejectedException exception) when (exception.Reason == MatchRejection.MatchNotWaiting)
                {
                }

                if (_created.TryGetValue(token, out ConcurrentDictionary<long, long>? trackedMatches))
                {
                    trackedMatches.TryRemove(preparation.Match.Id, out _);
                }

                throw new MatchRejectedException(MatchRejection.MatchNotWaiting);
            }

            return _store.ConfirmPrivate(player.AccountId, preparation.Match.Id, _clock.UtcNow);
        }
    }

    private static MatchSettings ValidateSettings(MatchSettings? settings)
    {
        if (settings == null || settings.OpponentKind != "Human" || settings.CountsTowardStatistics)
        {
            throw new MatchRejectedException(MatchRejection.InvalidConfiguration);
        }
        ValidateDuration(settings.TurnDurationSeconds);
        return settings;
    }

    // Creation and invitation call these bookkeeping methods while holding _creationSync.
    private void PrepareCreation(string token)
    {
        if (_pendingCleanup.Contains(token) || _created.ContainsKey(token))
        {
            CancelTracked(token);
        }
    }

    private void TrackCreatedMatch(string token, long accountId, long matchId)
    {
        ConcurrentDictionary<long, long> matches =
            _created.GetOrAdd(token, _ => new ConcurrentDictionary<long, long>());
        matches[matchId] = accountId;
    }

    /// <summary>Validates a live registered session and its current account eligibility.</summary>
    public void Authorize(string token)
    {
        _store.Authorize(RequirePlayer(token).AccountId);
    }

    /// <summary>Lists joinable waiting lobbies belonging to other players.</summary>
    public MatchRecord[] Available(string token) => _store.Available(RequirePlayer(token).AccountId);

    /// <summary>Occupies the second seat and starts the waiting match atomically.</summary>
    public MatchRecord Join(string token, string code)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SessionRejectedException(SessionRejection.MissingSessionToken);
        }

        string normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length != 6 || normalized.Any(character => !CodeAlphabet.Contains(character)))
        {
            throw new MatchRejectedException(MatchRejection.InvalidMatchCode);
        }

        if (_sessionRegistry != null && _sessionRegistry.TryGetGuest(token, out GuestSessionRecord? guest)
            && guest != null && guest.ExpiresAtUtc > _clock.UtcNow)
        {
            _store.ValidateJoinForGuest(normalized);
            throw new MatchRejectedException(MatchRejection.Unauthorized);
        }

        AuthenticatedSessionRecord player = RequirePlayer(token);
        return _store.Join(player, normalized, _clock.UtcNow);
    }

    /// <summary>Loads confirmed match data for an authorized participant.</summary>
    public MatchRecord Get(string token, long id) => _store.Get(RequirePlayer(token).AccountId, RequireId(id));

    /// <summary>Changes the duration only while the creator still owns a waiting lobby.</summary>
    public MatchRecord ChangeTurnDuration(string token, long id, int seconds)
    {
        AuthenticatedSessionRecord player = RequirePlayer(token);
        ValidateDuration(seconds);
        return _store.ChangeTurnDuration(player.AccountId, RequireId(id), seconds);
    }

    /// <summary>Cancels a waiting lobby owned by the requesting account.</summary>
    public void Cancel(string token, long id)
    {
        _store.Cancel(RequirePlayer(token).AccountId, RequireId(id));
        if (_created.TryGetValue(token, out ConcurrentDictionary<long, long>? matches))
        {
            matches.TryRemove(id, out _);
        }
    }

    /// <summary>Cancels waiting lobbies owned by a closed server session, after token revocation.</summary>
    public long[] CancelWaitingForSession(string token)
    {
        lock (_creationSync)
        {
            _pendingCleanup.Add(token);
            return CancelTracked(token);
        }
    }

    /// <summary>Cancels tracked waiting lobbies whose creating session is expired or revoked.</summary>
    public long[] CancelExpiredWaitingSessions()
    {
        lock (_creationSync)
        {
            List<long> cancelled = new List<long>();
            foreach (string token in _created.Keys)
            {
                if (_pendingCleanup.Contains(token)
                    || !_sessions.TryGet(token, out AuthenticatedSessionRecord? session)
                    || session == null || session.ExpiresAtUtc <= _clock.UtcNow)
                {
                    cancelled.AddRange(CancelTracked(token));
                }
            }
            return cancelled.ToArray();
        }
    }

    private long[] CancelTracked(string token)
    {
        if (!_created.TryGetValue(token, out ConcurrentDictionary<long, long>? matches))
        {
            _pendingCleanup.Remove(token);
            return Array.Empty<long>();
        }
        List<long> cancelled = new List<long>();
        foreach (KeyValuePair<long, long> match in matches)
        {
            try
            {
                _store.Cancel(match.Value, match.Key);
                cancelled.Add(match.Key);
                matches.TryRemove(match.Key, out _);
            }
            catch (MatchRejectedException exception) when (exception.Reason == MatchRejection.MatchNotWaiting)
            {
                // A concurrently started match is preserved.
                matches.TryRemove(match.Key, out _);
            }
        }
        if (matches.IsEmpty)
        {
            _created.TryRemove(token, out _);
            _pendingCleanup.Remove(token);
        }
        return cancelled.ToArray();
    }

    private AuthenticatedSessionRecord RequirePlayer(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SessionRejectedException(SessionRejection.MissingSessionToken);
        }
        if (!_sessions.TryGet(token, out AuthenticatedSessionRecord? player)
            || player == null || player.ExpiresAtUtc <= _clock.UtcNow)
        {
            throw new SessionRejectedException();
        }
        return player;
    }

    private static long RequireId(long id)
    {
        if (id <= 0)
        {
            throw new MatchRejectedException(MatchRejection.InvalidMatchId);
        }
        return id;
    }

    private static void ValidateDuration(int seconds)
    {
        if (seconds < 10 || seconds > 120)
        {
            throw new MatchRejectedException(MatchRejection.InvalidTurnDuration);
        }
    }
}
