using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Matches;
using ReversiGameSystem.Server.Persistence.Model;
using ApplicationMatchParticipant = ReversiGameSystem.Server.Application.Matches.MatchParticipant;
using PersistentMatchParticipant = ReversiGameSystem.Server.Persistence.Model.MatchParticipant;


namespace ReversiGameSystem.Server.Persistence.Matches;

/// <summary>Persists atomic human lobby transitions using transaction-owned database locks.</summary>
public sealed class MatchRepository : IMatchRepository, IGameplayRepository
{
    private const string FinishedMatchStatusCode = "FINISHED";
    private const string InProgressMatchStatusCode = "IN_PROGRESS";

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
    public MatchResult<Match> LoadParticipantMatch(long accountId, long matchId)
    {
        return LoadMatch(accountId, matchId);
    }

    public MatchResult<Match> LoadMatch(long matchId)
    {
        return LoadMatch(0, matchId);
    }

    private static MatchResult<Match> LoadMatch(long accountId, long matchId)
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

    public bool SynchronizeMatch(Match match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        bool synchronized;
        try
        {
            SynchronizeMatchState(match);
            synchronized = true;
        }
        catch (DbException)
        {
            synchronized = false;
        }
        catch (EntityException)
        {
            synchronized = false;
        }
        catch (TimeoutException)
        {
            synchronized = false;
        }

        return synchronized;
    }

    private static void SynchronizeMatchState(Match match)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(context, MatchDatabase.MatchLockResource(match.Id));

        GameMatch? persistentMatch = context.GameMatch
            .Include(candidate => candidate.MatchParticipant1)
            .Include(candidate => candidate.GameMove)
            .SingleOrDefault(candidate => candidate.matchId == match.Id);
        if (persistentMatch == null)
        {
            throw new InvalidOperationException("The active match does not exist in persistence.");
        }

        int persistentVersion = persistentMatch.GameMove.Count == 0
            ? 0
            : persistentMatch.GameMove.Max(move => move.moveNumber);
        bool snapshotFinished = string.Equals(match.State, "Finished", StringComparison.Ordinal);
        bool persistenceFinished = string.Equals(
            persistentMatch.statusCode,
            FinishedMatchStatusCode,
            StringComparison.Ordinal);

        if (match.MatchStateVersion < persistentVersion
            || persistenceFinished && !snapshotFinished)
        {
            transaction.Commit();
            return;
        }

        HashSet<int> persistentMoveNumbers = new HashSet<int>(
            persistentMatch.GameMove.Select(move => move.moveNumber));
        MatchMove[] missingMoves = match.Moves
            .Where(move => !persistentMoveNumbers.Contains(move.MoveNumber))
            .OrderBy(move => move.MoveNumber)
            .ToArray();
        for (int index = 0; index < missingMoves.Length; index++)
        {
            InsertMove(context, match.Id, missingMoves[index]);
        }

        if (snapshotFinished)
        {
            ApplyFinishedState(context, match);
        }
        else if (string.Equals(match.State, "InProgress", StringComparison.Ordinal))
        {
            ApplyActiveState(context, match);
        }
        else
        {
            throw new InvalidOperationException("Only active or finished matches can be synchronized.");
        }

        ApplyParticipantState(context, match);
        transaction.Commit();
    }

    private static void ApplyActiveState(ReversiGameSystemEntities context, Match match)
    {
        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET statusCode = @statusCode, "
            + "currentTurnSeatNumber = @currentTurnSeatNumber, turnStartedAtUtc = @turnStartedAtUtc "
            + "WHERE matchId = @matchId",
            new SqlParameter("@statusCode", InProgressMatchStatusCode),
            new SqlParameter("@currentTurnSeatNumber", SqlDbType.TinyInt)
            {
                Value = match.CurrentTurnSeatNumber.HasValue
                    ? (object)(byte)match.CurrentTurnSeatNumber.Value
                    : DBNull.Value
            },
            new SqlParameter("@turnStartedAtUtc", SqlDbType.DateTime2)
            {
                Value = match.TurnStartedAtUtc.HasValue
                    ? (object)match.TurnStartedAtUtc.Value
                    : DBNull.Value
            },
            new SqlParameter("@matchId", match.Id));
    }

    private static void ApplyFinishedState(ReversiGameSystemEntities context, Match match)
    {
        context.Database.ExecuteSqlCommand(
            "UPDATE dbo.GameMatch SET statusCode = @statusCode, endReasonCode = @endReasonCode, "
            + "endedAtUtc = @endedAtUtc, currentTurnSeatNumber = NULL, turnStartedAtUtc = NULL "
            + "WHERE matchId = @matchId",
            new SqlParameter("@statusCode", FinishedMatchStatusCode),
            new SqlParameter("@endReasonCode", (object?)match.EndReason ?? DBNull.Value),
            new SqlParameter("@endedAtUtc", SqlDbType.DateTime2)
            {
                Value = match.EndedAtUtc.HasValue
                    ? (object)match.EndedAtUtc.Value
                    : DBNull.Value
            },
            new SqlParameter("@matchId", match.Id));
    }

    private static void ApplyParticipantState(
        ReversiGameSystemEntities context,
        Match match)
    {
        int blackPieceCount = match.Board.Count(cell => cell == ReversiEngine.Black);
        int whitePieceCount = match.Board.Count(cell => cell == ReversiEngine.White);

        for (int index = 0; index < match.Participants.Length; index++)
        {
            ApplicationMatchParticipant participant = match.Participants[index];

            if (string.Equals(match.State, "Finished", StringComparison.Ordinal))
            {
                int finalPieceCount = participant.SeatNumber == 1
                    ? blackPieceCount
                    : whitePieceCount;
                string resultCode = DetermineParticipantResult(
                    participant.SeatNumber,
                    match.WinnerSeatNumber,
                    match.AbandoningSeatNumber);
                context.Database.ExecuteSqlCommand(
                    "UPDATE dbo.MatchParticipant SET consecutiveTimeoutCount = @timeoutCount, "
                    + "finalPieceCount = @finalPieceCount, resultCode = @resultCode "
                    + "WHERE matchId = @matchId AND seatNumber = @seatNumber",
                    CreateByteParameter("@timeoutCount", participant.ConsecutiveTimeoutCount),
                    CreateByteParameter("@finalPieceCount", finalPieceCount),
                    new SqlParameter("@resultCode", resultCode),
                    new SqlParameter("@matchId", match.Id),
                    CreateByteParameter("@seatNumber", participant.SeatNumber));
            }
            else
            {
                context.Database.ExecuteSqlCommand(
                    "UPDATE dbo.MatchParticipant SET consecutiveTimeoutCount = @timeoutCount "
                    + "WHERE matchId = @matchId AND seatNumber = @seatNumber",
                    CreateByteParameter("@timeoutCount", participant.ConsecutiveTimeoutCount),
                    new SqlParameter("@matchId", match.Id),
                    CreateByteParameter("@seatNumber", participant.SeatNumber));
            }
        }
    }

    private static SqlParameter CreateByteParameter(string name, int value)
    {
        return new SqlParameter(name, SqlDbType.TinyInt)
        {
            Value = (byte)value
        };
    }

    private static string DetermineParticipantResult(
        int seatNumber,
        int? winnerSeatNumber,
        int? abandoningSeatNumber)
    {
        string resultCode;
        if (abandoningSeatNumber == seatNumber)
        {
            resultCode = "ABANDONED";
        }
        else if (!winnerSeatNumber.HasValue)
        {
            resultCode = "DRAW";
        }
        else if (winnerSeatNumber == seatNumber)
        {
            resultCode = "WIN";
        }
        else
        {
            resultCode = "LOSS";
        }

        return resultCode;
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
    public MatchResult<Match[]> LoadMatchHistory(long accountId)
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
    public MatchResult<Match> LoadMatchForObservation(long accountId, long matchId)
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

