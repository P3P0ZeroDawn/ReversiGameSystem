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

        ChatChannelAccess actualAccess = Service.PrepareChatChannel(sessionToken, 10L);
        int actualCalls = Repository.LoadChannelAccessCalls;

        Assert.AreEqual(expectedStatus, actualAccess.Status);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void PrepareChatChannel_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ChatStatus expectedStatus = ChatStatus.InvalidSession;
        int expectedCalls = 0;

        ChatChannelAccess actualAccess = Service.PrepareChatChannel("unknown-token", 10L);
        int actualCalls = Repository.LoadChannelAccessCalls;

        Assert.AreEqual(expectedStatus, actualAccess.Status);
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

        ChatChannelAccess actualAccess = Service.PrepareChatChannel(
            FirstSession.SessionToken,
            matchId);
        int actualCalls = Repository.LoadChannelAccessCalls;

        Assert.AreEqual(expectedStatus, actualAccess.Status);
        Assert.AreEqual(expectedCalls, actualCalls);
    }

    [TestMethod]
    public void PrepareChatChannel_ValidRequest_PassesClockUtcNowAndReturnsAccess()
    {
        ChatMute activeMute = CreateSampleMute();
        Repository.ChannelAccess = new ChatChannelAccess(activeMute);
        ChatStatus expectedStatus = ChatStatus.Success;
        int expectedCalls = 1;
        long expectedAccountId = FirstSession.AccountId;
        long expectedMatchId = 25L;
        DateTime expectedNowUtc = Clock.UtcNow;

        ChatChannelAccess actualAccess = Service.PrepareChatChannel(
            FirstSession.SessionToken,
            25L);

        Assert.AreEqual(expectedStatus, actualAccess.Status);
        Assert.AreSame(activeMute, actualAccess.ActiveMute);
        Assert.AreEqual(expectedCalls, Repository.LoadChannelAccessCalls);
        Assert.AreEqual(expectedAccountId, Repository.LastAccountId);
        Assert.AreEqual(expectedMatchId, Repository.LastMatchId);
        Assert.AreEqual(expectedNowUtc, Repository.LastNowUtc);
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
