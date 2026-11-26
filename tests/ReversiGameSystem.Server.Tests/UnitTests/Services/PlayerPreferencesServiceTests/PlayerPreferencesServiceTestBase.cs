using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;
using ContractPreferences = ReversiGameSystem.Server.Contracts.Players.PlayerPreferences;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.PlayerPreferencesServiceTests;

public abstract class PlayerPreferencesServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected PreferencesRepositoryStub Repository { get; }

    private protected TestDoubles.PlayerImageRepositoryStub Images { get; }

    private protected AuthenticatedSession Session { get; }

    private protected PlayerPreferencesService Service { get; }

    protected PlayerPreferencesServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock);
        Repository = new PreferencesRepositoryStub();
        Images = new TestDoubles.PlayerImageRepositoryStub();
        Session = TestSessionHelper.CreateSession(Sessions, 1, "PreferencesPlayer", "es-MX");
        PreferencesApplicationService applicationService =
            new PreferencesApplicationService(Sessions, Repository, Images);
        Service = new PlayerPreferencesService(applicationService);
    }

    private protected static ContractPreferences CreateContractPreferences(
        string localeCode = "es-MX",
        string pieceStyleCode = "STANDARD",
        string? timeZoneId = null,
        bool useCustomBoardBackground = false)
    {
        return new ContractPreferences
        {
            PreferredLocaleCode = localeCode,
            PieceStyleCode = pieceStyleCode,
            TimeZoneId = timeZoneId,
            UseCustomBoardBackground = useCustomBoardBackground
        };
    }

    private protected static ImageContent CreateValidPngImageContent()
    {
        return new ImageContent
        {
            Content = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
            MediaType = PlayerImagePolicy.PngMediaType
        };
    }
}
