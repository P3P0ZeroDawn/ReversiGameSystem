using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class ChatApplicationServiceTests
{
    [TestMethod]
    public void SubmitChatMessage_EmptyContent_ReturnsEmptyMessageStatus()
    {
        ChatTestContext context = new ChatTestContext();
        ChatStatus expectedStatus = ChatStatus.EmptyMessage;

        ChatMessageSubmission submission = context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "   ");
        ChatStatus actualStatus = submission.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void SubmitChatMessage_EmptyContent_DoesNotCallRepository()
    {
        ChatTestContext context = new ChatTestContext();
        int expectedCalls = 0;

        context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "   ");
        int actualCalls = context.Repository.SubmissionCalls;

        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_ValidContent_ReturnsSuccessStatus()
    {
        ChatTestContext context = CreateContextWithSubmission(out _);
        ChatStatus expectedStatus = ChatStatus.Success;

        ChatMessageSubmission submission = context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "  Hello  ");
        ChatStatus actualStatus = submission.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void SubmitChatMessage_ValidContent_ReturnsPersistedMessage()
    {
        ChatTestContext context = CreateContextWithSubmission(out ChatMessage expectedMessage);

        ChatMessageSubmission submission = context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "  Hello  ");
        ChatMessage? actualMessage = submission.Message;

        Assert.AreEqual(expectedMessage, actualMessage);
    }

    [TestMethod]
    public void SubmitChatMessage_ValidContent_TrimsContentBeforeRepositoryCall()
    {
        ChatTestContext context = CreateContextWithSubmission(out _);
        string expectedContent = "Hello";

        context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "  Hello  ");
        string actualContent = context.Repository.LastContent;

        Assert.AreEqual(expectedContent, actualContent);
    }

    [TestMethod]
    public void SubmitChatMessage_ValidContent_CallsRepositoryOnce()
    {
        ChatTestContext context = CreateContextWithSubmission(out _);
        int expectedCalls = 1;

        context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "  Hello  ");
        int actualCalls = context.Repository.SubmissionCalls;

        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_RepositoryFailure_PropagatesException()
    {
        ChatTestContext context = new ChatTestContext();
        context.Repository.ThrowOnSubmission = true;
        Action action = () => context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "Hello");

        InvalidOperationException actualException =
            Assert.Throws<InvalidOperationException>(action);

        Assert.IsNotNull(actualException);
    }

    [TestMethod]
    public void CanReceiveMuteNotification_SameAccountSession_ReturnsTrue()
    {
        ChatTestContext context = new ChatTestContext();
        AuthenticatedSession secondSession = TestSessionHelper.CreateSession(
            context.Sessions, 1, "PlayerOne");
        bool expectedResult = true;

        bool actualResult = context.ApplicationService.CanReceiveMuteNotification(
            context.FirstSession.SessionToken,
            secondSession.SessionToken,
            10);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void CanReceiveMuteNotification_DifferentAccountSession_ReturnsFalse()
    {
        ChatTestContext context = new ChatTestContext();
        AuthenticatedSession differentAccount = TestSessionHelper.CreateSession(
            context.Sessions, 2, "PlayerTwo");
        bool expectedResult = false;

        bool actualResult = context.ApplicationService.CanReceiveMuteNotification(
            context.FirstSession.SessionToken,
            differentAccount.SessionToken,
            10);

        Assert.AreEqual(expectedResult, actualResult);
    }

    [TestMethod]
    public void CanReceiveMuteNotification_SessionCheck_DoesNotCallRepository()
    {
        ChatTestContext context = new ChatTestContext();
        AuthenticatedSession secondSession = TestSessionHelper.CreateSession(
            context.Sessions, 1, "PlayerOne");
        int expectedCalls = 0;

        context.ApplicationService.CanReceiveMuteNotification(
            context.FirstSession.SessionToken,
            secondSession.SessionToken,
            10);
        int actualCalls = context.Repository.AccessEvaluationCalls;

        Assert.AreEqual(expectedCalls, actualCalls);
    }

    private static ChatTestContext CreateContextWithSubmission(out ChatMessage expectedMessage)
    {
        ChatTestContext context = new ChatTestContext();
        expectedMessage = new ChatMessage
        {
            MatchId = 10,
            MessageNumber = 3,
            SenderSeatNumber = 1,
            Content = "Hello",
            SentAtUtc = context.Clock.UtcNow
        };
        context.Repository.Submission = new ChatMessageSubmission(expectedMessage);
        return context;
    }

    private sealed class ChatTestContext
    {
        internal ChatTestContext()
        {
            Clock = new ChatClock(new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc));
            Sessions = new SessionStore(Clock);
            Repository = new ChatRepositoryStub();
            FirstSession = TestSessionHelper.CreateSession(Sessions, 1, "PlayerOne");
            ApplicationService = new ChatApplicationService(Sessions, Repository, Clock);
        }

        internal ChatClock Clock { get; }

        internal SessionStore Sessions { get; }

        internal ChatRepositoryStub Repository { get; }

        internal AuthenticatedSession FirstSession { get; }

        internal ChatApplicationService ApplicationService { get; }
    }

    private sealed class ChatClock : IUtcClock
    {
        internal ChatClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class ChatRepositoryStub : IChatRepository
    {
        internal ChatRepositoryStub()
        {
            ChatMessage message = new ChatMessage();
            Submission = new ChatMessageSubmission(message);
        }

        internal int AccessEvaluationCalls { get; private set; }

        internal int SubmissionCalls { get; private set; }

        internal string LastContent { get; private set; } = string.Empty;

        internal bool ThrowOnSubmission { get; set; }

        internal ChatMessageSubmission Submission { get; set; }

        public ChatStatus EvaluateParticipantAccess(long accountId, long matchId)
        {
            AccessEvaluationCalls++;
            return ChatStatus.Success;
        }

        public ChatChannelAccess LoadChannelAccess(
            long accountId,
            long matchId,
            DateTime nowUtc)
        {
            return new ChatChannelAccess(null);
        }

        public ChatMessageSubmission ProcessMessageSubmission(
            long accountId,
            long matchId,
            string content,
            int maximumMessages,
            TimeSpan rateWindow)
        {
            SubmissionCalls++;
            LastContent = content;
            if (ThrowOnSubmission)
            {
                throw new InvalidOperationException("Simulated repository failure.");
            }

            return Submission;
        }

        public ChatHistory LoadMessageHistory(long accountId, long matchId)
        {
            return new ChatHistory(Array.Empty<ChatMessage>());
        }
    }
}
