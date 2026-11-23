using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Configuration;
using ReversiGameSystem.Server.Persistence.Friendships;
using ReversiGameSystem.Server.Persistence.Leaderboards;
using ReversiGameSystem.Server.Persistence.Mail;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Players;
using ReversiGameSystem.Server.Persistence.Preferences;
using ReversiGameSystem.Server.Persistence.Verification;
using ReversiGameSystem.Server.ServiceApi;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ReversiGameSystem.Server.Host;

internal static class Program
{
    private const string ServerDataDirectoryName = "ReversiGameSystem.Server";
    private const string BoardBackgroundDirectoryName = "board-backgrounds";
    private const string BoardBackgroundReferencePrefix = "board-backgrounds/";
    private const long MaximumLogFileSizeBytes = 10_485_760;
    private const int RetainedLogFileCount = 7;
    private const int MaximumFriendshipSweepChannels = 64;
    private const string PersistentLogOutputTemplate =
        "{UtcTimestamp} [{Level:u3}] "
        + "{SourceContext} {Message:lj}{NewLine}{Exception}";

    private static int Main()
    {
        string logDirectory = GetLogDirectory();
        Directory.CreateDirectory(logDirectory);
        Logger fileLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.With(new UtcTimestampEnricher())
            .WriteTo.File(
                Path.Combine(logDirectory, "server-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: MaximumLogFileSizeBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: RetainedLogFileCount,
                outputTemplate: PersistentLogOutputTemplate)
            .CreateLogger();

        using (fileLogger)
        using (ILoggerFactory loggerFactory = LoggerFactory.Create(
            builder => builder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole(options => options.FormatterName = SafeConsoleFormatter.FormatterName)
                .AddConsoleFormatter<SafeConsoleFormatter, ConsoleFormatterOptions>()
                .AddSerilog(fileLogger, dispose: false)))
        {
            ILogger<ServerHostLogCategory> hostLogger =
                loggerFactory.CreateLogger<ServerHostLogCategory>();
            using (ProcessErrorHooks processErrorHooks = new ProcessErrorHooks(
                loggerFactory.CreateLogger<ProcessErrorHooks>()))
            {
                hostLogger.LogInformation(
                    "Persistent server logs are written to {LogDirectory}.",
                    logDirectory);
                try
                {
                    SystemUtcClock clock = new SystemUtcClock();
                    SessionStore sessions = new SessionStore(clock);
                    SmtpEmailSender emailSender = new SmtpEmailSender(MailSettings.Load());
                    AccountRepository accountRepository = new AccountRepository(clock);
                    VerificationCodeService verificationCodes = CreateVerificationCodeService(
                        emailSender, clock);
                    AccountPresenceRegistry presenceRegistry = new AccountPresenceRegistry();
                    using (FriendshipSubscriptions friendshipSubscriptions =
                        new FriendshipSubscriptions(
                            loggerFactory.CreateLogger<FriendshipSubscriptions>()))
                    {
                        FriendshipApplicationService friendshipApplicationService =
                            new FriendshipApplicationService(sessions, new FriendRepository());
                        presenceRegistry.PresenceChanged += (accountId, isOnline) =>
                            PublishPresenceChange(
                                accountId,
                                isOnline,
                                friendshipApplicationService,
                                friendshipSubscriptions,
                                hostLogger);
                        FriendshipService friendship = new FriendshipService(
                            friendshipApplicationService,
                            presenceRegistry,
                            friendshipSubscriptions);
                        MatchRepository matchRepository = new MatchRepository();
                        matchRepository.CancelAbandonedWaitingMatches();
                        MatchApplicationService matchApplicationService =
                            new MatchApplicationService(
                                sessions,
                                matchRepository,
                                clock,
                                emailSender);
                        GameplayMatchApplicationService gameplayMatchApplicationService =
                            new GameplayMatchApplicationService(
                                sessions,
                                matchRepository,
                                clock);
                        matchApplicationService.MatchStarted +=
                            gameplayMatchApplicationService.TrackStartedMatch;
                        MatchCallbackSubscriptions<IMatchLobbyServiceCallback>? lobbySubscriptions =
                            null;
                        lobbySubscriptions =
                            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                                (token, _) => matchApplicationService.EvaluateLobbyAccess(token)
                                    == MatchStatus.Success,
                                (token, _) =>
                                {
                                    if (lobbySubscriptions != null)
                                    {
                                        CancelWaitingLobbiesForSession(
                                            token,
                                            matchApplicationService,
                                            lobbySubscriptions);
                                    }
                                },
                                loggerFactory.CreateLogger<
                                    MatchCallbackSubscriptions<IMatchLobbyServiceCallback>>());
                        using (lobbySubscriptions)
                        {
                            MatchLobbyService matchLobbyService = new MatchLobbyService(
                                matchApplicationService,
                                lobbySubscriptions);
                            GameplayMatchPresence gameplayPresence = new GameplayMatchPresence();
                            MatchCallbackSubscriptions<IGameplayMatchServiceCallback>?
                                gameplaySubscriptions = null;
                            gameplaySubscriptions =
                                new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
                                    (token, matchId) => IsGameplayChannelAuthorized(
                                        token,
                                        matchId,
                                        gameplayMatchApplicationService),
                                    (sessionToken, matchId) =>
                                    {
                                        if (gameplaySubscriptions != null)
                                        {
                                            HandleGameplaySubscriptionRemoved(
                                                sessionToken,
                                                matchId,
                                                gameplayPresence,
                                                gameplaySubscriptions);
                                        }
                                    },
                                    loggerFactory.CreateLogger<
                                        MatchCallbackSubscriptions<
                                            IGameplayMatchServiceCallback>>());
                            using (gameplaySubscriptions)
                            {
                                GameplayMatchService gameplayMatchService =
                                    new GameplayMatchService(
                                        gameplayMatchApplicationService,
                                        gameplayPresence,
                                        gameplaySubscriptions);
                                ChatRepository chatRepository = new ChatRepository(clock);
                                ChatApplicationService chatApplicationService =
                                    new ChatApplicationService(sessions, chatRepository, clock);
                                using MatchCallbackSubscriptions<IMatchChatServiceCallback>
                                    chatSubscriptions =
                                    new MatchCallbackSubscriptions<IMatchChatServiceCallback>(
                                        (sessionToken, matchId) =>
                                            chatApplicationService.EvaluateParticipantAccess(
                                                sessionToken,
                                                matchId) == ChatStatus.Success,
                                        loggerFactory.CreateLogger<
                                            MatchCallbackSubscriptions<
                                                IMatchChatServiceCallback>>());
                                MatchChatService matchChatService = new MatchChatService(
                                    chatApplicationService,
                                    chatSubscriptions);
                                SessionService sessionService = new SessionService(
                                    new SessionApplicationService(
                                        accountRepository,
                                        verificationCodes,
                                        sessions),
                                    loggerFactory.CreateLogger<SessionService>());
                                AccountService accountService = CreateAccountService(
                                    accountRepository,
                                    verificationCodes,
                                    sessions);
                                sessions.SessionRevoked += friendshipSubscriptions.Remove;
                                sessions.SessionRevoked += sessionToken =>
                                {
                                    lobbySubscriptions.RemoveSession(sessionToken);
                                    CancelWaitingLobbiesForSession(
                                        sessionToken,
                                        matchApplicationService,
                                        lobbySubscriptions);
                                };
                                sessions.SessionRevoked += gameplaySubscriptions.RemoveSession;
                                sessions.SessionRevoked += chatSubscriptions.RemoveSession;
                                ServiceHost[] hosts =
                                {
                                    CreateServiceHost(accountService, loggerFactory),
                                    CreateServiceHost(sessionService, loggerFactory),
                                    CreateServiceHost(
                                        CreatePlayerProfileService(sessions),
                                        loggerFactory),
                                    CreateServiceHost(
                                        CreateLeaderboardService(),
                                        loggerFactory),
                                    CreateServiceHost(CreatePlayerPreferencesService(sessions),
                                        loggerFactory),
                                    CreateServiceHost(friendship, loggerFactory),
                                    CreateServiceHost(matchLobbyService, loggerFactory),
                                    CreateServiceHost(gameplayMatchService, loggerFactory),
                                    CreateServiceHost(matchChatService, loggerFactory)
                                };
                                PeriodicTaskRunner[] runners =
                                {
                                    new PeriodicTaskRunner(
                                        nameof(GameplayMatchApplicationService
                                            .SynchronizePendingMatches),
                                        TimeSpan.FromSeconds(1),
                                        gameplayMatchApplicationService.SynchronizePendingMatches,
                                        loggerFactory.CreateLogger<PeriodicTaskRunner>()),
                                    new PeriodicTaskRunner(
                                        nameof(GameplayMatchApplicationService.ProcessTurnTimeouts),
                                        TimeSpan.FromSeconds(1),
                                        () => ProcessGameplayTimeouts(
                                            gameplayMatchApplicationService,
                                            gameplayPresence,
                                            gameplaySubscriptions),
                                        loggerFactory.CreateLogger<PeriodicTaskRunner>()),
                                    new PeriodicTaskRunner(
                                        nameof(FriendshipSubscriptions.SweepChannels),
                                        TimeSpan.FromSeconds(5),
                                        () => friendshipSubscriptions.SweepChannels(
                                            MaximumFriendshipSweepChannels,
                                            token => friendshipApplicationService
                                                .EvaluateChannelAccess(token).Status
                                                == FriendshipStatus.Success),
                                        loggerFactory.CreateLogger<PeriodicTaskRunner>()),
                                    new PeriodicTaskRunner(
                                        nameof(MatchApplicationService
                                            .CancelExpiredWaitingSessions),
                                        TimeSpan.FromSeconds(5),
                                        () => SweepExpiredLobbies(
                                            matchApplicationService,
                                            lobbySubscriptions),
                                        loggerFactory.CreateLogger<PeriodicTaskRunner>()),
                                    new PeriodicTaskRunner(
                                        nameof(SessionStore.RemoveExpiredSessions),
                                        TimeSpan.FromSeconds(30),
                                        sessions.RemoveExpiredSessions,
                                        loggerFactory.CreateLogger<PeriodicTaskRunner>())
                                };
                                return Run(hosts, runners, hostLogger);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    hostLogger.LogCritical(exception, "The server could not start.");
                    return 1;
                }
            }
        }
    }

    private static AccountService CreateAccountService(
        AccountRepository accounts,
        VerificationCodeService verificationCodes,
        SessionStore sessions)
    {
        ProfileImageRepository images = new ProfileImageRepository(GetProfileImageDirectory());
        AccountApplicationService applicationService = new AccountApplicationService(
            accounts,
            images,
            verificationCodes,
            sessions);
        return new AccountService(applicationService);
    }

    private static VerificationCodeService CreateVerificationCodeService(
        IEmailSender emailSender,
        IUtcClock clock)
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

        VerificationCodeIssuer issuer = new VerificationCodeIssuer(emailSender, protector, clock);
        VerificationCodeService verification = new VerificationCodeService(
            issuer, new VerificationCodeRepository(), clock);
        return verification;
    }

    private static PlayerProfileService CreatePlayerProfileService(
        SessionStore sessions)
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

    private static PlayerPreferencesService CreatePlayerPreferencesService(
        SessionStore sessions)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ServerDataDirectoryName, BoardBackgroundDirectoryName);
        PreferencesRepository preferences = new PreferencesRepository();
        ProfileImageRepository backgrounds = new ProfileImageRepository(
            directory, BoardBackgroundReferencePrefix);
        PreferencesApplicationService applicationService = new PreferencesApplicationService(
            sessions, preferences, backgrounds);
        return new PlayerPreferencesService(applicationService);
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

    private static string GetLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReversiGameSystem.Server",
            "logs");
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

    private static void PublishPresenceChange(
        long accountId,
        bool isOnline,
        FriendshipApplicationService applicationService,
        FriendshipSubscriptions subscriptions,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            long[] friendIds = applicationService.ReadConfirmedFriendIds(accountId);
            if (friendIds.Length == 0)
            {
                return;
            }

            HashSet<long> targets = new HashSet<long>(friendIds);
            FriendChange change = isOnline ? FriendChange.Connected : FriendChange.Disconnected;
            subscriptions.PublishToAccounts(
                targets,
                callback => callback.OnFriendChanged(accountId, change));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to publish presence change for account {AccountId}.",
                accountId);
        }
    }

    private static void SweepExpiredLobbies(
        MatchApplicationService applicationService,
        MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions)
    {
        foreach (long matchId in applicationService.CancelExpiredWaitingSessions())
        {
            PublishLobbyClosed(matchId, applicationService, subscriptions);
        }
    }

    private static void CancelWaitingLobbiesForSession(
        string sessionToken,
        MatchApplicationService applicationService,
        MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions)
    {
        foreach (long matchId in applicationService.CancelWaitingForSession(sessionToken))
        {
            PublishLobbyClosed(matchId, applicationService, subscriptions);
        }
    }

    private static void PublishLobbyClosed(
        long matchId,
        MatchApplicationService applicationService,
        MatchCallbackSubscriptions<IMatchLobbyServiceCallback> subscriptions)
    {
        subscriptions.Publish(matchId, (token, callback) =>
        {
            MatchResult<Match> result = applicationService.GetParticipantMatch(token, matchId);
            if (result.Status == MatchStatus.Success)
            {
                callback.OnLobbyClosed(matchId);
            }
        });
    }

    private static bool IsGameplayChannelAuthorized(
        string token,
        long matchId,
        GameplayMatchApplicationService applicationService)
    {
        MatchResult<Match> result = applicationService.GetMatchState(token, matchId);
        if (result.Status != MatchStatus.Success || result.Value == null)
        {
            return false;
        }

        return string.Equals(result.Value.State, "InProgress", StringComparison.Ordinal)
            || string.Equals(result.Value.State, "Finished", StringComparison.Ordinal);
    }

    private static void HandleGameplaySubscriptionRemoved(
        string sessionToken,
        long matchId,
        GameplayMatchPresence presence,
        MatchCallbackSubscriptions<IGameplayMatchServiceCallback> subscriptions)
    {
        KeyValuePair<int, bool>[]? snapshot = presence.RemoveConnection(
            sessionToken,
            matchId,
            subscriptions.HasSubscription);
        if (snapshot == null)
        {
            return;
        }

        subscriptions.Publish(matchId, (token, callback) =>
        {
            for (int index = 0; index < snapshot.Length; index++)
            {
                callback.OnParticipantConnectionChanged(
                    matchId,
                    snapshot[index].Key,
                    snapshot[index].Value);
            }
        });
    }

    private static void ProcessGameplayTimeouts(
        GameplayMatchApplicationService applicationService,
        GameplayMatchPresence presence,
        MatchCallbackSubscriptions<IGameplayMatchServiceCallback> subscriptions)
    {
        foreach ((long MatchId, int SeatNumber) key in presence.CollectExpiredReconnections())
        {
            MatchResult<Match> result = applicationService.ForfeitDisconnectedParticipant(
                key.MatchId,
                key.SeatNumber);
            if (result.Status == MatchStatus.Success && result.Value != null)
            {
                MatchStateSnapshot snapshot = MatchContractMapper.MapState(result.Value);
                subscriptions.Publish(
                    result.Value.Id,
                    (token, callback) => callback.OnMatchStateChanged(snapshot));
            }
        }

        Match[] transitionedMatches = applicationService.ProcessTurnTimeouts();
        for (int index = 0; index < transitionedMatches.Length; index++)
        {
            Match match = transitionedMatches[index];
            MatchStateSnapshot snapshot = MatchContractMapper.MapState(match);
            subscriptions.Publish(
                match.Id,
                (token, callback) => callback.OnMatchStateChanged(snapshot));
        }
    }

    private static int Run(
        ServiceHost[] hosts,
        PeriodicTaskRunner[] runners,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            foreach (ServiceHost host in hosts)
            {
                host.Open();
            }

            foreach (PeriodicTaskRunner runner in runners)
            {
                runner.Start();
            }

            logger.LogInformation(
                "The Reversi server is listening. Press Enter to stop it.");
            Console.ReadLine();

            foreach (PeriodicTaskRunner runner in runners)
            {
                runner.Dispose();
            }

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
            foreach (PeriodicTaskRunner runner in runners)
            {
                runner.Dispose();
            }

            foreach (ServiceHost host in hosts)
            {
                host.Abort();
            }
        }
    }

    private sealed class UtcTimestampEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            string utcTimestamp = logEvent.Timestamp.UtcDateTime.ToString(
                "yyyy-MM-dd HH:mm:ss.fff 'Z'",
                CultureInfo.InvariantCulture);
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("UtcTimestamp", utcTimestamp));
        }
    }
}
