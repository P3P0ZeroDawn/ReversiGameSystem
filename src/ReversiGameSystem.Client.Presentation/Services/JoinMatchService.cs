using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum JoinMatchResult
{
    Success,
    NoLongerAvailable,
    CannotJoinOwnMatch,
    InvalidMatchCode,
    MatchCodeNotAvailable,
    ServerConnectionFailed,
    PersistenceError
}

/// <summary>
/// Revalidates and joins a selected temporary match as its second participant.
/// </summary>
internal sealed class JoinMatchService
{
    private readonly string _playerName;
    private readonly string? _guestName;

    internal JoinMatchService(string playerName, string? guestName)
    {
        _playerName = playerName;
        _guestName = guestName;
    }

    internal JoinMatchResult TryJoinById(string matchId, out MatchSession? session)
    {
        PrototypeMatchJoinResult result = PrototypeMatchStore.TryJoinById(
            matchId,
            _playerName,
            _guestName,
            out session);
        return MapCatalogResult(result, false);
    }

    internal JoinMatchResult TryJoinByCode(string enteredCode, out MatchSession? session)
    {
        session = null;
        if (!MatchCodePolicy.TryNormalize(enteredCode, out string code))
        {
            return JoinMatchResult.InvalidMatchCode;
        }

        PrototypeMatchJoinResult result = PrototypeMatchStore.TryJoinByCode(
            code,
            _playerName,
            _guestName,
            out session);
        return MapCatalogResult(result, true);
    }

    private static JoinMatchResult MapCatalogResult(
        PrototypeMatchJoinResult result,
        bool joinedByCode)
    {
        return result switch
        {
            PrototypeMatchJoinResult.Success => JoinMatchResult.Success,
            PrototypeMatchJoinResult.OwnMatch => JoinMatchResult.CannotJoinOwnMatch,
            PrototypeMatchJoinResult.NotFound when joinedByCode =>
                JoinMatchResult.MatchCodeNotAvailable,
            PrototypeMatchJoinResult.NoLongerAvailable when joinedByCode =>
                JoinMatchResult.MatchCodeNotAvailable,
            _ => JoinMatchResult.NoLongerAvailable
        };
    }
}
