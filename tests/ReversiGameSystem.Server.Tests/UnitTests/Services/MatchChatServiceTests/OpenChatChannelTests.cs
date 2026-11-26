using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.MatchChatServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class OpenChatChannelTests : MatchChatServiceTestBase
{
    [TestMethod]
    [DataRow("", SessionFailure.MissingSessionToken)]
    [DataRow("unknown-token", SessionFailure.InvalidSession)]
    public void OpenChatChannel_InvalidSession_ThrowsSessionFault(
        string sessionToken,
        SessionFailure expectedFailure)
    {
        Action action = () => Service.OpenChatChannel(sessionToken, 10L);

        FaultException<SessionFault> exception =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    public void OpenChatChannel_NonPositiveMatchId_ThrowsMatchFault()
    {
        MatchFailure expectedFailure = MatchFailure.InvalidMatchId;
        Action action = () => Service.OpenChatChannel(FirstSession.SessionToken, 0L);

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }

    [TestMethod]
    [DataRow(ChatStatus.InactiveAccount, MatchFailure.InactiveAccount)]
    [DataRow(ChatStatus.MatchNotFound, MatchFailure.MatchNotFound)]
    [DataRow(ChatStatus.Unauthorized, MatchFailure.Unauthorized)]
    [DataRow(ChatStatus.MatchNotInProgress, MatchFailure.MatchNotInProgress)]
    public void OpenChatChannel_RepositoryRejection_ThrowsMatchFault(
        ChatStatus repositoryStatus,
        MatchFailure expectedFailure)
    {
        Repository.ChannelAccess = new ChatChannelAccess(repositoryStatus);
        Action action = () => Service.OpenChatChannel(FirstSession.SessionToken, 10L);

        FaultException<MatchFault> exception =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualFailure = exception.Detail.Reason;

        Assert.AreEqual(expectedFailure, actualFailure);
    }
}
