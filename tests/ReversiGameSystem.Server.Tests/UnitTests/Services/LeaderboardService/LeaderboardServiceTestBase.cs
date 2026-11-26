using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;
using SharedLeaderboardRepositoryStub =
    ReversiGameSystem.Server.Tests.TestDoubles.LeaderboardRepositoryStub;
using WcfLeaderboardService = ReversiGameSystem.Server.ServiceApi.LeaderboardService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.LeaderboardService;

public abstract class LeaderboardServiceTestBase
{
    protected SharedLeaderboardRepositoryStub Repository { get; private set; } = null!;

    protected WcfLeaderboardService Service { get; private set; } = null!;

    [TestInitialize]
    public void InitializeBase()
    {
        Repository = new SharedLeaderboardRepositoryStub();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(Repository);
        Service = new WcfLeaderboardService(applicationService);
    }
}
