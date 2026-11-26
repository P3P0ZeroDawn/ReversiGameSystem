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

        ChatMessageSubmission actualSubmission = Service.SubmitChatMessage(
            sessionToken,
            10L,
            "Hello");
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualSubmission.Status);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ChatStatus expectedStatus = ChatStatus.InvalidSession;
        int expectedCalls = 0;

        ChatMessageSubmission actualSubmission = Service.SubmitChatMessage(
            "unknown-token",
            10L,
            "Hello");
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualSubmission.Status);
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

        ChatMessageSubmission actualSubmission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            matchId,
            "Hello");
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualSubmission.Status);
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

        ChatMessageSubmission actualSubmission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            content!);
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualSubmission.Status);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_ExceedsMaxLength_ReturnsMessageTooLongWithoutRepositoryCall()
    {
        string oversizedContent = new string('a', ChatContentPolicy.MaximumMessageLength + 1);
        ChatStatus expectedStatus = ChatStatus.MessageTooLong;
        int expectedCalls = 0;

        ChatMessageSubmission actualSubmission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            oversizedContent);
        int actualCalls = Repository.SubmissionCalls;

        Assert.AreEqual(expectedStatus, actualSubmission.Status);
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

        ChatMessageSubmission actualSubmission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            maxLengthContent);

        Assert.AreEqual(expectedStatus, actualSubmission.Status);
        Assert.AreEqual(expectedCalls, Repository.SubmissionCalls);
        Assert.AreEqual(maxLengthContent, Repository.LastContent);
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

        ChatMessageSubmission actualSubmission = Service.SubmitChatMessage(
            FirstSession.SessionToken,
            10L,
            "  Hello  ");

        Assert.AreEqual(expectedStatus, actualSubmission.Status);
        Assert.AreSame(expectedMessage, actualSubmission.Message);
        Assert.AreEqual(expectedCalls, Repository.SubmissionCalls);
        Assert.AreEqual(expectedContent, Repository.LastContent);
        Assert.AreEqual(expectedMaximumMessages, Repository.LastMaximumMessages);
        Assert.AreEqual(expectedRateWindow, Repository.LastRateWindow);
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
