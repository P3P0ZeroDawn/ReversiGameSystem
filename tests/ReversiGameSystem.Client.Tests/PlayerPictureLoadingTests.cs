using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Chat;
using ReversiGameSystem.Features.Gameplay;
using ReversiGameSystem.Features.Matches;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PlayerPictureLoadingTests
{
    private const long MatchId = 7;
    private const long LocalAccount = 11;
    private const long OpponentAccount = 22;
    private const string SessionToken = "picture-test-session";
    private const double Resolution = 96;

    [TestMethod]
    public async Task LoadPlayerPictures_TwoAccountParticipants_LoadsBothPhotos()
    {
        using PictureHost host = new PictureHost();
        using GameplayClient gameplay = host.CreateGameplay();
        using MatchChatClient chat = host.CreateChat();
        ClientSession session = CreateSession();
        GameViewModel model = new GameViewModel(gameplay, chat, MatchId, false,
            new SynchronizationContext());
        await model.ActivateMatch();
        try
        {
            ProfileStatus status = await model.LoadPlayerPictures(host.CreateProfiles(), session);

            Assert.AreEqual(ProfileStatus.Success, status);
            Assert.IsNotNull(model.BlackPlayerPicture);
            Assert.IsNotNull(model.WhitePlayerPicture);
            Assert.AreSame(model.BlackPlayerPicture, session.ProfilePicture);
            Assert.AreEqual(2, host.Service.ImageReads);
        }
        finally
        {
            await model.DeactivateMatch();
            UserPreferencesState.Reset();
        }
    }

    [TestMethod]
    public async Task LoadPlayerPictures_OpponentWithoutPhoto_UsesDefaultWithoutImageRequest()
    {
        using PictureHost host = new PictureHost();
        host.Service.OpponentHasImage = false;
        using GameplayClient gameplay = host.CreateGameplay();
        using MatchChatClient chat = host.CreateChat();
        ClientSession session = CreateSession();
        GameViewModel model = new GameViewModel(gameplay, chat, MatchId, false,
            new SynchronizationContext());
        await model.ActivateMatch();
        try
        {
            ProfileStatus status = await model.LoadPlayerPictures(host.CreateProfiles(), session);

            Assert.AreEqual(ProfileStatus.Success, status);
            Assert.IsNull(model.WhitePlayerPicture);
            Assert.AreEqual(1, host.Service.ImageReads);
        }
        finally
        {
            await model.DeactivateMatch();
            UserPreferencesState.Reset();
        }
    }

    [TestMethod]
    public async Task LoadPlayerPictures_SessionChangedDuringRead_DiscardsPhoto()
    {
        using PictureHost host = new PictureHost();
        using GameplayClient gameplay = host.CreateGameplay();
        using MatchChatClient chat = host.CreateChat();
        ClientSession session = CreateSession();
        host.Service.BeforeImageResponse = session.Clear;
        GameViewModel model = new GameViewModel(gameplay, chat, MatchId, false,
            new SynchronizationContext());
        await model.ActivateMatch();
        try
        {
            ProfileStatus status = await model.LoadPlayerPictures(host.CreateProfiles(), session);

            Assert.AreEqual(ProfileStatus.InvalidSession, status);
            Assert.IsNull(model.BlackPlayerPicture);
            Assert.IsNull(session.ProfilePicture);
        }
        finally
        {
            await model.DeactivateMatch();
            UserPreferencesState.Reset();
        }
    }

    [TestMethod]
    public async Task ReplayPlayerPictures_TwoAccounts_LoadsBothPhotos()
    {
        using PictureHost host = new PictureHost();
        ClientSession session = CreateSession();
        MatchReplayViewModel model = CreateReplay(false);
        try
        {
            await model.PlayerPictures.LoadPlayerPictures(host.CreateProfiles(), session);

            Assert.IsNotNull(model.PlayerPictures.BlackPlayerPicture);
            Assert.IsNotNull(model.PlayerPictures.WhitePlayerPicture);
        }
        finally
        {
            model.PlayerPictures.Deactivate();
            UserPreferencesState.Reset();
        }
    }

    [TestMethod]
    public async Task ReplayPlayerPictures_SystemOpponent_UsesRobotWithoutRemotePhotoQuery()
    {
        using PictureHost host = new PictureHost();
        ClientSession session = CreateSession();
        MatchReplayViewModel model = CreateReplay(true);
        try
        {
            await model.PlayerPictures.LoadPlayerPictures(host.CreateProfiles(), session);

            Assert.AreEqual(1, host.Service.ImageReads);
            Assert.AreEqual(ReversiGameSystem.Game.DiscColor.White,
                model.PlayerPictures.SystemPlayerColor);
        }
        finally
        {
            model.PlayerPictures.Deactivate();
            UserPreferencesState.Reset();
        }
    }

    [TestMethod]
    public async Task ReplayPlayerPictures_PreferenceDisabled_DoesNotRequestPhotos()
    {
        using PictureHost host = new PictureHost();
        ClientSession session = CreateSession();
        UserPreferencesState.Reset();
        MatchReplayViewModel model = CreateReplay(false);
        try
        {
            await model.PlayerPictures.LoadPlayerPictures(host.CreateProfiles(), session);

            Assert.AreEqual(0, host.Service.ImageReads);
        }
        finally
        {
            model.PlayerPictures.Deactivate();
            UserPreferencesState.Reset();
        }
    }

    [TestMethod]
    public async Task ActivateMatch_GuestPlayer_CanPlayWithoutChat()
    {
        using PictureHost host = new PictureHost();
        using GameplayClient gameplay = host.CreateGameplay();
        using MatchChatClient chat = host.CreateChat();
        GameViewModel model = new GameViewModel(gameplay, chat, MatchId, true,
            new SynchronizationContext());
        try
        {
            await model.ActivateMatch();

            Assert.IsTrue(model.IsParticipant);
            Assert.IsTrue(model.CanForfeit);
            Assert.IsFalse(model.IsChatVisible);
        }
        finally
        {
            await model.DeactivateMatch();
        }
    }

    private static MatchReplayViewModel CreateReplay(bool systemOpponent)
    {
        const int BlackSeat = 1;
        const int WhiteSeat = 2;
        const int BoardCellCount = 64;
        MatchRecord match = new MatchRecord
        {
            MatchId = MatchId,
            Participants = new[]
            {
                new MatchParticipant(BlackSeat, "Local") { AccountId = LocalAccount },
                new MatchParticipant(WhiteSeat, "Opponent")
                {
                    AccountId = systemOpponent ? null : OpponentAccount,
                    IsSystem = systemOpponent
                }
            }
        };
        MatchRecording recording = new MatchRecording(match, Array.Empty<MatchMove>(),
            new[] { new ConfirmedBoardCell[BoardCellCount] });
        return new MatchReplayViewModel(recording);
    }

    private static ClientSession CreateSession()
    {
        ClientSession session = new ClientSession();
        session.StartAuthenticated(new AuthenticatedAccountSession(SessionToken, LocalAccount,
            "Local", "local@example.test", "en-US", DateTime.UtcNow.AddHours(1)));
        UserPreferencesState.ApplyPreferences(new PlayerPreferences
        {
            PieceStyleCode = PlayerPreferences.ProfilePicturesPieceStyle
        });
        return session;
    }

    [ServiceContract(Name = "IGameplayMatchService", Namespace = "urn:reversi:services:v1",
        CallbackContract = typeof(Proxy.IGameplayMatchServiceCallback))]
    internal interface IPictureGameplay
    {
        [OperationContract]
        void OpenMatchChannel(string sessionToken, long matchId);
        [OperationContract]
        Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId);
        [OperationContract]
        Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId);
    }

    [ServiceContract(Name = "IMatchChatService", Namespace = "urn:reversi:services:v1",
        CallbackContract = typeof(Proxy.IMatchChatServiceCallback))]
    internal interface IPictureChat
    {
        [OperationContract]
        void OpenChatChannel(string sessionToken, long matchId);
        [OperationContract]
        Proxy.MatchChatEntry[] GetChatHistory(string sessionToken, long matchId);
    }

    [ServiceContract(Name = "IPlayerProfileService", Namespace = "urn:reversi:services:v1")]
    internal interface IPictureProfiles
    {
        [OperationContract]
        Proxy.PlayerProfile GetPlayerProfile(string sessionToken, long? targetAccountId);
        [OperationContract]
        Proxy.ImageContent GetProfileImage(string sessionToken, long? targetAccountId);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    internal sealed class PlayerPictureServiceStub
        : IPictureGameplay, IPictureChat, IPictureProfiles
    {
        internal bool OpponentHasImage { get; set; } = true;
        internal int ImageReads { get; private set; }
        internal Action? BeforeImageResponse { get; set; }

        public void OpenMatchChannel(string sessionToken, long matchId)
        {
            _ = sessionToken;
            _ = matchId;
        }

        public Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId)
        {
            return new Proxy.MatchInformation
            {
                MatchId = matchId,
                AssignedSeatNumber = 1,
                TurnDurationSeconds = 30,
                Participants = new[]
                {
                    new Proxy.MatchParticipantSummary
                    {
                        SeatNumber = 1, AccountId = LocalAccount, DisplayName = "Local"
                    },
                    new Proxy.MatchParticipantSummary
                    {
                        SeatNumber = 2, AccountId = OpponentAccount, DisplayName = "Opponent"
                    }
                }
            };
        }

        public Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
        {
            return new Proxy.MatchStateSnapshot
            {
                MatchId = matchId,
                MatchStateVersion = 1,
                State = Proxy.MatchState.InProgress,
                Board = new Proxy.BoardCellState[64]
            };
        }

        public void OpenChatChannel(string sessionToken, long matchId)
        {
            _ = sessionToken;
            _ = matchId;
        }

        public Proxy.MatchChatEntry[] GetChatHistory(string sessionToken, long matchId)
        {
            return Array.Empty<Proxy.MatchChatEntry>();
        }

        public Proxy.PlayerProfile GetPlayerProfile(string sessionToken, long? targetAccountId)
        {
            return new Proxy.PlayerProfile
            {
                Account = new Proxy.AccountReference { AccountId = targetAccountId ?? LocalAccount },
                Statistics = new Proxy.PlayerStatistics(),
                HasProfileImage = targetAccountId != OpponentAccount || OpponentHasImage
            };
        }

        public Proxy.ImageContent GetProfileImage(string sessionToken, long? targetAccountId)
        {
            ImageReads++;
            BeforeImageResponse?.Invoke();
            byte[] pixel = targetAccountId == LocalAccount
                ? new byte[] { 0, 0, 255, 255 } : new byte[] { 255, 0, 0, 255 };
            BitmapSource source = BitmapSource.Create(1, 1, Resolution, Resolution,
                PixelFormats.Bgra32, null, pixel, pixel.Length);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                return new Proxy.ImageContent { Content = stream.ToArray(),
                    MediaType = "image/png" };
            }
        }
    }

    private sealed class PictureHost : IDisposable
    {
        private readonly ServiceHost _host;
        private readonly string _address;
        internal PlayerPictureServiceStub Service { get; } = new PlayerPictureServiceStub();

        internal PictureHost()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            _address = "net.tcp://localhost:" + port + "/PlayerPictures/";
            _host = new ServiceHost(Service);
            _host.AddServiceEndpoint(typeof(IPictureGameplay),
                new NetTcpBinding(SecurityMode.None), _address + "Gameplay");
            _host.AddServiceEndpoint(typeof(IPictureChat),
                new NetTcpBinding(SecurityMode.None), _address + "Chat");
            _host.AddServiceEndpoint(typeof(IPictureProfiles),
                new NetTcpBinding(SecurityMode.None), _address + "Profiles");
            _host.Open();
        }

        internal GameplayClient CreateGameplay()
        {
            return new GameplayClient(SessionToken, _address + "Gameplay",
                NullLogger<GameplayClient>.Instance);
        }

        internal MatchChatClient CreateChat()
        {
            return new MatchChatClient(SessionToken, _address + "Chat",
                NullLogger<MatchChatClient>.Instance);
        }

        internal PlayerProfileClient CreateProfiles()
        {
            return new PlayerProfileClient(NullLogger<PlayerProfileClient>.Instance,
                () => new Proxy.PlayerProfileServiceClient(new NetTcpBinding(SecurityMode.None),
                    new EndpointAddress(_address + "Profiles")));
        }

        public void Dispose()
        {
            _host.Abort();
        }
    }
}
