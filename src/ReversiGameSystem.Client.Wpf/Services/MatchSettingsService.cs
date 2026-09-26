using System;
using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum MatchSettingsModificationResult
{
    Success,
    AlreadyStarted,
    Unauthorized,
    InvalidTurnTime,
    ServerConnectionFailed,
    PersistenceError
}

/// <summary>
/// Updates the editable settings of a match that is still waiting for an opponent.
/// </summary>
internal sealed class MatchSettingsService
{
    internal MatchSettingsModificationResult ChangeTurnTime(
        MatchSession session,
        string requestingPlayerName,
        int turnTimeSeconds)
    {
        if (session.State != MatchState.Waiting)
        {
            return MatchSettingsModificationResult.AlreadyStarted;
        }

        if (!string.Equals(
            session.CreatorName,
            requestingPlayerName,
            StringComparison.Ordinal))
        {
            return MatchSettingsModificationResult.Unauthorized;
        }

        if (turnTimeSeconds < TurnTimePolicy.MinimumSeconds
            || turnTimeSeconds > TurnTimePolicy.MaximumSeconds)
        {
            return MatchSettingsModificationResult.InvalidTurnTime;
        }

        session.ApplyTurnTime(turnTimeSeconds);
        return MatchSettingsModificationResult.Success;
    }
}
