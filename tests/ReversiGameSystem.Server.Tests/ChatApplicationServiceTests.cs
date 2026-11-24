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
    public void SubmitChatMessage_EmptyContent_RejectsBeforeRepositoryAccess()
    {
        ChatTestContext context = new ChatTestContext();

        ChatMessageSubmission result = context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "   ");

        Assert.AreEqual(ChatStatus.EmptyMessage, result.Status);
        Assert.AreEqual(0, context.Repository.SubmissionCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_ValidContent_ReturnsPersistedMessage()
    {
        ChatTestContext context = new ChatTestContext();
        ChatMessage expected = new ChatMessage
        {
            MatchId = 10,
            MessageNumber = 3,
            SenderSeatNumber = 1,
            Content = "Hello",
            SentAtUtc = context.Clock.UtcNow
        };
        context.Repository.Submission = new ChatMessageSubmission(expected);

        ChatMessageSubmission result = context.ApplicationService.SubmitChatMessage(
            context.FirstSession.SessionToken,
            10,
            "  Hello  ");

        Assert.AreEqual(ChatStatus.Success, result.Status);
        Assert.AreEqual(expected, result.Message);
        Assert.AreEqual("Hello", context.Repository.LastContent);
        Assert.AreEqual(1, context.Repository.SubmissionCalls);
    }

    [TestMethod]
    public void SubmitChatMessage_RepositoryFailure_PropagatesException()
    {
        ChatTestContext context = new ChatTestContext();
        context.Repository.ThrowOnSubmission = true;

        Assert.Throws<InvalidOperationException>(() =>
            context.ApplicationService.SubmitChatMessage(
                context.FirstSession.SessionToken,
                10,
                "Hello"));
    }

    [TestMethod]
    public void CanReceiveMuteNotification_SameAccountSession_AuthorizesRecipient()
    {
        ChatTestContext context = new ChatTestContext();
        AuthenticatedSession secondSession = TestSessionHelper.CreateSession(
            context.Sessions, 1, "PlayerOne");
        AuthenticatedSession differentAccount = TestSessionHelper.CreateSession(
            context.Sessions, 2, "PlayerTwo");

        bool sameAccount = context.ApplicationService.CanReceiveMuteNotification(
            context.FirstSession.SessionToken,
            secondSession.SessionToken,
            10);
        bool otherAccount = context.ApplicationService.CanReceiveMuteNotification(
            context.FirstSession.SessionToken,
            differentAccount.SessionToken,
            10);

        Assert.AreEqual(true, sameAccount);
        Assert.AreEqual(false, otherAccount);
        Assert.AreEqual(0, context.Repository.AccessEvaluationCalls);
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

        internal static LoginAccount CreateAccount(long accountId, string username)
        {
            return new LoginAccount(
                accountId,
                username,
                username + "@example.test",
                "hash",
                "ACTIVE",
                "en-US");
        }
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
