using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.ChatApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CanReceiveMuteNotificationTests : ChatApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    public void CanReceiveMuteNotification_NonPositiveMatchId_ReturnsFalse(long matchId)
    {
        AuthenticatedSession secondSession = CreateSession(1L, "PlayerOne");
        bool expectedResult = false;

        bool actualResult = Service.CanReceiveMuteNotification(
            FirstSession.SessionToken,
            secondSession.SessionToken,
            matchId);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void CanReceiveMuteNotification_InvalidSession_ReturnsFalse()
    {
        bool expectedResult = false;

        bool actualResult = Service.CanReceiveMuteNotification(
            FirstSession.SessionToken,
            "unknown-token",
            10L);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void CanReceiveMuteNotification_SameAccountSession_ReturnsTrueWithoutRepositoryCall()
    {
        AuthenticatedSession secondSession = CreateSession(1L, "PlayerOne");
        bool expectedResult = true;
        int expectedCalls = 0;

        bool actualResult = Service.CanReceiveMuteNotification(
            FirstSession.SessionToken,
            secondSession.SessionToken,
            10L);
        int actualCalls = Repository.AccessEvaluationCalls;

        Assert.AreEqual(expectedResult, actualResult);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void CanReceiveMuteNotification_DifferentAccountSession_ReturnsFalse()
    {
        AuthenticatedSession differentAccount = CreateSession(2L, "PlayerTwo");
        bool expectedResult = false;

        bool actualResult = Service.CanReceiveMuteNotification(
            FirstSession.SessionToken,
            differentAccount.SessionToken,
            10L);

        Assert.AreEqual(expectedResult, actualResult);
    }
}
