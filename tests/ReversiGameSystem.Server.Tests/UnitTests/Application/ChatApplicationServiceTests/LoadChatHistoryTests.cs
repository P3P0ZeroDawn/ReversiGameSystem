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

        ChatHistory history = Service.LoadChatHistory(sessionToken, 10L);
        ChatStatus actualStatus = history.Status;
        int actualCalls = Repository.LoadHistoryCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void LoadChatHistory_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ChatStatus expectedStatus = ChatStatus.InvalidSession;
        int expectedCalls = 0;

        ChatHistory history = Service.LoadChatHistory("unknown-token", 10L);
        ChatStatus actualStatus = history.Status;
        int actualCalls = Repository.LoadHistoryCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
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

        ChatHistory history = Service.LoadChatHistory(
            FirstSession.SessionToken,
            matchId);
        ChatStatus actualStatus = history.Status;
        int actualCalls = Repository.LoadHistoryCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
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

        ChatHistory history = Service.LoadChatHistory(
            FirstSession.SessionToken,
            18L);
        ChatStatus actualStatus = history.Status;
        int actualCount = history.Messages.Length;
        ChatMessage actualFirstMessage = history.Messages[0];
        ChatMessage actualSecondMessage = history.Messages[1];
        int actualCalls = Repository.LoadHistoryCalls;
        long actualAccountId = Repository.LastAccountId;
        long actualMatchId = Repository.LastMatchId;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreSame(firstMessage, actualFirstMessage);
        Assert.AreSame(secondMessage, actualSecondMessage);
        Assert.AreEqual(expectedCalls, actualCalls);
        Assert.AreEqual(expectedAccountId, actualAccountId);
        Assert.AreEqual(expectedMatchId, actualMatchId);
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
