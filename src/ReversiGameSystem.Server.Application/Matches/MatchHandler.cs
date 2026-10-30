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
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, long>> _created = new();
    private readonly HashSet<string> _pendingCleanup = new HashSet<string>(StringComparer.Ordinal);
    private readonly object _creationSync = new object();

    /// <summary>Creates a match handler with server sessions, durable storage, and a clock.</summary>
    public MatchHandler(IAuthenticatedSessionStore sessions, IMatchStore store, IUtcClock clock)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Creates the waiting lobby and its creator seat atomically.</summary>
    public MatchRecord Create(string token, MatchSettings settings)
    {
        lock (_creationSync)
        {
            return CreateTracked(token, settings);
        }
    }

    private MatchRecord CreateTracked(string token, MatchSettings settings)
    {
        AuthenticatedSessionRecord player = RequirePlayer(token);
        if (_pendingCleanup.Contains(token))
        {
            CancelTracked(token);
        }
        if (settings == null || settings.OpponentKind != "Human" || settings.CountsTowardStatistics)
        {
            throw new MatchRejectedException(MatchRejection.InvalidConfiguration);
        }
        ValidateDuration(settings.TurnDurationSeconds);
        MatchRecord result = _store.Create(player, settings, _clock.UtcNow);
        _created.GetOrAdd(token, _ => new ConcurrentDictionary<long, long>())[result.Id] = player.AccountId;
        return result;
    }

    /// <summary>Lists joinable waiting lobbies belonging to other players.</summary>
    public MatchRecord[] Available(string token) => _store.Available(RequirePlayer(token).AccountId);

    /// <summary>Occupies the second seat and starts the waiting match atomically.</summary>
    public MatchRecord Join(string token, string code)
    {
        AuthenticatedSessionRecord player = RequirePlayer(token);
        string normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length != 6 || normalized.Any(character => !CodeAlphabet.Contains(character)))
        {
            throw new MatchRejectedException(MatchRejection.InvalidMatchCode);
        }
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
