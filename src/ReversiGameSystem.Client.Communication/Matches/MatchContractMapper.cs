using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using ReversiGameSystem.Matches;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class MatchContractMapper
{
    private static readonly IReadOnlyDictionary<Proxy.MatchFailure, MatchStatus>
        s_matchStatuses = new Dictionary<Proxy.MatchFailure, MatchStatus>
        {
            { Proxy.MatchFailure.InvalidMatchId, MatchStatus.InvalidMatchId },
            { Proxy.MatchFailure.InvalidConfiguration, MatchStatus.InvalidConfiguration },
            { Proxy.MatchFailure.InvalidTurnDuration, MatchStatus.InvalidTurnDuration },
            { Proxy.MatchFailure.MatchNotFound, MatchStatus.MatchNotFound },
            { Proxy.MatchFailure.CannotJoinOwnMatch, MatchStatus.CannotJoinOwnMatch },
            { Proxy.MatchFailure.InvalidMatchCode, MatchStatus.InvalidMatchCode },
            { Proxy.MatchFailure.MatchCodeUnavailable, MatchStatus.MatchCodeUnavailable },
            { Proxy.MatchFailure.Unauthorized, MatchStatus.Unauthorized },
            { Proxy.MatchFailure.MatchNotWaiting, MatchStatus.MatchNotWaiting },
            { Proxy.MatchFailure.MatchNotInProgress, MatchStatus.MatchNotInProgress },
            { Proxy.MatchFailure.MissingPlacement, MatchStatus.MissingPlacement },
            { Proxy.MatchFailure.InvalidMatchStateVersion, MatchStatus.InvalidMatchStateVersion },
            { Proxy.MatchFailure.StaleMatchStateVersion, MatchStatus.StaleMatchStateVersion },
            { Proxy.MatchFailure.NotPlayersTurn, MatchStatus.NotPlayersTurn },
            { Proxy.MatchFailure.InvalidPosition, MatchStatus.InvalidPosition },
            { Proxy.MatchFailure.SpectatingUnavailable, MatchStatus.SpectatingUnavailable },
            { Proxy.MatchFailure.InactiveAccount, MatchStatus.InactiveAccount }
        };
    internal static MatchStateSnapshot MapState(Proxy.MatchStateSnapshot state)
    {
        ConfirmedBoardCell[] board = state.Board.Select(MapBoardCell).ToArray();
        RemoteMatchConclusion? conclusion = state.Conclusion == null
            ? null
            : MapConclusion(state.Conclusion);
        return new MatchStateSnapshot(
            state.MatchId,
            state.MatchStateVersion,
            (ConfirmedMatchState)state.State,
            board,
            state.BlackPieceCount,
            state.WhitePieceCount,
            state.CurrentTurnSeatNumber)
        {
            TurnStartedAtUtc = state.TurnStartedAtUtc,
            Conclusion = conclusion
        };
    }
    private static ConfirmedBoardCell MapBoardCell(Proxy.BoardCellState cell)
    {
        return cell switch
        {
            Proxy.BoardCellState.Black => ConfirmedBoardCell.Black,
            Proxy.BoardCellState.White => ConfirmedBoardCell.White,
            _ => ConfirmedBoardCell.Empty
        };
    }
    private static RemoteMatchConclusion MapConclusion(Proxy.MatchConclusion conclusion)
    {
        return new RemoteMatchConclusion(
            (RemoteMatchEndReason)conclusion.Reason,
            conclusion.WinnerSeatNumber);
    }
    internal static MatchStatus MapFailure(Exception exception)
    {
        MatchStatus status = MatchStatus.ServiceUnavailable;
        if (exception is FaultException<Proxy.MatchFault> matchFault)
        {
            status = MapMatchFailure(matchFault.Detail.Reason);
        }
        else if (exception is FaultException<Proxy.SessionFault>)
        {
            status = MatchStatus.InvalidSession;
        }
        else if (exception is FaultException<Proxy.FriendshipFault> friendshipFault)
        {
            status = MapFriendshipFailure(friendshipFault.Detail.Reason);
        }
        return status;
    }
    private static MatchStatus MapFriendshipFailure(Proxy.FriendshipFailure reason)
    {
        return reason switch
        {
            Proxy.FriendshipFailure.AccountUnavailable => MatchStatus.AccountUnavailable,
            Proxy.FriendshipFailure.FriendshipNotFound => MatchStatus.FriendshipNotFound,
            Proxy.FriendshipFailure.InvalidAccountId => MatchStatus.InvalidAccountId,
            _ => MatchStatus.InactiveAccount
        };
    }
    private static MatchStatus MapMatchFailure(Proxy.MatchFailure failure)
    {
        MatchStatus status = MatchStatus.InactiveAccount;
        if (s_matchStatuses.ContainsKey(failure))
        {
            status = s_matchStatuses[failure];
        }
        return status;
    }
}
