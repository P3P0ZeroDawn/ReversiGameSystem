using System;
using System.Data.SqlClient;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Runs real match boundaries exclusively against a disposable review database.</summary>
internal static class ReviewMatchHost
{
    internal static void Run() => ReviewAccountDatabaseTests.RunWithReviewAccounts(Host);

    private static void Host(
        SessionApplicationService applicationService,
        ISessionRepository store)
    {
        PrepareSchema();
        SystemUtcClock clock = new SystemUtcClock();
        MatchApplicationService matches = new MatchApplicationService(store, new MatchRepository(), clock);
        ChatApplicationService messages = new ChatApplicationService(store, new ChatRepository(), clock);
        SessionService session = new SessionService(
            applicationService,
            NullLogger<SessionService>.Instance);
        using MatchLobbyService lobby = new MatchLobbyService(matches,
            NullLogger<MatchLobbyService>.Instance);
        using GameplayMatchService gameplay = new GameplayMatchService(matches, store,
            NullLogger<GameplayMatchService>.Instance);
        using MatchChatService chat = new MatchChatService(messages, store,
            NullLogger<MatchChatService>.Instance);
        session.SessionClosed += lobby.RemoveSession;
        session.SessionClosed += gameplay.RemoveSession;
        session.SessionClosed += chat.RemoveSession;
        ServiceHost[] hosts =
        {
            CreateHost<ISessionService>(session, "18091/ReversiReview/Session"),
            CreateHost<IMatchLobbyService>(lobby, "18095/ReversiReview/MatchLobby"),
            CreateHost<IGameplayMatchService>(gameplay, "18096/ReversiReview/GameplayMatch"),
            CreateHost<IMatchChatService>(chat, "18097/ReversiReview/MatchChat")
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
            session.SessionClosed -= lobby.RemoveSession;
            session.SessionClosed -= gameplay.RemoveSession;
            session.SessionClosed -= chat.RemoveSession;
            foreach (ServiceHost host in hosts)
            {
                host.Abort();
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
    }
}
