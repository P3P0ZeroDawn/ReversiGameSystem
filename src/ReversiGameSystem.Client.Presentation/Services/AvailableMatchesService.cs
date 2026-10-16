using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum AvailableMatchesRetrievalResult
{
    Success,
    ServerConnectionFailed,
    RetrievalError
}

/// <summary>
/// Supplies joinable prototype matches until server discovery is connected.
/// </summary>
internal sealed class AvailableMatchesService
{
    private readonly string _currentPlayerName;

    internal AvailableMatchesService(string currentPlayerName)
    {
        _currentPlayerName = currentPlayerName;
    }

    internal AvailableMatchesRetrievalResult TryGetAvailableMatches(
        out IReadOnlyList<AvailableMatch> matches)
    {
        matches = FilterJoinable(PrototypeMatchStore.GetAll(), _currentPlayerName);
        return AvailableMatchesRetrievalResult.Success;
    }

    internal static IReadOnlyList<AvailableMatch> FilterJoinable(
        IEnumerable<AvailableMatch> matches,
        string currentPlayerName)
    {
        return matches.Where(match =>
                match.State == AvailableMatchState.Waiting
                && match.ParticipantCount == 1
                && !match.UsesSystemOpponent
                && !string.Equals(
                    match.HostName,
                    currentPlayerName,
                    StringComparison.Ordinal))
            .ToArray();
    }
}
