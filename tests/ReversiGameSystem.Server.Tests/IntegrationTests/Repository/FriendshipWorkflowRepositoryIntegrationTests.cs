using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Persistence.Friendships;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[TestCategory("IntegrationSql")]
[DoNotParallelize]
public sealed class FriendshipWorkflowRepositoryIntegrationTests
{
    private const long SenderId = 1;
    private const long RecipientId = 2;
    private const long OtherAccountId = 3;
    private const string SeedAccountsSql =
        "INSERT dbo.Account (username,emailAddress,passwordHash,statusCode,registeredAtUtc) "
        + "VALUES ('Alpha','alpha@example.test','fixture-hash','ACTIVE',SYSUTCDATETIME()),"
        + "('Beta','beta@example.test','fixture-hash','ACTIVE',SYSUTCDATETIME()),"
        + "('Gamma','gamma@example.test','fixture-hash','ACTIVE',SYSUTCDATETIME())";
    private const string DeactivateRecipientSql =
        "UPDATE dbo.Account SET statusCode = 'INACTIVE' WHERE accountId = 2";

    [TestMethod]
    public void CreatePendingRequest_ReloadedFromDatabase_PreservesRequestId()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            PendingFriendRequest created = repository.CreatePendingRequest(
                SenderId, RecipientId).Value!;

            PendingFriendRequest reloaded = repository.ReadPendingRequests(RecipientId).Single();

            Assert.AreEqual(created.RequestId, reloaded.RequestId);
        });
    }

    [TestMethod]
    public void CreatePendingRequest_ReversePendingRequest_ReturnsRequestAlreadyPending()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            repository.CreatePendingRequest(SenderId, RecipientId);

            FriendshipResult<PendingFriendRequest> result = repository.CreatePendingRequest(
                RecipientId, SenderId);

            Assert.AreEqual(FriendshipStatus.RequestAlreadyPending, result.Status);
        });
    }

    [TestMethod]
    public void CreatePendingRequest_ExistingFriendship_ReturnsFriendshipAlreadyExists()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            PendingFriendRequest request = repository.CreatePendingRequest(
                SenderId, RecipientId).Value!;
            repository.RespondToRequest(RecipientId, request, true);

            FriendshipResult<PendingFriendRequest> result = repository.CreatePendingRequest(
                SenderId, RecipientId);

            Assert.AreEqual(FriendshipStatus.FriendshipAlreadyExists, result.Status);
        });
    }

    [TestMethod]
    public void RespondToRequest_AcceptedRequest_CreatesConfirmedFriendship()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            PendingFriendRequest request = repository.CreatePendingRequest(
                SenderId, RecipientId).Value!;

            repository.RespondToRequest(RecipientId, request, true);

            Assert.IsTrue(repository.AreFriends(SenderId, RecipientId));
        });
    }

    [TestMethod]
    public void RespondToRequest_RejectedRequest_DisappearsFromPendingRequests()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            PendingFriendRequest request = repository.CreatePendingRequest(
                SenderId, RecipientId).Value!;

            repository.RespondToRequest(RecipientId, request, false);

            Assert.AreEqual(0, repository.ReadPendingRequests(SenderId).Length);
        });
    }

    [TestMethod]
    public void RespondToRequest_SenderAttemptsResponse_ReturnsUnauthorizedRequest()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            PendingFriendRequest request = repository.CreatePendingRequest(
                SenderId, RecipientId).Value!;

            FriendshipStatus status = repository.RespondToRequest(SenderId, request, true);

            Assert.AreEqual(FriendshipStatus.UnauthorizedRequest, status);
        });
    }

    [TestMethod]
    public void RespondToRequest_StaleRequestCannotResolveReplacement_ReturnsRequestUnavailable()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            PendingFriendRequest original = repository.CreatePendingRequest(
                SenderId, RecipientId).Value!;
            repository.RespondToRequest(RecipientId, original, false);
            repository.CreatePendingRequest(SenderId, RecipientId);

            FriendshipStatus status = repository.RespondToRequest(RecipientId, original, true);

            Assert.AreEqual(FriendshipStatus.RequestUnavailable, status);
        });
    }

    [TestMethod]
    public void RemoveFriendship_ConfirmedRemoval_RemovesFriendFromBothAccounts()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            PendingFriendRequest request = repository.CreatePendingRequest(
                SenderId, RecipientId).Value!;
            repository.RespondToRequest(RecipientId, request, true);

            repository.RemoveFriendship(SenderId, RecipientId);

            Assert.AreEqual(0, repository.GetFriends(SenderId).Length
                + repository.GetFriends(RecipientId).Length);
        });
    }

    [TestMethod]
    public void CreatePendingRequest_InactiveRecipient_ReturnsAccountNotFound()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
            context.Database.ExecuteSqlCommand(DeactivateRecipientSql);

            FriendshipResult<PendingFriendRequest> result = repository.CreatePendingRequest(
                SenderId, RecipientId);

            Assert.AreEqual(FriendshipStatus.AccountNotFound, result.Status);
        });
    }

    [TestMethod]
    public void FindPlayers_UsernameSubstring_ExcludesCallerAndReturnsMatchingAccount()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();

            FriendAccount[] players = repository.FindPlayers(SenderId, "et");

            Assert.AreEqual(RecipientId, players.Single().AccountId);
        });
    }

    [TestMethod]
    public void FindPlayers_LiteralWildcard_DoesNotMatchAllAccounts()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();

            FriendAccount[] players = repository.FindPlayers(SenderId, "%");

            Assert.AreEqual(0, players.Length);
        });
    }

    [TestMethod]
    public void ReadPendingRequests_UnrelatedAccount_DoesNotExposePrivateRequest()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            repository.CreatePendingRequest(SenderId, RecipientId);

            PendingFriendRequest[] requests = repository.ReadPendingRequests(OtherAccountId);

            Assert.AreEqual(0, requests.Length);
        });
    }

    [TestMethod]
    public void CreatePendingRequest_ConcurrentOppositeRequests_CommitsOnlyOnePendingRequest()
    {
        ReviewAccountDatabaseTests.WithDisposableDatabase(() =>
        {
            FriendRepository repository = CreateRepository();
            FriendshipStatus firstStatus = FriendshipStatus.RequestUnavailable;
            FriendshipStatus secondStatus = FriendshipStatus.RequestUnavailable;

            Parallel.Invoke(
                () => firstStatus = repository.CreatePendingRequest(SenderId, RecipientId).Status,
                () => secondStatus = repository.CreatePendingRequest(RecipientId, SenderId).Status);

            CollectionAssert.AreEquivalent(
                new[] { FriendshipStatus.Success, FriendshipStatus.RequestAlreadyPending },
                new[] { firstStatus, secondStatus });
        });
    }

    private static FriendRepository CreateRepository()
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        context.Database.ExecuteSqlCommand(SeedAccountsSql);
        return new FriendRepository();
    }
}
