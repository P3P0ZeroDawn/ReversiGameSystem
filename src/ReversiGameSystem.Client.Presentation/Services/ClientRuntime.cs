using System;
using Microsoft.Extensions.Logging;

namespace ReversiGameSystem.Services;

internal static class ClientRuntime
{
    private static readonly Lazy<ILoggerFactory> Logging =
        new Lazy<ILoggerFactory>(CreateLoggerFactory);
    private static readonly Lazy<SessionClient> Session =
        new Lazy<SessionClient>(CreateSessionService);

    internal static AccountClient AccountService { get; } =
        new AccountClient(Logging.Value.CreateLogger<AccountClient>());
    internal static PlayerProfileClient ProfileService { get; } =
        new PlayerProfileClient(Logging.Value.CreateLogger<PlayerProfileClient>());
    internal static LeaderboardClient LeaderboardService { get; } =
        new LeaderboardClient(Logging.Value.CreateLogger<LeaderboardClient>());

    internal static SessionClient SessionService =>
        Session.Value;

    internal static void Dispose()
    {
        if (Logging.IsValueCreated)
        {
            Logging.Value.Dispose();
        }
    }

    private static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(
            builder => builder
                .SetMinimumLevel(LogLevel.Information)
                .AddDebug());
    }

    private static SessionClient CreateSessionService()
    {
        ILogger<SessionClient> logger =
            Logging.Value.CreateLogger<SessionClient>();
        return new SessionClient(logger);
    }
}
