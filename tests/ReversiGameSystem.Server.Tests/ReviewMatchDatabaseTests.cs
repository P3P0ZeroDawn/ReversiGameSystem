using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Matches;
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
        string alpha = sessions.CompleteLogin("ReviewAlpha", "000001")
            .GetValue().SessionToken;
        string beta = sessions.CompleteLogin("ReviewBeta", "000002")
            .GetValue().SessionToken;
        MatchHandler matches = new MatchHandler(store, new EntityFrameworkMatchStore(), new SystemUtcClock());
        ChatHandler chat = new ChatHandler(store, new EntityFrameworkChatStore(), new SystemUtcClock());
        MatchRecord waiting = matches.Create(alpha, new MatchSettings());

        // Act / Assert: a rejected operation must leave the waiting match unchanged.
        TestAssert.AreEqual(0, matches.Available(alpha).Length);
        TestAssert.AreEqual(waiting.Id, matches.Available(beta).Single().Id);
        TestAssert.AreEqual(MatchRejection.CannotJoinOwnMatch,
            TestAssert.Throws<MatchRejectedException>(() => matches.Join(alpha, waiting.Code!)).Reason);
        TestAssert.AreEqual(MatchRejection.MatchNotInProgress,
            TestAssert.Throws<MatchRejectedException>(() => chat.History(alpha, waiting.Id)).Reason);
        TestAssert.AreEqual(MatchRejection.Unauthorized,
            TestAssert.Throws<MatchRejectedException>(() => chat.History(beta, waiting.Id)).Reason);
        Join_ConcurrentRequests_RegistersOneSecondSeat(matches, beta, waiting);
        TestAssert.AreEqual("InProgress", matches.Get(alpha, waiting.Id).State);
        TestAssert.AreEqual(2, matches.Get(beta, waiting.Id).Participants.Length);
        TestAssert.AreEqual(0, matches.Available(beta).Length);
        Send_ConcurrentRequests_AssignsUniqueOrderedNumbers(chat, alpha, waiting.Id);
        Moderate_RepeatedMessages_CommitsAccountMute(chat, alpha, waiting.Id);
        Cancel_RepeatedRequest_InvalidatesCode(matches, alpha, beta);
        Restart_AbandonedWaitingMatch_InvalidatesCode(matches, alpha, beta);
        Join_InactiveCreator_RejectsWithoutOccupyingSecondSeat(matches, alpha, beta);
        sessions.CloseSession(alpha);
        TestAssert.Throws<SessionRejectedException>(() => matches.Available(alpha));
        Console.WriteLine("Passed real match SQL checks: atomic join, chat ordering, "
            + "moderation, authorization, cancellation and session revocation.");
    }

    private static void Join_ConcurrentRequests_RegistersOneSecondSeat(
        MatchHandler matches, string token, MatchRecord waiting)
    {
        // Arrange / Act
        Task<bool>[] joins = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            try
            {
                matches.Join(token, waiting.Code!);
                return true;
            }
            catch (MatchRejectedException exception)
                when (exception.Reason == MatchRejection.MatchCodeUnavailable)
            {
                return false;
            }
        })).ToArray();
        Task.WaitAll(joins);

        // Assert
        TestAssert.AreEqual(1, joins.Count(task => task.Result));
    }

    private static void Send_ConcurrentRequests_AssignsUniqueOrderedNumbers(
        ChatHandler chat, string token, long id)
    {
        // Arrange / Act
        Task<ChatEntryRecord>[] sends = Enumerable.Range(0, 8)
            .Select(number => Task.Run(() => chat.Send(token, id, "Message " + number))).ToArray();
        Task.WaitAll(sends);

        // Assert
        ChatEntryRecord[] history = chat.History(token, id);
        TestAssert.AreEqual(8, history.Length);
        TestAssert.IsTrue(history.Select(entry => entry.MessageNumber)
            .SequenceEqual(Enumerable.Range(1, 8)), "Concurrent messages must have stable unique numbers.");
    }

    private static void Moderate_RepeatedMessages_CommitsAccountMute(
        ChatHandler chat, string token, long id)
    {
        // Arrange
        TestAssert.AreEqual(ChatRejection.EmptyMessage,
            TestAssert.Throws<ChatRejectedException>(() => chat.Send(token, id, " ")).Reason);
        TestAssert.AreEqual(ChatRejection.MessageTooLong,
            TestAssert.Throws<ChatRejectedException>(() => chat.Send(token, id, new string('a', 201))).Reason);

        // Act / Assert
        for (int attempt = 0; attempt < 3; attempt++)
        {
            ChatRejectedException blocked = TestAssert.Throws<ChatRejectedException>(
                () => chat.Send(token, id, "BLOCKEDWORD!"));
            TestAssert.AreEqual(attempt < 2 ? ChatRejection.MessageBlocked : ChatRejection.PlayerMuted,
                blocked.Reason);
        }
        ChatRejectedException muted = TestAssert.Throws<ChatRejectedException>(
            () => chat.Send(token, id, "Otherwise acceptable"));
        TestAssert.AreEqual(ChatRejection.PlayerMuted, muted.Reason);
        TestAssert.AreEqual(8, chat.History(token, id).Length);
    }

    private static void Cancel_RepeatedRequest_InvalidatesCode(
        MatchHandler matches, string creator, string other)
    {
        // Arrange
        MatchRecord waiting = matches.Create(creator, new MatchSettings());

        // Act
        matches.Cancel(creator, waiting.Id);
        matches.Cancel(creator, waiting.Id);

        // Assert
        TestAssert.AreEqual("Cancelled", matches.Get(creator, waiting.Id).State);
        TestAssert.AreEqual(MatchRejection.MatchCodeUnavailable,
            TestAssert.Throws<MatchRejectedException>(() => matches.Join(other, waiting.Code!)).Reason);
    }

    private static void Restart_AbandonedWaitingMatch_InvalidatesCode(
        MatchHandler matches, string creator, string other)
    {
        // Arrange
        MatchRecord abandoned = matches.Create(creator, new MatchSettings());
        EntityFrameworkMatchStore restartedStore = new EntityFrameworkMatchStore();

        // Act
        TestAssert.AreEqual(1, restartedStore.CancelAbandonedWaitingMatches());

        // Assert
        TestAssert.AreEqual(0, restartedStore.CancelAbandonedWaitingMatches());
        TestAssert.AreEqual("Cancelled", matches.Get(creator, abandoned.Id).State);
        TestAssert.AreEqual(MatchRejection.MatchCodeUnavailable,
            TestAssert.Throws<MatchRejectedException>(() => matches.Join(other, abandoned.Code!)).Reason);
    }

    private static void Join_InactiveCreator_RejectsWithoutOccupyingSecondSeat(
        MatchHandler matches, string creator, string other)
    {
        // Arrange
        MatchRecord waiting = matches.Create(creator, new MatchSettings());
        SetCreatorStatus("INACTIVE");
        try
        {
            // Act / Assert
            TestAssert.AreEqual(0, matches.Available(other).Length);
            TestAssert.AreEqual(MatchRejection.InactiveAccount,
                TestAssert.Throws<MatchRejectedException>(() => matches.Join(other, waiting.Code!)).Reason);
        }
        finally
        {
            SetCreatorStatus("ACTIVE");
        }
        TestAssert.AreEqual(1, matches.Get(creator, waiting.Id).Participants.Length);
        matches.Cancel(creator, waiting.Id);
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
}
