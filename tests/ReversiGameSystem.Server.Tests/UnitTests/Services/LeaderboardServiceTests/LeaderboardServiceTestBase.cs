using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.LeaderboardServiceTests;

public abstract class LeaderboardServiceTestBase
{
    private protected LeaderboardRepositoryStub Repository { get; private set; } = null!;

    protected LeaderboardService Service { get; private set; } = null!;

    [TestInitialize]
    public void InitializeBase()
    {
        Repository = new LeaderboardRepositoryStub();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(Repository);
        Service = new LeaderboardService(applicationService);
    }
}
