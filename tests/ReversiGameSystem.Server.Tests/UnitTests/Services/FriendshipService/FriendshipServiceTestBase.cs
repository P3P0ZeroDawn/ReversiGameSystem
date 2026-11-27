using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.FriendshipService;

public abstract class FriendshipServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected FriendshipRepositoryStub Friendships { get; }

    protected FriendshipApplicationService ApplicationService { get; }

    protected AccountPresenceRegistry PresenceRegistry { get; }

    protected FriendshipSubscriptions Subscriptions { get; }

    protected Server.ServiceApi.FriendshipService Service { get; }

    protected FriendshipServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock, NullLogger<SessionStore>.Instance);
        Friendships = new FriendshipRepositoryStub();
        ApplicationService = new FriendshipApplicationService(Sessions, Friendships);
        PresenceRegistry = new AccountPresenceRegistry();
        Subscriptions = new FriendshipSubscriptions(NullLogger<FriendshipSubscriptions>.Instance);
        Service = new Server.ServiceApi.FriendshipService(
            ApplicationService,
            PresenceRegistry,
            Subscriptions);
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
