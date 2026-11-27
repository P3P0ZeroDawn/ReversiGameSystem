using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipServiceFindPlayersTests : FriendshipServiceTestBase
{
    private const long CallerId = 10L;
    private const long MatchAccountId = 20L;
    private const string CallerUsername = "Caller";
    private const string MatchUsername = "TargetPlayer";
    private const string SearchQuery = "Target";

    [TestMethod]
    public void FindPlayers_ValidQuery_MapsMatchingPlayersToContract()
    {
        string token = AddAuthenticatedSession(CallerId, CallerUsername);
        Friendships.SearchPlayers = new[]
        {
            new FriendAccount(MatchAccountId, MatchUsername, true)
        };
        int expectedCount = 1;

        PlayerSummary[] actualPlayers = Service.FindPlayers(token, SearchQuery);

        Assert.AreEqual(expectedCount, actualPlayers.Length);
        Assert.AreEqual(MatchAccountId, actualPlayers[0].Account.AccountId);
        Assert.AreEqual(MatchUsername, actualPlayers[0].Account.Username);
        Assert.IsTrue(actualPlayers[0].HasProfileImage);
    }

    [TestMethod]
    public void FindPlayers_InvalidSession_ThrowsSessionFault()
    {
        SessionFailure expectedReason = SessionFailure.InvalidSession;
        Action action = () => Service.FindPlayers("missing-token", SearchQuery);

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedReason, fault.Detail.Reason);
    }
}
