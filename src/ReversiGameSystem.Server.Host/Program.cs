using System;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Configuration;
using ReversiGameSystem.Server.Persistence.Friendships;
using ReversiGameSystem.Server.Persistence.Leaderboards;
using ReversiGameSystem.Server.Persistence.Mail;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Players;
using ReversiGameSystem.Server.Persistence.Sessions;
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
                SessionRepository sessions = new SessionRepository(clock);
                using (FriendshipService friendship = new FriendshipService(sessions,
                    new FriendRepository(), loggerFactory.CreateLogger<FriendshipService>()))
                {
                    MatchInvitationEmailSender matchInvitationEmailSender =
                        new MatchInvitationEmailSender(MailSettings.Load());
                    MatchRepository matchRepository = new MatchRepository();
                    matchRepository.CancelAbandonedWaitingMatches();
                    MatchApplicationService matchApplicationService =
                        new MatchApplicationService(
                        sessions,
                        matchRepository,
                        clock,
                        matchInvitationEmailSender);
                    using MatchLobbyService matchLobbyService = new MatchLobbyService(
                        matchApplicationService,
                        loggerFactory.CreateLogger<MatchLobbyService>());
                    using GameplayMatchService gameplayMatchService = new GameplayMatchService(
                        matchApplicationService,
                        sessions,
                        loggerFactory.CreateLogger<GameplayMatchService>());
                    ChatRepository chatRepository = new ChatRepository();
                    ChatApplicationService chatApplicationService =
                        new ChatApplicationService(sessions, chatRepository, clock);
                    using MatchChatService matchChatService = new MatchChatService(
                        chatApplicationService,
                        sessions,
                        loggerFactory.CreateLogger<MatchChatService>());
                    SessionService sessionService = CreateSessionService(
                        loggerFactory.CreateLogger<SessionService>(), sessions, clock);
                    sessionService.SessionClosed += friendship.RemoveSession;
                    sessionService.SessionClosed += matchLobbyService.RemoveSession;
                    sessionService.SessionClosed += gameplayMatchService.RemoveSession;
                    sessionService.SessionClosed += matchChatService.RemoveSession;
                    ServiceHost[] hosts =
                    {
                        CreateServiceHost(CreateAccountAccessService(serviceLogger), loggerFactory),
                        CreateServiceHost(sessionService, loggerFactory),
                        CreateServiceHost(
                            CreatePlayerProfileService(sessions),
                            loggerFactory),
                        CreateServiceHost(
                            CreateLeaderboardService(),
                            loggerFactory),
                        CreateServiceHost(friendship, loggerFactory),
                        CreateServiceHost(matchLobbyService, loggerFactory),
                        CreateServiceHost(gameplayMatchService, loggerFactory),
                        CreateServiceHost(matchChatService, loggerFactory)
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
        ISessionRepository sessions, IUtcClock clock)
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
        SessionApplicationService applicationService = new SessionApplicationService(
            new AccountLoginRepository(),
            new Pbkdf2PasswordHasher(),
            verification,
            sessions);
        return new SessionService(applicationService, logger);
    }

    private static PlayerProfileService CreatePlayerProfileService(
        ISessionRepository sessions)
    {
        PlayerProfileRepository profiles = new PlayerProfileRepository();
        ProfileImageRepository images = new ProfileImageRepository(GetProfileImageDirectory());
        PlayerProfileApplicationService applicationService =
            new PlayerProfileApplicationService(
            sessions,
            profiles,
            images);
        return new PlayerProfileService(applicationService);
    }

    private static LeaderboardService CreateLeaderboardService()
    {
        LeaderboardRepository leaderboard = new LeaderboardRepository();
        LeaderboardApplicationService applicationService =
            new LeaderboardApplicationService(leaderboard);
        return new LeaderboardService(applicationService);
    }

    private static string GetProfileImageDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReversiGameSystem.Server", "profile-images");
    }

    private static ServiceHost CreateServiceHost(
        object service,
        ILoggerFactory loggerFactory)
    {
        ServiceHost host = new ServiceHost(service);
        host.Description.Behaviors.Add(new ServiceErrorBehavior(
            loggerFactory.CreateLogger<ServiceErrorBehavior>()));
        return host;
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
