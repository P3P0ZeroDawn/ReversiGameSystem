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

/// <summary>
/// Represents local runtime services.
/// </summary>
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

    /// <summary>
    /// Gets the account service.
    /// </summary>
    public IAccountService AccountService { get; }

    /// <summary>
    /// Gets the player service.
    /// </summary>
    public IPlayerService PlayerService { get; }

    /// <summary>
    /// Gets the profile image directory.
    /// </summary>
    public string? ProfileImageDirectory { get; }
}

/// <summary>
/// Represents local runtime factory.
/// </summary>
public static class LocalRuntimeFactory
{
    /// <summary>
    /// Performs the create default operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static LocalRuntimeServices CreateDefault()
    {
        string profileImageDirectory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReversiGameSystem",
            "profile-images");
        return CreateDefault(profileImageDirectory);
    }

    /// <summary>
    /// Performs the create default operation.
    /// </summary>
    /// <param name="profileImageDirectory">The profile image directory.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="createAccountHandler">The create account handler.</param>
    /// <param name="loginHandler">The login handler.</param>
    /// <param name="getPlayerProfileHandler">The get player profile handler.</param>
    /// <param name="getLeaderboardHandler">The get leaderboard handler.</param>
    /// <returns>The result of the operation.</returns>
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
