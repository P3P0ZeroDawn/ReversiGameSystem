using System;
using ReversiGameSystem.Contracts.Services;
using ReversiGameSystem.LocalRuntime.Services;
using ReversiGameSystem.LocalRuntime.Sessions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Leaderboards;
using ReversiGameSystem.Server.Persistence.Players;

namespace ReversiGameSystem.LocalRuntime;

public sealed class LocalRuntimeServices
{
    internal LocalRuntimeServices(
        IAccountService accountService,
        IPlayerService playerService,
        string? profileImageDirectory = null)
    {
        AccountService = accountService;
        PlayerService = playerService;
        ProfileImageDirectory = profileImageDirectory;
    }

    public IAccountService AccountService { get; }

    public IPlayerService PlayerService { get; }

    public string? ProfileImageDirectory { get; }
}

public static class LocalRuntimeFactory
{
    public static LocalRuntimeServices CreateDefault()
    {
        string profileImageDirectory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReversiGameSystem",
            "profile-images");
        return CreateDefault(profileImageDirectory);
    }

    public static LocalRuntimeServices CreateDefault(string profileImageDirectory)
    {
        SystemUtcClock clock = new SystemUtcClock();
        Pbkdf2PasswordHasher passwordHasher = new Pbkdf2PasswordHasher();
        InMemoryAuthenticatedSessionStore sessionStore =
            new InMemoryAuthenticatedSessionStore(clock);

        return new LocalRuntimeServices(
            new LocalAccountService(
                new CreateAccountHandler(
                    new EntityFrameworkAccountRegistrationStore(),
                    passwordHasher,
                    new FileSystemProfileImageStore(profileImageDirectory),
                    clock),
                new LoginHandler(
                    new EntityFrameworkAccountLoginStore(),
                    passwordHasher,
                    sessionStore)),
            new LocalPlayerService(
                new GetPlayerProfileHandler(
                    sessionStore,
                    new EntityFrameworkPlayerProfileStore()),
                new GetLeaderboardHandler(new EntityFrameworkLeaderboardStore())),
            profileImageDirectory);
    }

    public static LocalRuntimeServices Create(
        ICreateAccountHandler createAccountHandler,
        ILoginHandler loginHandler,
        IGetPlayerProfileHandler getPlayerProfileHandler,
        IGetLeaderboardHandler getLeaderboardHandler)
    {
        if (createAccountHandler == null)
        {
            throw new ArgumentNullException(nameof(createAccountHandler));
        }

        if (loginHandler == null)
        {
            throw new ArgumentNullException(nameof(loginHandler));
        }

        if (getPlayerProfileHandler == null)
        {
            throw new ArgumentNullException(nameof(getPlayerProfileHandler));
        }

        if (getLeaderboardHandler == null)
        {
            throw new ArgumentNullException(nameof(getLeaderboardHandler));
        }

        return new LocalRuntimeServices(
            new LocalAccountService(createAccountHandler, loginHandler),
            new LocalPlayerService(getPlayerProfileHandler, getLeaderboardHandler));
    }
}
