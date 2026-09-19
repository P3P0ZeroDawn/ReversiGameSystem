using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum GameMoveResult
{
    Success,
    InvalidPosition,
    MatchNotInProgress,
    NotPlayersTurn,
    ServerConnectionFailed,
    PersistenceError
}

internal enum TurnPreparationResult
{
    Ready,
    TurnPassed,
    MatchFinished
}

internal enum TurnTimeoutResult
{
    AutomaticMove,
    TurnPassed,
    MatchFinished,
    MatchFinishedByInactivity,
    MatchNotInProgress,
    ResultPersistenceError
}

/// <summary>
/// Applies Reversi moves and turn transitions to one active match session.
/// </summary>
internal sealed class GameMoveService
{
    private const int InactivityLossThreshold = 3;
    private readonly Random _random;

    internal GameMoveService() : this(new Random())
    {
    }

    internal GameMoveService(Random random)
    {
        _random = random;
    }

    internal IReadOnlyList<BoardPosition> GetCurrentValidMoves(MatchSession session)
    {
        return session.Board.GetValidMoves(session.CurrentTurnColor);
    }

    internal TurnPreparationResult PrepareCurrentTurn(
        MatchSession session,
        out DiscColor? skippedColor)
    {
        skippedColor = null;
        if (session.State != MatchState.InProgress)
        {
            return TurnPreparationResult.MatchFinished;
        }

        DiscColor currentColor = session.CurrentTurnColor;
        if (session.Board.GetValidMoves(currentColor).Count > 0)
        {
            return TurnPreparationResult.Ready;
        }

        DiscColor opponentColor = ReversiBoard.GetOpponent(currentColor);
        if (session.Board.GetValidMoves(opponentColor).Count > 0)
        {
            skippedColor = currentColor;
            session.SetTurn(opponentColor);
            return TurnPreparationResult.TurnPassed;
        }

        FinishFromBoard(session);
        return TurnPreparationResult.MatchFinished;
    }

    internal GameMoveResult TryMakeMove(
        MatchSession session,
        DiscColor playerColor,
        BoardPosition position,
        out MatchMove? move,
        out DiscColor? skippedColor)
    {
        move = null;
        skippedColor = null;
        if (session.State != MatchState.InProgress)
        {
            return GameMoveResult.MatchNotInProgress;
        }

        if (session.CurrentTurnColor != playerColor)
        {
            return GameMoveResult.NotPlayersTurn;
        }

        if (!session.Board.TryApplyMove(playerColor, position, out ReversiBoard? board)
            || board == null)
        {
            return GameMoveResult.InvalidPosition;
        }

        move = CreateMove(session, playerColor, position, MatchMoveOrigin.Manual);
        session.CommitMove(board, move, true);
        AdvanceAfterMove(session, playerColor, out skippedColor);
        return GameMoveResult.Success;
    }

    internal TurnTimeoutResult HandleTimeout(
        MatchSession session,
        out MatchMove? move,
        out DiscColor? skippedColor)
    {
        move = null;
        skippedColor = null;
        if (session.State != MatchState.InProgress)
        {
            return TurnTimeoutResult.MatchNotInProgress;
        }

        DiscColor playerColor = session.CurrentTurnColor;
        IReadOnlyList<BoardPosition> validMoves =
            session.Board.GetValidMoves(playerColor);
        if (validMoves.Count == 0)
        {
            TurnPreparationResult preparation = PrepareCurrentTurn(
                session,
                out skippedColor);
            return preparation == TurnPreparationResult.TurnPassed
                ? TurnTimeoutResult.TurnPassed
                : TurnTimeoutResult.MatchFinished;
        }

        int consecutiveTimeouts = session.GetConsecutiveTimeouts(playerColor) + 1;
        if (consecutiveTimeouts >= InactivityLossThreshold)
        {
            session.RegisterTurnTimeout(playerColor);
            session.Finish(
                ReversiBoard.GetOpponent(playerColor),
                MatchEndReason.Inactivity);
            return TurnTimeoutResult.MatchFinishedByInactivity;
        }

        BoardPosition selectedPosition = validMoves[_random.Next(validMoves.Count)];
        if (!session.Board.TryApplyMove(
            playerColor,
            selectedPosition,
            out ReversiBoard? board)
            || board == null)
        {
            return TurnTimeoutResult.ResultPersistenceError;
        }

        session.RegisterTurnTimeout(playerColor);
        move = CreateMove(session, playerColor, selectedPosition, MatchMoveOrigin.Timeout);
        session.CommitMove(board, move, false);
        AdvanceAfterMove(session, playerColor, out skippedColor);
        return session.State == MatchState.Finished
            ? TurnTimeoutResult.MatchFinished
            : TurnTimeoutResult.AutomaticMove;
    }

    internal GameMoveResult TryMakeSystemMove(
        MatchSession session,
        out MatchMove? move,
        out DiscColor? skippedColor)
    {
        move = null;
        skippedColor = null;
        if (session.State != MatchState.InProgress)
        {
            return GameMoveResult.MatchNotInProgress;
        }

        DiscColor playerColor = session.CurrentTurnColor;
        IReadOnlyList<BoardPosition> validMoves =
            session.Board.GetValidMoves(playerColor);
        if (validMoves.Count == 0)
        {
            return GameMoveResult.InvalidPosition;
        }

        BoardPosition selectedPosition = validMoves[_random.Next(validMoves.Count)];
        if (!session.Board.TryApplyMove(
            playerColor,
            selectedPosition,
            out ReversiBoard? board)
            || board == null)
        {
            return GameMoveResult.PersistenceError;
        }

        move = CreateMove(
            session,
            playerColor,
            selectedPosition,
            MatchMoveOrigin.System);
        session.CommitMove(board, move, true);
        AdvanceAfterMove(session, playerColor, out skippedColor);
        return GameMoveResult.Success;
    }

    private static MatchMove CreateMove(
        MatchSession session,
        DiscColor playerColor,
        BoardPosition position,
        MatchMoveOrigin origin)
    {
        return new MatchMove(
            session.Moves.Count + 1,
            playerColor,
            position,
            origin,
            DateTime.UtcNow);
    }

    private static void AdvanceAfterMove(
        MatchSession session,
        DiscColor playerColor,
        out DiscColor? skippedColor)
    {
        skippedColor = null;
        DiscColor opponentColor = ReversiBoard.GetOpponent(playerColor);
        if (session.Board.GetValidMoves(opponentColor).Count > 0)
        {
            session.SetTurn(opponentColor);
            return;
        }

        if (session.Board.GetValidMoves(playerColor).Count > 0)
        {
            skippedColor = opponentColor;
            session.SetTurn(playerColor);
            return;
        }

        FinishFromBoard(session);
    }

    private static void FinishFromBoard(MatchSession session)
    {
        int blackCount = session.Board.Count(DiscColor.Black);
        int whiteCount = session.Board.Count(DiscColor.White);
        DiscColor? winnerColor = blackCount == whiteCount
            ? null
            : blackCount > whiteCount
                ? DiscColor.Black
                : DiscColor.White;
        session.Finish(winnerColor, MatchEndReason.BoardCompleted);
    }
}
