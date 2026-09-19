using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum AbandonMatchResult
{
    Success,
    MatchAlreadyFinished,
    ServerConnectionFailed,
    PersistenceError
}

/// <summary>
/// Finalizes an active match when one of its participants leaves.
/// </summary>
internal sealed class AbandonMatchService
{
    internal AbandonMatchResult TryAbandon(
        MatchSession session,
        DiscColor abandoningColor)
    {
        if (session.State == MatchState.Finished)
        {
            return AbandonMatchResult.MatchAlreadyFinished;
        }

        if (session.State != MatchState.InProgress)
        {
            return AbandonMatchResult.PersistenceError;
        }

        return session.TryRegisterAbandonment(abandoningColor)
            ? AbandonMatchResult.Success
            : AbandonMatchResult.PersistenceError;
    }
}
