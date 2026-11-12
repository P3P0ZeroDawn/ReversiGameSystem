using System;
using Microsoft.Extensions.Logging;

namespace ReversiGameSystem.Services;

internal static class ClientRuntime
{
    internal static ClientSession CurrentSession { get; } = new ClientSession();

    private static RemoteMatchClient? _match;
    private static string? _matchToken;

    internal static RemoteMatchClient GetMatchClient(bool guest)
    {
        string token = CurrentSession.SessionToken
            ?? throw new InvalidOperationException("A player session is required.");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("A valid player session is required.");
        }
        if (CurrentSession.IsGuest != guest)
        {
            throw new InvalidOperationException("The requested player type does not match the session.");
        }
        if (_match == null || _matchToken != token)
        {
            _match?.Dispose();
            _match = new RemoteMatchClient(token, Logging.Value.CreateLogger<RemoteMatchClient>());
            _matchToken = token;
        }
        return _match;
    }

    private static readonly Lazy<ILoggerFactory> Logging =
        new Lazy<ILoggerFactory>(CreateLoggerFactory);
    private static readonly Lazy<SessionClient> Session =
        new Lazy<SessionClient>(CreateSessionService);
    private static readonly Lazy<FriendshipClient> Friendship =
        new Lazy<FriendshipClient>(CreateFriendshipService);
    private static readonly Lazy<AccountClient> Account =
        new Lazy<AccountClient>(CreateAccountService);
    private static readonly Lazy<PlayerProfileClient> Profile =
        new Lazy<PlayerProfileClient>(CreateProfileService);
    private static readonly Lazy<LeaderboardClient> Leaderboard =
        new Lazy<LeaderboardClient>(CreateLeaderboardService);

    internal static FriendshipClient FriendshipService
    {
        get { return Friendship.Value; }
    }

    internal static AccountClient AccountService
    {
        get { return Account.Value; }
    }

    internal static PlayerProfileClient ProfileService
    {
        get { return Profile.Value; }
    }

    internal static LeaderboardClient LeaderboardService
    {
        get { return Leaderboard.Value; }
    }

    internal static SessionClient SessionService
    {
        get { return Session.Value; }
    }

    internal static void Dispose()
    {
        _match?.Dispose();
        if (Friendship.IsValueCreated)
        {
            Friendship.Value.Dispose();
        }
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

    private static AccountClient CreateAccountService()
    {
        return new AccountClient(
            Logging.Value.CreateLogger<AccountClient>(),
            ServerConnectionFactory.CreateAccountClient);
    }

    private static SessionClient CreateSessionService()
    {
        return new SessionClient(
            Logging.Value.CreateLogger<SessionClient>(),
            ServerConnectionFactory.CreateSessionClient);
    }

    private static PlayerProfileClient CreateProfileService()
    {
        return new PlayerProfileClient(
            Logging.Value.CreateLogger<PlayerProfileClient>(),
            ServerConnectionFactory.CreateProfileClient);
    }

    private static LeaderboardClient CreateLeaderboardService()
    {
        return new LeaderboardClient(
            Logging.Value.CreateLogger<LeaderboardClient>(),
            ServerConnectionFactory.CreateLeaderboardClient);
    }

    private static FriendshipClient CreateFriendshipService()
    {
        return new FriendshipClient(
            Logging.Value.CreateLogger<FriendshipClient>(),
            context => ServerConnectionFactory.CreateFriendshipClient(context));
    }
}
