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
    private const string ManualMoveSourceCode = "MANUAL";
    private const string SystemMoveSourceCode = "SYSTEM_AUTOMATIC";
    private const string TimeoutMoveSourceCode = "TIMEOUT_AUTOMATIC";
    private const string WaitingStatusCode = "WAITING";
    private const string InProgressStatusCode = "IN_PROGRESS";
    private const string CancelledStatusCode = "CANCELLED";
    private const string WinnerResultCode = "WIN";
    private const string LossResultCode = "LOSS";
    private const string AbandonmentEndReasonCode = "ABANDONMENT";
    private const string WaitingState = "Waiting";
    private const string InProgressState = "InProgress";
    private const string CancelledState = "Cancelled";
    private const string FinishedState = "Finished";
    private const string PlayerMoveOrigin = "Player";
    private const string TimeoutMoveOrigin = "Timeout";
    private const string SystemMoveOrigin = "System";

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
            Code = match.statusCode == WaitingStatusCode ? match.matchCode : null,
            State = MapMatchState(match.statusCode),
            TurnDurationSeconds = match.turnDurationSeconds,
            CountsTowardStatistics = match.countsTowardStatistics,
            StartedAtUtc = SpecifyUtc(match.startedAtUtc),
            Participants = MapParticipants(match),
            EndReason = match.endReasonCode,
            EndedAtUtc = SpecifyUtc(match.endedAtUtc)
        };

        if (match.currentTurnSeatNumber.HasValue)
        {
            mapped.CurrentTurnSeatNumber = match.currentTurnSeatNumber.Value;
        }

        if (match.turnStartedAtUtc.HasValue)
        {
            mapped.TurnStartedAtUtc = DateTime.SpecifyKind(
                match.turnStartedAtUtc.Value,
                DateTimeKind.Utc);
        }

        MapParticipantsOutcome(mapped, match);
        ApplyReplayMoves(mapped, match);
        return mapped;
    }

    internal static string MoveSourceCode(string origin)
    {
        string sourceCode = ManualMoveSourceCode;
        if (string.Equals(origin, TimeoutMoveOrigin, StringComparison.OrdinalIgnoreCase))
        {
            sourceCode = TimeoutMoveSourceCode;
        }
        else if (string.Equals(origin, SystemMoveOrigin, StringComparison.OrdinalIgnoreCase))
        {
            sourceCode = SystemMoveSourceCode;
        }

        return sourceCode;
    }

    private static string MoveOrigin(string sourceCode)
    {
        string origin = PlayerMoveOrigin;
        if (string.Equals(sourceCode, TimeoutMoveSourceCode, StringComparison.OrdinalIgnoreCase))
        {
            origin = TimeoutMoveOrigin;
        }
        else if (string.Equals(
            sourceCode,
            SystemMoveSourceCode,
            StringComparison.OrdinalIgnoreCase))
        {
            origin = SystemMoveOrigin;
        }

        return origin;
    }

    private static string MapMatchState(string statusCode)
    {
        switch (statusCode)
        {
            case WaitingStatusCode:
                return WaitingState;
            case InProgressStatusCode:
                return InProgressState;
            case CancelledStatusCode:
                return CancelledState;
            default:
                return FinishedState;
        }
    }

    private static DateTime? SpecifyUtc(DateTime? timestamp)
    {
        return timestamp.HasValue
            ? DateTime.SpecifyKind(timestamp.Value, DateTimeKind.Utc)
            : (DateTime?)null;
    }

    private static ApplicationMatchParticipant[] MapParticipants(GameMatch match)
    {
        return match.MatchParticipant1
            .OrderBy(player => player.seatNumber)
            .Select(player => new ApplicationMatchParticipant
            {
                SeatNumber = player.seatNumber,
                AccountId = player.accountId ?? 0,
                DisplayName = player.displayNameSnapshot,
                ConsecutiveTimeoutCount = player.consecutiveTimeoutCount
            })
            .ToArray();
    }

    private static void MapParticipantsOutcome(Match mapped, GameMatch match)
    {
        PersistentMatchParticipant? winner = match.MatchParticipant1
            .FirstOrDefault(player => player.resultCode == WinnerResultCode);
        if (winner != null)
        {
            mapped.WinnerSeatNumber = winner.seatNumber;
        }

        if (string.Equals(match.endReasonCode, AbandonmentEndReasonCode, StringComparison.Ordinal))
        {
            PersistentMatchParticipant? abandoner = match.MatchParticipant1
                .FirstOrDefault(player => player.resultCode == LossResultCode);
            if (abandoner != null)
            {
                mapped.AbandoningSeatNumber = abandoner.seatNumber;
            }
        }
    }

    private static void ApplyReplayMoves(Match mapped, GameMatch match)
    {
        if (match.GameMove == null || !match.GameMove.Any())
        {
            return;
        }

        int[] board = ReversiEngine.CreateInitialBoard();
        List<MatchMove> moves = new List<MatchMove>();
        GameMove[] sortedMoves = match.GameMove.OrderBy(m => m.moveNumber).ToArray();
        for (int i = 0; i < sortedMoves.Length; i++)
        {
            GameMove entity = sortedMoves[i];
            int[] flipped = ReversiEngine.ApplyMove(
                board,
                entity.rowIndex,
                entity.columnIndex,
                entity.seatNumber);
            MatchMove moveModel = new MatchMove
            {
                MoveNumber = entity.moveNumber,
                SeatNumber = entity.seatNumber,
                Row = entity.rowIndex,
                Column = entity.columnIndex,
                PlayedAtUtc = DateTime.SpecifyKind(entity.playedAtUtc, DateTimeKind.Utc),
                FlippedPieceCount = flipped.Length,
                Origin = MoveOrigin(entity.moveSourceCode)
            };
            moves.Add(moveModel);
        }

        mapped.Board = board;
        mapped.Moves = moves;
        mapped.MatchStateVersion = moves.Count;
        mapped.LastMove = moves[moves.Count - 1];
    }
}
