using System;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Friendships;
using ReversiGameSystem.Server.Persistence.Players;
using ReversiGameSystem.Server.Persistence.Leaderboards;
using ReversiGameSystem.Server.Persistence.Configuration;
using ReversiGameSystem.Server.Persistence.Mail;
using ReversiGameSystem.Server.Persistence.Verification;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Host;

internal static class Program
{
    private static int Main()
    {
        using (ILoggerFactory loggerFactory = LoggerFactory.Create(
            builder => builder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole(options => options.FormatterName = SafeConsoleFormatter.FormatterName)
                .AddConsoleFormatter<SafeConsoleFormatter, ConsoleFormatterOptions>()))
        {
            ILogger<AccountAccessService> serviceLogger =
                loggerFactory.CreateLogger<AccountAccessService>();
            ILogger<ServerHostLogCategory> hostLogger =
                loggerFactory.CreateLogger<ServerHostLogCategory>();
            try
            {
                SystemUtcClock clock = new SystemUtcClock();
                InMemoryAuthenticatedSessionStore sessions =
                    new InMemoryAuthenticatedSessionStore(clock);
                SessionRegistry sessionRegistry = new SessionRegistry(sessions, clock);
                using (FriendshipService friendship = new FriendshipService(sessions,
                    new FriendRepository(), loggerFactory.CreateLogger<FriendshipService>()))
                {
                    MatchInvitationEmailSender matchMailer = new MatchInvitationEmailSender(MailSettings.Load());
                    EntityFrameworkMatchStore matchStore = new EntityFrameworkMatchStore();
                    matchStore.CancelAbandonedWaitingMatches();
                    MatchHandler matches = new MatchHandler(sessions, matchStore, clock, sessionRegistry, matchMailer);
                    using MatchLobbyService lobby = new MatchLobbyService(matches,
                        loggerFactory.CreateLogger<MatchLobbyService>());
                    using GameplayMatchService gameplay = new GameplayMatchService(matches, sessions,
                        loggerFactory.CreateLogger<GameplayMatchService>());
                    using MatchChatService chat = new MatchChatService(
                        new ChatHandler(sessions, new EntityFrameworkChatStore(), clock), sessions,
                        loggerFactory.CreateLogger<MatchChatService>());
                    SessionService session = CreateSessionService(
                        loggerFactory.CreateLogger<SessionService>(), sessionRegistry, clock);
                    session.SessionClosed += friendship.RemoveSession;
                    session.SessionClosed += lobby.RemoveSession;
                    session.SessionClosed += gameplay.RemoveSession;
                    session.SessionClosed += chat.RemoveSession;
                    ServiceHost[] hosts =
                    {
                        new ServiceHost(CreateAccountAccessService(serviceLogger)),
                        new ServiceHost(session),
                        new ServiceHost(CreatePlayerProfileService(
                            loggerFactory.CreateLogger<PlayerProfileService>(), sessions)),
                        new ServiceHost(new LeaderboardService(new GetLeaderboardHandler(
                            new EntityFrameworkLeaderboardStore()),
                            loggerFactory.CreateLogger<LeaderboardService>())),
                        new ServiceHost(friendship),
                        new ServiceHost(lobby),
                        new ServiceHost(gameplay),
                        new ServiceHost(chat)
                    };
                    return Run(hosts, hostLogger);
                }
            }
            catch (Exception exception)
            {
                hostLogger.LogCritical(exception, "The server could not start.");
                return 1;
            }
        }
    }

    private static AccountAccessService CreateAccountAccessService(
        ILogger<AccountAccessService> logger)
    {
        SystemUtcClock clock = new SystemUtcClock();
        Pbkdf2PasswordHasher passwordHasher = new Pbkdf2PasswordHasher();
        string imageDirectory = GetProfileImageDirectory();

        CreateAccountHandler createAccountHandler = new CreateAccountHandler(
            new EntityFrameworkAccountRegistrationStore(),
            passwordHasher,
            new FileSystemProfileImageStore(imageDirectory),
            clock);
        return new AccountAccessService(createAccountHandler, logger);
    }

    private static SessionService CreateSessionService(ILogger<SessionService> logger,
        SessionRegistry sessionRegistry, IUtcClock clock)
    {
        byte[] secret = Convert.FromBase64String(
            EnvironmentSettings.ReadRequired("REVERSI_VERIFICATION_SECRET"));
        VerificationCodeProtector protector;
        try
        {
            protector = new VerificationCodeProtector(secret);
        }
        finally
        {
            Array.Clear(secret, 0, secret.Length);
        }

        VerificationCodeIssuer issuer = new VerificationCodeIssuer(
            new VerificationEmailSender(MailSettings.Load()), protector, clock);
        VerificationCodeService verification = new VerificationCodeService(
            issuer, new VerificationCodeRepository(), clock);
        SessionHandler handler = new SessionHandler(new VerifiedLoginService(
            new EntityFrameworkAccountLoginStore(), new Pbkdf2PasswordHasher()),
            verification, sessionRegistry);
        return new SessionService(handler, logger);
    }

    private static PlayerProfileService CreatePlayerProfileService(
        ILogger<PlayerProfileService> logger, IAuthenticatedSessionStore sessions)
    {
        ProfileImageRepository images = new ProfileImageRepository(GetProfileImageDirectory());
        PlayerProfileReader reader = new PlayerProfileReader(sessions,
            new EntityFrameworkPlayerProfileStore(), images);
        PlayerProfileEditor editor = new PlayerProfileEditor(sessions,
            new ProfileRepository(), images);
        return new PlayerProfileService(reader, editor, logger);
    }

    private static string GetProfileImageDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReversiGameSystem.Server", "profile-images");
    }

    private static int Run(ServiceHost[] hosts, ILogger logger)
    {
        try
        {
            foreach (ServiceHost host in hosts)
            {
                host.Open();
            }
            logger.LogInformation(
                "The Reversi server is listening. Press Enter to stop it.");
            Console.ReadLine();
            for (int index = hosts.Length - 1; index >= 0; index--)
            {
                hosts[index].Close();
            }
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "The Reversi server stopped unexpectedly.");
            return 1;
        }
        finally
        {
            foreach (ServiceHost host in hosts)
            {
                host.Abort();
            }
        }
    }
}
