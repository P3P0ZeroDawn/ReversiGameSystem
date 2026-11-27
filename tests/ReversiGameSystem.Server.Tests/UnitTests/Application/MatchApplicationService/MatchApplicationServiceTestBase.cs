using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Tests.TestDoubles;
using ApplicationService =
    ReversiGameSystem.Server.Application.Matches.MatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.MatchApplicationService;

public abstract class MatchApplicationServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected MatchRepositoryStub Matches { get; }

    private protected TemplatedEmailSenderStub EmailSender { get; }

    protected ApplicationService Service { get; }

    protected MatchApplicationServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock);
        Matches = new MatchRepositoryStub();
        EmailSender = new TemplatedEmailSenderStub();
        Service = new ApplicationService(Sessions, Matches, Clock, EmailSender);
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

    protected string AddGuestSession(string token)
    {
        Sessions.TryReserveGuest(new GuestSession(
            token,
            "Guest",
            Sessions.CalculateExpirationUtc()));
        return token;
    }
}
