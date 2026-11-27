using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Features.Profiles;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PlayerProfileViewModelTests
{
    private const long OwnAccountId = 11;
    private const long OtherAccountId = 22;
    private const string Token = "profile-test-session";
    private const double ImageResolution = 96;

    [TestMethod]
    public async Task LoadProfile_OwnAccount_UsesServerStatisticsWithoutExtraQuery()
    {
        using ProfileHost host = new ProfileHost();
        ClientSession session = CreateSession();
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, session);
        try
        {
            await model.LoadProfile();
            ProfileStatistics statistics = model.Statistics;

            Assert.AreEqual(4, statistics.MatchesPlayed);
            Assert.AreEqual(1, host.ProfileReads);
        }
        finally
        {
            model.Deactivate();
        }
    }

    [TestMethod]
    public async Task LoadProfile_SessionClearedDuringQuery_DiscardsData()
    {
        using ProfileHost host = new ProfileHost();
        ClientSession session = CreateSession();
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, session);
        host.BeforeProfileResponse = session.Clear;

        ProfileStatus status = await model.LoadProfile();

        Assert.AreEqual(ProfileStatus.InvalidSession, status);
        Assert.IsFalse(model.IsReady);
    }

    [TestMethod]
    public async Task LoadProfile_PageClosedDuringQuery_DiscardsData()
    {
        using ProfileHost host = new ProfileHost();
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, CreateSession());
        host.BeforeProfileResponse = model.Deactivate;

        await model.LoadProfile();

        Assert.IsFalse(model.IsReady);
    }

    [TestMethod]
    public async Task ChangeUsername_PublicProfile_CannotEditAnotherAccount()
    {
        using ProfileHost host = new ProfileHost();
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles,
            CreateSession(), OtherAccountId);
        try
        {
            await model.LoadProfile();

            ProfileStatus status = await model.ChangeUsername("NewName");

            Assert.AreEqual(ProfileStatus.Unauthorized, status);
            Assert.AreEqual(0, host.UsernameWrites);
        }
        finally
        {
            model.Deactivate();
        }
    }

    [TestMethod]
    public async Task ChangeUsername_OwnProfile_UpdatesSessionAfterServerConfirms()
    {
        using ProfileHost host = new ProfileHost();
        ClientSession session = CreateSession();
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, session);
        try
        {
            await model.LoadProfile();

            await model.ChangeUsername("NewName");

            Assert.AreEqual("NewName", session.Username);
            Assert.AreEqual(1, host.UsernameWrites);
        }
        finally
        {
            model.Deactivate();
        }
    }

    [TestMethod]
    public async Task ChangeUsername_InvalidText_DoesNotSendRequest()
    {
        using ProfileHost host = new ProfileHost();
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, CreateSession());
        try
        {
            await model.LoadProfile();

            await model.ChangeUsername("name with spaces");

            Assert.AreEqual(0, host.UsernameWrites);
        }
        finally
        {
            model.Deactivate();
        }
    }

    [TestMethod]
    public async Task LoadProfile_TechnicalFault_PropagatesAndRestoresBusyState()
    {
        using ProfileHost host = new ProfileHost();
        host.FailProfile = true;
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, CreateSession());
        try
        {
            await Assert.ThrowsExactlyAsync<FaultException<Proxy.ServiceFault>>(
                () => model.LoadProfile());
            Assert.IsFalse(model.IsBusy);
        }
        finally
        {
            model.Deactivate();
        }
    }

    [TestMethod]
    public async Task LoadProfileImage_NoPhoto_UsesDefaultWithoutNullResponseWrapper()
    {
        using ProfileHost host = new ProfileHost();

        await host.Profiles.LoadProfileImage();

        Assert.AreEqual(0, host.Profiles.ReadProfileImage().Content.Length);
        Assert.IsNull(PlayerImageService.CreatePreview(host.Profiles.ReadProfileImage()));
    }

    [TestMethod]
    public async Task LoadProfile_ConcurrentTargets_KeepsEachAccountSeparate()
    {
        using ProfileHost host = new ProfileHost();

        await Task.WhenAll(host.Profiles.LoadProfile(OwnAccountId),
            host.Profiles.LoadProfile(OtherAccountId));

        Assert.AreEqual(OwnAccountId, host.Profiles.ReadProfile(OwnAccountId).Account.AccountId);
        Assert.AreEqual(OtherAccountId, host.Profiles.ReadProfile(OtherAccountId).Account.AccountId);
    }

    [TestMethod]
    public async Task LoadProfile_SessionDiscardedDuringQuery_DoesNotRepopulateCache()
    {
        using ProfileHost host = new ProfileHost();
        host.BeforeProfileResponse = host.Profiles.DiscardSession;

        await host.Profiles.LoadProfile();

        Assert.ThrowsExactly<InvalidOperationException>(() => host.Profiles.ReadProfile());
    }

    [TestMethod]
    public async Task UpdateProfileImage_Success_ShowsServerEncodedImage()
    {
        using ProfileHost host = new ProfileHost();
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, CreateSession());
        string imagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(imagePath, CreateImage(new byte[] { 0, 0, 255, 255 }));
        try
        {
            await model.LoadProfile();

            await model.UpdateProfileImage(imagePath);

            byte[] pixel = new byte[4];
            model.Picture!.CopyPixels(new Int32Rect(0, 0, 1, 1), pixel, pixel.Length, 0);
            Assert.AreEqual((byte)255, pixel[0]);
        }
        finally
        {
            model.Deactivate();
            File.Delete(imagePath);
        }
    }

    [TestMethod]
    public async Task LoadProfile_RejectedSession_ReturnsInvalidSession()
    {
        using ProfileHost host = new ProfileHost();
        host.RejectSession = true;
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, CreateSession());
        try
        {
            ProfileStatus status = await model.LoadProfile();

            Assert.AreEqual(ProfileStatus.InvalidSession, status);
        }
        finally
        {
            model.Deactivate();
        }
    }

    [TestMethod]
    public async Task LoadProfile_WrongTargetPayload_RejectsAnotherAccountData()
    {
        using ProfileHost host = new ProfileHost();
        host.ReturnWrongAccount = true;

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => host.Profiles.LoadProfile(OtherAccountId));
    }

    [TestMethod]
    public void Email_AnotherAccountSignsIn_DoesNotExposeNewSessionData()
    {
        using ProfileHost host = new ProfileHost();
        ClientSession session = CreateSession();
        PlayerProfileViewModel model = new PlayerProfileViewModel(host.Profiles, session);

        session.StartAuthenticated(new AuthenticatedAccountSession("other-session", OtherAccountId,
            "Other", "other@example.test", "en-US", DateTime.UtcNow.AddHours(1)));

        Assert.AreEqual(string.Empty, model.Email);
    }

    private static ClientSession CreateSession()
    {
        ClientSession session = new ClientSession();
        session.StartAuthenticated(new AuthenticatedAccountSession(Token, OwnAccountId,
            "Local", "local@example.test", "en-US", DateTime.UtcNow.AddHours(1)));
        return session;
    }

    private static byte[] CreateImage(byte[] pixel)
    {
        BitmapSource source = BitmapSource.Create(1, 1, ImageResolution, ImageResolution,
            PixelFormats.Bgra32, null, pixel, pixel.Length);
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using MemoryStream stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    [ServiceContract(Name = "IPlayerProfileService", Namespace = "urn:reversi:services:v1")]
    internal interface IProfileTestService
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.ServiceFault))]
        [FaultContract(typeof(Proxy.SessionFault))]
        Proxy.PlayerProfile GetPlayerProfile(string sessionToken, long? targetAccountId);
        [OperationContract]
        Proxy.ImageContent? GetProfileImage(string sessionToken, long? targetAccountId);
        [OperationContract]
        void ChangeUsername(string sessionToken, string username);
        [OperationContract]
        void UpdateProfileImage(string sessionToken, Proxy.ImageContent image);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    private sealed class ProfileHost : IProfileTestService, IDisposable
    {
        private readonly ServiceHost _host;
        private string _username = "Local";
        private bool _hasImage;
        private readonly byte[] _confirmedImage = CreateImage(new byte[] { 255, 0, 0, 255 });
        internal Action? BeforeProfileResponse { get; set; }
        internal int ProfileReads { get; private set; }
        internal int UsernameWrites { get; private set; }
        internal bool FailProfile { get; set; }
        internal bool RejectSession { get; set; }
        internal bool ReturnWrongAccount { get; set; }
        internal PlayerProfileClient Profiles { get; }

        internal ProfileHost()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            string endpoint = "net.tcp://localhost:" + port + "/ProfileMigration";
            _host = new ServiceHost(this);
            _host.AddServiceEndpoint(typeof(IProfileTestService),
                new NetTcpBinding(SecurityMode.None), endpoint);
            _host.Open();
            Profiles = new PlayerProfileClient(Token, NullLogger<PlayerProfileClient>.Instance,
                () => new Proxy.PlayerProfileServiceClient(new NetTcpBinding(SecurityMode.None),
                    new EndpointAddress(endpoint)));
        }

        public Proxy.PlayerProfile GetPlayerProfile(string sessionToken, long? targetAccountId)
        {
            ProfileReads++;
            if (FailProfile)
            {
                throw new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault());
            }
            if (RejectSession)
            {
                throw new FaultException<Proxy.SessionFault>(new Proxy.SessionFault
                {
                    Reason = Proxy.SessionFailure.InvalidSession
                });
            }
            BeforeProfileResponse?.Invoke();
            return new Proxy.PlayerProfile
            {
                Account = new Proxy.AccountReference
                {
                    AccountId = ReturnWrongAccount ? OwnAccountId : targetAccountId ?? OwnAccountId,
                    Username = _username
                },
                HasProfileImage = _hasImage,
                Statistics = new Proxy.PlayerStatistics
                {
                    MatchesPlayed = 4, Wins = 1, Losses = 2, Draws = 1, WinRate = 0.25
                }
            };
        }

        public Proxy.ImageContent? GetProfileImage(string sessionToken, long? targetAccountId)
        {
            Proxy.ImageContent? image = null;
            if (_hasImage)
            {
                image = new Proxy.ImageContent { Content = _confirmedImage, MediaType = "image/png" };
            }
            return image;
        }

        public void ChangeUsername(string sessionToken, string username)
        {
            UsernameWrites++;
            _username = username;
        }

        public void UpdateProfileImage(string sessionToken, Proxy.ImageContent image)
        {
            _hasImage = true;
        }

        public void Dispose()
        {
            Profiles.DiscardSession();
            _host.Abort();
        }
    }
}
