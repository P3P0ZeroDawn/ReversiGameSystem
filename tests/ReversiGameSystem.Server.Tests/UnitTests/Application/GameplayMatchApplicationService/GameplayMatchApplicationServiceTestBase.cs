using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Tests.TestDoubles;
using ApplicationService =
    ReversiGameSystem.Server.Application.Matches.GameplayMatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

public abstract class GameplayMatchApplicationServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected GameplayRepositoryStub Matches { get; }

    protected ApplicationService Service { get; }

    protected GameplayMatchApplicationServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock, NullLogger<SessionStore>.Instance);
        Matches = new GameplayRepositoryStub();
        Service = new ApplicationService(
            Sessions,
            Matches,
            Clock,
            NullLogger<ApplicationService>.Instance);
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
