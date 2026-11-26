using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.LeaderboardApplicationServiceTests;

public abstract class LeaderboardApplicationServiceTestBase
{
    private protected LeaderboardRepositoryStub Repository { get; private set; } = null!;

    protected LeaderboardApplicationService Service { get; private set; } = null!;

    [TestInitialize]
    public void InitializeBase()
    {
        Repository = new LeaderboardRepositoryStub();
        Service = new LeaderboardApplicationService(Repository);
    }
}
