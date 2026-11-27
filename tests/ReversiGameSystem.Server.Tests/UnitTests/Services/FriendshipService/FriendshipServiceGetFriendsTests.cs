using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipServiceGetFriendsTests : FriendshipServiceTestBase
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GetFriends_MissingSessionToken_ThrowsMissingSessionTokenFault(string? sessionToken)
    {
        SessionFailure expectedFailure = SessionFailure.MissingSessionToken;
        int expectedCallCount = 0;
        Action action = () => Service.GetFriends(sessionToken!);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Friendships.GetFriendsCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetFriends_InvalidSession_ThrowsInvalidSessionFault()
    {
        SessionFailure expectedFailure = SessionFailure.InvalidSession;
        int expectedCallCount = 0;
        Action action = () => Service.GetFriends("invalid-token");

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Friendships.GetFriendsCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetFriends_InactiveAccount_ThrowsInactiveAccountFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Friendships.IsAccountActiveResult = false;
        SessionFailure expectedFailure = SessionFailure.InactiveAccount;
        int expectedCallCount = 0;
        Action action = () => Service.GetFriends(token);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Friendships.GetFriendsCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetFriends_ActiveSession_ReturnsMappedFriendSummariesWithPresence()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        long expectedFirstAccountId = 20L;
        string expectedFirstUsername = "PlayerTwenty";
        long expectedSecondAccountId = 30L;
        string expectedSecondUsername = "PlayerThirty";
        int expectedCount = 2;
        Friendships.Friends = new FriendAccount[]
        {
            new FriendAccount(expectedFirstAccountId, expectedFirstUsername, true),
            new FriendAccount(expectedSecondAccountId, expectedSecondUsername, false)
        };
        using IDisposable onlineFriend =
            PresenceRegistry.Register("friend-20-token", expectedFirstAccountId);

        FriendSummary[] actualFriends = Service.GetFriends(token);
        int actualCount = actualFriends.Length;

        Assert.AreEqual(expectedCount, actualCount);
        long actualFirstAccountId = actualFriends[0].Account.AccountId;
        string actualFirstUsername = actualFriends[0].Account.Username;
        bool actualFirstHasImage = actualFriends[0].HasProfileImage;
        bool actualFirstIsOnline = actualFriends[0].IsOnline;
        long actualSecondAccountId = actualFriends[1].Account.AccountId;
        string actualSecondUsername = actualFriends[1].Account.Username;
        bool actualSecondHasImage = actualFriends[1].HasProfileImage;
        bool actualSecondIsOnline = actualFriends[1].IsOnline;
        Assert.AreEqual(expectedFirstAccountId, actualFirstAccountId);
        Assert.AreEqual(expectedFirstUsername, actualFirstUsername);
        Assert.IsTrue(actualFirstHasImage);
        Assert.IsTrue(actualFirstIsOnline);
        Assert.AreEqual(expectedSecondAccountId, actualSecondAccountId);
        Assert.AreEqual(expectedSecondUsername, actualSecondUsername);
        Assert.IsFalse(actualSecondHasImage);
        Assert.IsFalse(actualSecondIsOnline);
    }
}
