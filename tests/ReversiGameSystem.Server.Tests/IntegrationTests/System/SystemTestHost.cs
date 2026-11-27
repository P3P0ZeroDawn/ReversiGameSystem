using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Preferences;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Persistence.Accounts;
using ReversiGameSystem.Server.Persistence.Chat;
using ReversiGameSystem.Server.Persistence.Friendships;
using ReversiGameSystem.Server.Persistence.Leaderboards;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Players;
using ReversiGameSystem.Server.Persistence.Preferences;
using ReversiGameSystem.Server.Persistence.Verification;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.IntegrationTests.Repository;
using ReversiGameSystem.Server.Tests.IntegrationTests.Services;
using ReversiGameSystem.Server.Tests.TestDoubles;

using ContractSession = ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.SystemTests;

internal sealed class SystemTestHost : IDisposable
{
    private const string ProfileImagesFolderName = "profile-images";
    private const string BoardBackgroundsFolderName = "board-backgrounds";
    private const string BoardBackgroundReferencePrefix = "board-backgrounds/";
    private const string VerificationCodeKey = "VerificationCode";
    private const int HmacKeySize = 32;

    private readonly LocalDbTestDatabase _database;
    private readonly string _tempRootDirectory;
    private readonly string _profileImagesDirectory;
    private readonly string _boardBackgroundsDirectory;
    private readonly CapturingTemplatedEmailSender _emailSender;
    private readonly List<ServiceHost> _hosts = new List<ServiceHost>();
    private readonly List<IDisposable> _disposables = new List<IDisposable>();
    private readonly Dictionary<Type, Uri> _endpoints = new Dictionary<Type, Uri>();

    internal SystemTestHost()
    {
        _database = new LocalDbTestDatabase();
        _tempRootDirectory = Path.Combine(
            Path.GetTempPath(),
            "ReversiSystemTests_" + Guid.NewGuid().ToString("N"));
        _profileImagesDirectory = Path.Combine(_tempRootDirectory, ProfileImagesFolderName);
        _boardBackgroundsDirectory = Path.Combine(_tempRootDirectory, BoardBackgroundsFolderName);
        Directory.CreateDirectory(_profileImagesDirectory);
        Directory.CreateDirectory(_boardBackgroundsDirectory);
        _emailSender = new CapturingTemplatedEmailSender();
        InitializeServices();
    }

    internal void ResetState()
    {
        CloseHostsAndSubscriptions();
        _database.ResetMutableTables();
        _emailSender.Clear();
        RecreateDirectory(_profileImagesDirectory);
        RecreateDirectory(_boardBackgroundsDirectory);
        InitializeServices();
    }

    internal string GetLatestVerificationCode(string emailAddress)
    {
        return _emailSender.GetLatestCode(emailAddress);
    }

    internal ContractSession RegisterAndLogin(
        string username,
        string emailAddress,
        string password)
    {
        using ClientChannelScope<IAccountService> accountScope = OpenChannel<IAccountService>();
        using ClientChannelScope<ISessionService> sessionScope = OpenChannel<ISessionService>();
        accountScope.Channel.RequestRegistrationCode(username, emailAddress);
        string registrationCode = GetLatestVerificationCode(emailAddress);
        accountScope.Channel.CompleteRegistration(new NewAccount
        {
            Username = username,
            EmailAddress = emailAddress,
            Password = password,
            PreferredLocaleCode = "en-US",
            VerificationCode = registrationCode
        });

        sessionScope.Channel.RequestLoginCode(username, password);
        string loginCode = GetLatestVerificationCode(emailAddress);
        return sessionScope.Channel.CompleteLogin(username, loginCode);
    }

    internal ClientChannelScope<TContract> OpenChannel<TContract>()
        where TContract : class
    {
        Uri endpointAddress = _endpoints[typeof(TContract)];
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        ChannelFactory<TContract> factory = new ChannelFactory<TContract>(
            binding,
            new EndpointAddress(endpointAddress));
        TContract channel = factory.CreateChannel();
        return new ClientChannelScope<TContract>(factory, channel);
    }

    internal ClientChannelScope<TContract> OpenDuplexChannel<TContract, TCallback>(
        TCallback callback)
        where TContract : class
        where TCallback : class
    {
        Uri endpointAddress = _endpoints[typeof(TContract)];
        DuplexChannelFactory<TContract> factory =
            WcfTestHostHelper.CreateDuplexFactory<TContract, TCallback>(
                endpointAddress,
                callback);
        TContract channel = factory.CreateChannel();
        return new ClientChannelScope<TContract>(factory, channel);
    }

    public void Dispose()
    {
        CloseHostsAndSubscriptions();
        _database.Dispose();
        if (Directory.Exists(_tempRootDirectory))
        {
            Directory.Delete(_tempRootDirectory, true);
        }
    }

    private void InitializeServices()
    {
        UtcClockStub clock = new UtcClockStub(DateTime.UtcNow);
        SessionStore sessions = new SessionStore(clock, NullLogger<SessionStore>.Instance);
        AccountRepository accountRepository = new AccountRepository(clock);
        VerificationCodeService verificationCodes = new VerificationCodeService(
            new VerificationCodeProtector(CreateHmacKey()),
            new VerificationCodeRepository(),
            _emailSender,
            clock);
        PlayerImageRepository profileImages = new PlayerImageRepository(_profileImagesDirectory);
        PlayerImageRepository boardBackgrounds = new PlayerImageRepository(
            _boardBackgroundsDirectory,
            BoardBackgroundReferencePrefix);

        AccountService accountService = new AccountService(new AccountApplicationService(
            accountRepository,
            profileImages,
            verificationCodes,
            sessions,
            NullLogger<AccountApplicationService>.Instance));
        SessionService sessionService = new SessionService(
            new SessionApplicationService(accountRepository, verificationCodes, sessions, NullLogger<SessionApplicationService>.Instance),
            NullLogger<SessionService>.Instance);
        PlayerProfileService playerProfileService = new PlayerProfileService(
            new PlayerProfileApplicationService(
                sessions,
                new PlayerProfileRepository(),
                profileImages,
                NullLogger<PlayerProfileApplicationService>.Instance));
        PlayerPreferencesService playerPreferencesService = new PlayerPreferencesService(
            new PreferencesApplicationService(
                sessions,
                new PreferencesRepository(),
                boardBackgrounds));
        LeaderboardService leaderboardService = new LeaderboardService(
            new LeaderboardApplicationService(new LeaderboardRepository()));

        FriendshipService friendshipService = CreateFriendshipService(sessions);
        MatchRepository matchRepository = new MatchRepository(
            NullLogger<MatchRepository>.Instance);
        MatchApplicationService matchApplicationService = new MatchApplicationService(
            sessions,
            matchRepository,
            clock,
            _emailSender,
            NullLogger<MatchApplicationService>.Instance);
        GameplayMatchApplicationService gameplayApplicationService =
            new GameplayMatchApplicationService(
                sessions,
                matchRepository,
                clock,
                NullLogger<GameplayMatchApplicationService>.Instance);
        MatchCallbackSubscriptions<IMatchLobbyServiceCallback> lobbySubscriptions =
            new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
                (token, _) => matchApplicationService.EvaluateLobbyAccess(token)
                    == MatchStatus.Success,
                NullLogger<MatchCallbackSubscriptions<IMatchLobbyServiceCallback>>.Instance);
        _disposables.Add(lobbySubscriptions);
        MatchLobbyService matchLobbyService = new MatchLobbyService(
            matchApplicationService,
            lobbySubscriptions);

        GameplayMatchPresence gameplayPresence = new GameplayMatchPresence(NullLogger<GameplayMatchPresence>.Instance);
        MatchCallbackSubscriptions<IGameplayMatchServiceCallback> gameplaySubscriptions =
            new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
                (token, matchId) => gameplayApplicationService
                    .GetMatchState(token, matchId).Status == MatchStatus.Success,
                NullLogger<MatchCallbackSubscriptions<IGameplayMatchServiceCallback>>.Instance);
        _disposables.Add(gameplaySubscriptions);
        GameplayMatchService gameplayMatchService = new GameplayMatchService(
            gameplayApplicationService,
            gameplayPresence,
            gameplaySubscriptions,
            NullLogger<GameplayMatchService>.Instance);

        ChatApplicationService chatApplicationService = new ChatApplicationService(
            sessions,
            new ChatRepository(clock),
            clock);
        MatchCallbackSubscriptions<IMatchChatServiceCallback> chatSubscriptions =
            new MatchCallbackSubscriptions<IMatchChatServiceCallback>(
                (token, matchId) => chatApplicationService
                    .EvaluateParticipantAccess(token, matchId) == ChatStatus.Success,
                NullLogger<MatchCallbackSubscriptions<IMatchChatServiceCallback>>.Instance);
        _disposables.Add(chatSubscriptions);
        MatchChatService matchChatService = new MatchChatService(
            chatApplicationService,
            chatSubscriptions);

        matchApplicationService.MatchStarted += gameplayApplicationService.TrackStartedMatch;
        sessions.SessionRevoked += lobbySubscriptions.RemoveSession;
        sessions.SessionRevoked += gameplaySubscriptions.RemoveSession;
        sessions.SessionRevoked += chatSubscriptions.RemoveSession;

        OpenServiceHost<IAccountService>(accountService, "AccountService");
        OpenServiceHost<ISessionService>(sessionService, "SessionService");
        OpenServiceHost<IPlayerProfileService>(playerProfileService, "PlayerProfileService");
        OpenServiceHost<IPlayerPreferencesService>(
            playerPreferencesService,
            "PlayerPreferencesService");
        OpenServiceHost<ILeaderboardService>(leaderboardService, "LeaderboardService");
        OpenServiceHost<IFriendshipService>(friendshipService, "FriendshipService");
        OpenServiceHost<IMatchLobbyService>(matchLobbyService, "MatchLobbyService");
        OpenServiceHost<IGameplayMatchService>(gameplayMatchService, "GameplayMatchService");
        OpenServiceHost<IMatchChatService>(matchChatService, "MatchChatService");
    }

    private FriendshipService CreateFriendshipService(SessionStore sessions)
    {
        AccountPresenceRegistry presenceRegistry = new AccountPresenceRegistry();
        FriendshipSubscriptions subscriptions = new FriendshipSubscriptions(
            NullLogger<FriendshipSubscriptions>.Instance);
        _disposables.Add(subscriptions);
        FriendshipApplicationService applicationService = new FriendshipApplicationService(
            sessions,
            new FriendRepository());
        presenceRegistry.PresenceChanged += (accountId, isOnline) =>
        {
            FriendChange change = isOnline ? FriendChange.Connected : FriendChange.Disconnected;
            HashSet<long> targets = new HashSet<long>(
                applicationService.ReadConfirmedFriendIds(accountId));
            if (targets.Count > 0)
            {
                subscriptions.PublishToAccounts(
                    targets,
                    callback => callback.OnFriendChanged(accountId, change));
            }
        };
        sessions.SessionRevoked += subscriptions.Remove;
        return new FriendshipService(applicationService, presenceRegistry, subscriptions);
    }

    private void OpenServiceHost<TContract>(object serviceInstance, string endpointName)
        where TContract : class
    {
        Uri baseAddress = new Uri(
            "net.pipe://localhost/ReversiGameSystem.SystemTests/"
            + Guid.NewGuid().ToString("N") + "/");
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        ServiceHost host = new ServiceHost(serviceInstance, baseAddress);
        host.Description.Behaviors.Add(new ServiceErrorBehavior(
            NullLogger<ServiceErrorBehavior>.Instance));
        host.AddServiceEndpoint(typeof(TContract), binding, endpointName);
        host.Open();
        _hosts.Add(host);
        _endpoints[typeof(TContract)] = new Uri(baseAddress, endpointName);
    }

    private void CloseHostsAndSubscriptions()
    {
        for (int index = _hosts.Count - 1; index >= 0; index--)
        {
            WcfTestHostHelper.CloseOrAbort(_hosts[index]);
        }

        _hosts.Clear();
        _endpoints.Clear();

        for (int index = _disposables.Count - 1; index >= 0; index--)
        {
            _disposables[index].Dispose();
        }

        _disposables.Clear();
    }

    private static byte[] CreateHmacKey()
    {
        byte[] key = new byte[HmacKeySize];
        for (int index = 0; index < key.Length; index++)
        {
            key[index] = (byte)(index + 1);
        }

        return key;
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
    }

    internal sealed class ClientChannelScope<TContract> : IDisposable
        where TContract : class
    {
        private readonly ChannelFactory<TContract> _factory;

        internal ClientChannelScope(ChannelFactory<TContract> factory, TContract channel)
        {
            _factory = factory;
            Channel = channel;
        }

        internal TContract Channel { get; }

        public void Dispose()
        {
            WcfTestHostHelper.CloseOrAbort(Channel);
            WcfTestHostHelper.CloseOrAbort(_factory);
        }
    }

    private sealed class CapturingTemplatedEmailSender : ITemplatedEmailSender
    {
        private readonly ConcurrentDictionary<string, string> _latestCodes =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void SendTemplatedEmail(
            string recipientAddress,
            string templateName,
            IReadOnlyDictionary<string, string> templateValues)
        {
            if (templateValues.TryGetValue(VerificationCodeKey, out string code))
            {
                _latestCodes[recipientAddress] = code;
            }
        }

        internal void Clear()
        {
            _latestCodes.Clear();
        }

        internal string GetLatestCode(string recipientAddress)
        {
            if (_latestCodes.TryGetValue(recipientAddress, out string code))
            {
                return code;
            }

            throw new InvalidOperationException(
                $"No verification code was sent to {recipientAddress}.");
        }
    }
}
