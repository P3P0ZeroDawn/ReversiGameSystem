using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.FriendshipApplicationService;

public abstract class FriendshipApplicationServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected FriendshipRepositoryStub Friendships { get; }

    protected Server.Application.Friendships.FriendshipApplicationService Service { get; }

    protected FriendshipApplicationServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock);
        Friendships = new FriendshipRepositoryStub();
        Service = new Server.Application.Friendships.FriendshipApplicationService(
            Sessions,
            Friendships);
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
