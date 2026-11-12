using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Domain.Chat;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Matches;

namespace ReversiGameSystem.Server.Tests;

internal static class ReviewMatchDatabaseTests
{
    internal static void Run() => ReviewAccountDatabaseTests.RunWithReviewAccounts(Check);

    private static void Check(
        SessionApplicationService sessions,
        ISessionRepository store)
    {
        // Arrange
        ReviewMatchHost.PrepareSchema();
        AuthenticatedSession alphaSession = sessions.CompleteLogin("ReviewAlpha", "000001")
            .GetValue();
        string alpha = alphaSession.SessionToken;
        long alphaAccountId = alphaSession.AccountId;
        string beta = sessions.CompleteLogin("ReviewBeta", "000002")
            .GetValue().SessionToken;
        MatchApplicationService matches = new MatchApplicationService(store, new MatchRepository(), new SystemUtcClock());
        ChatApplicationService chat = new ChatApplicationService(store, new ChatRepository(), new SystemUtcClock());
        Match waiting = matches.Create(alpha, new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act / Assert: a rejected operation must leave the waiting match unchanged.
        TestAssert.AreEqual(0, (matches.Available(alpha).Value
            ?? throw new InvalidOperationException("Matches expected.")).Length);
        TestAssert.AreEqual(waiting.Id, (matches.Available(beta).Value
            ?? throw new InvalidOperationException("Matches expected.")).Single().Id);
        TestAssert.AreEqual(
            MatchStatus.CannotJoinOwnMatch,
            matches.Join(alpha, waiting.Code!).Status);
        TestAssert.AreEqual(
            ChatStatus.MatchNotInProgress,
            chat.LoadChatHistory(alpha, waiting.Id).Status);
        TestAssert.AreEqual(
            ChatStatus.Unauthorized,
            chat.LoadChatHistory(beta, waiting.Id).Status);
        Join_ConcurrentRequests_RegistersOneSecondSeat(matches, beta, waiting);
        TestAssert.AreEqual("InProgress", (matches.Get(alpha, waiting.Id).Value
            ?? throw new InvalidOperationException("Match expected.")).State);
        TestAssert.AreEqual(2, (matches.Get(beta, waiting.Id).Value
            ?? throw new InvalidOperationException("Match expected.")).Participants.Length);
        TestAssert.AreEqual(0, (matches.Available(beta).Value
            ?? throw new InvalidOperationException("Matches expected.")).Length);
        Send_ConcurrentRequests_AssignsUniqueOrderedNumbers(chat, alpha, waiting.Id);
        Moderate_RepeatedMessages_CommitsAccountMute(chat, alpha, waiting.Id);
        Gameplay_RealMovePersistence_UpdatesBoardAndHistory(new MatchRepository(), alphaAccountId, waiting.Id);
        Cancel_RepeatedRequest_InvalidatesCode(matches, alpha, beta);
        Restart_AbandonedWaitingMatch_InvalidatesCode(matches, alpha, beta);
        Join_InactiveCreator_RejectsWithoutOccupyingSecondSeat(matches, alpha, beta);
        sessions.CloseSession(alpha);
        TestAssert.AreEqual(
            MatchStatus.InvalidSession,
            matches.Available(alpha).Status);
        Console.WriteLine("Passed real match SQL checks: atomic join, chat ordering, "
            + "moderation, authorization, cancellation and session revocation.");
    }

    private static void Join_ConcurrentRequests_RegistersOneSecondSeat(
        MatchApplicationService matches, string token, Match waiting)
    {
        // Arrange / Act
        Task<bool>[] joins = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            MatchResult<Match> result = matches.Join(token, waiting.Code!);
            return result.Status == MatchStatus.Success;
        })).ToArray();
        Task.WaitAll(joins);

        // Assert
        TestAssert.AreEqual(1, joins.Count(task => task.Result));
    }

    private static void Send_ConcurrentRequests_AssignsUniqueOrderedNumbers(
        ChatApplicationService chat, string token, long id)
    {
        // Arrange / Act
        Task<ChatMessageSubmission>[] sends = Enumerable.Range(0, 8)
            .Select(number => Task.Run(() => chat.SubmitChatMessage(
                token,
                id,
                "Message " + number)))
            .ToArray();
        Task.WaitAll(sends);

        // Assert
        TestAssert.IsTrue(
            sends.All(task => task.Result.Status == ChatStatus.Success),
            "Every concurrent message must be accepted.");
        ChatMessage[] history = chat.LoadChatHistory(token, id).Messages;
        TestAssert.AreEqual(8, history.Length);
        TestAssert.IsTrue(history.Select(entry => entry.MessageNumber)
            .SequenceEqual(Enumerable.Range(1, 8)), "Concurrent messages must have stable unique numbers.");
    }

    private static void Moderate_RepeatedMessages_CommitsAccountMute(
        ChatApplicationService chat, string token, long id)
    {
        // Arrange
        TestAssert.AreEqual(
            ChatStatus.EmptyMessage,
            chat.SubmitChatMessage(token, id, " ").Status);
        TestAssert.AreEqual(
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
            TestAssert.AreEqual(
                attempt < 2
                    ? ChatStatus.MessageBlocked
                    : ChatStatus.PlayerMuted,
                blocked.Status);
        }
        ChatMessageSubmission muted = chat.SubmitChatMessage(
            token,
            id,
            "Otherwise acceptable");
        TestAssert.AreEqual(ChatStatus.PlayerMuted, muted.Status);
        TestAssert.IsTrue(
            muted.Mute is not null,
            "A muted result must contain its durable sanction.");
        TestAssert.AreEqual(8, chat.LoadChatHistory(token, id).Messages.Length);
    }

    private static void Cancel_RepeatedRequest_InvalidatesCode(
        MatchApplicationService matches, string creator, string other)
    {
        // Arrange
        Match waiting = matches.Create(creator, new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");

        // Act
        TestAssert.AreEqual(
            MatchStatus.Success,
            matches.Cancel(creator, waiting.Id));
        TestAssert.AreEqual(
            MatchStatus.Success,
            matches.Cancel(creator, waiting.Id));

        // Assert
        TestAssert.AreEqual(
            "Cancelled",
            (matches.Get(creator, waiting.Id).Value
                ?? throw new InvalidOperationException("Match expected.")).State);
        TestAssert.AreEqual(
            MatchStatus.MatchCodeUnavailable,
            matches.Join(other, waiting.Code!).Status);
    }

    private static void Restart_AbandonedWaitingMatch_InvalidatesCode(
        MatchApplicationService matches, string creator, string other)
    {
        // Arrange
        Match abandoned = matches.Create(creator, new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");
        MatchRepository restartedStore = new MatchRepository();

        // Act
        TestAssert.AreEqual(1, restartedStore.CancelAbandonedWaitingMatches());

        // Assert
        TestAssert.AreEqual(0, restartedStore.CancelAbandonedWaitingMatches());
        TestAssert.AreEqual(
            "Cancelled",
            (matches.Get(creator, abandoned.Id).Value
                ?? throw new InvalidOperationException("Match expected.")).State);
        TestAssert.AreEqual(
            MatchStatus.MatchCodeUnavailable,
            matches.Join(other, abandoned.Code!).Status);
    }

    private static void Join_InactiveCreator_RejectsWithoutOccupyingSecondSeat(
        MatchApplicationService matches, string creator, string other)
    {
        // Arrange
        Match waiting = matches.Create(creator, new MatchSettings()).Value
            ?? throw new InvalidOperationException("Match expected.");
        SetCreatorStatus("INACTIVE");
        try
        {
            // Act / Assert
            TestAssert.AreEqual(0, (matches.Available(other).Value
                ?? throw new InvalidOperationException("Matches expected.")).Length);
            TestAssert.AreEqual(
                MatchStatus.InactiveAccount,
                matches.Join(other, waiting.Code!).Status);
        }
        finally
        {
            SetCreatorStatus("ACTIVE");
        }
        TestAssert.AreEqual(
            1,
            (matches.Get(creator, waiting.Id).Value
                ?? throw new InvalidOperationException("Match expected.")).Participants.Length);
        TestAssert.AreEqual(
            MatchStatus.Success,
            matches.Cancel(creator, waiting.Id));
    }

    private static void SetCreatorStatus(string status)
    {
        using SqlConnection connection = new SqlConnection(
            Environment.GetEnvironmentVariable("REVERSI_DB_CONNECTION_STRING"));
        connection.Open();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.Account SET statusCode=@status WHERE username=N'ReviewAlpha'";
        command.Parameters.AddWithValue("@status", status);
        TestAssert.AreEqual(1, command.ExecuteNonQuery());
    }

    private static void Gameplay_RealMovePersistence_UpdatesBoardAndHistory(
        MatchRepository repository,
        long accountId,
        long matchId)
    {
        MatchMove move = new MatchMove
        {
            MoveNumber = 1,
            SeatNumber = 1,
            Row = 2,
            Column = 3,
            PlayedAtUtc = DateTime.UtcNow,
            Origin = "Player"
        };
        MatchStatus moveStatus = repository.SaveMove(matchId, move, 0, 2, DateTime.UtcNow);
        TestAssert.AreEqual(MatchStatus.Success, moveStatus);

        MatchMove timeoutMove = new MatchMove
        {
            MoveNumber = 2,
            SeatNumber = 2,
            Row = 2,
            Column = 2,
            PlayedAtUtc = DateTime.UtcNow,
            Origin = "Timeout"
        };
        MatchStatus timeoutStatus = repository.SaveMove(
            matchId,
            timeoutMove,
            1,
            1,
            DateTime.UtcNow);
        TestAssert.AreEqual(MatchStatus.Success, timeoutStatus);

        Match loaded = repository.Get(accountId, matchId).Value
            ?? throw new InvalidOperationException("Match expected.");
        TestAssert.AreEqual(2, loaded.Moves.Count);
        TestAssert.AreEqual(1, loaded.CurrentTurnSeatNumber);
        TestAssert.AreEqual(1, loaded.LastMove?.FlippedPieceCount);
        TestAssert.AreEqual("Timeout", loaded.LastMove?.Origin);
        TestAssert.AreEqual(2, loaded.Board[(2 * 8) + 2]);

        MatchStatus concludeStatus = repository.ConcludeMatch(
            matchId,
            "NORMAL",
            1,
            null,
            4,
            1,
            DateTime.UtcNow);
        TestAssert.AreEqual(MatchStatus.Success, concludeStatus);

        MatchResult<Match[]> history = repository.GetMatchHistory(accountId);
        TestAssert.AreEqual(MatchStatus.Success, history.Status);
        TestAssert.IsTrue(
            history.Value != null && history.Value.Any(m => m.Id == matchId && m.State == "Finished" && m.WinnerSeatNumber == 1),
            "Expected historical match to reflect completed state and winner.");
    }
}
