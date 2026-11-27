using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class ClientRuntime
{
    private static MatchLobbyClient? s_lobby;

    private static GameplayClient? s_gameplay;

    private static MatchChatClient? s_chat;

    private static string? s_matchToken;

    private static int s_sessionChannelWarmedUp;

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

    private static PlayerPreferencesClient? s_preferences;

    static ClientRuntime()
    {
        CurrentSession.SessionChanged += OnSessionChanged;
    }

    internal static PlayerPreferencesClient PreferencesService
    {
        get
        {
            if (s_preferences == null)
            {
                s_preferences = CreatePreferencesService();
            }
            return s_preferences;
        }
    }

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

    internal static void WarmUpSessionChannel()
    {
        if (Interlocked.Exchange(ref s_sessionChannelWarmedUp, 1) != 0)
        {
            return;
        }

        _ = Task.Run(() =>
        {
            Proxy.SessionServiceClient? client = null;
            try
            {
                client = ServerConnectionFactory.CreateSessionClient();
                _ = client.ChannelFactory;
            }
            catch (Exception exception)
            {
                Logging.Value.CreateLogger(typeof(ClientRuntime).FullName ?? nameof(ClientRuntime))
                    .LogDebug(exception, "Local session channel warm-up did not complete.");
            }
            finally
            {
                client?.Abort();
            }
        });
    }

    internal static ClientSession CurrentSession { get; } = new ClientSession();

    internal static ClientErrorHandler Errors { get; } = new ClientErrorHandler(
        Logging.Value.CreateLogger<ClientErrorHandler>(), ShowErrorMessage);

    internal static MatchLobbyClient GetMatchLobbyClient(bool guest)
    {
        string token = ValidateMatchSession(guest);
        if (s_lobby == null)
        {
            s_lobby = new MatchLobbyClient(token, Logging.Value.CreateLogger<MatchLobbyClient>());
        }
        return s_lobby;
    }

    internal static GameplayClient GetGameplayClient(bool guest)
    {
        string token = ValidateMatchSession(guest);
        if (s_gameplay == null)
        {
            s_gameplay = new GameplayClient(token, Logging.Value.CreateLogger<GameplayClient>());
        }
        return s_gameplay;
    }

    internal static MatchChatClient GetMatchChatClient(bool guest)
    {
        string token = ValidateMatchSession(guest);
        if (s_chat == null)
        {
            s_chat = new MatchChatClient(token, Logging.Value.CreateLogger<MatchChatClient>());
        }
        return s_chat;
    }

    private static string ValidateMatchSession(bool guest)
    {
        string token = CurrentSession.SessionToken
            ?? throw new InvalidOperationException("A player session is required.");
        if (string.IsNullOrWhiteSpace(token) || CurrentSession.IsGuest != guest)
        {
            throw new InvalidOperationException("The requested player session is invalid.");
        }
        if (s_matchToken != token)
        {
            s_lobby?.Dispose();
            s_gameplay?.Dispose();
            s_chat?.Dispose();
            s_lobby = null;
            s_gameplay = null;
            s_chat = null;
            s_matchToken = token;
        }
        return token;
    }

    private static void OnSessionChanged()
    {
        if (Friendship.IsValueCreated)
        {
            Friendship.Value.Close();
        }
        s_preferences?.DiscardSession();
        s_preferences = null;
    }

    private static void ShowErrorMessage(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static PlayerPreferencesClient CreatePreferencesService()
    {
        return new PlayerPreferencesClient(
            Logging.Value.CreateLogger<PlayerPreferencesClient>(),
            ServerConnectionFactory.CreatePreferencesClient);
    }

    internal static void Dispose()
    {
        OnSessionChanged();
        s_lobby?.Dispose();
        s_gameplay?.Dispose();
        s_chat?.Dispose();
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
        string path = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "ReversiGameSystem", "logs",
            "client-" + Guid.NewGuid().ToString("N") + ".log");
        return LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information)
            .AddProvider(new ClientFileLoggerProvider(path)));
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
