using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.ChatApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class LoadChatHistoryTests : ChatApplicationServiceTestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void LoadChatHistory_MissingToken_ReturnsMissingSessionTokenWithoutRepositoryCall(
        string sessionToken)
    {
        ChatStatus expectedStatus = ChatStatus.MissingSessionToken;
        int expectedCalls = 0;

        ChatHistory actualHistory = Service.LoadChatHistory(sessionToken, 10L);
        int actualCalls = Repository.LoadHistoryCalls;

        Assert.AreEqual(expectedStatus, actualHistory.Status);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void LoadChatHistory_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ChatStatus expectedStatus = ChatStatus.InvalidSession;
        int expectedCalls = 0;

        ChatHistory actualHistory = Service.LoadChatHistory("unknown-token", 10L);
        int actualCalls = Repository.LoadHistoryCalls;

        Assert.AreEqual(expectedStatus, actualHistory.Status);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-2L)]
    public void LoadChatHistory_NonPositiveMatchId_ReturnsInvalidMatchIdWithoutRepositoryCall(
        long matchId)
    {
        ChatStatus expectedStatus = ChatStatus.InvalidMatchId;
        int expectedCalls = 0;

        ChatHistory actualHistory = Service.LoadChatHistory(
            FirstSession.SessionToken,
            matchId);
        int actualCalls = Repository.LoadHistoryCalls;

        Assert.AreEqual(expectedStatus, actualHistory.Status);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void LoadChatHistory_ValidRequest_ForwardsToRepositoryAndReturnsHistory()
    {
        ChatMessage firstMessage = CreateSampleMessage(messageNumber: 1, content: "Hi");
        ChatMessage secondMessage = CreateSampleMessage(messageNumber: 2, content: "GLHF");
        Repository.History = new ChatHistory(new[] { firstMessage, secondMessage });
        ChatStatus expectedStatus = ChatStatus.Success;
        int expectedCount = 2;
        int expectedCalls = 1;
        long expectedAccountId = FirstSession.AccountId;
        long expectedMatchId = 18L;

        ChatHistory actualHistory = Service.LoadChatHistory(
            FirstSession.SessionToken,
            18L);

        Assert.AreEqual(expectedStatus, actualHistory.Status);
        Assert.HasCount(expectedCount, actualHistory.Messages);
        Assert.AreSame(firstMessage, actualHistory.Messages[0]);
        Assert.AreSame(secondMessage, actualHistory.Messages[1]);
        Assert.AreEqual(expectedCalls, Repository.LoadHistoryCalls);
        Assert.AreEqual(expectedAccountId, Repository.LastAccountId);
        Assert.AreEqual(expectedMatchId, Repository.LastMatchId);
    }

    [TestMethod]
    public void LoadChatHistory_RepositoryFailure_PropagatesException()
    {
        Repository.ThrowOnLoadHistory = true;
        Action action = () => Service.LoadChatHistory(FirstSession.SessionToken, 10L);

        InvalidOperationException actualException =
            Assert.Throws<InvalidOperationException>(action);

        Assert.IsNotNull(actualException);
    }
}
