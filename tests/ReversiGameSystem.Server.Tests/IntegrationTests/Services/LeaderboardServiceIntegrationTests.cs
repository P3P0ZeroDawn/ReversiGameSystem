using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;
using ContractLeaderboardEntry =
    ReversiGameSystem.Server.Contracts.Leaderboards.LeaderboardEntry;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class LeaderboardServiceIntegrationTests
{
    private LeaderboardRepositoryStub _repository = null!;
    private WcfChannelScope<ILeaderboardService> _scope = null!;
    private ILeaderboardService _channel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _repository = new LeaderboardRepositoryStub();
        LeaderboardService service = new LeaderboardService(
            new LeaderboardApplicationService(_repository));
        _scope = WcfTestHostHelper.OpenChannel<ILeaderboardService>(service, "Leaderboard");
        _channel = _scope.Channel;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void GetLeaderboard_ThroughWcf_MapsOrderedEntries()
    {
        long expectedAccountId = 42L;
        int expectedCount = 1;
        int expectedPosition = 1;
        double expectedWinRate = 0.5d;
        bool expectedHasImage = true;
        bool expectedWasCalled = true;
        _repository.Players = new LeaderboardPlayer[]
        {
            new LeaderboardPlayer(expectedAccountId, "PlayerOne", "image", 6, 3)
        };

        ContractLeaderboardEntry[] actualEntries = _channel.GetLeaderboard(10);
        int actualCount = actualEntries.Length;
        int actualPosition = actualEntries[0].Position;
        long actualAccountId = actualEntries[0].Account.AccountId;
        bool actualHasImage = actualEntries[0].HasProfileImage;
        double actualWinRate = actualEntries[0].WinRate;
        bool actualWasCalled = _repository.WasCalled;

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedPosition, actualPosition);
        Assert.AreEqual(expectedAccountId, actualAccountId);
        Assert.AreEqual(expectedHasImage, actualHasImage);
        Assert.AreEqual(expectedWinRate, actualWinRate);
        Assert.AreEqual(expectedWasCalled, actualWasCalled);
    }

    [TestMethod]
    public void GetLeaderboard_InvalidLimitThroughWcf_ReturnsTypedFaultAndKeepsChannelOpen()
    {
        LeaderboardFailure expectedReason = LeaderboardFailure.InvalidMaximumEntries;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.GetLeaderboard(0);

        FaultException<LeaderboardFault> fault =
            Assert.Throws<FaultException<LeaderboardFault>>(action);
        LeaderboardFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void GetLeaderboard_UnavailableThroughWcf_ReturnsServiceFault()
    {
        _repository.ThrowOnGet = true;
        string expectedFailureCode = "LEADERBOARD_OPERATION_FAILURE";
        bool expectedHasCorrelation = true;
        Action action = () => _channel.GetLeaderboard(10);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        string actualFailureCode = fault.Detail.FailureCode;
        bool actualHasCorrelation = !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId);

        Assert.AreEqual(expectedFailureCode, actualFailureCode);
        Assert.AreEqual(expectedHasCorrelation, actualHasCorrelation);
    }
}
