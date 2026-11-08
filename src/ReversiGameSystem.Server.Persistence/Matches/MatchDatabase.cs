using System;
using System.Data.SqlClient;
using System.Linq;
using ReversiGameSystem.Server.Application.Matches;
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

    internal static void RequireActive(ReversiGameSystemEntities context, long accountId)
    {
        if (!context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
        {
            throw new MatchRejectedException(MatchRejection.InactiveAccount);
        }
    }

    internal static GameMatch RequireMatch(ReversiGameSystemEntities context, long matchId)
    {
        GameMatch? match = context.GameMatch.SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            throw new MatchRejectedException(MatchRejection.MatchNotFound);
        }
        return match;
    }

    internal static PersistentMatchParticipant RequireParticipant(
        ReversiGameSystemEntities context, long accountId, long matchId)
    {
        RequireActive(context, accountId);
        PersistentMatchParticipant? participant = context.MatchParticipant.SingleOrDefault(candidate =>
            candidate.matchId == matchId && candidate.accountId == accountId
            && candidate.participantTypeCode == "REGISTERED");
        if (participant == null)
        {
            throw new MatchRejectedException(MatchRejection.Unauthorized);
        }
        return participant;
    }

    internal static Match Map(GameMatch match)
    {
        if (match.statusCode == "IN_PROGRESS" && match.GameMove.Any())
        {
            throw new NotSupportedException("This match contains gameplay beyond the supported initial position.");
        }
        return new Match
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
                    DisplayName = player.displayNameSnapshot
                }).ToArray()
        };
    }
}
