using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.ChatApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class PrepareChatChannelTests : ChatApplicationServiceTestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void PrepareChatChannel_MissingToken_ReturnsMissingSessionTokenWithoutRepositoryCall(
        string sessionToken)
    {
        ChatStatus expectedStatus = ChatStatus.MissingSessionToken;
        int expectedCalls = 0;

        ChatChannelAccess access = Service.PrepareChatChannel(sessionToken, 10L);
        ChatStatus actualStatus = access.Status;
        int actualCalls = Repository.LoadChannelAccessCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void PrepareChatChannel_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ChatStatus expectedStatus = ChatStatus.InvalidSession;
        int expectedCalls = 0;

        ChatChannelAccess access = Service.PrepareChatChannel("unknown-token", 10L);
        ChatStatus actualStatus = access.Status;
        int actualCalls = Repository.LoadChannelAccessCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    public void PrepareChatChannel_NonPositiveMatchId_ReturnsInvalidMatchIdWithoutRepositoryCall(
        long matchId)
    {
        ChatStatus expectedStatus = ChatStatus.InvalidMatchId;
        int expectedCalls = 0;

        ChatChannelAccess access = Service.PrepareChatChannel(
            FirstSession.SessionToken,
            matchId);
        ChatStatus actualStatus = access.Status;
        int actualCalls = Repository.LoadChannelAccessCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void PrepareChatChannel_ValidRequest_PassesClockUtcNowAndReturnsAccess()
    {
        ChatMute expectedMute = CreateSampleMute();
        Repository.ChannelAccess = new ChatChannelAccess(expectedMute);
        ChatStatus expectedStatus = ChatStatus.Success;
        int expectedCalls = 1;
        long expectedAccountId = FirstSession.AccountId;
        long expectedMatchId = 25L;
        DateTime expectedNowUtc = Clock.UtcNow;

        ChatChannelAccess access = Service.PrepareChatChannel(
            FirstSession.SessionToken,
            25L);
        ChatStatus actualStatus = access.Status;
        ChatMute? actualMute = access.ActiveMute;
        int actualCalls = Repository.LoadChannelAccessCalls;
        long actualAccountId = Repository.LastAccountId;
        long actualMatchId = Repository.LastMatchId;
        DateTime actualNowUtc = Repository.LastNowUtc;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedMute, actualMute);
        Assert.AreEqual(expectedCalls, actualCalls);
        Assert.AreEqual(expectedAccountId, actualAccountId);
        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedNowUtc, actualNowUtc);
    }

    [TestMethod]
    public void PrepareChatChannel_RepositoryFailure_PropagatesException()
    {
        Repository.ThrowOnLoadChannelAccess = true;
        Action action = () => Service.PrepareChatChannel(FirstSession.SessionToken, 10L);

        InvalidOperationException actualException =
            Assert.Throws<InvalidOperationException>(action);

        Assert.IsNotNull(actualException);
    }
}
