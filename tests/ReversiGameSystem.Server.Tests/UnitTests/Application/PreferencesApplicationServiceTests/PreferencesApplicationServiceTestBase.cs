using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PreferencesApplicationServiceTests;

public abstract class PreferencesApplicationServiceTestBase
{
    private protected UtcClockStub Clock { get; }

    private protected SessionStore Sessions { get; }

    private protected PreferencesRepositoryStub Repository { get; }

    private protected TestDoubles.PlayerImageRepositoryStub Images { get; }

    private protected AuthenticatedSession Session { get; }

    private protected PreferencesApplicationService Service { get; }

    protected PreferencesApplicationServiceTestBase()
    {
        Clock = new UtcClockStub();
        Sessions = new SessionStore(Clock);
        Repository = new PreferencesRepositoryStub();
        Images = new TestDoubles.PlayerImageRepositoryStub();
        Session = TestSessionHelper.CreateSession(Sessions, 1, "PreferencesPlayer", "es-MX");
        Service = new PreferencesApplicationService(Sessions, Repository, Images);
    }

    private protected static PlayerPreferences CreatePreferences(
        string localeCode = "es-MX",
        string pieceStyleCode = "STANDARD",
        string? timeZoneId = null)
    {
        return new PlayerPreferences
        {
            PreferredLocaleCode = localeCode,
            PieceStyleCode = pieceStyleCode,
            TimeZoneId = timeZoneId
        };
    }

    private protected static ProfileImageData CreateValidPngImage()
    {
        return new ProfileImageData(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
            PlayerImagePolicy.PngMediaType);
    }
}
