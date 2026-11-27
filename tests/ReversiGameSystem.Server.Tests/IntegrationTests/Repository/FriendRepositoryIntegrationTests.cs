using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Persistence.Friendships;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
[TestCategory("Database")]
public sealed class FriendRepositoryIntegrationTests : RepositoryIntegrationTestBase
{
    private const string CallerUsername = "callerUser";
    private const string CallerEmail = "caller@example.com";
    private const string AmyUsername = "amyFriend";
    private const string ZedUsername = "zedFriend";
    private const string AcceptedStatus = "ACCEPTED";
    private const string PendingStatus = "PENDING";
    private const string RemovedStatus = "REMOVED";

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
    public void GetFriends_MixedStatusesAndInactiveAccounts_ReturnsActiveAcceptedSorted()
    {
        long callerId = SeedAccount(CallerUsername, CallerEmail);
        long expectedZedId = SeedAccountWithImage(
            ZedUsername,
            "zed@example.com",
            "avatars/zed.png");
        long expectedAmyId = SeedAccount(AmyUsername, "amy@example.com");
        long pendingId = SeedAccount("pendingUser", "pending@example.com");
        long removedId = SeedAccount("endedUser", "ended@example.com");
        long inactiveId = SeedAccount("inactiveUser", "inactive@example.com", "DEACTIVATED");
        int expectedFriendCount = 2;

        SeedFriendship(new FriendshipSeedData(callerId, expectedZedId, AcceptedStatus, null));
        SeedFriendship(new FriendshipSeedData(callerId, expectedAmyId, AcceptedStatus, null));
        SeedFriendship(new FriendshipSeedData(callerId, pendingId, PendingStatus, null));
        SeedFriendship(new FriendshipSeedData(callerId, removedId, AcceptedStatus, DateTime.UtcNow));
        SeedFriendship(new FriendshipSeedData(callerId, inactiveId, AcceptedStatus, null));
        FriendRepository repository = new FriendRepository();

        FriendAccount[] actualFriends = repository.GetFriends(callerId);

        Assert.AreEqual(expectedFriendCount, actualFriends.Length);
        Assert.AreEqual(expectedAmyId, actualFriends[0].AccountId);
        Assert.AreEqual(AmyUsername, actualFriends[0].Username);
        Assert.IsFalse(actualFriends[0].HasProfileImage);
        Assert.AreEqual(expectedZedId, actualFriends[1].AccountId);
        Assert.AreEqual(ZedUsername, actualFriends[1].Username);
        Assert.IsTrue(actualFriends[1].HasProfileImage);
    }

    [TestMethod]
    public void AreFriends_AcceptedAndRemovedRelationships_EvaluatesBidirectionally()
    {
        long firstId = SeedAccount(CallerUsername, CallerEmail);
        long secondId = SeedAccount("secondFriend", "second@example.com");
        long removedId = SeedAccount("formerFriend", "former@example.com");
        SeedFriendship(new FriendshipSeedData(firstId, secondId, AcceptedStatus, null));
        SeedFriendship(new FriendshipSeedData(firstId, removedId, RemovedStatus, DateTime.UtcNow));
        FriendRepository repository = new FriendRepository();

        bool actualForward = repository.AreFriends(firstId, secondId);
        bool actualReverse = repository.AreFriends(secondId, firstId);
        bool actualRemoved = repository.AreFriends(firstId, removedId);

        Assert.IsTrue(actualForward);
        Assert.IsTrue(actualReverse);
        Assert.IsFalse(actualRemoved);
    }

    [TestMethod]
    public void ReadConfirmedFriendIds_AcceptedFriendships_ReturnsConfirmedActiveIds()
    {
        long callerId = SeedAccount(CallerUsername, CallerEmail);
        long friendOneId = SeedAccount("friendOne", "one@example.com");
        long friendTwoId = SeedAccount("friendTwo", "two@example.com");
        long pendingId = SeedAccount("pendingFriend", "pend@example.com");
        SeedFriendship(new FriendshipSeedData(callerId, friendOneId, AcceptedStatus, null));
        SeedFriendship(new FriendshipSeedData(callerId, friendTwoId, AcceptedStatus, null));
        SeedFriendship(new FriendshipSeedData(callerId, pendingId, PendingStatus, null));
        FriendRepository repository = new FriendRepository();

        long[] actualIds = repository.ReadConfirmedFriendIds(callerId)
            .OrderBy(id => id)
            .ToArray();

        long[] expectedIds = new[] { friendOneId, friendTwoId }
            .OrderBy(id => id)
            .ToArray();
        CollectionAssert.AreEqual(expectedIds, actualIds);
    }

    [TestMethod]
    public void FindPlayers_MatchingText_ExcludesCallerAndDeactivatedAccounts()
    {
        long callerId = SeedAccount("alphaCaller", "alpha.c@example.com");
        long expectedTargetId = SeedAccount("alphaTarget", "alpha.t@example.com");
        SeedAccount("alphaInactive", "alpha.i@example.com", "DEACTIVATED");
        SeedAccount("betaOther", "beta.o@example.com");
        FriendRepository repository = new FriendRepository();
        int expectedCount = 1;
        string expectedUsername = "alphaTarget";

        FriendAccount[] actualMatches = repository.FindPlayers(callerId, "alpha");

        Assert.AreEqual(expectedCount, actualMatches.Length);
        Assert.AreEqual(expectedTargetId, actualMatches[0].AccountId);
        Assert.AreEqual(expectedUsername, actualMatches[0].Username);
    }

    [TestMethod]
    public void CreatePendingRequest_ValidAccounts_PersistsOrderedPendingRow()
    {
        long secondCreatedId = SeedAccount(CallerUsername, CallerEmail);
        long firstCreatedId = SeedAccount(AmyUsername, "amy.req@example.com");
        long higherAccountId = Math.Max(secondCreatedId, firstCreatedId);
        long lowerAccountId = Math.Min(secondCreatedId, firstCreatedId);
        FriendRepository repository = new FriendRepository();
        FriendshipStatus expectedStatus = FriendshipStatus.Success;

        FriendshipResult<PendingFriendRequest> actualResult = repository.CreatePendingRequest(
            higherAccountId,
            lowerAccountId);

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.IsNotNull(actualResult.Value);
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        Friendship persisted = verificationContext.Friendship.Single(
            row => row.accountIdA == lowerAccountId && row.accountIdB == higherAccountId);
        Assert.AreEqual(PendingStatus, persisted.statusCode);
        Assert.AreEqual(higherAccountId, persisted.requestedByAccountId);
    }

    [TestMethod]
    public void CreatePendingRequest_AlreadyPending_ReturnsRequestAlreadyPending()
    {
        long senderId = SeedAccount(CallerUsername, CallerEmail);
        long recipientId = SeedAccount(ZedUsername, "zed.dup@example.com");
        FriendRepository repository = new FriendRepository();
        repository.CreatePendingRequest(senderId, recipientId);
        FriendshipStatus expectedStatus = FriendshipStatus.RequestAlreadyPending;

        FriendshipResult<PendingFriendRequest> actualDuplicate = repository.CreatePendingRequest(
            recipientId,
            senderId);

        Assert.AreEqual(expectedStatus, actualDuplicate.Status);
    }

    [TestMethod]
    public void RespondToRequest_Accept_TransitionsPendingRowToAccepted()
    {
        long senderId = SeedAccount(CallerUsername, CallerEmail);
        long recipientId = SeedAccount(AmyUsername, "amy.acc@example.com");
        FriendRepository repository = new FriendRepository();
        repository.CreatePendingRequest(senderId, recipientId);
        PendingFriendRequest pending = repository.ReadPendingRequests(recipientId).Single();
        FriendshipStatus expectedStatus = FriendshipStatus.Success;

        FriendshipStatus actualStatus = repository.RespondToRequest(recipientId, pending, true);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsTrue(repository.AreFriends(senderId, recipientId));
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        long minId = Math.Min(senderId, recipientId);
        long maxId = Math.Max(senderId, recipientId);
        Friendship persisted = verificationContext.Friendship.Single(
            row => row.accountIdA == minId && row.accountIdB == maxId);
        Assert.AreEqual(AcceptedStatus, persisted.statusCode);
        Assert.IsNotNull(persisted.respondedAtUtc);
    }

    [TestMethod]
    public void RemoveFriendship_AcceptedFriendship_TransitionsToEndedWithTimestamp()
    {
        long firstId = SeedAccount(CallerUsername, CallerEmail);
        long secondId = SeedAccount(ZedUsername, "zed.rem@example.com");
        SeedFriendship(new FriendshipSeedData(firstId, secondId, AcceptedStatus, null));
        FriendRepository repository = new FriendRepository();
        FriendshipStatus expectedStatus = FriendshipStatus.Success;

        FriendshipStatus actualStatus = repository.RemoveFriendship(firstId, secondId);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsFalse(repository.AreFriends(firstId, secondId));
        using ReversiGameSystemEntities verificationContext = CreateFreshContext();
        long minId = Math.Min(firstId, secondId);
        long maxId = Math.Max(firstId, secondId);
        Friendship persisted = verificationContext.Friendship.Single(
            row => row.accountIdA == minId && row.accountIdB == maxId);
        Assert.AreEqual(RemovedStatus, persisted.statusCode);
        Assert.IsNotNull(persisted.endedAtUtc);
    }

    private static void SeedFriendship(FriendshipSeedData seed)
    {
        long accountIdA = Math.Min(seed.FirstAccountId, seed.SecondAccountId);
        long accountIdB = Math.Max(seed.FirstAccountId, seed.SecondAccountId);
        DateTime nowUtc = DateTime.UtcNow;

        using ReversiGameSystemEntities context = CreateFreshContext();
        context.Friendship.Add(new Friendship
        {
            accountIdA = accountIdA,
            accountIdB = accountIdB,
            requestedByAccountId = seed.FirstAccountId,
            statusCode = seed.StatusCode,
            requestedAtUtc = nowUtc.AddMinutes(-10),
            respondedAtUtc = seed.StatusCode == PendingStatus ? null : nowUtc.AddMinutes(-5),
            endedAtUtc = seed.EndedAtUtc
        });
        context.SaveChanges();
    }

    private sealed class FriendshipSeedData
    {
        internal FriendshipSeedData(
            long firstAccountId,
            long secondAccountId,
            string statusCode,
            DateTime? endedAtUtc)
        {
            FirstAccountId = firstAccountId;
            SecondAccountId = secondAccountId;
            StatusCode = statusCode;
            EndedAtUtc = endedAtUtc;
        }

        internal long FirstAccountId { get; }

        internal long SecondAccountId { get; }

        internal string StatusCode { get; }

        internal DateTime? EndedAtUtc { get; }
    }
}
