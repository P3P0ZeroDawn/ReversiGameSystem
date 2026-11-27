using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.PlayerProfileService;

public abstract class PlayerProfileServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected TestDoubles.PlayerProfileRepositoryStub Profiles { get; }

    private protected TestDoubles.PlayerImageRepositoryStub Images { get; }

    protected PlayerProfileApplicationService ApplicationService { get; }

    protected Server.ServiceApi.PlayerProfileService Service { get; }

    protected PlayerProfileServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock, NullLogger<SessionStore>.Instance);
        Profiles = new TestDoubles.PlayerProfileRepositoryStub();
        Images = new TestDoubles.PlayerImageRepositoryStub();
        ApplicationService = new PlayerProfileApplicationService(Sessions, Profiles, Images);
        Service = new Server.ServiceApi.PlayerProfileService(ApplicationService);
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
