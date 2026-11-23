using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Players;
using ReversiGameSystem.Server.Persistence.Preferences;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal static class ReviewMatchHost
{
    private const string PreferencesMigration = "20261121_CustomBoardBackgroundPreference.sql";
    private const string PreferencesAddress = "18098/ReversiReview/PlayerPreferences";

    internal static void Run()
    {
        ReviewAccountDatabaseTests.RunWithReviewAccounts(Host);
    }

    private static void Host(
        SessionApplicationService applicationService,
        SessionStore store)
    {
        PrepareSchema();
        TestUtcClock clock = new TestUtcClock();
        MatchRepository matchRepository = new MatchRepository();
        MatchApplicationService matches = new MatchApplicationService(store, matchRepository, clock);
        GameplayMatchApplicationService gameplayMatches =
            new GameplayMatchApplicationService(store, matchRepository, clock);
        matches.MatchStarted += gameplayMatches.TrackStartedMatch;
        ChatApplicationService messages = new ChatApplicationService(
            store, new ChatRepository(clock), clock);
        SessionService session = new SessionService(
            applicationService,
            NullLogger<SessionService>.Instance);
        using MatchCallbackSubscriptions<IMatchLobbyServiceCallback> lobbySubscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        GameplayMatchPresence gameplayPresence = new GameplayMatchPresence();
        MatchCallbackSubscriptions<IGameplayMatchServiceCallback>? presenceSubscriptions = null;
        void RemoveParticipantConnection(string token, long matchId)
        {
            if (presenceSubscriptions == null)
            {
                return;
            }
            KeyValuePair<int, bool>[]? snapshot = gameplayPresence.RemoveConnection(
                token, matchId, presenceSubscriptions.HasSubscription);
            if (snapshot == null)
            {
                return;
            }
            void PublishConnection(string subscriber, IGameplayMatchServiceCallback callback)
            {
                foreach (KeyValuePair<int, bool> participant in snapshot)
                {
                    callback.OnParticipantConnectionChanged(
                        matchId, participant.Key, participant.Value);
                }
            }
            presenceSubscriptions.Publish(matchId, PublishConnection);
        }
        using MatchCallbackSubscriptions<IGameplayMatchServiceCallback> gameplaySubscriptions =
            new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
                (_, _) => true,
                RemoveParticipantConnection,
                NullLogger.Instance);
        presenceSubscriptions = gameplaySubscriptions;
        using MatchCallbackSubscriptions<IMatchChatServiceCallback> chatSubscriptions =
            new MatchCallbackSubscriptions<IMatchChatServiceCallback>(
                (_, _) => true,
                NullLogger.Instance);
        MatchLobbyService lobby = new MatchLobbyService(matches, lobbySubscriptions);
        GameplayMatchService gameplay = new GameplayMatchService(
            gameplayMatches,
            gameplayPresence,
            gameplaySubscriptions);
        MatchChatService chat = new MatchChatService(messages, chatSubscriptions);
        session.SessionClosed += lobbySubscriptions.RemoveSession;
        session.SessionClosed += gameplaySubscriptions.RemoveSession;
        session.SessionClosed += chatSubscriptions.RemoveSession;
        string backgroundDirectory = Path.Combine(Path.GetTempPath(),
            "ReversiReviewPreferences-" + Guid.NewGuid().ToString("N"));
        ProfileImageRepository backgrounds = new ProfileImageRepository(
            backgroundDirectory, "board-backgrounds/");
        PreferencesApplicationService preferencesApplication = new PreferencesApplicationService(
            store, new PreferencesRepository(), backgrounds);
        PlayerPreferencesService preferences = new PlayerPreferencesService(preferencesApplication);
        ServiceHost preferencesHost = CreateHost<IPlayerPreferencesService>(
            preferences, PreferencesAddress);
        preferencesHost.Description.Behaviors.Add(
            new ServiceErrorBehavior(NullLogger<ServiceErrorBehavior>.Instance));
        ServiceHost[] hosts =
        {
            CreateHost<ISessionService>(session, "18091/ReversiReview/Session"),
            CreateHost<IMatchLobbyService>(lobby, "18095/ReversiReview/MatchLobby"),
            CreateHost<IGameplayMatchService>(gameplay, "18096/ReversiReview/GameplayMatch"),
            CreateHost<IMatchChatService>(chat, "18097/ReversiReview/MatchChat"),
            preferencesHost
        };
        try
        {
            foreach (ServiceHost host in hosts)
            {
                host.Open();
            }
            Console.WriteLine("Review match host ready.");
            Console.ReadLine();
            foreach (ServiceHost host in hosts)
            {
                host.Close();
            }
        }
        finally
        {
            session.SessionClosed -= lobbySubscriptions.RemoveSession;
            session.SessionClosed -= gameplaySubscriptions.RemoveSession;
            session.SessionClosed -= chatSubscriptions.RemoveSession;
            foreach (ServiceHost host in hosts)
            {
                host.Abort();
            }
            if (Directory.Exists(backgroundDirectory))
            {
                Directory.Delete(backgroundDirectory, recursive: true);
            }
        }
    }

    private static ServiceHost CreateHost<T>(object service, string address)
    {
        NetTcpBinding binding = new NetTcpBinding(SecurityMode.None)
        {
            OpenTimeout = TimeSpan.FromSeconds(10),
            CloseTimeout = TimeSpan.FromSeconds(5),
            SendTimeout = TimeSpan.FromSeconds(5),
            ReceiveTimeout = TimeSpan.FromSeconds(45)
        };
        ServiceHost host = new ServiceHost(service);
        host.AddServiceEndpoint(typeof(T), binding, "net.tcp://localhost:" + address);
        return host;
    }

    internal static void PrepareSchema()
    {
        const string prefix = "ReversiReviewAccountTests_";
        SqlConnectionStringBuilder settings = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("REVERSI_DB_CONNECTION_STRING"));
        if (!settings.InitialCatalog.StartsWith(prefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(settings.InitialCatalog.Substring(prefix.Length), "N", out _))
        {
            throw new InvalidOperationException("A disposable review database is required.");
        }
        using SqlConnection connection = new SqlConnection(settings.ConnectionString);
        connection.Open();
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Fixtures", "ReviewMatchSchema.sql"));
        command.ExecuteNonQuery();
        command.CommandText = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "migrations", PreferencesMigration));
        command.ExecuteNonQuery();
    }
}
