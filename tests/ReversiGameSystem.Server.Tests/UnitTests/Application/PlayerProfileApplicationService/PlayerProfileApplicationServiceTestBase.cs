using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Tests.TestDoubles;
using ApplicationService =
    ReversiGameSystem.Server.Application.Players.PlayerProfileApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PlayerProfileApplicationService;

public abstract class PlayerProfileApplicationServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected TestDoubles.PlayerProfileRepositoryStub Profiles { get; }

    private protected TestDoubles.PlayerImageRepositoryStub Images { get; }

    protected ApplicationService Service { get; }

    protected PlayerProfileApplicationServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock, NullLogger<SessionStore>.Instance);
        Profiles = new TestDoubles.PlayerProfileRepositoryStub();
        Images = new TestDoubles.PlayerImageRepositoryStub();
        Service = new ApplicationService(Sessions, Profiles, Images);
    }

    protected string AddAuthenticatedSession(
        long accountId,
        string username,
        string localeCode = "en-US")
    {
        return TestSessionHelper.CreateSession(
            Sessions,
            accountId,
            username,
            localeCode).SessionToken;
    }
}
