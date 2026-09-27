using System.Threading.Tasks;
using ReversiGameSystem.Contracts.Accounts;
using ReversiGameSystem.Contracts.Leaderboards;
using ReversiGameSystem.Contracts.Media;
using ReversiGameSystem.Contracts.Players;
using ReversiGameSystem.LocalRuntime;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.Tests;

internal sealed class LocalRuntimeTests
{
    internal void CreateAccountAsync_MapsRequestAndResponse()
    {
        // Arrange
        RecordingHandlers handlers = new RecordingHandlers
        {
            CreateAccountResult = new CreateAccountResult(CreateAccountOutcome.Success, 42)
        };
        LocalRuntimeServices services = CreateServices(handlers);
        byte[] imageContent = { 1, 2, 3 };

        // Act
        CreateAccountResponse response = services.AccountService.CreateAccountAsync(
            new CreateAccountRequest
            {
                Username = "PlayerOne",
                EmailAddress = "player@example.com",
                Password = "Password1!",
                PreferredLocaleCode = "en-US",
                ProfileImage = new ProfileImageUploadDto
                {
                    Content = imageContent,
                    MediaType = "image/png"
                }
            }).GetAwaiter().GetResult();
        imageContent[0] = 9;

        // Assert
        TestAssert.AreEqual("PlayerOne", handlers.CreateAccountCommand?.Username);
        TestAssert.AreEqual("player@example.com", handlers.CreateAccountCommand?.EmailAddress);
        TestAssert.AreEqual("en-US", handlers.CreateAccountCommand?.PreferredLocaleCode);
        TestAssert.AreEqual("image/png", handlers.CreateAccountCommand?.ProfileImage?.MediaType);
        TestAssert.AreEqual((byte)1, handlers.CreateAccountCommand?.ProfileImage?.Content[0]);
        TestAssert.AreEqual(CreateAccountResultCode.Success, response.ResultCode);
        TestAssert.AreEqual<long?>(42, response.AccountId);
    }

    internal void LoginAsync_MapsAuthenticatedSession()
    {
        // Arrange
        RecordingHandlers handlers = new RecordingHandlers
        {
            LoginResult = new LoginResult(
                LoginOutcome.Success,
                new AuthenticatedSession("session-token", 42, "PlayerOne", "player@example.com"))
        };
        LocalRuntimeServices services = CreateServices(handlers);

        // Act
        LoginResponse response = services.AccountService.LoginAsync(
            new LoginRequest
            {
                Login = "PlayerOne",
                Password = "Password1!"
            }).GetAwaiter().GetResult();

        // Assert
        TestAssert.AreEqual("PlayerOne", handlers.LoginCommand?.Login);
        TestAssert.AreEqual(LoginResultCode.Success, response.ResultCode);
        TestAssert.AreEqual("session-token", response.Session?.SessionToken);
        TestAssert.AreEqual<long?>(42, response.Session?.AccountId);
        TestAssert.AreEqual("player@example.com", response.Session?.EmailAddress);
    }

    internal void GetPlayerProfileAsync_MapsProfileStatistics()
    {
        // Arrange
        RecordingHandlers handlers = new RecordingHandlers
        {
            PlayerProfileResult = new PlayerProfileResult(
                PlayerProfileOutcome.Success,
                new PlayerProfile(
                    42,
                    "PlayerOne",
                    "profiles/42.png",
                    new PlayerStatistics(12, 7, 3, 2, 7d / 12d)))
        };
        LocalRuntimeServices services = CreateServices(handlers);

        // Act
        PlayerProfileResponse response = services.PlayerService.GetPlayerProfileAsync(
            new PlayerProfileRequest
            {
                SessionToken = "session-token",
                AccountId = 42
            }).GetAwaiter().GetResult();

        // Assert
        TestAssert.AreEqual("session-token", handlers.PlayerProfileQuery?.SessionToken);
        TestAssert.AreEqual<long?>(42, handlers.PlayerProfileQuery?.AccountId);
        TestAssert.AreEqual(PlayerProfileResultCode.Success, response.ResultCode);
        TestAssert.AreEqual("profiles/42.png", response.Profile?.ProfileImageUri);
        TestAssert.AreEqual(12, response.Profile?.Statistics.MatchesPlayed);
        TestAssert.AreEqual(7, response.Profile?.Statistics.Wins);
        TestAssert.AreEqual(2, response.Profile?.Statistics.Draws);
    }

    internal void GetLeaderboardAsync_MapsRankedEntries()
    {
        // Arrange
        RecordingHandlers handlers = new RecordingHandlers
        {
            LeaderboardResult = new LeaderboardResult(
                LeaderboardOutcome.Success,
                new[]
                {
                    new LeaderboardEntry(1, 42, "PlayerOne", null, 12, 7, 7d / 12d)
                })
        };
        LocalRuntimeServices services = CreateServices(handlers);

        // Act
        LeaderboardResponse response = services.PlayerService.GetLeaderboardAsync(
            new LeaderboardRequest { MaximumEntries = 10 }).GetAwaiter().GetResult();

        // Assert
        TestAssert.AreEqual(10, handlers.LeaderboardQuery?.MaximumEntries);
        TestAssert.AreEqual(LeaderboardResultCode.Success, response.ResultCode);
        TestAssert.AreEqual(1, response.Entries.Length);
        TestAssert.AreEqual(1, response.Entries[0].Rank);
        TestAssert.AreEqual("PlayerOne", response.Entries[0].Username);
        TestAssert.AreEqual(7, response.Entries[0].Wins);
    }

    private static LocalRuntimeServices CreateServices(RecordingHandlers handlers)
    {
        return LocalRuntimeFactory.Create(handlers, handlers, handlers, handlers);
    }
}

internal sealed class RecordingHandlers :
    ICreateAccountHandler,
    ILoginHandler,
    IGetPlayerProfileHandler,
    IGetLeaderboardHandler
{
    internal CreateAccountCommand? CreateAccountCommand { get; private set; }

    internal LoginCommand? LoginCommand { get; private set; }

    internal PlayerProfileQuery? PlayerProfileQuery { get; private set; }

    internal LeaderboardQuery? LeaderboardQuery { get; private set; }

    internal CreateAccountResult CreateAccountResult { get; set; } =
        new CreateAccountResult(CreateAccountOutcome.ServiceUnavailable);

    internal LoginResult LoginResult { get; set; } =
        new LoginResult(LoginOutcome.ServiceUnavailable);

    internal PlayerProfileResult PlayerProfileResult { get; set; } =
        new PlayerProfileResult(PlayerProfileOutcome.ServiceUnavailable);

    internal LeaderboardResult LeaderboardResult { get; set; } =
        new LeaderboardResult(LeaderboardOutcome.ServiceUnavailable);

    public Task<CreateAccountResult> ExecuteAsync(CreateAccountCommand command)
    {
        CreateAccountCommand = command;
        return Task.FromResult(CreateAccountResult);
    }

    public Task<LoginResult> ExecuteAsync(LoginCommand command)
    {
        LoginCommand = command;
        return Task.FromResult(LoginResult);
    }

    public Task<PlayerProfileResult> ExecuteAsync(PlayerProfileQuery query)
    {
        PlayerProfileQuery = query;
        return Task.FromResult(PlayerProfileResult);
    }

    public Task<LeaderboardResult> ExecuteAsync(LeaderboardQuery query)
    {
        LeaderboardQuery = query;
        return Task.FromResult(LeaderboardResult);
    }
}
