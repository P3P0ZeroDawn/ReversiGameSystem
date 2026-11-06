using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Persistence.Model;
using PersistentMatchParticipant = ReversiGameSystem.Server.Persistence.Model.MatchParticipant;


namespace ReversiGameSystem.Server.Persistence.Matches;

/// <summary>Persists atomic human lobby transitions using transaction-owned database locks.</summary>
public sealed class MatchRepository : IMatchRepository
{
    /// <inheritdoc />
    public void Authorize(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        MatchDatabase.RequireActive(context, accountId);
    }

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Cancels orphaned waiting lobbies before the single authoritative host accepts callers.</summary>
    /// <returns>The number of waiting lobbies closed during startup.</returns>
    /// <remarks>Startup cannot recover the creating sessions, which exist only in server memory.</remarks>
    public int CancelAbandonedWaitingMatches()
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.Lock(context, "lobby");
        int cancelled = context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET statusCode = 'CANCELLED', endedAtUtc = SYSUTCDATETIME() "
            + "WHERE statusCode = 'WAITING';");
        transaction.Commit();
        return cancelled;
    }

    /// <summary>Creates the waiting lobby and its creator seat atomically.</summary>
    public Match Create(AuthenticatedSession player, MatchSettings settings, DateTime now)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.Lock(context, "lobby");
        MatchDatabase.RequireActive(context, player.AccountId);
        string code;
        do
        {
            code = CreateCode();
        }
        while (context.GameMatch.Any(match => match.matchCode == code));
        GameMatch match = new GameMatch
        {
            matchCode = code, statusCode = "WAITING", turnDurationSeconds = (short)settings.TurnDurationSeconds,
            countsTowardStatistics = settings.CountsTowardStatistics, createdAtUtc = now
        };
        match.MatchParticipant1.Add(CreateParticipant(player, 1, now));
        context.GameMatch.Add(match);
        context.SaveChanges();
        Match result = MatchDatabase.Map(match);
        transaction.Commit();
        return result;
    }

    /// <inheritdoc />
    public MatchInvitationPreparation PreparePrivateInvitation(
        AuthenticatedSession player,
        long friendAccountId,
        MatchSettings settings,
        DateTime now)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.Lock(context, "lobby");
        MatchDatabase.RequireActive(context, player.AccountId);

        Account? friend = context.Account.SingleOrDefault(candidate => candidate.accountId == friendAccountId);
        if (friend == null || friend.statusCode != "ACTIVE")
        {
            throw new FriendshipRejectedException(FriendshipRejection.AccountUnavailable);
        }

        long minId = Math.Min(player.AccountId, friendAccountId);
        long maxId = Math.Max(player.AccountId, friendAccountId);
        bool areFriends = context.Friendship.Any(candidate =>
            candidate.accountIdA == minId
            && candidate.accountIdB == maxId
            && candidate.statusCode == "ACCEPTED"
            && candidate.endedAtUtc == null);
        if (!areFriends)
        {
            throw new FriendshipRejectedException(FriendshipRejection.FriendshipNotFound);
        }

        string code;
        do
        {
            code = CreateCode();
        }
        while (context.GameMatch.Any(match => match.matchCode == code));

        GameMatch match = new GameMatch
        {
            matchCode = code,
            statusCode = "WAITING",
            turnDurationSeconds = (short)settings.TurnDurationSeconds,
            countsTowardStatistics = settings.CountsTowardStatistics,
            createdAtUtc = now
        };
        match.MatchParticipant1.Add(CreateParticipant(player, 1, now));
        context.GameMatch.Add(match);
        context.SaveChanges();

        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET isPrivate = 1, invitedAccountId = @friendAccountId WHERE matchId = @matchId",
            new SqlParameter("@friendAccountId", friendAccountId),
            new SqlParameter("@matchId", match.matchId));

        Match result = MatchDatabase.Map(match);
        result.IsPrivate = true;
        transaction.Commit();
        return new MatchInvitationPreparation(result, friend.emailAddress);
    }

    /// <inheritdoc />
    public Match MarkInvitationDelivered(long accountId, long matchId, DateTime now)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.Lock(context, "lobby");
        MatchDatabase.RequireActive(context, accountId);

        GameMatch match = RequireCreator(context, accountId, matchId);
        if (match.statusCode != "WAITING")
        {
            throw new MatchRejectedException(MatchRejection.MatchNotWaiting);
        }

        MatchPrivacyRow? privacy = context.Database.SqlQuery<MatchPrivacyRow>(
            "SELECT isPrivate AS IsPrivate, invitedAccountId AS InvitedAccountId, invitationConfirmedAtUtc AS InvitationConfirmedAtUtc FROM dbo.GameMatch WHERE matchId = @matchId",
            new SqlParameter("@matchId", matchId)).SingleOrDefault();

        if (privacy == null || !privacy.IsPrivate || privacy.InvitedAccountId == null)
        {
            throw new MatchRejectedException(MatchRejection.MatchNotWaiting);
        }

        MatchDatabase.RequireActive(context, privacy.InvitedAccountId.Value);

        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET invitationConfirmedAtUtc = @now WHERE matchId = @matchId AND statusCode = 'WAITING'",
            new SqlParameter("@now", now),
            new SqlParameter("@matchId", matchId));

        Match result = MatchDatabase.Map(match);
        result.IsPrivate = true;
        transaction.Commit();
        return result;
    }

    /// <summary>Lists joinable waiting lobbies belonging to other players.</summary>
    public Match[] Available(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        MatchDatabase.RequireActive(context, accountId);
        long[] publicMatchIds = context.Database.SqlQuery<long>(
            "SELECT matchId FROM dbo.GameMatch WHERE isPrivate = 0 AND statusCode = 'WAITING'").ToArray();
        if (publicMatchIds.Length == 0)
        {
            return Array.Empty<Match>();
        }

        return context.GameMatch.Include(match => match.MatchParticipant1)
            .Where(match => publicMatchIds.Contains(match.matchId)
                && match.statusCode == "WAITING" && match.MatchParticipant1.Count == 1
                && match.MatchParticipant1.All(player => player.Account.statusCode == "ACTIVE")
                && !match.MatchParticipant1.Any(player => player.accountId == accountId))
            .OrderBy(match => match.matchId).ToArray().Select(MatchDatabase.Map).ToArray();
    }

    /// <summary>Occupies the second seat and starts the waiting match atomically.</summary>
    public Match Join(AuthenticatedSession player, string code, DateTime now)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.Lock(context, "lobby");
        MatchDatabase.RequireActive(context, player.AccountId);
        GameMatch? match = context.GameMatch.Include(candidate => candidate.MatchParticipant1)
            .SingleOrDefault(candidate => candidate.matchCode == code && candidate.statusCode == "WAITING");
        if (match == null || match.MatchParticipant1.Count != 1)
        {
            throw new MatchRejectedException(MatchRejection.MatchCodeUnavailable);
        }

        MatchPrivacyRow? privacy = context.Database.SqlQuery<MatchPrivacyRow>(
            "SELECT isPrivate AS IsPrivate, invitedAccountId AS InvitedAccountId, invitationConfirmedAtUtc AS InvitationConfirmedAtUtc FROM dbo.GameMatch WHERE matchId = @matchId",
            new SqlParameter("@matchId", match.matchId)).SingleOrDefault();

        if (privacy != null && privacy.IsPrivate && privacy.InvitationConfirmedAtUtc == null)
        {
            throw new MatchRejectedException(MatchRejection.MatchCodeUnavailable);
        }

        if (match.MatchParticipant1.Any(participant => participant.accountId == player.AccountId))
        {
            throw new MatchRejectedException(MatchRejection.CannotJoinOwnMatch);
        }

        if (privacy != null && privacy.IsPrivate)
        {
            if (privacy.InvitedAccountId != player.AccountId)
            {
                throw new MatchRejectedException(MatchRejection.Unauthorized);
            }
        }

        PersistentMatchParticipant creator = match.MatchParticipant1.Single();
        MatchDatabase.RequireActive(context, creator.accountId ?? 0);
        MatchDatabase.RequireActive(context, player.AccountId);
        match.MatchParticipant1.Add(CreateParticipant(player, 2, now));
        match.statusCode = "IN_PROGRESS";
        match.startedAtUtc = now;
        match.currentTurnSeatNumber = 1;
        match.turnStartedAtUtc = now;
        context.SaveChanges();
        Match result = MatchDatabase.Map(match);
        if (privacy != null && privacy.IsPrivate)
        {
            result.IsPrivate = true;
        }

        transaction.Commit();
        return result;
    }

    /// <inheritdoc />
    public void ValidateJoinForGuest(string code)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        GameMatch? match = context.GameMatch.SingleOrDefault(candidate =>
            candidate.matchCode == code && candidate.statusCode == "WAITING");
        if (match == null)
        {
            throw new MatchRejectedException(MatchRejection.MatchCodeUnavailable);
        }

        MatchPrivacyRow? privacy = context.Database.SqlQuery<MatchPrivacyRow>(
            "SELECT isPrivate AS IsPrivate, invitedAccountId AS InvitedAccountId, invitationConfirmedAtUtc AS InvitationConfirmedAtUtc FROM dbo.GameMatch WHERE matchId = @matchId",
            new SqlParameter("@matchId", match.matchId)).SingleOrDefault();

        if (privacy != null && privacy.IsPrivate && privacy.InvitationConfirmedAtUtc == null)
        {
            throw new MatchRejectedException(MatchRejection.MatchCodeUnavailable);
        }

        if (privacy != null && privacy.IsPrivate)
        {
            throw new MatchRejectedException(MatchRejection.Unauthorized);
        }

        throw new MatchRejectedException(MatchRejection.Unauthorized);
    }

    /// <summary>Loads confirmed match data for an authorized participant.</summary>
    public Match Get(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        MatchDatabase.RequireParticipant(context, accountId, matchId);
        return MatchDatabase.Map(MatchDatabase.RequireMatch(context, matchId));
    }

    /// <summary>Changes the duration only while the creator still owns a waiting lobby.</summary>
    public Match ChangeTurnDuration(long accountId, long matchId, int seconds)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.Lock(context, "lobby");
        GameMatch match = RequireCreator(context, accountId, matchId);
        if (match.statusCode != "WAITING")
        {
            throw new MatchRejectedException(MatchRejection.MatchNotWaiting);
        }
        match.turnDurationSeconds = (short)seconds;
        context.SaveChanges();
        Match result = MatchDatabase.Map(match);
        transaction.Commit();
        return result;
    }

    /// <summary>Cancels a waiting lobby owned by the requesting account.</summary>
    public void Cancel(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.Lock(context, "lobby");
        GameMatch match = RequireCreator(context, accountId, matchId);
        if (match.statusCode != "WAITING" && match.statusCode != "CANCELLED")
        {
            throw new MatchRejectedException(MatchRejection.MatchNotWaiting);
        }
        match.statusCode = "CANCELLED";
        context.SaveChanges();
        transaction.Commit();
    }

    private static GameMatch RequireCreator(ReversiGameSystemEntities context, long accountId, long matchId)
    {
        PersistentMatchParticipant player = MatchDatabase.RequireParticipant(context, accountId, matchId);
        if (!player.isCreator)
        {
            throw new MatchRejectedException(MatchRejection.Unauthorized);
        }
        return MatchDatabase.RequireMatch(context, matchId);
    }

    private static PersistentMatchParticipant CreateParticipant(
        AuthenticatedSession player,
        byte seat,
        DateTime now)
    {
        return new PersistentMatchParticipant
        {
            seatNumber = seat, accountId = player.AccountId, participantTypeCode = "REGISTERED",
            displayNameSnapshot = player.Username, pieceColorCode = seat == 1 ? "BLACK" : "WHITE",
            joinedAtUtc = now, isCreator = seat == 1
        };
    }

    private static string CreateCode()
    {
        byte[] random = new byte[6];
        using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
        {
            generator.GetBytes(random);
        }
        return new string(random.Select(value => Alphabet[value % Alphabet.Length]).ToArray());
    }

    private sealed class MatchPrivacyRow
    {
        public bool IsPrivate { get; set; }
        public long? InvitedAccountId { get; set; }
        public DateTime? InvitationConfirmedAtUtc { get; set; }
    }
}

