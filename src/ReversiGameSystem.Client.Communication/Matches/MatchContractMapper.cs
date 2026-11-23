using System;
using System.Linq;
using System.ServiceModel;
using ReversiGameSystem.Matches;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class MatchContractMapper
{
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
        ConfirmedBoardCell boardCell = cell switch
        {
            Proxy.BoardCellState.Black => ConfirmedBoardCell.Black,
            Proxy.BoardCellState.White => ConfirmedBoardCell.White,
            Proxy.BoardCellState.Empty => ConfirmedBoardCell.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(cell))
        };
        return boardCell;
    }
    private static RemoteMatchConclusion MapConclusion(Proxy.MatchConclusion conclusion)
    {
        return new RemoteMatchConclusion(
            (RemoteMatchEndReason)conclusion.Reason,
            conclusion.WinnerSeatNumber);
    }
    internal static bool CanMapFailure(CommunicationException exception)
    {
        bool recognized = exception is FaultException<Proxy.SessionFault> session
            && Enum.IsDefined(typeof(Proxy.SessionFailure), session.Detail.Reason);
        if (exception is FaultException<Proxy.MatchFault> match)
        {
            recognized = Enum.IsDefined(typeof(Proxy.MatchFailure), match.Detail.Reason);
        }
        else if (exception is FaultException<Proxy.FriendshipFault> friendship)
        {
            recognized = friendship.Detail.Reason is Proxy.FriendshipFailure.AccountUnavailable
                or Proxy.FriendshipFailure.FriendshipNotFound
                or Proxy.FriendshipFailure.InvalidAccountId
                or Proxy.FriendshipFailure.InactiveAccount;
        }
        return recognized;
    }

    internal static MatchStatus MapFailure(Exception exception)
    {
        MatchStatus status;
        if (exception is FaultException<Proxy.MatchFault> matchFault)
        {
            status = matchFault.Detail.Reason switch
            {
                Proxy.MatchFailure.InvalidMatchId => MatchStatus.InvalidMatchId,
                Proxy.MatchFailure.InvalidConfiguration => MatchStatus.InvalidConfiguration,
                Proxy.MatchFailure.InvalidTurnDuration => MatchStatus.InvalidTurnDuration,
                Proxy.MatchFailure.MatchNotFound => MatchStatus.MatchNotFound,
                Proxy.MatchFailure.CannotJoinOwnMatch => MatchStatus.CannotJoinOwnMatch,
                Proxy.MatchFailure.InvalidMatchCode => MatchStatus.InvalidMatchCode,
                Proxy.MatchFailure.MatchCodeUnavailable => MatchStatus.MatchCodeUnavailable,
                Proxy.MatchFailure.Unauthorized => MatchStatus.Unauthorized,
                Proxy.MatchFailure.MatchNotWaiting => MatchStatus.MatchNotWaiting,
                Proxy.MatchFailure.MatchNotInProgress => MatchStatus.MatchNotInProgress,
                Proxy.MatchFailure.MissingPlacement => MatchStatus.MissingPlacement,
                Proxy.MatchFailure.InvalidMatchStateVersion => MatchStatus.InvalidMatchStateVersion,
                Proxy.MatchFailure.StaleMatchStateVersion => MatchStatus.StaleMatchStateVersion,
                Proxy.MatchFailure.NotPlayersTurn => MatchStatus.NotPlayersTurn,
                Proxy.MatchFailure.InvalidPosition => MatchStatus.InvalidPosition,
                Proxy.MatchFailure.SpectatingUnavailable => MatchStatus.SpectatingUnavailable,
                Proxy.MatchFailure.InactiveAccount => MatchStatus.InactiveAccount,
                _ => throw new ArgumentOutOfRangeException(nameof(exception))
            };
        }
        else if (exception is FaultException<Proxy.SessionFault>)
        {
            status = MatchStatus.InvalidSession;
        }
        else if (exception is FaultException<Proxy.FriendshipFault> friendshipFault)
        {
            status = friendshipFault.Detail.Reason switch
            {
                Proxy.FriendshipFailure.AccountUnavailable => MatchStatus.AccountUnavailable,
                Proxy.FriendshipFailure.FriendshipNotFound => MatchStatus.FriendshipNotFound,
                Proxy.FriendshipFailure.InvalidAccountId => MatchStatus.InvalidAccountId,
                Proxy.FriendshipFailure.InactiveAccount => MatchStatus.InactiveAccount,
                _ => throw new ArgumentOutOfRangeException(nameof(exception))
            };
        }
        else
        {
            throw new ArgumentException("The failure has no declared match status.",
                nameof(exception));
        }
        return status;
    }
}
