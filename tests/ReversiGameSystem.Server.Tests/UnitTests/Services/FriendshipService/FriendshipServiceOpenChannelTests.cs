using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

[TestClass]
[TestCategory("Unit")]
public sealed class FriendshipServiceOpenChannelTests : FriendshipServiceTestBase
{
    [TestMethod]
    public void Constructor_NullApplicationService_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.FriendshipService(
            null!,
            PresenceRegistry,
            Subscriptions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullPresenceRegistry_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.FriendshipService(
            ApplicationService,
            null!,
            Subscriptions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullSubscriptions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new Server.ServiceApi.FriendshipService(
            ApplicationService,
            PresenceRegistry,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void OpenFriendshipChannel_MissingSessionToken_ThrowsMissingSessionTokenFault(
        string? sessionToken)
    {
        SessionFailure expectedFailure = SessionFailure.MissingSessionToken;
        int expectedCallCount = 0;
        Action action = () => Service.OpenFriendshipChannel(sessionToken!);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Friendships.IsAccountActiveCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void OpenFriendshipChannel_InvalidSession_ThrowsInvalidSessionFault()
    {
        SessionFailure expectedFailure = SessionFailure.InvalidSession;
        int expectedCallCount = 0;
        Action action = () => Service.OpenFriendshipChannel("invalid-token");

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;
        int actualCallCount = Friendships.IsAccountActiveCalls;

        Assert.AreEqual(expectedFailure, actualFailure);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void OpenFriendshipChannel_InactiveAccount_ThrowsInactiveAccountFault()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Friendships.IsAccountActiveResult = false;
        SessionFailure expectedFailure = SessionFailure.InactiveAccount;
        Action action = () => Service.OpenFriendshipChannel(token);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }
}
