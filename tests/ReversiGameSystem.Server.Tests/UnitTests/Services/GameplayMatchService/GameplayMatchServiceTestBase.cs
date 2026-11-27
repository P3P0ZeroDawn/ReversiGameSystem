using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.GameplayMatchService;

public abstract class GameplayMatchServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected GameplayRepositoryStub Matches { get; }

    protected GameplayMatchApplicationService ApplicationService { get; }

    protected GameplayMatchPresence Presence { get; }

    protected MatchCallbackSubscriptions<IGameplayMatchServiceCallback> Subscriptions { get; }

    protected Server.ServiceApi.GameplayMatchService Service { get; }

    protected GameplayMatchServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock, NullLogger<SessionStore>.Instance);
        Matches = new GameplayRepositoryStub();
        ApplicationService = new GameplayMatchApplicationService(Sessions, Matches, Clock);
        Presence = new GameplayMatchPresence();
        Subscriptions = new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
            (_, _) => true,
            NullLogger<MatchCallbackSubscriptions<IGameplayMatchServiceCallback>>.Instance);
        Service = new Server.ServiceApi.GameplayMatchService(
            ApplicationService,
            Presence,
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

    protected static Match CreateTwoPlayerMatch(
        long matchId = 100L,
        long playerOneId = 10L,
        long playerTwoId = 20L,
        int turnDurationSeconds = 40)
    {
        return new Match
        {
            Id = matchId,
            Code = "ABC234",
            State = "InProgress",
            TurnDurationSeconds = turnDurationSeconds,
            Participants = new MatchParticipant[]
            {
                new MatchParticipant
                {
                    SeatNumber = 1,
                    AccountId = playerOneId,
                    DisplayName = "PlayerOne"
                },
                new MatchParticipant
                {
                    SeatNumber = 2,
                    AccountId = playerTwoId,
                    DisplayName = "PlayerTwo"
                }
            }
        };
    }
}
