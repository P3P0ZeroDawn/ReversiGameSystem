using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.MatchChatServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class GetChatHistoryTests : MatchChatServiceTestBase
{
    [TestMethod]
    [DataRow("", SessionFailure.MissingSessionToken)]
    [DataRow("unknown-token", SessionFailure.InvalidSession)]
    public void GetChatHistory_InvalidSession_ThrowsSessionFault(
        string sessionToken,
        SessionFailure expectedFailure)
    {
        Action action = () => Service.GetChatHistory(sessionToken, 10L);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    [DataRow(ChatStatus.InactiveAccount, MatchFailure.InactiveAccount)]
    [DataRow(ChatStatus.MatchNotFound, MatchFailure.MatchNotFound)]
    [DataRow(ChatStatus.Unauthorized, MatchFailure.Unauthorized)]
    public void GetChatHistory_MatchRejection_ThrowsMatchFault(
        ChatStatus repositoryStatus,
        MatchFailure expectedFailure)
    {
        Repository.History = new ChatHistory(repositoryStatus);
        Action action = () => Service.GetChatHistory(FirstSession.SessionToken, 10L);

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void GetChatHistory_ValidRequest_ReturnsMappedEntriesInOrder()
    {
        ChatMessage first = CreateSampleMessage(
            matchId: 20L,
            messageNumber: 1,
            senderSeatNumber: 1,
            content: "Hello");
        ChatMessage second = CreateSampleMessage(
            matchId: 20L,
            messageNumber: 2,
            senderSeatNumber: 2,
            content: "Hi!");
        Repository.History = new ChatHistory(new[] { first, second });
        int expectedCount = 2;

        MatchChatEntry[] actualEntries = Service.GetChatHistory(
            FirstSession.SessionToken,
            20L);
        int actualCount = actualEntries.Length;

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(first.MessageNumber, actualEntries[0].MessageNumber);
        Assert.AreEqual(first.Content, actualEntries[0].Content);
        Assert.AreEqual(second.MessageNumber, actualEntries[1].MessageNumber);
        Assert.AreEqual(second.Content, actualEntries[1].Content);
    }
}
