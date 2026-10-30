using System;
using System.Data.SqlClient;
using System.Linq;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Matches;

/// <summary>Transaction-scoped database coordination shared by match and chat stores.</summary>
internal static class MatchDatabase
{
    internal static void Lock(ReversiGameSystemEntities context, string resource)
    {
        context.Database.ExecuteSqlCommand(
            "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=@resource, "
            + "@LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; "
            + "IF @result < 0 THROW 51000, 'Match operation lock unavailable.', 1;",
            new SqlParameter("@resource", "Reversi:" + resource));
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

    internal static MatchParticipant RequireParticipant(
        ReversiGameSystemEntities context, long accountId, long matchId)
    {
        RequireActive(context, accountId);
        MatchParticipant? participant = context.MatchParticipant.SingleOrDefault(candidate =>
            candidate.matchId == matchId && candidate.accountId == accountId
            && candidate.participantTypeCode == "REGISTERED");
        if (participant == null)
        {
            throw new MatchRejectedException(MatchRejection.Unauthorized);
        }
        return participant;
    }

    internal static MatchRecord Map(GameMatch match)
    {
        if (match.statusCode == "IN_PROGRESS" && match.GameMove.Any())
        {
            throw new NotSupportedException("This match contains gameplay beyond the supported initial position.");
        }
        return new MatchRecord
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
                .Select(player => new ParticipantRecord
                {
                    SeatNumber = player.seatNumber, AccountId = player.accountId ?? 0,
                    DisplayName = player.displayNameSnapshot
                }).ToArray()
        };
    }
}
