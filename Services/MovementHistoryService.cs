using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum MovementHistoryRetrievalResult
{
    Success,
    ServerConnectionFailed,
    RetrievalError
}

/// <summary>
/// Retrieves the registered movements for one accessible match.
/// </summary>
internal sealed class MovementHistoryService
{
    private readonly IReadOnlyList<MatchInformation> _matches;

    internal MovementHistoryService(IEnumerable<MatchInformation> matches)
    {
        _matches = matches.ToArray();
    }

    internal MovementHistoryRetrievalResult TryGetMovements(
        string matchId,
        out IReadOnlyList<RecordedMove> movements)
    {
        MatchInformation? match = _matches.FirstOrDefault(
            candidate => string.Equals(candidate.Id, matchId, StringComparison.Ordinal));
        if (match == null)
        {
            movements = Array.Empty<RecordedMove>();
            return MovementHistoryRetrievalResult.RetrievalError;
        }

        movements = match.Moves
            .OrderBy(move => move.Number)
            .ToArray();
        return MovementHistoryRetrievalResult.Success;
    }
}
