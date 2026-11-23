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
    internal static bool CanMapFailure(CommunicationException exception)
    {
        bool recognized = exception is FaultException<Proxy.SessionFault> session
            && Enum.IsDefined(typeof(Proxy.SessionFailure), session.Detail.Reason);
        if (exception is FaultException<Proxy.MatchFault> match)
        {
            recognized = s_matchStatuses.ContainsKey(match.Detail.Reason);
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
            status = s_matchStatuses[matchFault.Detail.Reason];
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
