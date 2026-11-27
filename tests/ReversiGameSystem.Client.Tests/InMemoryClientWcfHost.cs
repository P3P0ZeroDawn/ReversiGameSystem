using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal sealed class InMemoryClientWcfHost : IDisposable
{
    private const string ServicesNamespace = "urn:reversi:services:v1";
    private const string ContractsNamespace = "urn:reversi:contracts:v1";
    private const string DiagnosticsNamespace = "urn:reversi:diagnostics:v1";
    private const long ReviewAlphaAccountId = 1L;
    private const long ReviewBetaAccountId = 2L;
    private const string ReviewAlphaDisplayName = "ReviewAlpha";
    private const string ReviewBetaDisplayName = "ReviewBeta";
    private const int FirstPlayerSeatNumber = 1;
    private const int SecondPlayerSeatNumber = 2;
    private const long InitialMatchVersion = 1L;

    private readonly string _basePath;
    private readonly List<ServiceHost> _hosts = new List<ServiceHost>();
    private readonly InMemorySessionBackend _sessionBackend = new InMemorySessionBackend();
    private readonly InMemoryFriendshipBackend _friendshipBackend;
    private readonly InMemoryPlayerPreferencesBackend _preferencesBackend;
    private readonly InMemoryMatchBackend _matchBackend;

    internal InMemoryClientWcfHost()
    {
        _basePath = "net.pipe://localhost/ReversiClientTests_" + Guid.NewGuid().ToString("N") + "/";
        _friendshipBackend = new InMemoryFriendshipBackend(_sessionBackend);
        _preferencesBackend = new InMemoryPlayerPreferencesBackend(_sessionBackend);
        _matchBackend = new InMemoryMatchBackend(_sessionBackend);
        _sessionBackend.SessionClosed += _friendshipBackend.NotifySessionClosed;
        _sessionBackend.SessionClosed += _matchBackend.NotifySessionClosed;

        SessionEndpoint = OpenHost<ISessionHostContract>(_sessionBackend, "Session");
        FriendshipEndpoint = OpenHost<IFriendshipHostContract>(_friendshipBackend, "Friendship");
        PlayerPreferencesEndpoint = OpenHost<IPlayerPreferencesHostContract>(
            _preferencesBackend,
            "PlayerPreferences");
        MatchLobbyEndpoint = OpenHost<IMatchLobbyHostContract>(_matchBackend, "MatchLobby");
        GameplayMatchEndpoint = OpenHost<IGameplayMatchHostContract>(
            _matchBackend,
            "GameplayMatch");
        MatchChatEndpoint = OpenHost<IMatchChatHostContract>(_matchBackend, "MatchChat");
    }

    internal EndpointAddress SessionEndpoint { get; }

    internal EndpointAddress FriendshipEndpoint { get; }

    internal EndpointAddress PlayerPreferencesEndpoint { get; }

    internal EndpointAddress MatchLobbyEndpoint { get; }

    internal EndpointAddress GameplayMatchEndpoint { get; }

    internal EndpointAddress MatchChatEndpoint { get; }

    internal static NetNamedPipeBinding CreateBinding()
    {
        return new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
        {
            OpenTimeout = TimeSpan.FromSeconds(10),
            CloseTimeout = TimeSpan.FromSeconds(5),
            SendTimeout = TimeSpan.FromSeconds(10),
            ReceiveTimeout = TimeSpan.FromSeconds(15)
        };
    }

    public void Dispose()
    {
        for (int index = _hosts.Count - 1; index >= 0; index--)
        {
            ServiceHost host = _hosts[index];
            try
            {
                if (host.State == CommunicationState.Opened)
                {
                    host.Close();
                }
                else
                {
                    host.Abort();
                }
            }
            catch (CommunicationException)
            {
                host.Abort();
            }
            catch (TimeoutException)
            {
                host.Abort();
            }
        }

        _hosts.Clear();
    }

    private EndpointAddress OpenHost<TContract>(object singleton, string relativePath)
    {
        Uri baseAddress = new Uri(_basePath);
        ServiceHost host = new ServiceHost(singleton, baseAddress);
        host.AddServiceEndpoint(typeof(TContract), CreateBinding(), relativePath);
        host.Open();
        _hosts.Add(host);
        return new EndpointAddress(new Uri(baseAddress, relativePath));
    }

    [ServiceContract(Name = "ISessionService", Namespace = ServicesNamespace)]
    private interface ISessionHostContract
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        DateTime RequestLoginCode(string loginIdentifier, string password);

        [OperationContract]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.AuthenticatedSession CompleteLogin(string loginIdentifier, string verificationCode);

        [OperationContract]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.GuestSession OpenGuestSession(string displayName);

        [OperationContract]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void CloseSession(string sessionToken);
    }

    [ServiceContract(
        Name = "IFriendshipService",
        Namespace = ServicesNamespace,
        CallbackContract = typeof(Proxy.IFriendshipServiceCallback))]
    private interface IFriendshipHostContract
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.FriendshipFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void OpenFriendshipChannel(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.FriendshipFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.PlayerSummary[] FindPlayers(string sessionToken, string searchText);

        [OperationContract]
        [FaultContract(typeof(Proxy.FriendshipFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId);

        [OperationContract]
        [FaultContract(typeof(Proxy.FriendshipFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.FriendRequest[] GetPendingFriendRequests(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.FriendshipFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void RespondToFriendRequest(string sessionToken, string requestId, bool accept);

        [OperationContract]
        [FaultContract(typeof(Proxy.FriendshipFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.FriendSummary[] GetFriends(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.FriendshipFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void RemoveFriend(string sessionToken, long friendAccountId);
    }

    [ServiceContract(Name = "IPlayerPreferencesService", Namespace = ServicesNamespace)]
    private interface IPlayerPreferencesHostContract
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.PreferencesFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.PlayerPreferences GetPlayerPreferences(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.PreferencesFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void UpdatePlayerPreferences(string sessionToken, Proxy.PlayerPreferences preferences);

        [OperationContract]
        [FaultContract(typeof(Proxy.PreferencesFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.ImageContent GetBoardBackground(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.PreferencesFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void ChangeBoardBackground(string sessionToken, Proxy.ImageContent image);
    }

    [ServiceContract(
        Name = "IMatchLobbyService",
        Namespace = ServicesNamespace,
        CallbackContract = typeof(Proxy.IMatchLobbyServiceCallback))]
    private interface IMatchLobbyHostContract
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void OpenLobbyChannel(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.LobbySnapshot CreateMatch(
            string sessionToken,
            Proxy.MatchConfiguration configuration);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.FriendshipFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.LobbySnapshot InviteToMatch(
            string sessionToken,
            long friendAccountId,
            Proxy.MatchConfiguration configuration);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.AvailableMatch[] GetAvailableMatches(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.LobbySnapshot JoinMatch(string sessionToken, string matchCode);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.LobbySnapshot ChangeTurnDuration(
            string sessionToken,
            long matchId,
            int turnDurationSeconds);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void CancelMatch(string sessionToken, long matchId);
    }

    [ServiceContract(
        Name = "IGameplayMatchService",
        Namespace = ServicesNamespace,
        CallbackContract = typeof(Proxy.IGameplayMatchServiceCallback))]
    private interface IGameplayMatchHostContract
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void OpenMatchChannel(string sessionToken, long matchId);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchInformation[] GetObservableMatches(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchStateSnapshot PlacePiece(string sessionToken, Proxy.PiecePlacement placement);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchConclusion ForfeitMatch(string sessionToken, long matchId);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchStateSnapshot ObserveMatch(string sessionToken, long matchId);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchSummary[] GetMatchHistory(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchDetails GetMatchDetails(string sessionToken, long matchId);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MoveRecord[] GetMoveHistory(string sessionToken, long matchId);
    }

    [ServiceContract(
        Name = "IMatchChatService",
        Namespace = ServicesNamespace,
        CallbackContract = typeof(Proxy.IMatchChatServiceCallback))]
    private interface IMatchChatHostContract
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void OpenChatChannel(string sessionToken, long matchId);

        [OperationContract]
        [FaultContract(typeof(Proxy.ChatFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchChatEntry SendChatMessage(string sessionToken, long matchId, string content);

        [OperationContract]
        [FaultContract(typeof(Proxy.SessionFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = ContractsNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        Proxy.MatchChatEntry[] GetChatHistory(string sessionToken, long matchId);
    }

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    private sealed class InMemorySessionBackend : ISessionHostContract
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, Proxy.AuthenticatedSession> _authenticated =
            new Dictionary<string, Proxy.AuthenticatedSession>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _guestTokens =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> _reservedGuestNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal event Action<long>? SessionClosed;

        public DateTime RequestLoginCode(string loginIdentifier, string password)
        {
            return DateTime.UtcNow.AddMinutes(5);
        }

        public Proxy.AuthenticatedSession CompleteLogin(
            string loginIdentifier,
            string verificationCode)
        {
            long accountId;
            string username;
            if (string.Equals(loginIdentifier, ReviewAlphaDisplayName, StringComparison.Ordinal)
                && string.Equals(verificationCode, "000001", StringComparison.Ordinal))
            {
                accountId = ReviewAlphaAccountId;
                username = ReviewAlphaDisplayName;
            }
            else if (string.Equals(loginIdentifier, ReviewBetaDisplayName, StringComparison.Ordinal)
                && string.Equals(verificationCode, "000002", StringComparison.Ordinal))
            {
                accountId = ReviewBetaAccountId;
                username = ReviewBetaDisplayName;
            }
            else
            {
                throw new FaultException<Proxy.SessionFault>(
                    new Proxy.SessionFault { Reason = Proxy.SessionFailure.InvalidVerificationCode });
            }

            string token = "auth-" + Guid.NewGuid().ToString("N");
            Proxy.AuthenticatedSession session = new Proxy.AuthenticatedSession
            {
                SessionToken = token,
                Account = new Proxy.AccountReference
                {
                    AccountId = accountId,
                    Username = username
                },
                EmailAddress = username.ToLowerInvariant() + "@example.com",
                PreferredLocaleCode = "en-US",
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            };

            lock (_sync)
            {
                _authenticated[token] = session;
            }

            return session;
        }

        public Proxy.GuestSession OpenGuestSession(string displayName)
        {
            string normalized = "Guest-" + (displayName ?? string.Empty).Trim();
            lock (_sync)
            {
                if (_reservedGuestNames.Contains(normalized))
                {
                    throw new FaultException<Proxy.SessionFault>(
                        new Proxy.SessionFault
                        {
                            Reason = Proxy.SessionFailure.GuestNameUnavailable
                        });
                }

                string token = "guest-" + Guid.NewGuid().ToString("N");
                _reservedGuestNames.Add(normalized);
                _guestTokens[token] = normalized;
                return new Proxy.GuestSession
                {
                    SessionToken = token,
                    DisplayName = normalized,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
                };
            }
        }

        public void CloseSession(string sessionToken)
        {
            long? closedAccountId = null;
            lock (_sync)
            {
                if (_guestTokens.TryGetValue(sessionToken, out string? guestName))
                {
                    _guestTokens.Remove(sessionToken);
                    _reservedGuestNames.Remove(guestName);
                }

                if (_authenticated.TryGetValue(
                    sessionToken,
                    out Proxy.AuthenticatedSession? session))
                {
                    _authenticated.Remove(sessionToken);
                    if (!_authenticated.Values.Any(
                        item => item.Account.AccountId == session.Account.AccountId))
                    {
                        closedAccountId = session.Account.AccountId;
                    }
                }
            }

            if (closedAccountId.HasValue)
            {
                SessionClosed?.Invoke(closedAccountId.Value);
            }
        }

        internal long RequireAuthenticatedAccount(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                throw new FaultException<Proxy.SessionFault>(
                    new Proxy.SessionFault { Reason = Proxy.SessionFailure.MissingSessionToken });
            }

            lock (_sync)
            {
                if (_authenticated.TryGetValue(
                    sessionToken,
                    out Proxy.AuthenticatedSession? session))
                {
                    return session.Account.AccountId;
                }
            }

            throw new FaultException<Proxy.SessionFault>(
                new Proxy.SessionFault { Reason = Proxy.SessionFailure.InvalidSession });
        }

        internal string GetDisplayName(long accountId)
        {
            return accountId == ReviewAlphaAccountId
                ? ReviewAlphaDisplayName
                : ReviewBetaDisplayName;
        }

        internal static long GetPeerAccountId(long accountId)
        {
            return accountId == ReviewAlphaAccountId
                ? ReviewBetaAccountId
                : ReviewAlphaAccountId;
        }
    }

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    private sealed class InMemoryFriendshipBackend : IFriendshipHostContract
    {
        private readonly InMemorySessionBackend _sessions;
        private readonly ConcurrentDictionary<long, Proxy.IFriendshipServiceCallback> _callbacks =
            new ConcurrentDictionary<long, Proxy.IFriendshipServiceCallback>();
        private readonly ConcurrentDictionary<long, bool> _online =
            new ConcurrentDictionary<long, bool>();

        internal InMemoryFriendshipBackend(InMemorySessionBackend sessions)
        {
            _sessions = sessions;
        }

        public void OpenFriendshipChannel(string sessionToken)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            Proxy.IFriendshipServiceCallback callback =
                OperationContext.Current.GetCallbackChannel<Proxy.IFriendshipServiceCallback>();
            _callbacks[accountId] = callback;
            _online[accountId] = true;
            NotifyFriend(accountId, Proxy.FriendChange.Connected);
        }

        public Proxy.FriendSummary[] GetFriends(string sessionToken)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            long friendId = InMemorySessionBackend.GetPeerAccountId(accountId);
            string friendName = _sessions.GetDisplayName(friendId);
            bool isOnline = _online.TryGetValue(friendId, out bool online) && online;
            return new[]
            {
                new Proxy.FriendSummary
                {
                    Account = new Proxy.AccountReference
                    {
                        AccountId = friendId,
                        Username = friendName
                    },
                    IsOnline = isOnline,
                    HasProfileImage = false
                }
            };
        }

        public Proxy.PlayerSummary[] FindPlayers(string sessionToken, string searchText)
        {
            return Array.Empty<Proxy.PlayerSummary>();
        }

        public Proxy.FriendRequest SendFriendRequest(string sessionToken, long recipientAccountId)
        {
            throw new NotSupportedException();
        }

        public Proxy.FriendRequest[] GetPendingFriendRequests(string sessionToken)
        {
            return Array.Empty<Proxy.FriendRequest>();
        }

        public void RespondToFriendRequest(string sessionToken, string requestId, bool accept)
        {
        }

        public void RemoveFriend(string sessionToken, long friendAccountId)
        {
        }

        internal void NotifySessionClosed(long accountId)
        {
            _online[accountId] = false;
            _callbacks.TryRemove(accountId, out _);
            NotifyFriend(accountId, Proxy.FriendChange.Disconnected);
        }

        private void NotifyFriend(long accountId, Proxy.FriendChange change)
        {
            long friendId = InMemorySessionBackend.GetPeerAccountId(accountId);
            if (_callbacks.TryGetValue(friendId, out Proxy.IFriendshipServiceCallback? callback))
            {
                try
                {
                    callback.OnFriendChanged(accountId, change);
                }
                catch (CommunicationException)
                {
                    _callbacks.TryRemove(friendId, out _);
                }
            }
        }
    }

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    private sealed class InMemoryPlayerPreferencesBackend : IPlayerPreferencesHostContract
    {
        private readonly InMemorySessionBackend _sessions;
        private readonly ConcurrentDictionary<long, Proxy.PlayerPreferences> _preferences =
            new ConcurrentDictionary<long, Proxy.PlayerPreferences>();

        internal InMemoryPlayerPreferencesBackend(InMemorySessionBackend sessions)
        {
            _sessions = sessions;
        }

        public Proxy.PlayerPreferences GetPlayerPreferences(string sessionToken)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            return _preferences.GetOrAdd(
                accountId,
                _ => new Proxy.PlayerPreferences
                {
                    PreferredLocaleCode = "es-MX",
                    PieceStyleCode = "Classic",
                    TimeZoneId = "UTC",
                    UseCustomBoardBackground = false
                });
        }

        public void UpdatePlayerPreferences(
            string sessionToken,
            Proxy.PlayerPreferences preferences)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            _preferences[accountId] = preferences;
        }

        public Proxy.ImageContent GetBoardBackground(string sessionToken)
        {
            _sessions.RequireAuthenticatedAccount(sessionToken);
            return new Proxy.ImageContent
            {
                MediaType = "image/png",
                Content = Array.Empty<byte>()
            };
        }

        public void ChangeBoardBackground(string sessionToken, Proxy.ImageContent image)
        {
            _sessions.RequireAuthenticatedAccount(sessionToken);
        }
    }

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    private sealed class InMemoryMatchBackend :
        IMatchLobbyHostContract,
        IGameplayMatchHostContract,
        IMatchChatHostContract
    {
        private sealed class MatchRecordState
        {
            internal long MatchId;
            internal string MatchCode = string.Empty;
            internal Proxy.MatchConfiguration Configuration = new Proxy.MatchConfiguration();
            internal Proxy.MatchState State;
            internal List<Proxy.MatchParticipantSummary> Participants =
                new List<Proxy.MatchParticipantSummary>();
            internal Proxy.BoardCellState[] Board = new Proxy.BoardCellState[64];
            internal long Version;
            internal int? CurrentTurnSeatNumber;
            internal Dictionary<long, Proxy.IGameplayMatchServiceCallback> GameplayCallbacks =
                new Dictionary<long, Proxy.IGameplayMatchServiceCallback>();
            internal Dictionary<long, Proxy.IMatchChatServiceCallback> ChatCallbacks =
                new Dictionary<long, Proxy.IMatchChatServiceCallback>();
            internal List<Proxy.MatchChatEntry> ChatMessages = new List<Proxy.MatchChatEntry>();
        }

        private readonly InMemorySessionBackend _sessions;
        private readonly object _sync = new object();
        private readonly Dictionary<long, MatchRecordState> _matches =
            new Dictionary<long, MatchRecordState>();
        private readonly Dictionary<long, Proxy.IMatchLobbyServiceCallback> _lobbyCallbacks =
            new Dictionary<long, Proxy.IMatchLobbyServiceCallback>();
        private long _nextMatchId = 100L;

        internal InMemoryMatchBackend(InMemorySessionBackend sessions)
        {
            _sessions = sessions;
        }

        public void OpenLobbyChannel(string sessionToken)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            Proxy.IMatchLobbyServiceCallback callback =
                OperationContext.Current.GetCallbackChannel<Proxy.IMatchLobbyServiceCallback>();
            lock (_sync)
            {
                _lobbyCallbacks[accountId] = callback;
            }
        }

        public Proxy.LobbySnapshot CreateMatch(
            string sessionToken,
            Proxy.MatchConfiguration configuration)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                long matchId = ++_nextMatchId;
                MatchRecordState match = new MatchRecordState
                {
                    MatchId = matchId,
                    MatchCode = "M" + matchId,
                    Configuration = configuration,
                    State = Proxy.MatchState.Waiting,
                    Participants = new List<Proxy.MatchParticipantSummary>
                    {
                        new Proxy.MatchParticipantSummary
                        {
                            SeatNumber = FirstPlayerSeatNumber,
                            Kind = Proxy.ParticipantKind.Registered,
                            AccountId = accountId,
                            DisplayName = _sessions.GetDisplayName(accountId)
                        }
                    }
                };
                _matches[matchId] = match;
                return ToLobbySnapshot(match);
            }
        }

        public Proxy.LobbySnapshot InviteToMatch(
            string sessionToken,
            long friendAccountId,
            Proxy.MatchConfiguration configuration)
        {
            throw new NotSupportedException();
        }

        public Proxy.AvailableMatch[] GetAvailableMatches(string sessionToken)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                return _matches.Values
                    .Where(match => match.State == Proxy.MatchState.Waiting
                        && !match.Participants.Any(player => player.AccountId == accountId))
                    .Select(match => new Proxy.AvailableMatch
                    {
                        MatchCode = match.MatchCode,
                        CreatorDisplayName = match.Participants[0].DisplayName,
                        TurnDurationSeconds = match.Configuration.TurnDurationSeconds
                    })
                    .ToArray();
            }
        }

        public Proxy.LobbySnapshot JoinMatch(string sessionToken, string matchCode)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            Proxy.MatchStateSnapshot startedState;
            Proxy.LobbySnapshot lobbySnapshot;
            Proxy.IMatchLobbyServiceCallback? creatorCallback = null;

            lock (_sync)
            {
                MatchRecordState? match = _matches.Values.FirstOrDefault(
                    item => string.Equals(item.MatchCode, matchCode, StringComparison.Ordinal)
                        && item.State == Proxy.MatchState.Waiting);
                if (match == null)
                {
                    throw new FaultException<Proxy.MatchFault>(
                        new Proxy.MatchFault { Reason = Proxy.MatchFailure.MatchCodeUnavailable });
                }

                match.Participants.Add(new Proxy.MatchParticipantSummary
                {
                    SeatNumber = SecondPlayerSeatNumber,
                    Kind = Proxy.ParticipantKind.Registered,
                    AccountId = accountId,
                    DisplayName = _sessions.GetDisplayName(accountId)
                });
                InitializeBoard(match);
                startedState = ToMatchStateSnapshot(match);
                lobbySnapshot = ToLobbySnapshot(match);

                long? creatorId = match.Participants[0].AccountId;
                if (creatorId.HasValue)
                {
                    _lobbyCallbacks.TryGetValue(creatorId.Value, out creatorCallback);
                }
            }

            if (creatorCallback != null)
            {
                try
                {
                    creatorCallback.OnMatchStarted(startedState);
                }
                catch (CommunicationException)
                {
                    RemoveLobbyCallback(creatorCallback);
                }
            }

            return lobbySnapshot;
        }

        public Proxy.LobbySnapshot ChangeTurnDuration(
            string sessionToken,
            long matchId,
            int turnDurationSeconds)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                match.Configuration.TurnDurationSeconds = turnDurationSeconds;
                return ToLobbySnapshot(match);
            }
        }

        public Proxy.MatchStateSnapshot StartSystemMatch(string sessionToken, long matchId)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                match.Participants.Add(new Proxy.MatchParticipantSummary
                {
                    SeatNumber = SecondPlayerSeatNumber,
                    Kind = Proxy.ParticipantKind.System,
                    AccountId = null,
                    DisplayName = "System"
                });
                InitializeBoard(match);
                return ToMatchStateSnapshot(match);
            }
        }

        public void CancelMatch(string sessionToken, long matchId)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                match.State = Proxy.MatchState.Cancelled;
            }
        }

        public void OpenMatchChannel(string sessionToken, long matchId)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            Proxy.IGameplayMatchServiceCallback callback =
                OperationContext.Current.GetCallbackChannel<Proxy.IGameplayMatchServiceCallback>();
            List<(Proxy.IGameplayMatchServiceCallback Callback, int SeatNumber)> notifications =
                new List<(Proxy.IGameplayMatchServiceCallback, int)>();
            int callerSeat;

            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                callerSeat = match.Participants
                    .First(player => player.AccountId == accountId)
                    .SeatNumber;
                match.GameplayCallbacks[accountId] = callback;

                foreach (Proxy.MatchParticipantSummary participant in match.Participants)
                {
                    if (participant.AccountId.HasValue
                        && match.GameplayCallbacks.ContainsKey(participant.AccountId.Value))
                    {
                        notifications.Add((callback, participant.SeatNumber));
                    }
                }

                foreach (KeyValuePair<long, Proxy.IGameplayMatchServiceCallback> entry
                    in match.GameplayCallbacks)
                {
                    if (entry.Key != accountId)
                    {
                        notifications.Add((entry.Value, callerSeat));
                    }
                }
            }

            foreach ((Proxy.IGameplayMatchServiceCallback target, int seat) in notifications)
            {
                try
                {
                    target.OnParticipantConnectionChanged(matchId, seat, true);
                }
                catch (CommunicationException)
                {
                    RemoveGameplayCallback(matchId, target);
                }
            }
        }

        public Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                int seat = match.Participants
                    .First(player => player.AccountId == accountId)
                    .SeatNumber;
                return new Proxy.MatchInformation
                {
                    MatchId = match.MatchId,
                    Participants = match.Participants.ToArray(),
                    AssignedSeatNumber = seat,
                    TurnDurationSeconds = match.Configuration.TurnDurationSeconds
                };
            }
        }

        public Proxy.MatchInformation[] GetObservableMatches(string sessionToken)
        {
            _sessions.RequireAuthenticatedAccount(sessionToken);
            return Array.Empty<Proxy.MatchInformation>();
        }

        public Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                return ToMatchStateSnapshot(match);
            }
        }

        public Proxy.MatchStateSnapshot PlacePiece(
            string sessionToken,
            Proxy.PiecePlacement placement)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            Proxy.MatchStateSnapshot snapshot;
            Proxy.IGameplayMatchServiceCallback[] callbacks;

            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, placement.MatchId);
                int callerSeat = match.Participants
                    .First(player => player.AccountId == accountId)
                    .SeatNumber;
                if (match.CurrentTurnSeatNumber != callerSeat)
                {
                    throw new FaultException<Proxy.MatchFault>(
                        new Proxy.MatchFault { Reason = Proxy.MatchFailure.NotPlayersTurn });
                }

                ApplyReversiMove(match, placement.Row, placement.Column, callerSeat);
                match.Version++;
                match.CurrentTurnSeatNumber = GetOpponentSeatNumber(callerSeat);
                snapshot = ToMatchStateSnapshot(match);
                callbacks = match.GameplayCallbacks.Values.ToArray();
            }

            foreach (Proxy.IGameplayMatchServiceCallback callback in callbacks)
            {
                try
                {
                    callback.OnMatchStateChanged(snapshot);
                }
                catch (CommunicationException)
                {
                    RemoveGameplayCallback(placement.MatchId, callback);
                }
            }

            return snapshot;
        }

        public Proxy.MatchConclusion ForfeitMatch(string sessionToken, long matchId)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                int callerSeat = match.Participants
                    .First(player => player.AccountId == accountId)
                    .SeatNumber;
                match.State = Proxy.MatchState.Finished;
                match.Version++;
                return new Proxy.MatchConclusion
                {
                    MatchId = match.MatchId,
                    MatchStateVersion = match.Version,
                    Reason = Proxy.MatchEndReason.Abandonment,
                    AbandoningSeatNumber = callerSeat,
                    WinnerSeatNumber = GetOpponentSeatNumber(callerSeat),
                    BlackPieceCount = match.Board.Count(
                        cell => cell == Proxy.BoardCellState.Black),
                    WhitePieceCount = match.Board.Count(
                        cell => cell == Proxy.BoardCellState.White),
                    EndedAtUtc = DateTime.UtcNow
                };
            }
        }

        public Proxy.MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
        {
            throw new NotSupportedException();
        }

        public Proxy.MatchSummary[] GetMatchHistory(string sessionToken)
        {
            return Array.Empty<Proxy.MatchSummary>();
        }

        public Proxy.MatchDetails GetMatchDetails(string sessionToken, long matchId)
        {
            throw new NotSupportedException();
        }

        public Proxy.MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
        {
            return Array.Empty<Proxy.MoveRecord>();
        }

        public void OpenChatChannel(string sessionToken, long matchId)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            Proxy.IMatchChatServiceCallback callback =
                OperationContext.Current.GetCallbackChannel<Proxy.IMatchChatServiceCallback>();
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                match.ChatCallbacks[accountId] = callback;
            }
        }

        public Proxy.MatchChatEntry SendChatMessage(
            string sessionToken,
            long matchId,
            string content)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            string trimmed = (content ?? string.Empty).Trim();
            if (string.Equals(trimmed, "blockedword", StringComparison.OrdinalIgnoreCase))
            {
                throw new FaultException<Proxy.ChatFault>(
                    new Proxy.ChatFault { Reason = Proxy.ChatFailure.MessageBlocked });
            }

            Proxy.MatchChatEntry entry;
            Proxy.IMatchChatServiceCallback[] callbacks;
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                int callerSeat = match.Participants
                    .First(player => player.AccountId == accountId)
                    .SeatNumber;
                entry = new Proxy.MatchChatEntry
                {
                    MatchId = match.MatchId,
                    MessageNumber = match.ChatMessages.Count + 1,
                    SenderSeatNumber = callerSeat,
                    Content = trimmed,
                    SentAtUtc = DateTime.UtcNow
                };
                match.ChatMessages.Add(entry);
                callbacks = match.ChatCallbacks.Values.ToArray();
            }

            foreach (Proxy.IMatchChatServiceCallback callback in callbacks)
            {
                try
                {
                    callback.OnChatMessageReceived(entry);
                }
                catch (CommunicationException)
                {
                    RemoveChatCallback(matchId, callback);
                }
            }

            return entry;
        }

        public Proxy.MatchChatEntry[] GetChatHistory(string sessionToken, long matchId)
        {
            long accountId = _sessions.RequireAuthenticatedAccount(sessionToken);
            lock (_sync)
            {
                MatchRecordState match = RequireParticipantMatch(accountId, matchId);
                return match.ChatMessages.ToArray();
            }
        }

        internal void NotifySessionClosed(long accountId)
        {
            List<(long MatchId, int SeatNumber, Proxy.IGameplayMatchServiceCallback Callback)>
                notifications =
                    new List<(long, int, Proxy.IGameplayMatchServiceCallback)>();

            lock (_sync)
            {
                _lobbyCallbacks.Remove(accountId);
                foreach (MatchRecordState match in _matches.Values)
                {
                    match.GameplayCallbacks.Remove(accountId);
                    match.ChatCallbacks.Remove(accountId);
                    Proxy.MatchParticipantSummary? participant = match.Participants
                        .FirstOrDefault(player => player.AccountId == accountId);
                    if (participant != null && match.State == Proxy.MatchState.InProgress)
                    {
                        foreach (Proxy.IGameplayMatchServiceCallback callback
                            in match.GameplayCallbacks.Values)
                        {
                            notifications.Add((match.MatchId, participant.SeatNumber, callback));
                        }
                    }
                }
            }

            foreach ((long matchId, int seat, Proxy.IGameplayMatchServiceCallback callback)
                in notifications)
            {
                try
                {
                    callback.OnParticipantConnectionChanged(matchId, seat, false);
                }
                catch (CommunicationException)
                {
                    RemoveGameplayCallback(matchId, callback);
                }
            }
        }

        private void RemoveLobbyCallback(Proxy.IMatchLobbyServiceCallback callback)
        {
            lock (_sync)
            {
                long[] disconnectedAccounts = _lobbyCallbacks
                    .Where(entry => ReferenceEquals(entry.Value, callback))
                    .Select(entry => entry.Key)
                    .ToArray();
                foreach (long accountId in disconnectedAccounts)
                {
                    _lobbyCallbacks.Remove(accountId);
                }
            }
        }

        private void RemoveGameplayCallback(long matchId, Proxy.IGameplayMatchServiceCallback callback)
        {
            lock (_sync)
            {
                if (_matches.TryGetValue(matchId, out MatchRecordState? match))
                {
                    long[] disconnectedAccounts = match.GameplayCallbacks
                        .Where(entry => ReferenceEquals(entry.Value, callback))
                        .Select(entry => entry.Key)
                        .ToArray();
                    foreach (long accountId in disconnectedAccounts)
                    {
                        match.GameplayCallbacks.Remove(accountId);
                    }
                }
            }
        }

        private void RemoveChatCallback(long matchId, Proxy.IMatchChatServiceCallback callback)
        {
            lock (_sync)
            {
                if (_matches.TryGetValue(matchId, out MatchRecordState? match))
                {
                    long[] disconnectedAccounts = match.ChatCallbacks
                        .Where(entry => ReferenceEquals(entry.Value, callback))
                        .Select(entry => entry.Key)
                        .ToArray();
                    foreach (long accountId in disconnectedAccounts)
                    {
                        match.ChatCallbacks.Remove(accountId);
                    }
                }
            }
        }

        private static int GetOpponentSeatNumber(int seatNumber)
        {
            return seatNumber == FirstPlayerSeatNumber
                ? SecondPlayerSeatNumber
                : FirstPlayerSeatNumber;
        }

        private MatchRecordState RequireParticipantMatch(long accountId, long matchId)
        {
            if (!_matches.TryGetValue(matchId, out MatchRecordState? match))
            {
                throw new FaultException<Proxy.MatchFault>(
                    new Proxy.MatchFault { Reason = Proxy.MatchFailure.MatchNotFound });
            }

            if (!match.Participants.Any(player => player.AccountId == accountId))
            {
                throw new FaultException<Proxy.MatchFault>(
                    new Proxy.MatchFault { Reason = Proxy.MatchFailure.Unauthorized });
            }

            return match;
        }

        private static void InitializeBoard(MatchRecordState match)
        {
            Array.Clear(match.Board, 0, match.Board.Length);
            match.Board[27] = Proxy.BoardCellState.White;
            match.Board[28] = Proxy.BoardCellState.Black;
            match.Board[35] = Proxy.BoardCellState.Black;
            match.Board[36] = Proxy.BoardCellState.White;
            match.State = Proxy.MatchState.InProgress;
            match.Version = InitialMatchVersion;
            match.CurrentTurnSeatNumber = FirstPlayerSeatNumber;
        }

        private static void ApplyReversiMove(
            MatchRecordState match,
            int row,
            int column,
            int seatNumber)
        {
            Proxy.BoardCellState mine = seatNumber == FirstPlayerSeatNumber
                ? Proxy.BoardCellState.Black
                : Proxy.BoardCellState.White;
            Proxy.BoardCellState opponent = seatNumber == FirstPlayerSeatNumber
                ? Proxy.BoardCellState.White
                : Proxy.BoardCellState.Black;
            match.Board[(row * 8) + column] = mine;

            int[] directions = { -1, 0, 1 };
            foreach (int dRow in directions)
            {
                foreach (int dCol in directions)
                {
                    if (dRow == 0 && dCol == 0)
                    {
                        continue;
                    }

                    List<int> toFlip = new List<int>();
                    int currentRow = row + dRow;
                    int currentCol = column + dCol;
                    while (currentRow >= 0 && currentRow < 8 && currentCol >= 0 && currentCol < 8)
                    {
                        int index = (currentRow * 8) + currentCol;
                        if (match.Board[index] == opponent)
                        {
                            toFlip.Add(index);
                        }
                        else if (match.Board[index] == mine && toFlip.Count > 0)
                        {
                            foreach (int flipIndex in toFlip)
                            {
                                match.Board[flipIndex] = mine;
                            }
                            break;
                        }
                        else
                        {
                            break;
                        }

                        currentRow += dRow;
                        currentCol += dCol;
                    }
                }
            }
        }

        private static Proxy.LobbySnapshot ToLobbySnapshot(MatchRecordState match)
        {
            return new Proxy.LobbySnapshot
            {
                MatchId = match.MatchId,
                MatchCode = match.MatchCode,
                Participants = match.Participants.ToArray(),
                Configuration = match.Configuration,
                State = match.State
            };
        }

        private static Proxy.MatchStateSnapshot ToMatchStateSnapshot(MatchRecordState match)
        {
            return new Proxy.MatchStateSnapshot
            {
                MatchId = match.MatchId,
                MatchStateVersion = match.Version,
                State = match.State,
                Board = match.Board.ToArray(),
                BlackPieceCount = match.Board.Count(cell => cell == Proxy.BoardCellState.Black),
                WhitePieceCount = match.Board.Count(cell => cell == Proxy.BoardCellState.White),
                CurrentTurnSeatNumber = match.CurrentTurnSeatNumber,
                TurnStartedAtUtc = DateTime.UtcNow
            };
        }
    }
}
