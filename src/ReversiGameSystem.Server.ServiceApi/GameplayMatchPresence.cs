using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Tracks connected participant sessions and reconnection deadlines for active matches.
/// </summary>
public sealed class GameplayMatchPresence
{
    private static readonly TimeSpan ReconnectionWindow = TimeSpan.FromSeconds(30);
    private readonly object _presenceSync = new object();
    private readonly Dictionary<(long MatchId, int SeatNumber), DateTime> _reconnectionDeadlines =
        new Dictionary<(long MatchId, int SeatNumber), DateTime>();
    private readonly Dictionary<long, Dictionary<int, HashSet<string>>> _participantPresence =
        new Dictionary<long, Dictionary<int, HashSet<string>>>();
    private readonly ILogger<GameplayMatchPresence> _logger;

    /// <summary>Initializes the match participant presence tracker.</summary>
    /// <param name="logger">The presence audit logger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is null.</exception>
    public GameplayMatchPresence(ILogger<GameplayMatchPresence> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal MatchConnectionState? RegisterConnection(
        Match match,
        int seatNumber,
        string sessionToken)
    {
        (long MatchId, int SeatNumber) key = (match.Id, seatNumber);
        lock (_presenceSync)
        {
            bool reconnectionExpired =
                _reconnectionDeadlines.TryGetValue(key, out DateTime deadline)
                && DateTime.UtcNow >= deadline;
            if (reconnectionExpired)
            {
                return null;
            }

            bool isReconnection = _reconnectionDeadlines.Remove(key);
            Dictionary<int, HashSet<string>> matchPresence = GetOrCreateMatchPresence(match);
            matchPresence[seatNumber].Add(sessionToken);
            return new MatchConnectionState(
                CreatePresenceSnapshot(matchPresence),
                isReconnection);
        }
    }

    /// <summary>
    /// Removes a disconnected session from match presence and schedules reconnection expiry.
    /// </summary>
    /// <param name="sessionToken">The disconnected session token.</param>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="hasSubscription">
    /// Predicate checking whether another subscription remains active for the session and match.
    /// </param>
    /// <returns>The updated participant connection snapshot, or <c>null</c> if unchanged.</returns>
    public KeyValuePair<int, bool>[]? RemoveConnection(
        string sessionToken,
        long matchId,
        Func<string, long, bool> hasSubscription)
    {
        KeyValuePair<int, bool>[] snapshot;
        List<int>? disconnectedSeats = null;
        lock (_presenceSync)
        {
            if (hasSubscription(sessionToken, matchId))
            {
                return null;
            }

            if (!_participantPresence.TryGetValue(
                matchId,
                out Dictionary<int, HashSet<string>>? matchPresence))
            {
                return null;
            }

            foreach (KeyValuePair<int, HashSet<string>> seat in matchPresence)
            {
                if (seat.Value.Remove(sessionToken) && seat.Value.Count == 0)
                {
                    _reconnectionDeadlines[(matchId, seat.Key)] =
                        DateTime.UtcNow.Add(ReconnectionWindow);
                    disconnectedSeats ??= new List<int>();
                    disconnectedSeats.Add(seat.Key);
                }
            }

            snapshot = CreatePresenceSnapshot(matchPresence);
            if (!matchPresence.Values.Any(tokens => tokens.Count > 0))
            {
                _participantPresence.Remove(matchId);
            }
        }

        if (disconnectedSeats != null)
        {
            for (int index = 0; index < disconnectedSeats.Count; index++)
            {
                _logger.LogWarning(
                    "Participant seat {SeatNumber} disconnected from match {MatchId}; 30s reconnection window started.",
                    disconnectedSeats[index],
                    matchId);
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Collects and removes participant seats whose reconnection window has expired.
    /// </summary>
    /// <returns>The expired match and seat pairs ordered by deadline.</returns>
    public (long MatchId, int SeatNumber)[] CollectExpiredReconnections()
    {
        DateTime now = DateTime.UtcNow;
        lock (_presenceSync)
        {
            (long MatchId, int SeatNumber)[] expiredSeats = _reconnectionDeadlines
                .Where(entry => now >= entry.Value)
                .OrderBy(entry => entry.Value)
                .Select(entry => entry.Key)
                .ToArray();
            foreach ((long MatchId, int SeatNumber) key in expiredSeats)
            {
                _reconnectionDeadlines.Remove(key);
            }

            return expiredSeats;
        }
    }

    private Dictionary<int, HashSet<string>> GetOrCreateMatchPresence(Match match)
    {
        if (!_participantPresence.TryGetValue(
            match.Id,
            out Dictionary<int, HashSet<string>>? matchPresence))
        {
            matchPresence = new Dictionary<int, HashSet<string>>();
            foreach (MatchParticipant participant in match.Participants)
            {
                matchPresence.Add(
                    participant.SeatNumber,
                    new HashSet<string>(StringComparer.Ordinal));
            }

            _participantPresence.Add(match.Id, matchPresence);
        }

        return matchPresence;
    }

    private static KeyValuePair<int, bool>[] CreatePresenceSnapshot(
        Dictionary<int, HashSet<string>> matchPresence)
    {
        return matchPresence
            .OrderBy(entry => entry.Key)
            .Select(entry => new KeyValuePair<int, bool>(entry.Key, entry.Value.Count > 0))
            .ToArray();
    }
}

internal sealed class MatchConnectionState
{
    public MatchConnectionState(
        KeyValuePair<int, bool>[] seats,
        bool isReconnection)
    {
        Seats = seats;
        IsReconnection = isReconnection;
    }

    public KeyValuePair<int, bool>[] Seats { get; }

    public bool IsReconnection { get; }
}
