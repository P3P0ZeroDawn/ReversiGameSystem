using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.MatchChatServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class SendChatMessageTests : MatchChatServiceTestBase
{
    [TestMethod]
    [DataRow("", SessionFailure.MissingSessionToken)]
    [DataRow("unknown-token", SessionFailure.InvalidSession)]
    public void SendChatMessage_InvalidSession_ThrowsSessionFault(
        string sessionToken,
        SessionFailure expectedFailure)
    {
        Action action = () => Service.SendChatMessage(sessionToken, 10L, "Hello");

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);

        Assert.AreEqual(expectedFailure, exception.Detail.Reason);
    }

    [TestMethod]
    [DataRow(ChatStatus.InactiveAccount, MatchFailure.InactiveAccount)]
    [DataRow(ChatStatus.MatchNotFound, MatchFailure.MatchNotFound)]
    [DataRow(ChatStatus.Unauthorized, MatchFailure.Unauthorized)]
    [DataRow(ChatStatus.MatchNotInProgress, MatchFailure.MatchNotInProgress)]
    public void SendChatMessage_MatchRejection_ThrowsMatchFault(
        ChatStatus repositoryStatus,
        MatchFailure expectedFailure)
    {
        Repository.Submission = new ChatMessageSubmission(repositoryStatus);
        Action action = () => Service.SendChatMessage(
            FirstSession.SessionToken,
            10L,
            "Hello");

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);

        Assert.AreEqual(expectedFailure, exception.Detail.Reason);
    }

    [TestMethod]
    [DataRow(ChatStatus.MessageBlocked, ChatFailure.MessageBlocked)]
    [DataRow(ChatStatus.RateLimitExceeded, ChatFailure.RateLimitExceeded)]
    public void SendChatMessage_ModerationOrRateRejection_ThrowsChatFault(
        ChatStatus repositoryStatus,
        ChatFailure expectedFailure)
    {
        Repository.Submission = new ChatMessageSubmission(repositoryStatus);
        Action action = () => Service.SendChatMessage(
            FirstSession.SessionToken,
            10L,
            "Hello");

        FaultException<ChatFault> exception =
            Assert.Throws<FaultException<ChatFault>>(action);

        Assert.AreEqual(expectedFailure, exception.Detail.Reason);
        Assert.IsNull(exception.Detail.MuteEndsAtUtc);
    }

    [TestMethod]
    public void SendChatMessage_EmptyContent_ThrowsEmptyMessageChatFault()
    {
        ChatFailure expectedFailure = ChatFailure.EmptyMessage;
        Action action = () => Service.SendChatMessage(
            FirstSession.SessionToken,
            10L,
            "   ");

        FaultException<ChatFault> exception =
            Assert.Throws<FaultException<ChatFault>>(action);

        Assert.AreEqual(expectedFailure, exception.Detail.Reason);
    }

    [TestMethod]
    public void SendChatMessage_PlayerMuted_ThrowsChatFaultWithMuteEndsAtUtc()
    {
        ChatMute mute = CreateSampleMute();
        Repository.Submission = new ChatMessageSubmission(mute);
        ChatFailure expectedFailure = ChatFailure.PlayerMuted;
        DateTime expectedEndsAtUtc = mute.EndsAtUtc;
        Action action = () => Service.SendChatMessage(
            FirstSession.SessionToken,
            10L,
            "Hello");

        FaultException<ChatFault> exception =
            Assert.Throws<FaultException<ChatFault>>(action);

        Assert.AreEqual(expectedFailure, exception.Detail.Reason);
        Assert.AreEqual(expectedEndsAtUtc, exception.Detail.MuteEndsAtUtc);
    }

    [TestMethod]
    public void SendChatMessage_ValidMessage_ReturnsMappedMatchChatEntry()
    {
        ChatMessage persistedMessage = CreateSampleMessage(
            matchId: 12L,
            messageNumber: 7,
            senderSeatNumber: 2,
            content: "GG");
        Repository.Submission = new ChatMessageSubmission(persistedMessage);

        MatchChatEntry actualEntry = Service.SendChatMessage(
            FirstSession.SessionToken,
            12L,
            "GG");

        Assert.AreEqual(persistedMessage.MatchId, actualEntry.MatchId);
        Assert.AreEqual(persistedMessage.MessageNumber, actualEntry.MessageNumber);
        Assert.AreEqual(persistedMessage.SenderSeatNumber, actualEntry.SenderSeatNumber);
        Assert.AreEqual(persistedMessage.Content, actualEntry.Content);
        Assert.AreEqual(persistedMessage.SentAtUtc, actualEntry.SentAtUtc);
    }
}
