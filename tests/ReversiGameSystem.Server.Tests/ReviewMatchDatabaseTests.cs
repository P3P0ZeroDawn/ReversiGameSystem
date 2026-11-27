using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Chat;
using ReversiGameSystem.Server.Domain.Matches;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Matches;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Server.Tests;

internal static class ReviewMatchDatabaseTests
{
    internal static void Run() => ReviewAccountDatabaseTests.RunWithReviewAccounts(Check);

    private static void Check(
        SessionApplicationService sessions,
        SessionStore store)
    {
        // Arrange
        ReviewMatchHost.PrepareSchema();
        AuthenticatedSession alphaSession = sessions.CompleteLogin("ReviewAlpha", "000001")
            .Value!;
        string alpha = alphaSession.SessionToken;
        long alphaAccountId = alphaSession.AccountId;
        string beta = sessions.CompleteLogin("ReviewBeta", "000002").Value!.SessionToken;
        MatchRepository repository = new MatchRepository(NullLogger<MatchRepository>.Instance);
        MatchApplicationService matches = new MatchApplicationService(
            store, repository, new TestUtcClock());
        DateTime now = DateTime.UtcNow;
        DateTime wholeSecondUtc = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
        MutableTestClock chatClock = new MutableTestClock(wholeSecondUtc);
        ChatApplicationService chat = new ChatApplicationService(
            store, new ChatRepository(chatClock), chatClock);
        Match waiting = matches.CreateMatch(alpha, new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act / Assert: a rejected operation must leave the waiting match unchanged.
        Assert.AreEqual(0, (matches.GetAvailableMatches(alpha).Value
            ?? throw new InvalidOperationException("Matches expected.")).Length);
        Assert.AreEqual(waiting.Id, (matches.GetAvailableMatches(beta).Value
            ?? throw new InvalidOperationException("Matches expected.")).Single().Id);
        Assert.AreEqual(
            MatchStatus.CannotJoinOwnMatch,
            matches.JoinMatch(alpha, waiting.Code!).Status);
        Assert.AreEqual(
            ChatStatus.MatchNotInProgress,
            chat.LoadChatHistory(alpha, waiting.Id).Status);
        Assert.AreEqual(
            ChatStatus.Unauthorized,
            chat.LoadChatHistory(beta, waiting.Id).Status);
        Join_ConcurrentRequests_RegistersOneSecondSeat(matches, beta, waiting);
        Assert.AreEqual("InProgress", (matches.GetParticipantMatch(alpha, waiting.Id).Value
            ?? throw new InvalidOperationException("Match expected.")).State);
        Assert.AreEqual(2, (matches.GetParticipantMatch(beta, waiting.Id).Value
            ?? throw new InvalidOperationException("Match expected.")).Participants.Length);
        Assert.AreEqual(0, (matches.GetAvailableMatches(beta).Value
            ?? throw new InvalidOperationException("Matches expected.")).Length);
        Send_ConcurrentRequests_EnforcesAccountLimit(chat, chatClock, alpha, waiting.Id);
        Moderate_RepeatedMessages_CommitsAccountMute(chat, alpha, waiting.Id);
        Gameplay_RealMovePersistence_UpdatesBoardAndHistory(
            new MatchRepository(NullLogger<MatchRepository>.Instance), alphaAccountId, waiting.Id);
        Cancel_RepeatedRequest_InvalidatesCode(matches, alpha, beta);
        Restart_AbandonedWaitingMatch_InvalidatesCode(matches, alpha, beta);
        Join_InactiveCreator_RejectsWithoutOccupyingSecondSeat(matches, alpha, beta);
        sessions.CloseSession(alpha);
        Assert.AreEqual(
            MatchStatus.InvalidSession,
            matches.GetAvailableMatches(alpha).Status);
        Match systemLobby = matches.CreateMatch(beta, new MatchSettings()).Value
            ?? throw new InvalidOperationException("A system lobby was expected.");
        Match systemMatch = matches.StartSystemMatch(beta, systemLobby.Id).Value
            ?? throw new InvalidOperationException("A system match was expected.");
        Assert.AreEqual(false, systemMatch.CountsTowardStatistics);
        Assert.AreEqual(ChatStatus.Unauthorized,
            chat.LoadChatHistory(beta, systemMatch.Id).Status);
        MatchParticipant systemParticipant = systemMatch.Participants
            .Single(player => player.SeatNumber == 2);
        Assert.AreEqual(0L, systemParticipant.AccountId);
        Assert.AreEqual(true, systemParticipant.IsSystem);
        systemMatch.Board = ReversiEngine.CreateInitialBoard();
        systemMatch.CurrentTurnSeatNumber = 1;
        systemMatch.TurnStartedAtUtc = DateTime.UtcNow;
        MatchPlacement placement = new MatchPlacement(systemMatch.Id, 0, 2, 3);
        Assert.AreEqual(MatchStatus.Success,
            GameplayMatchRules.ApplyPlayerMove(systemMatch, placement, DateTime.UtcNow));
        DateTime systemTurnStart = systemMatch.TurnStartedAtUtc.Value;
        Assert.AreEqual(false,
            GameplayMatchRules.ApplySystemMoveIfReady(systemMatch, systemTurnStart.AddSeconds(4)));
        Assert.AreEqual(true,
            GameplayMatchRules.ApplySystemMoveIfReady(systemMatch, systemTurnStart.AddSeconds(5)));
        Assert.AreEqual<int?>(1, systemMatch.CurrentTurnSeatNumber);
        Assert.AreEqual(systemTurnStart.AddSeconds(5), systemMatch.TurnStartedAtUtc.Value);
        Assert.AreEqual(0, systemMatch.Participants[1].ConsecutiveTimeoutCount);
        MatchMove systemMove = systemMatch.LastMove
            ?? throw new InvalidOperationException("A system move was expected.");
        Assert.AreEqual("System", systemMove.Origin);
        Assert.AreEqual(true, repository.SynchronizeMatch(systemMatch));
        Console.WriteLine("Passed real match SQL checks: atomic join, chat ordering, "
            + "moderation, authorization, cancellation and session revocation.");
    }

    private static void Join_ConcurrentRequests_RegistersOneSecondSeat(
        MatchApplicationService matches, string token, Match waiting)
    {
        // Arrange / Act
        Task<bool>[] joins = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            MatchResult<Match> result = matches.JoinMatch(token, waiting.Code!);
            return result.Status == MatchStatus.Success;
        })).ToArray();
        Task.WaitAll(joins);

        // Assert
        Assert.AreEqual(1, joins.Count(task => task.Result));
    }

    private static void Send_ConcurrentRequests_EnforcesAccountLimit(
        ChatApplicationService chat, MutableTestClock clock, string token, long id)
    {
        // Arrange / Act
        Task<ChatMessageSubmission>[] sends = Enumerable.Range(0, 3)
            .Select(number => Task.Run(() => chat.SubmitChatMessage(
                token,
                id,
                "Message " + number)))
            .ToArray();
        Task.WaitAll(sends);

        // Assert
        Assert.AreEqual(2, sends.Count(task => task.Result.Status == ChatStatus.Success));
        Assert.AreEqual(1, sends.Count(task => task.Result.Status == ChatStatus.RateLimitExceeded));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.AreEqual(ChatStatus.Success,
            chat.SubmitChatMessage(token, id, "Next message").Status);
        ChatMessage[] history = chat.LoadChatHistory(token, id).Messages;
        Assert.AreEqual(3, history.Length);
        Assert.IsTrue(history.Select(entry => entry.MessageNumber)
            .SequenceEqual(Enumerable.Range(1, 3)), "Accepted messages must keep their order.");
    }

    private static void Moderate_RepeatedMessages_CommitsAccountMute(
        ChatApplicationService chat, string token, long id)
    {
        // Arrange
        Assert.AreEqual(
            ChatStatus.EmptyMessage,
            chat.SubmitChatMessage(token, id, " ").Status);
        Assert.AreEqual(
            ChatStatus.MessageTooLong,
            chat.SubmitChatMessage(
                token,
                id,
                new string('a', ChatContentPolicy.MaximumMessageLength + 1)).Status);

        // Act / Assert
        for (int attempt = 0; attempt < 3; attempt++)
        {
            ChatMessageSubmission blocked = chat.SubmitChatMessage(
                token,
                id,
                "BLOCKEDWORD!");
            Assert.AreEqual(
                attempt < 2
                    ? ChatStatus.MessageBlocked
                    : ChatStatus.PlayerMuted,
                blocked.Status);
        }
        ChatMessageSubmission muted = chat.SubmitChatMessage(
            token,
            id,
            "Otherwise acceptable");
        Assert.AreEqual(ChatStatus.PlayerMuted, muted.Status);
        Assert.IsTrue(
            muted.Mute is not null,
            "A muted result must contain its durable sanction.");
        Assert.AreEqual(3, chat.LoadChatHistory(token, id).Messages.Length);
    }

    private static void Cancel_RepeatedRequest_InvalidatesCode(
        MatchApplicationService matches, string creator, string other)
    {
        // Arrange
        Match waiting = matches.CreateMatch(creator, new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act
        Assert.AreEqual(
            MatchStatus.Success,
            matches.CancelMatch(creator, waiting.Id));
        Assert.AreEqual(
            MatchStatus.Success,
            matches.CancelMatch(creator, waiting.Id));

        // Assert
        Assert.AreEqual(
            "Cancelled",
            (matches.GetParticipantMatch(creator, waiting.Id).Value
                ?? throw new InvalidOperationException("Match expected.")).State);
        Assert.AreEqual(
            MatchStatus.MatchCodeUnavailable,
            matches.JoinMatch(other, waiting.Code!).Status);
    }

    private static void Restart_AbandonedWaitingMatch_InvalidatesCode(
        MatchApplicationService matches, string creator, string other)
    {
        // Arrange
        Match abandoned = matches.CreateMatch(creator, new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");
        MatchRepository restartedStore = new MatchRepository(NullLogger<MatchRepository>.Instance);

        // Act
        Assert.AreEqual(1, restartedStore.CancelAbandonedWaitingMatches());

        // Assert
        Assert.AreEqual(0, restartedStore.CancelAbandonedWaitingMatches());
        Assert.AreEqual(
            "Cancelled",
            (matches.GetParticipantMatch(creator, abandoned.Id).Value
                ?? throw new InvalidOperationException("Match expected.")).State);
        Assert.AreEqual(
            MatchStatus.MatchCodeUnavailable,
            matches.JoinMatch(other, abandoned.Code!).Status);
    }

    private static void Join_InactiveCreator_RejectsWithoutOccupyingSecondSeat(
        MatchApplicationService matches, string creator, string other)
    {
        // Arrange
        Match waiting = matches.CreateMatch(creator, new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");
        SetCreatorStatus("INACTIVE");
        try
        {
            // Act / Assert
            Assert.AreEqual(0, (matches.GetAvailableMatches(other).Value
                ?? throw new InvalidOperationException("Matches expected.")).Length);
            Assert.AreEqual(
                MatchStatus.InactiveAccount,
                matches.JoinMatch(other, waiting.Code!).Status);
        }
        finally
        {
            SetCreatorStatus("ACTIVE");
        }
        Assert.AreEqual(
            1,
            (matches.GetParticipantMatch(creator, waiting.Id).Value
                ?? throw new InvalidOperationException("Match expected.")).Participants.Length);
        Assert.AreEqual(
            MatchStatus.Success,
            matches.CancelMatch(creator, waiting.Id));
    }

    private static void SetCreatorStatus(string status)
    {
        using SqlConnection connection = new SqlConnection(
            Environment.GetEnvironmentVariable("REVERSI_DB_CONNECTION_STRING"));
        connection.Open();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.Account SET statusCode=@status WHERE username=N'ReviewAlpha'";
        command.Parameters.AddWithValue("@status", status);
        Assert.AreEqual(1, command.ExecuteNonQuery());
    }

    private static MatchPlayer CreateMatchPlayer(long accountId)
    {
        return new MatchPlayer(new AuthenticatedSession("review-token", accountId,
            "Review", "review@example.test", "en-US", DateTime.UtcNow.AddHours(1)));
    }

    private static void Gameplay_RealMovePersistence_UpdatesBoardAndHistory(
        MatchRepository repository,
        long accountId,
        long matchId)
    {
        Match match = repository.LoadParticipantMatch(CreateMatchPlayer(accountId), matchId).Value
            ?? throw new InvalidOperationException("Match expected.");
        int[] flippedPieces = ReversiEngine.ApplyMove(match.Board, 2, 3, ReversiEngine.Black);
        MatchMove move = new MatchMove
        {
            MoveNumber = 1,
            SeatNumber = 1,
            Row = 2,
            Column = 3,
            PlayedAtUtc = DateTime.UtcNow,
            FlippedPieceCount = flippedPieces.Length,
            Origin = "Player"
        };
        match.MatchStateVersion = move.MoveNumber;
        match.Moves.Add(move);
        match.LastMove = move;
        match.CurrentTurnSeatNumber = 2;
        match.TurnStartedAtUtc = move.PlayedAtUtc;
        Assert.IsTrue(
            repository.SynchronizeMatch(match),
            "Expected the manual move snapshot to be synchronized.");

        flippedPieces = ReversiEngine.ApplyMove(match.Board, 2, 2, ReversiEngine.White);
        MatchMove timeoutMove = new MatchMove
        {
            MoveNumber = 2,
            SeatNumber = 2,
            Row = 2,
            Column = 2,
            PlayedAtUtc = DateTime.UtcNow,
            FlippedPieceCount = flippedPieces.Length,
            Origin = "Timeout"
        };
        match.MatchStateVersion = timeoutMove.MoveNumber;
        match.Moves.Add(timeoutMove);
        match.LastMove = timeoutMove;
        match.CurrentTurnSeatNumber = 1;
        match.TurnStartedAtUtc = timeoutMove.PlayedAtUtc;
        match.Participants.Single(participant => participant.SeatNumber == 2)
            .ConsecutiveTimeoutCount = 1;
        Assert.IsTrue(
            repository.SynchronizeMatch(match),
            "Expected the timeout move snapshot to be synchronized.");

        Match loaded = repository.LoadParticipantMatch(CreateMatchPlayer(accountId), matchId).Value
            ?? throw new InvalidOperationException("Match expected.");
        Assert.AreEqual(2, loaded.Moves.Count);
        Assert.AreEqual(1, loaded.CurrentTurnSeatNumber);
        Assert.AreEqual(1, loaded.LastMove?.FlippedPieceCount);
        Assert.AreEqual("Timeout", loaded.LastMove?.Origin);
        Assert.AreEqual(ReversiEngine.White, loaded.Board[(2 * ReversiEngine.BoardSize) + 2]);

        match.State = "Finished";
        match.EndReason = "ABANDONMENT";
        match.WinnerSeatNumber = 1;
        match.AbandoningSeatNumber = 2;
        match.EndedAtUtc = DateTime.UtcNow;
        match.CurrentTurnSeatNumber = null;
        match.TurnStartedAtUtc = null;
        Assert.IsTrue(
            repository.SynchronizeMatch(match),
            "Expected the finished match snapshot to be synchronized.");
        Assert.IsTrue(
            repository.SynchronizeMatch(match),
            "Expected repeated synchronization to remain idempotent.");

        MatchResult<Match[]> history = repository.LoadMatchHistory(accountId);
        Assert.AreEqual(MatchStatus.Success, history.Status);
        Assert.IsTrue(
            history.Value != null && history.Value.Any(m =>
                m.Id == matchId
                && m.State == "Finished"
                && m.EndReason == "ABANDONMENT"
                && m.WinnerSeatNumber == 1
                && m.AbandoningSeatNumber == 2),
            "Expected historical match to reflect completed abandonment state, winner, and abandoner.");
    }
}
