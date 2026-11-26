using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class PlayerPreferencesClientTests
{
    private const string ServiceNamespace = "urn:reversi:services:v1";
    private const string ContractNamespace = "urn:reversi:contracts:v1";
    private const string DiagnosticsNamespace = "urn:reversi:diagnostics:v1";
    private const string SessionToken = "preferences-test-session";
    private const string SavedLocale = "es-MX";
    private const string RequestedLocale = "en-US";
    private const string SavedTimeZone = "UTC";
    private const string SavedPieceStyle = "STANDARD";
    private const string OtherSessionToken = "other-preferences-test-session";
    private const string JpegMediaType = "image/jpeg";
    private const string PngMediaType = "image/png";
    private const string ServerCorrelation = "preferences-test-correlation";
    private static readonly TimeSpan PendingReadTimeout = TimeSpan.FromSeconds(5);

    [TestMethod]
    public void ChangeLanguage_ExistingPreferences_PreservesOtherSettings()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();

        PreferencesStatus status = client.ChangeLanguage(SessionToken, RequestedLocale)
            .GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.Success, status);
        Assert.AreEqual(RequestedLocale, host.Service.Preferences.PreferredLocaleCode);
        Assert.AreEqual(SavedTimeZone, host.Service.Preferences.TimeZoneId);
        Assert.AreEqual(SavedPieceStyle, host.Service.Preferences.PieceStyleCode);
        Assert.IsTrue(host.Service.Preferences.UseCustomBoardBackground);
    }

    [TestMethod]
    public void ChangeLanguage_InvalidLocale_ReturnsDeclaredStatus()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.RejectLocale = true;
        PlayerPreferencesClient client = host.CreateClient();

        PreferencesStatus status = client.ChangeLanguage(SessionToken, RequestedLocale)
            .GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.InvalidLocale, status);
    }

    [TestMethod]
    public void ChangeLanguage_RejectedWrite_DoesNotRetry()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.RejectLocale = true;
        PlayerPreferencesClient client = host.CreateClient();

        client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult();

        Assert.AreEqual(1, host.Service.WriteAttempts);
    }

    [TestMethod]
    public void ChangeLanguage_UnknownFault_PropagatesFailure()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.ThrowUnknownFault = true;
        PlayerPreferencesClient client = host.CreateClient();

        Assert.Throws<FaultException>(() =>
            client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult());
    }

    [TestMethod]
    public void ChangeLanguage_ServiceFault_PreservesServerCorrelation()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.ThrowServiceFault = true;
        PlayerPreferencesClient client = host.CreateClient();

        FaultException<Proxy.ServiceFault> fault =
            Assert.Throws<FaultException<Proxy.ServiceFault>>(() =>
                client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult());

        Assert.AreEqual(ServerCorrelation, fault.Detail.CorrelationId);
    }

    [TestMethod]
    public void ChangeLanguage_InvalidClientConfiguration_PropagatesFailure()
    {
        Proxy.PlayerPreferencesServiceClient CreateInvalidClient()
        {
            throw new InvalidOperationException("Invalid test client configuration.");
        }
        PlayerPreferencesClient client = new PlayerPreferencesClient(
            NullLogger<PlayerPreferencesClient>.Instance, CreateInvalidClient);

        Assert.Throws<InvalidOperationException>(() =>
            client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult());
    }

    [TestMethod]
    public void CreatePreferencesClient_CustomHost_UsesPreferencesEndpoint()
    {
        const string TestHost = "10.0.0.8";
        const string ExpectedAddress =
            "net.tcp://10.0.0.8:8098/ReversiGameSystem/PlayerPreferences";
        string? previousHost = Environment.GetEnvironmentVariable(
            ServerConnectionFactory.ServerHostVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable, TestHost);

            using Proxy.PlayerPreferencesServiceClient client =
                ServerConnectionFactory.CreatePreferencesClient();

            Assert.AreEqual(ExpectedAddress, client.Endpoint.Address.Uri.AbsoluteUri);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable, previousHost);
        }
    }

    [TestMethod]
    public void LoadPreferences_StoredSettings_ConfirmsServerValues()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();

        PreferencesStatus status = client.LoadPreferences(SessionToken).GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.Success, status);
        Assert.AreEqual(SavedLocale, client.ReadPreferences().PreferredLocaleCode);
    }

    [TestMethod]
    public void ReadPreferences_ReturnedObjectChanges_DoNotModifyConfirmedSettings()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();
        client.LoadPreferences(SessionToken).GetAwaiter().GetResult();
        PlayerPreferences preferences = client.ReadPreferences();

        preferences.PreferredLocaleCode = RequestedLocale;

        Assert.AreEqual(SavedLocale, client.ReadPreferences().PreferredLocaleCode);
    }

    [TestMethod]
    public void UpdatePreferences_ValidSettings_PersistsAppearanceAndTimeZone()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();
        PlayerPreferences preferences = new PlayerPreferences
        {
            PreferredLocaleCode = RequestedLocale,
            TimeZoneId = null,
            PieceStyleCode = PlayerPreferences.ProfilePicturesPieceStyle,
            UseCustomBoardBackground = false
        };

        PreferencesStatus status = client.UpdatePreferences(SessionToken, preferences)
            .GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.Success, status);
        Assert.AreEqual(preferences.PieceStyleCode, host.Service.Preferences.PieceStyleCode);
        Assert.IsFalse(client.ReadPreferences().UseCustomBoardBackground);
        Assert.IsNull(client.ReadPreferences().TimeZoneId);
    }

    [TestMethod]
    public void UpdatePreferences_RejectedSettings_PreservesConfirmedSettings()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();
        client.LoadPreferences(SessionToken).GetAwaiter().GetResult();
        host.Service.RejectLocale = true;
        PlayerPreferences preferences = client.ReadPreferences();
        preferences.PreferredLocaleCode = RequestedLocale;

        client.UpdatePreferences(SessionToken, preferences).GetAwaiter().GetResult();

        Assert.AreEqual(SavedLocale, client.ReadPreferences().PreferredLocaleCode);
    }

    [TestMethod]
    public void HasBoardBackground_BeforeLoad_RejectsUnknownState()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();

        Assert.Throws<InvalidOperationException>(() => _ = client.HasBoardBackground);
    }

    [TestMethod]
    public void LoadBoardBackground_NoStoredImage_ConfirmsAbsence()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();

        PreferencesStatus status = client.LoadBoardBackground(SessionToken)
            .GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.Success, status);
        Assert.IsFalse(client.HasBoardBackground);
    }

    [TestMethod]
    public void ReadBoardBackground_ConfirmedAbsence_RejectsImageRead()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();
        client.LoadBoardBackground(SessionToken).GetAwaiter().GetResult();

        Assert.Throws<InvalidOperationException>(() => client.ReadBoardBackground());
    }

    [TestMethod]
    public void ReadBoardBackground_ReturnedBytesChange_DoesNotModifyConfirmedImage()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.Background = new Proxy.ImageContent
        {
            Content = new byte[] { 1, 2, 3 },
            MediaType = JpegMediaType
        };
        PlayerPreferencesClient client = host.CreateClient();
        client.LoadBoardBackground(SessionToken).GetAwaiter().GetResult();

        client.ReadBoardBackground().Content[0] = byte.MaxValue;

        Assert.AreEqual((byte)1, client.ReadBoardBackground().Content[0]);
    }

    [TestMethod]
    public void ChangeBoardBackground_Success_CachesStoredRepresentation()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();
        PlayerImageContent image = new PlayerImageContent(new byte[] { 1, 2, 3 }, PngMediaType);

        PreferencesStatus status = client.ChangeBoardBackground(SessionToken, image)
            .GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.Success, status);
        Assert.AreEqual(JpegMediaType, client.ReadBoardBackground().MediaType);
        CollectionAssert.AreEqual(new byte[] { 4, 5, 6 },
            client.ReadBoardBackground().Content);
    }

    [TestMethod]
    public void ChangeBoardBackground_Success_EnablesConfirmedBackgroundPreference()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.Preferences.UseCustomBoardBackground = false;
        PlayerPreferencesClient client = host.CreateClient();
        client.LoadPreferences(SessionToken).GetAwaiter().GetResult();
        PlayerImageContent image = new PlayerImageContent(new byte[] { 1, 2, 3 }, PngMediaType);

        client.ChangeBoardBackground(SessionToken, image).GetAwaiter().GetResult();

        Assert.IsTrue(client.ReadPreferences().UseCustomBoardBackground);
    }

    [TestMethod]
    public void LoadPreferences_DifferentSession_DoesNotQueryServer()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();
        client.LoadPreferences(SessionToken).GetAwaiter().GetResult();

        PreferencesStatus status = client.LoadPreferences(OtherSessionToken)
            .GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.InvalidSession, status);
        Assert.AreEqual(1, host.Service.PreferenceReads);
    }

    [TestMethod]
    public void DiscardSession_PendingLoad_DoesNotRestoreConfirmedCache()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.DelayPreferencesRead = true;
        PlayerPreferencesClient client = host.CreateClient();
        Task<PreferencesStatus> pending = client.LoadPreferences(SessionToken);
        try
        {
            Assert.IsTrue(host.Service.ReadStarted.Wait(PendingReadTimeout));
            client.DiscardSession();
        }
        finally
        {
            host.Service.ContinueRead.Set();
        }

        PreferencesStatus status = pending.GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.InvalidSession, status);
        Assert.Throws<InvalidOperationException>(() => client.ReadPreferences());
    }

    [TestMethod]
    public void ChangeLanguage_DiscardedClient_DoesNotWrite()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();
        client.DiscardSession();

        PreferencesStatus status = client.ChangeLanguage(SessionToken, RequestedLocale)
            .GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.InvalidSession, status);
        Assert.AreEqual(0, host.Service.WriteAttempts);
    }

    [TestMethod]
    public void ChangeLanguage_Success_UpdatesConfirmedPreferences()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();

        client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult();

        Assert.AreEqual(RequestedLocale, client.ReadPreferences().PreferredLocaleCode);
    }

    [TestMethod]
    public void ChangeLanguage_DiscardDuringRead_DoesNotWrite()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.DelayPreferencesRead = true;
        PlayerPreferencesClient client = host.CreateClient();
        Task<PreferencesStatus> pending = client.ChangeLanguage(SessionToken, RequestedLocale);
        try
        {
            Assert.IsTrue(host.Service.ReadStarted.Wait(PendingReadTimeout));
            client.DiscardSession();
        }
        finally
        {
            host.Service.ContinueRead.Set();
        }

        PreferencesStatus status = pending.GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.InvalidSession, status);
        Assert.AreEqual(0, host.Service.WriteAttempts);
    }

    [TestMethod]
    public void ChangeBoardBackground_RejectedUpload_PreservesConfirmedImage()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.Background = new Proxy.ImageContent
        {
            Content = new byte[] { 1, 2, 3 },
            MediaType = JpegMediaType
        };
        host.Service.RejectImage = true;
        PlayerPreferencesClient client = host.CreateClient();
        client.LoadBoardBackground(SessionToken).GetAwaiter().GetResult();
        PlayerImageContent image = new PlayerImageContent(new byte[] { 4, 5, 6 }, PngMediaType);

        PreferencesStatus status = client.ChangeBoardBackground(SessionToken, image)
            .GetAwaiter().GetResult();

        Assert.AreEqual(PreferencesStatus.InvalidImage, status);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, client.ReadBoardBackground().Content);
    }

    [ServiceContract(Name = "IPlayerPreferencesService", Namespace = ServiceNamespace)]
    internal interface IPreferencesTestService
    {
        [OperationContract]
        Proxy.PlayerPreferences GetPlayerPreferences(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.PreferencesFault), Namespace = ContractNamespace)]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = DiagnosticsNamespace)]
        void UpdatePlayerPreferences(string sessionToken, Proxy.PlayerPreferences preferences);

        [OperationContract]
        Proxy.ImageContent? GetBoardBackground(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.PreferencesFault), Namespace = ContractNamespace)]
        void ChangeBoardBackground(string sessionToken, Proxy.ImageContent image);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    internal sealed class PreferencesService : IPreferencesTestService, IDisposable
    {
        internal bool RejectLocale { get; set; }

        internal bool RejectImage { get; set; }

        internal bool ThrowUnknownFault { get; set; }

        internal bool ThrowServiceFault { get; set; }

        internal int WriteAttempts { get; private set; }

        internal int PreferenceReads { get; private set; }

        internal volatile bool DelayPreferencesRead;
        internal Proxy.ImageContent? Background { get; set; }

        internal ManualResetEventSlim ReadStarted { get; } = new ManualResetEventSlim();

        internal ManualResetEventSlim ContinueRead { get; } = new ManualResetEventSlim();

        internal Proxy.PlayerPreferences Preferences { get; private set; } =
            new Proxy.PlayerPreferences
            {
                PreferredLocaleCode = SavedLocale,
                TimeZoneId = SavedTimeZone,
                PieceStyleCode = SavedPieceStyle,
                UseCustomBoardBackground = true
            };

        public Proxy.PlayerPreferences GetPlayerPreferences(string sessionToken)
        {
            PreferenceReads++;
            if (DelayPreferencesRead)
            {
                ReadStarted.Set();
                if (!ContinueRead.Wait(PendingReadTimeout))
                {
                    throw new TimeoutException("The preference test did not release its read.");
                }
            }
            return Preferences;
        }

        public void UpdatePlayerPreferences(
            string sessionToken, Proxy.PlayerPreferences preferences)
        {
            WriteAttempts++;
            if (RejectLocale)
            {
                throw new FaultException<Proxy.PreferencesFault>(new Proxy.PreferencesFault
                {
                    Reason = Proxy.PreferencesFailure.InvalidLocale
                });
            }
            if (ThrowUnknownFault)
            {
                throw new FaultException("An unknown test service fault.");
            }
            if (ThrowServiceFault)
            {
                throw new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault
                {
                    FailureCode = "UnexpectedFailure",
                    CorrelationId = ServerCorrelation
                });
            }
            Preferences = preferences;
        }

        public Proxy.ImageContent? GetBoardBackground(string sessionToken)
        {
            return Background;
        }

        public void ChangeBoardBackground(string sessionToken, Proxy.ImageContent image)
        {
            if (RejectImage)
            {
                throw new FaultException<Proxy.PreferencesFault>(new Proxy.PreferencesFault
                {
                    Reason = Proxy.PreferencesFailure.InvalidImage
                });
            }
            Background = new Proxy.ImageContent
            {
                Content = new byte[] { 4, 5, 6 },
                MediaType = JpegMediaType
            };
            Preferences.UseCustomBoardBackground = true;
        }

        public void Dispose()
        {
            ReadStarted.Dispose();
            ContinueRead.Dispose();
        }
    }

    private sealed class PreferencesHost : IDisposable
    {
        private const int DynamicPort = 0;
        private readonly ServiceHost _host;
        private readonly string _address;
        internal PreferencesService Service { get; } = new PreferencesService();

        internal PreferencesHost()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, DynamicPort);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            _address = "net.tcp://localhost:" + port + "/PlayerPreferencesTests";
            _host = new ServiceHost(Service);
            _host.AddServiceEndpoint(typeof(IPreferencesTestService),
                new NetTcpBinding(SecurityMode.None), _address);
            _host.Open();
        }

        internal PlayerPreferencesClient CreateClient()
        {
            Proxy.PlayerPreferencesServiceClient CreateProxy()
            {
                return new Proxy.PlayerPreferencesServiceClient(
                    new NetTcpBinding(SecurityMode.None),
                    new EndpointAddress(_address));
            }
            return new PlayerPreferencesClient(
                NullLogger<PlayerPreferencesClient>.Instance, CreateProxy);
        }

        public void Dispose()
        {
            Service.ContinueRead.Set();
            _host.Abort();
            Service.Dispose();
        }
    }
}
