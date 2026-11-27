using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Model;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class ChatRepositoryIntegrationTests : RepositoryIntegrationTestBase
{
    private const string FirstUsername = "chatPlayerOne";
    private const string FirstEmail = "chat1@example.com";
    private const string SecondUsername = "chatPlayerTwo";
    private const string SecondEmail = "chat2@example.com";
    private const string CleanMessage = "Good game!";
    private const string BlockedWordMessage = "This contains blockedword inside";
    private const int DefaultRateLimit = 5;

    private static LocalDbTestDatabase? _database;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _database = InitializeDatabase();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        CleanupDatabase(_database);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _database?.ResetMutableTables();
    }

    [TestMethod]
    public void ProcessMessageSubmission_CleanMessage_PersistsAndAppearsInHistory()
    {
        long firstAccountId = SeedAccount(FirstUsername, FirstEmail);
        long secondAccountId = SeedAccount(SecondUsername, SecondEmail);
        long matchId = SeedInProgressMatch(firstAccountId, secondAccountId);
        DateTime nowUtc = new DateTime(2026, 11, 2, 16, 0, 0, DateTimeKind.Utc);
        ChatRepository repository = new ChatRepository(new UtcClockStub(nowUtc));
        ChatStatus expectedStatus = ChatStatus.Success;
        int expectedMessageCount = 1;
        int expectedSenderSeat = 1;

        ChatMessageSubmission actualSubmission = repository.ProcessMessageSubmission(
            firstAccountId,
            matchId,
            CleanMessage,
            DefaultRateLimit,
            TimeSpan.FromSeconds(10));
        ChatHistory actualHistory = repository.LoadMessageHistory(secondAccountId, matchId);

        Assert.AreEqual(expectedStatus, actualSubmission.Status);
        Assert.AreEqual(expectedStatus, actualHistory.Status);
        Assert.AreEqual(expectedMessageCount, actualHistory.Messages.Length);
        Assert.AreEqual(CleanMessage, actualHistory.Messages[0].Content);
        Assert.AreEqual(expectedSenderSeat, actualHistory.Messages[0].SenderSeatNumber);
    }

    [TestMethod]
    public void ProcessMessageSubmission_BlockedWordThresholdReached_PersistsViolationAndMute()
    {
        long firstAccountId = SeedAccount(FirstUsername, FirstEmail);
        long secondAccountId = SeedAccount(SecondUsername, SecondEmail);
        long matchId = SeedInProgressMatch(firstAccountId, secondAccountId);
        DateTime nowUtc = new DateTime(2026, 11, 2, 16, 0, 0, DateTimeKind.Utc);
        ChatRepository repository = new ChatRepository(new UtcClockStub(nowUtc));
        TimeSpan rateWindow = TimeSpan.FromSeconds(10);
        ChatStatus expectedStatus = ChatStatus.PlayerMuted;
        int expectedViolationCount = 3;
        int expectedMuteCount = 1;

        repository.ProcessMessageSubmission(
            firstAccountId, matchId, BlockedWordMessage, DefaultRateLimit, rateWindow);
        repository.ProcessMessageSubmission(
            firstAccountId, matchId, BlockedWordMessage, DefaultRateLimit, rateWindow);
        ChatMessageSubmission actualThirdAttempt = repository.ProcessMessageSubmission(
            firstAccountId, matchId, BlockedWordMessage, DefaultRateLimit, rateWindow);

        Assert.AreEqual(expectedStatus, actualThirdAttempt.Status);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        int actualViolationCount = verificationContext.ChatViolation
            .Count(violation => violation.matchId == matchId);
        int actualMuteCount = verificationContext.ChatMute.Count();
        Assert.AreEqual(expectedViolationCount, actualViolationCount);
        Assert.AreEqual(expectedMuteCount, actualMuteCount);
    }

    [TestMethod]
    public void ProcessMessageSubmission_RateLimitExceeded_RejectsWithoutSavingMessage()
    {
        long firstAccountId = SeedAccount(FirstUsername, FirstEmail);
        long secondAccountId = SeedAccount(SecondUsername, SecondEmail);
        long matchId = SeedInProgressMatch(firstAccountId, secondAccountId);
        DateTime nowUtc = new DateTime(2026, 11, 2, 16, 0, 0, DateTimeKind.Utc);
        ChatRepository repository = new ChatRepository(new UtcClockStub(nowUtc));
        ChatStatus expectedStatus = ChatStatus.RateLimitExceeded;
        int expectedMessageCount = 1;

        repository.ProcessMessageSubmission(
            firstAccountId, matchId, CleanMessage, 1, TimeSpan.FromSeconds(30));
        ChatMessageSubmission actualLimited = repository.ProcessMessageSubmission(
            firstAccountId, matchId, "Second message", 1, TimeSpan.FromSeconds(30));

        Assert.AreEqual(expectedStatus, actualLimited.Status);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        int actualMessageCount = verificationContext.ChatMessage
            .Count(message => message.matchId == matchId);
        Assert.AreEqual(expectedMessageCount, actualMessageCount);
    }

    private static long SeedInProgressMatch(long firstAccountId, long secondAccountId)
    {
        DateTime nowUtc = DateTime.UtcNow;
        using ReversiGameSystemEntities context = CreateFreshContext();
        GameMatch match = new GameMatch
        {
            matchCode = "CHAT01",
            statusCode = "IN_PROGRESS",
            turnDurationSeconds = 30,
            countsTowardStatistics = true,
            createdAtUtc = nowUtc,
            startedAtUtc = nowUtc
        };
        context.GameMatch.Add(match);
        context.SaveChanges();

        context.MatchParticipant.Add(new MatchParticipant
        {
            matchId = match.matchId,
            seatNumber = 1,
            accountId = firstAccountId,
            participantTypeCode = "REGISTERED",
            displayNameSnapshot = FirstUsername,
            pieceColorCode = "BLACK",
            consecutiveTimeoutCount = 0,
            joinedAtUtc = nowUtc,
            isCreator = true
        });
        context.MatchParticipant.Add(new MatchParticipant
        {
            matchId = match.matchId,
            seatNumber = 2,
            accountId = secondAccountId,
            participantTypeCode = "REGISTERED",
            displayNameSnapshot = SecondUsername,
            pieceColorCode = "WHITE",
            consecutiveTimeoutCount = 0,
            joinedAtUtc = nowUtc,
            isCreator = false
        });
        context.SaveChanges();
        return match.matchId;
    }
}
