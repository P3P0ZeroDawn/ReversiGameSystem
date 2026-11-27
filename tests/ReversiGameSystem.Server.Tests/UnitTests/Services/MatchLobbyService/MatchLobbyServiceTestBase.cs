using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.MatchLobbyService;

public abstract class MatchLobbyServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected MatchRepositoryStub Matches { get; }

    private protected TemplatedEmailSenderStub EmailSender { get; }

    protected MatchApplicationService ApplicationService { get; }

    protected MatchCallbackSubscriptions<IMatchLobbyServiceCallback> Subscriptions { get; }

    protected Server.ServiceApi.MatchLobbyService Service { get; }

    protected MatchLobbyServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock, NullLogger<SessionStore>.Instance);
        Matches = new MatchRepositoryStub();
        EmailSender = new TemplatedEmailSenderStub();
        ApplicationService = new MatchApplicationService(Sessions, Matches, Clock, EmailSender, NullLogger<MatchApplicationService>.Instance);
        Subscriptions = new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
            (_, _) => true,
            NullLogger<MatchCallbackSubscriptions<IMatchLobbyServiceCallback>>.Instance);
        Service = new Server.ServiceApi.MatchLobbyService(ApplicationService, Subscriptions);
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
