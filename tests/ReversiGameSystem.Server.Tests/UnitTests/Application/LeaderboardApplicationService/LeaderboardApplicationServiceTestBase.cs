using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApplicationService =
    ReversiGameSystem.Server.Application.Leaderboards.LeaderboardApplicationService;
using SharedLeaderboardRepositoryStub =
    ReversiGameSystem.Server.Tests.TestDoubles.LeaderboardRepositoryStub;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.LeaderboardApplicationService;

public abstract class LeaderboardApplicationServiceTestBase
{
    protected SharedLeaderboardRepositoryStub Repository { get; private set; } = null!;

    protected ApplicationService Service { get; private set; } = null!;

    [TestInitialize]
    public void InitializeBase()
    {
        Repository = new SharedLeaderboardRepositoryStub();
        Service = new ApplicationService(Repository);
    }
}
