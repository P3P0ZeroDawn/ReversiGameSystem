using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;
using ReversiGameSystem.Server.Persistence.Model;
using ApplicationMatchParticipant = ReversiGameSystem.Server.Application.Matches.MatchParticipant;
using PersistentMatchParticipant = ReversiGameSystem.Server.Persistence.Model.MatchParticipant;

namespace ReversiGameSystem.Server.Persistence.Matches;

/// <summary>Transaction-scoped database coordination shared by match and chat stores.</summary>
internal static class MatchDatabase
{
    internal const string LobbyLockResource = "lobby";
    private const int LockTimeoutMilliseconds = 10000;
    private const int LockUnavailableErrorNumber = 51000;
    private const int LockUnavailableErrorState = 1;
    private const string LockResourcePrefix = "Reversi:";

    internal static string MatchLockResource(long matchId)
    {
        return "match:" + matchId.ToString();
    }

    internal static void AcquireTransactionLock(
        ReversiGameSystemEntities context,
        string resource)
    {
        context.Database.ExecuteSqlCommand(
            "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=@resource, "
            + "@LockMode='Exclusive', @LockOwner='Transaction', "
            + "@LockTimeout=@lockTimeout; IF @result < 0 "
            + "THROW @errorNumber, 'Match operation lock unavailable.', @errorState;",
            new SqlParameter("@resource", LockResourcePrefix + resource),
            new SqlParameter("@lockTimeout", LockTimeoutMilliseconds),
            new SqlParameter("@errorNumber", LockUnavailableErrorNumber),
            new SqlParameter("@errorState", LockUnavailableErrorState));
    }

    internal static Match Map(GameMatch match)
    {
        Match mapped = new Match
        {
            Id = match.matchId,
            Code = match.statusCode == "WAITING" ? match.matchCode : null,
            State = match.statusCode == "WAITING" ? "Waiting"
                : match.statusCode == "IN_PROGRESS" ? "InProgress"
                : match.statusCode == "CANCELLED" ? "Cancelled" : "Finished",
            TurnDurationSeconds = match.turnDurationSeconds,
            CountsTowardStatistics = match.countsTowardStatistics,
            StartedAtUtc = match.startedAtUtc.HasValue
                ? DateTime.SpecifyKind(match.startedAtUtc.Value, DateTimeKind.Utc) : (DateTime?)null,
            Participants = match.MatchParticipant1.OrderBy(player => player.seatNumber)
                .Select(player => new ApplicationMatchParticipant
                {
                    SeatNumber = player.seatNumber,
                    AccountId = player.accountId ?? 0,
                    DisplayName = player.displayNameSnapshot,
                    ConsecutiveTimeoutCount = player.consecutiveTimeoutCount
                }).ToArray()
        };

        if (match.currentTurnSeatNumber.HasValue)
        {
            mapped.CurrentTurnSeatNumber = match.currentTurnSeatNumber.Value;
        }

        if (match.turnStartedAtUtc.HasValue)
        {
            mapped.TurnStartedAtUtc = DateTime.SpecifyKind(match.turnStartedAtUtc.Value, DateTimeKind.Utc);
        }

        mapped.EndReason = match.endReasonCode;
        if (match.endedAtUtc.HasValue)
        {
            mapped.EndedAtUtc = DateTime.SpecifyKind(match.endedAtUtc.Value, DateTimeKind.Utc);
        }

        PersistentMatchParticipant? winner = match.MatchParticipant1.FirstOrDefault(player => player.resultCode == "WIN");
        if (winner != null)
        {
            mapped.WinnerSeatNumber = winner.seatNumber;
        }

        PersistentMatchParticipant? abandoner = match.MatchParticipant1.FirstOrDefault(player => player.resultCode == "ABANDONED");
        if (abandoner != null)
        {
            mapped.AbandoningSeatNumber = abandoner.seatNumber;
        }

        if (match.GameMove != null && match.GameMove.Any())
        {
            int[] board = ReversiEngine.CreateInitialBoard();
            List<MatchMove> moves = new List<MatchMove>();
            GameMove[] sortedMoves = match.GameMove.OrderBy(m => m.moveNumber).ToArray();
            for (int i = 0; i < sortedMoves.Length; i++)
            {
                GameMove entity = sortedMoves[i];
                int[] flipped = ReversiEngine.ApplyMove(board, entity.rowIndex, entity.columnIndex, entity.seatNumber);
                MatchMove moveModel = new MatchMove
                {
                    MoveNumber = entity.moveNumber,
                    SeatNumber = entity.seatNumber,
                    Row = entity.rowIndex,
                    Column = entity.columnIndex,
                    PlayedAtUtc = DateTime.SpecifyKind(entity.playedAtUtc, DateTimeKind.Utc),
                    FlippedPieceCount = flipped.Length,
                    Origin = entity.moveSourceCode == "TIMEOUT" ? "Timeout" : "Player"
                };
                moves.Add(moveModel);
            }

            mapped.Board = board;
            mapped.Moves = moves;
            mapped.MatchStateVersion = moves.Count;
            mapped.LastMove = moves[moves.Count - 1];
        }

        return mapped;
    }
}
