using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.ChatApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class EvaluateParticipantAccessTests : ChatApplicationServiceTestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void EvaluateParticipantAccess_MissingToken_ReturnsMissingSessionTokenWithoutRepositoryCall(
        string sessionToken)
    {
        ChatStatus expectedStatus = ChatStatus.MissingSessionToken;
        int expectedCalls = 0;

        ChatStatus actualStatus = Service.EvaluateParticipantAccess(sessionToken, 10L);
        int actualCalls = Repository.AccessEvaluationCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void EvaluateParticipantAccess_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ChatStatus expectedStatus = ChatStatus.InvalidSession;
        int expectedCalls = 0;

        ChatStatus actualStatus = Service.EvaluateParticipantAccess("unknown-token", 10L);
        int actualCalls = Repository.AccessEvaluationCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-5L)]
    public void EvaluateParticipantAccess_NonPositiveMatchId_ReturnsInvalidMatchIdWithoutRepositoryCall(
        long matchId)
    {
        ChatStatus expectedStatus = ChatStatus.InvalidMatchId;
        int expectedCalls = 0;

        ChatStatus actualStatus = Service.EvaluateParticipantAccess(
            FirstSession.SessionToken,
            matchId);
        int actualCalls = Repository.AccessEvaluationCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void EvaluateParticipantAccess_ValidRequest_ForwardsToRepositoryAndReturnsStatus()
    {
        Repository.AccessEvaluationStatus = ChatStatus.Unauthorized;
        ChatStatus expectedStatus = ChatStatus.Unauthorized;
        int expectedCalls = 1;
        long expectedAccountId = FirstSession.AccountId;
        long expectedMatchId = 15L;

        ChatStatus actualStatus = Service.EvaluateParticipantAccess(
            FirstSession.SessionToken,
            15L);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, Repository.AccessEvaluationCalls);
        Assert.AreEqual(expectedAccountId, Repository.LastAccountId);
        Assert.AreEqual(expectedMatchId, Repository.LastMatchId);
    }

    [TestMethod]
    public void EvaluateParticipantAccess_RepositoryFailure_PropagatesException()
    {
        Repository.ThrowOnAccessEvaluation = true;
        Action action = () => Service.EvaluateParticipantAccess(
            FirstSession.SessionToken,
            10L);

        InvalidOperationException actualException =
            Assert.Throws<InvalidOperationException>(action);

        Assert.IsNotNull(actualException);
    }
}
