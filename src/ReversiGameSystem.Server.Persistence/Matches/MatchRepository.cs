using System;
using System.Collections.Generic;
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
    public MatchStatus EvaluateLobbyAccess(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        if (!context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
        {
            return MatchStatus.InactiveAccount;
        }

        return MatchStatus.Success;
    }

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Cancels orphaned waiting lobbies before the single authoritative host accepts callers.</summary>
    /// <returns>The number of waiting lobbies closed during startup.</returns>
    /// <remarks>Startup cannot recover the creating sessions, which exist only in server memory.</remarks>
    public int CancelAbandonedWaitingMatches()
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.LobbyLockResource);
        int cancelled = context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET statusCode = 'CANCELLED', endedAtUtc = SYSUTCDATETIME() "
            + "WHERE statusCode = 'WAITING';");
        transaction.Commit();
        return cancelled;
    }

    /// <summary>Creates the waiting lobby and its creator seat atomically.</summary>
    public MatchResult<Match> Create(AuthenticatedSession player, MatchSettings settings, DateTime now)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.LobbyLockResource);
        if (!context.Account.Any(account => account.accountId == player.AccountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match>(MatchStatus.InactiveAccount);
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
        Match result = MatchDatabase.Map(match);
        transaction.Commit();
        return new MatchResult<Match>(result);
    }

    /// <inheritdoc />
    public MatchResult<MatchInvitationPreparation> PreparePrivateInvitation(
        AuthenticatedSession player,
        long friendAccountId,
        MatchSettings settings,
        DateTime now)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.LobbyLockResource);
        if (!context.Account.Any(account => account.accountId == player.AccountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<MatchInvitationPreparation>(MatchStatus.InactiveAccount);
        }

        Account? friend = context.Account.SingleOrDefault(candidate => candidate.accountId == friendAccountId);
        if (friend == null || friend.statusCode != "ACTIVE")
        {
            return new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendAccountUnavailable);
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
            return new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendshipNotFound);
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
        MatchInvitationPreparation preparation = new MatchInvitationPreparation(result, friend.emailAddress);
        return new MatchResult<MatchInvitationPreparation>(preparation);
    }

    /// <inheritdoc />
    public MatchResult<Match> MarkInvitationDelivered(long accountId, long matchId, DateTime now)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.LobbyLockResource);
        if (!context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match>(MatchStatus.InactiveAccount);
        }

        PersistentMatchParticipant? player = context.MatchParticipant.SingleOrDefault(candidate =>
            candidate.matchId == matchId && candidate.accountId == accountId
            && candidate.participantTypeCode == "REGISTERED");
        if (player == null || !player.isCreator)
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        GameMatch? match = context.GameMatch
            .Include(candidate => candidate.MatchParticipant1)
            .Include(candidate => candidate.GameMove)
            .SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            return new MatchResult<Match>(MatchStatus.MatchNotFound);
        }

        if (match.statusCode != "WAITING")
        {
            return new MatchResult<Match>(MatchStatus.MatchNotWaiting);
        }

        MatchPrivacyRow? privacy = context.Database.SqlQuery<MatchPrivacyRow>(
            "SELECT isPrivate AS IsPrivate, invitedAccountId AS InvitedAccountId, invitationConfirmedAtUtc AS InvitationConfirmedAtUtc FROM dbo.GameMatch WHERE matchId = @matchId",
            new SqlParameter("@matchId", matchId)).SingleOrDefault();

        if (privacy == null || !privacy.IsPrivate || privacy.InvitedAccountId == null)
        {
            return new MatchResult<Match>(MatchStatus.MatchNotWaiting);
        }

        if (!context.Account.Any(account => account.accountId == privacy.InvitedAccountId.Value && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match>(MatchStatus.InactiveAccount);
        }

        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET invitationConfirmedAtUtc = @now WHERE matchId = @matchId AND statusCode = 'WAITING'",
            new SqlParameter("@now", now),
            new SqlParameter("@matchId", matchId));

        Match result = MatchDatabase.Map(match);
        result.IsPrivate = true;
        transaction.Commit();
        return new MatchResult<Match>(result);
    }

    /// <summary>Lists joinable waiting lobbies belonging to other players.</summary>
    public MatchResult<Match[]> Available(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        if (!context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match[]>(MatchStatus.InactiveAccount);
        }

        long[] publicMatchIds = context.Database.SqlQuery<long>(
            "SELECT matchId FROM dbo.GameMatch WHERE isPrivate = 0 AND statusCode = 'WAITING'").ToArray();
        if (publicMatchIds.Length == 0)
        {
            return new MatchResult<Match[]>(Array.Empty<Match>());
        }

        Match[] matches = context.GameMatch.Include(match => match.MatchParticipant1)
            .Where(match => publicMatchIds.Contains(match.matchId)
                && match.statusCode == "WAITING" && match.MatchParticipant1.Count == 1
                && match.MatchParticipant1.All(player => player.Account.statusCode == "ACTIVE")
                && !match.MatchParticipant1.Any(player => player.accountId == accountId))
            .OrderBy(match => match.matchId).ToArray().Select(MatchDatabase.Map).ToArray();

        return new MatchResult<Match[]>(matches);
    }

    /// <summary>Occupies the second seat and starts the waiting match atomically.</summary>
    public MatchResult<Match> Join(AuthenticatedSession player, string code, DateTime now)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.LobbyLockResource);
        if (!context.Account.Any(account => account.accountId == player.AccountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match>(MatchStatus.InactiveAccount);
        }

        GameMatch? match = context.GameMatch.Include(candidate => candidate.MatchParticipant1)
            .SingleOrDefault(candidate => candidate.matchCode == code && candidate.statusCode == "WAITING");
        if (match == null || match.MatchParticipant1.Count != 1)
        {
            return new MatchResult<Match>(MatchStatus.MatchCodeUnavailable);
        }

        MatchPrivacyRow? privacy = context.Database.SqlQuery<MatchPrivacyRow>(
            "SELECT isPrivate AS IsPrivate, invitedAccountId AS InvitedAccountId, invitationConfirmedAtUtc AS InvitationConfirmedAtUtc FROM dbo.GameMatch WHERE matchId = @matchId",
            new SqlParameter("@matchId", match.matchId)).SingleOrDefault();

        if (privacy != null && privacy.IsPrivate && privacy.InvitationConfirmedAtUtc == null)
        {
            return new MatchResult<Match>(MatchStatus.MatchCodeUnavailable);
        }

        if (match.MatchParticipant1.Any(participant => participant.accountId == player.AccountId))
        {
            return new MatchResult<Match>(MatchStatus.CannotJoinOwnMatch);
        }

        if (privacy != null && privacy.IsPrivate)
        {
            if (privacy.InvitedAccountId != player.AccountId)
            {
                return new MatchResult<Match>(MatchStatus.Unauthorized);
            }
        }

        PersistentMatchParticipant creator = match.MatchParticipant1.Single();
        if (!context.Account.Any(account => account.accountId == creator.accountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match>(MatchStatus.InactiveAccount);
        }

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
        return new MatchResult<Match>(result);
    }

    /// <inheritdoc />
    public MatchStatus ValidateGuestJoin(string code)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        GameMatch? match = context.GameMatch.SingleOrDefault(candidate =>
            candidate.matchCode == code && candidate.statusCode == "WAITING");
        if (match == null)
        {
            return MatchStatus.MatchCodeUnavailable;
        }

        MatchPrivacyRow? privacy = context.Database.SqlQuery<MatchPrivacyRow>(
            "SELECT isPrivate AS IsPrivate, invitedAccountId AS InvitedAccountId, invitationConfirmedAtUtc AS InvitationConfirmedAtUtc FROM dbo.GameMatch WHERE matchId = @matchId",
            new SqlParameter("@matchId", match.matchId)).SingleOrDefault();

        if (privacy != null && privacy.IsPrivate && privacy.InvitationConfirmedAtUtc == null)
        {
            return MatchStatus.MatchCodeUnavailable;
        }

        return MatchStatus.Unauthorized;
    }

    /// <summary>Loads confirmed match data for an authorized participant.</summary>
    public MatchResult<Match> Get(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        if (accountId > 0)
        {
            if (!context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
            {
                return new MatchResult<Match>(MatchStatus.InactiveAccount);
            }

            PersistentMatchParticipant? participant = context.MatchParticipant.SingleOrDefault(candidate =>
                candidate.matchId == matchId && candidate.accountId == accountId
                && candidate.participantTypeCode == "REGISTERED");
            if (participant == null)
            {
                return new MatchResult<Match>(MatchStatus.Unauthorized);
            }
        }

        GameMatch? match = context.GameMatch
            .Include(candidate => candidate.MatchParticipant1)
            .Include(candidate => candidate.GameMove)
            .SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            return new MatchResult<Match>(MatchStatus.MatchNotFound);
        }

        return new MatchResult<Match>(MatchDatabase.Map(match));
    }

    /// <summary>Changes the duration only while the creator still owns a waiting lobby.</summary>
    public MatchResult<Match> ChangeTurnDuration(long accountId, long matchId, int seconds)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.LobbyLockResource);
        if (!context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match>(MatchStatus.InactiveAccount);
        }

        PersistentMatchParticipant? player = context.MatchParticipant.SingleOrDefault(candidate =>
            candidate.matchId == matchId && candidate.accountId == accountId
            && candidate.participantTypeCode == "REGISTERED");
        if (player == null || !player.isCreator)
        {
            return new MatchResult<Match>(MatchStatus.Unauthorized);
        }

        GameMatch? match = context.GameMatch
            .Include(candidate => candidate.MatchParticipant1)
            .Include(candidate => candidate.GameMove)
            .SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            return new MatchResult<Match>(MatchStatus.MatchNotFound);
        }

        if (match.statusCode != "WAITING")
        {
            return new MatchResult<Match>(MatchStatus.MatchNotWaiting);
        }

        match.turnDurationSeconds = (short)seconds;
        context.SaveChanges();
        Match result = MatchDatabase.Map(match);
        transaction.Commit();
        return new MatchResult<Match>(result);
    }

    /// <summary>Cancels a waiting lobby owned by the requesting account.</summary>
    public MatchStatus Cancel(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.LobbyLockResource);
        if (!context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
        {
            return MatchStatus.InactiveAccount;
        }

        PersistentMatchParticipant? player = context.MatchParticipant.SingleOrDefault(candidate =>
            candidate.matchId == matchId && candidate.accountId == accountId
            && candidate.participantTypeCode == "REGISTERED");
        if (player == null || !player.isCreator)
        {
            return MatchStatus.Unauthorized;
        }

        GameMatch? match = context.GameMatch
            .Include(candidate => candidate.MatchParticipant1)
            .Include(candidate => candidate.GameMove)
            .SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            return MatchStatus.MatchNotFound;
        }

        if (match.statusCode != "WAITING" && match.statusCode != "CANCELLED")
        {
            return MatchStatus.MatchNotWaiting;
        }

        match.statusCode = "CANCELLED";
        context.SaveChanges();
        transaction.Commit();
        return MatchStatus.Success;
    }

    /// <inheritdoc />
    public MatchStatus SaveMove(
        long matchId,
        MatchMove move,
        int consecutiveTimeouts,
        int nextTurnSeatNumber,
        DateTime turnStartedAtUtc)
    {
        if (move == null)
        {
            throw new ArgumentNullException(nameof(move));
        }

        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.MatchLockResource(matchId));

        GameMatch? match = context.GameMatch.SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            return MatchStatus.MatchNotFound;
        }

        if (match.statusCode != "IN_PROGRESS")
        {
            return MatchStatus.MatchNotInProgress;
        }

        InsertMove(context, matchId, move);
        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.MatchParticipant SET consecutiveTimeoutCount = @consecutiveTimeouts "
            + "WHERE matchId = @matchId AND seatNumber = @seatNumber",
            new SqlParameter("@consecutiveTimeouts", SqlDbType.TinyInt)
            {
                Value = (byte)consecutiveTimeouts
            },
            new SqlParameter("@matchId", matchId),
            new SqlParameter("@seatNumber", SqlDbType.TinyInt)
            {
                Value = (byte)move.SeatNumber
            });
        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET currentTurnSeatNumber = @nextTurnSeatNumber, "
            + "turnStartedAtUtc = @turnStartedAtUtc WHERE matchId = @matchId",
            new SqlParameter("@nextTurnSeatNumber", SqlDbType.TinyInt)
            {
                Value = (byte)nextTurnSeatNumber
            },
            new SqlParameter("@turnStartedAtUtc", SqlDbType.DateTime2)
            {
                Value = turnStartedAtUtc
            },
            new SqlParameter("@matchId", matchId));
        transaction.Commit();
        return MatchStatus.Success;
    }

    /// <inheritdoc />
    public MatchStatus SaveMoveAndConclude(
        long matchId,
        MatchMove move,
        int consecutiveTimeouts,
        string endReason,
        int? winnerSeatNumber,
        int blackPieceCount,
        int whitePieceCount,
        DateTime endedAtUtc)
    {
        if (move == null)
        {
            throw new ArgumentNullException(nameof(move));
        }

        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.MatchLockResource(matchId));

        GameMatch? match = context.GameMatch.SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            return MatchStatus.MatchNotFound;
        }

        if (match.statusCode != "IN_PROGRESS")
        {
            return MatchStatus.MatchNotInProgress;
        }

        InsertMove(context, matchId, move);
        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.MatchParticipant SET "
            + "consecutiveTimeoutCount = CASE WHEN seatNumber = @actingSeatNumber "
            + "THEN @consecutiveTimeouts ELSE consecutiveTimeoutCount END, "
            + "finalPieceCount = CASE WHEN seatNumber = 1 THEN @blackPieceCount "
            + "WHEN seatNumber = 2 THEN @whitePieceCount ELSE finalPieceCount END, "
            + "resultCode = CASE WHEN @winnerSeatNumber IS NULL THEN 'DRAW' "
            + "WHEN seatNumber = @winnerSeatNumber THEN 'WIN' ELSE 'LOSS' END "
            + "WHERE matchId = @matchId",
            new SqlParameter("@actingSeatNumber", SqlDbType.TinyInt)
            {
                Value = (byte)move.SeatNumber
            },
            new SqlParameter("@consecutiveTimeouts", SqlDbType.TinyInt)
            {
                Value = (byte)consecutiveTimeouts
            },
            new SqlParameter("@blackPieceCount", SqlDbType.TinyInt)
            {
                Value = (byte)blackPieceCount
            },
            new SqlParameter("@whitePieceCount", SqlDbType.TinyInt)
            {
                Value = (byte)whitePieceCount
            },
            new SqlParameter("@winnerSeatNumber", SqlDbType.TinyInt)
            {
                Value = winnerSeatNumber.HasValue
                    ? (object)(byte)winnerSeatNumber.Value
                    : DBNull.Value
            },
            new SqlParameter("@matchId", matchId));
        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET statusCode = 'FINISHED', endReasonCode = @endReason, "
            + "endedAtUtc = @endedAtUtc, currentTurnSeatNumber = NULL, turnStartedAtUtc = NULL "
            + "WHERE matchId = @matchId",
            new SqlParameter("@endReason", endReason),
            new SqlParameter("@endedAtUtc", SqlDbType.DateTime2)
            {
                Value = endedAtUtc
            },
            new SqlParameter("@matchId", matchId));
        transaction.Commit();
        return MatchStatus.Success;
    }

    private static void InsertMove(
        ReversiGameSystemEntities context,
        long matchId,
        MatchMove move)
    {
        context.Database.ExecuteSqlCommand(
            "INSERT dbo.GameMove "
            + "(matchId, moveNumber, seatNumber, rowIndex, columnIndex, moveSourceCode, playedAtUtc) "
            + "VALUES (@matchId, @moveNumber, @seatNumber, @rowIndex, @columnIndex, "
            + "@moveSourceCode, @playedAtUtc)",
            new SqlParameter("@matchId", matchId),
            new SqlParameter("@moveNumber", move.MoveNumber),
            new SqlParameter("@seatNumber", SqlDbType.TinyInt)
            {
                Value = (byte)move.SeatNumber
            },
            new SqlParameter("@rowIndex", SqlDbType.TinyInt)
            {
                Value = (byte)move.Row
            },
            new SqlParameter("@columnIndex", SqlDbType.TinyInt)
            {
                Value = (byte)move.Column
            },
            new SqlParameter("@moveSourceCode", MatchDatabase.MoveSourceCode(move.Origin)),
            new SqlParameter("@playedAtUtc", SqlDbType.DateTime2)
            {
                Value = move.PlayedAtUtc
            });
    }

    /// <inheritdoc />
    public MatchStatus ConcludeMatch(
        long matchId,
        string endReason,
        int? winnerSeatNumber,
        int? abandoningSeatNumber,
        int blackPieceCount,
        int whitePieceCount,
        DateTime endedAtUtc)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.MatchLockResource(matchId));

        GameMatch? match = context.GameMatch.SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            return MatchStatus.MatchNotFound;
        }

        if (match.statusCode == "FINISHED")
        {
            return MatchStatus.Success;
        }

        if (match.statusCode != "IN_PROGRESS")
        {
            return MatchStatus.MatchNotInProgress;
        }

        match.statusCode = "FINISHED";
        match.endReasonCode = endReason;
        match.endedAtUtc = endedAtUtc;
        match.currentTurnSeatNumber = null;
        match.turnStartedAtUtc = null;
        context.SaveChanges();

        foreach (PersistentMatchParticipant current in match.MatchParticipant1)
        {
            if (current.seatNumber == 1)
            {
                current.finalPieceCount = (byte)blackPieceCount;
            }
            else if (current.seatNumber == 2)
            {
                current.finalPieceCount = (byte)whitePieceCount;
            }

            if (abandoningSeatNumber.HasValue && current.seatNumber == (byte)abandoningSeatNumber.Value)
            {
                current.resultCode = "ABANDONED";
            }
            else if (!winnerSeatNumber.HasValue)
            {
                current.resultCode = "DRAW";
            }
            else if (winnerSeatNumber.HasValue && current.seatNumber == (byte)winnerSeatNumber.Value)
            {
                current.resultCode = "WIN";
            }
            else
            {
                current.resultCode = "LOSS";
            }
        }

        context.SaveChanges();
        transaction.Commit();
        return MatchStatus.Success;
    }

    /// <inheritdoc />
    public MatchResult<Match[]> GetMatchHistory(long accountId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        if (!context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match[]>(MatchStatus.InactiveAccount);
        }

        GameMatch[] matches = context.GameMatch
            .Include(candidate => candidate.MatchParticipant1)
            .Include(candidate => candidate.GameMove)
            .Where(candidate => candidate.MatchParticipant1.Any(player =>
                player.accountId == accountId && player.participantTypeCode == "REGISTERED")
                && candidate.statusCode == "FINISHED")
            .OrderByDescending(candidate => candidate.createdAtUtc)
            .ToArray();

        List<Match> results = new List<Match>(matches.Length);
        for (int i = 0; i < matches.Length; i++)
        {
            results.Add(MatchDatabase.Map(matches[i]));
        }

        return new MatchResult<Match[]>(results.ToArray());
    }

    /// <inheritdoc />
    public MatchResult<Match> GetForObservation(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        if (accountId > 0 && !context.Account.Any(account => account.accountId == accountId && account.statusCode == "ACTIVE"))
        {
            return new MatchResult<Match>(MatchStatus.InactiveAccount);
        }

        GameMatch? match = context.GameMatch
            .Include(candidate => candidate.MatchParticipant1)
            .Include(candidate => candidate.GameMove)
            .SingleOrDefault(candidate => candidate.matchId == matchId);
        if (match == null)
        {
            return new MatchResult<Match>(MatchStatus.MatchNotFound);
        }

        if (match.statusCode != "IN_PROGRESS")
        {
            return new MatchResult<Match>(MatchStatus.SpectatingUnavailable);
        }

        return new MatchResult<Match>(MatchDatabase.Map(match));
    }



    private static PersistentMatchParticipant CreateParticipant(
        AuthenticatedSession player,
        byte seat,
        DateTime now)
    {
        return new PersistentMatchParticipant
        {
            seatNumber = seat,
            accountId = player.AccountId,
            participantTypeCode = "REGISTERED",
            displayNameSnapshot = player.Username,
            pieceColorCode = seat == 1 ? "BLACK" : "WHITE",
            joinedAtUtc = now,
            isCreator = seat == 1
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

