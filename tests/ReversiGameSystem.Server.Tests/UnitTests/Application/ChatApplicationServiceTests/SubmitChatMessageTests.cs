using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Domain.Chat;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.ChatApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class SubmitChatMessageTests : ChatApplicationServiceTestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void SubmitChatMessage_MissingToken_ReturnsMissingSessionTokenWithoutRepositoryCall(
        string sessionToken)
    {
        ChatStatus expectedStatus = ChatStatus.MissingSessionToken;
        int expectedCalls = 0;

        ChatMessageSubmission submission = Service.SubmitChatMessage(
            sessionToken,
            10L,
            "Hello");
        ChatStatus actualStatus = submission.Status;
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ChatStatus expectedStatus = ChatStatus.InvalidSession;
        int expectedCalls = 0;

        ChatMessageSubmission submission = Service.SubmitChatMessage(
            "unknown-token",
            10L,
            "Hello");
        ChatStatus actualStatus = submission.Status;
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-3L)]
    public void SubmitChatMessage_NonPositiveMatchId_ReturnsInvalidMatchIdWithoutRepositoryCall(
        long matchId)
    {
        ChatStatus expectedStatus = ChatStatus.InvalidMatchId;
        int expectedCalls = 0;

        ChatMessageSubmission submission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            matchId,
            "Hello");
        ChatStatus actualStatus = submission.Status;
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void SubmitChatMessage_NullOrWhitespaceContent_ReturnsEmptyMessageWithoutRepositoryCall(
        string? content)
    {
        ChatStatus expectedStatus = ChatStatus.EmptyMessage;
        int expectedCalls = 0;

        ChatMessageSubmission submission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            content!);
        ChatStatus actualStatus = submission.Status;
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_ExceedsMaxLength_ReturnsMessageTooLongWithoutRepositoryCall()
    {
        string oversizedContent = new string('a', ChatContentPolicy.MaximumMessageLength + 1);
        ChatStatus expectedStatus = ChatStatus.MessageTooLong;
        int expectedCalls = 0;

        ChatMessageSubmission submission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            oversizedContent);
        ChatStatus actualStatus = submission.Status;
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_ExactMaxLength_ForwardsToRepository()
    {
        string maxLengthContent = new string('a', ChatContentPolicy.MaximumMessageLength);
        ChatMessage expectedMessage = CreateSampleMessage(content: maxLengthContent);
        Repository.Submission = new ChatMessageSubmission(expectedMessage);
        ChatStatus expectedStatus = ChatStatus.Success;
        int expectedCalls = 1;
        string expectedContent = maxLengthContent;

        ChatMessageSubmission submission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            maxLengthContent);
        ChatStatus actualStatus = submission.Status;
        int actualCalls = Repository.SubmissionCalls;
        string actualContent = Repository.LastContent;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
        Assert.AreEqual(expectedContent, actualContent);
    }

    [TestMethod]
    public void SubmitChatMessage_ValidContent_TrimsContentAndPassesRateLimitToRepository()
    {
        ChatMessage expectedMessage = CreateSampleMessage();
        Repository.Submission = new ChatMessageSubmission(expectedMessage);
        ChatStatus expectedStatus = ChatStatus.Success;
        string expectedContent = "Hello";
        int expectedCalls = 1;
        int expectedMaximumMessages = 2;
        TimeSpan expectedRateWindow = TimeSpan.FromSeconds(1);

        ChatMessageSubmission submission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            "  Hello  ");
        ChatStatus actualStatus = submission.Status;
        ChatMessage? actualMessage = submission.Message;
        int actualCalls = Repository.SubmissionCalls;
        string actualContent = Repository.LastContent;
        int actualMaximumMessages = Repository.LastMaximumMessages;
        TimeSpan actualRateWindow = Repository.LastRateWindow;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedMessage, actualMessage);
        Assert.AreEqual(expectedCalls, actualCalls);
        Assert.AreEqual(expectedContent, actualContent);
        Assert.AreEqual(expectedMaximumMessages, actualMaximumMessages);
        Assert.AreEqual(expectedRateWindow, actualRateWindow);
    }

    [TestMethod]
    public void SubmitChatMessage_RepositoryFailure_PropagatesException()
    {
        Repository.ThrowOnSubmission = true;
        Action action = () => Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            "Hello");

        InvalidOperationException actualException =
            Assert.Throws<InvalidOperationException>(action);

        Assert.IsNotNull(actualException);
    }
}
