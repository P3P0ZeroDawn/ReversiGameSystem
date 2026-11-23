using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class PlayerPreferencesClientTests
{
    private const string ServiceNamespace = "urn:reversi:services:v1";
    private const string ContractNamespace = "urn:reversi:contracts:v1";
    private const string DiagnosticsNamespace = "urn:reversi:diagnostics:v1";
    private const string SessionToken = "preferences-test-session";
    private const string SavedLocale = "es-MX";
    private const string RequestedLocale = "en-US";
    private const string SavedTimeZone = "UTC";
    private const string SavedPieceStyle = "STANDARD";
    private const string ServerCorrelation = "preferences-test-correlation";

    internal static void ChangeLanguage_ExistingPreferences_PreservesOtherSettings()
    {
        using PreferencesHost host = new PreferencesHost();
        PlayerPreferencesClient client = host.CreateClient();

        PreferencesStatus status = client.ChangeLanguage(SessionToken, RequestedLocale)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(PreferencesStatus.Success, status);
        TestAssert.AreEqual(RequestedLocale, host.Service.Preferences.PreferredLocaleCode);
        TestAssert.AreEqual(SavedTimeZone, host.Service.Preferences.TimeZoneId);
        TestAssert.AreEqual(SavedPieceStyle, host.Service.Preferences.PieceStyleCode);
        TestAssert.AreEqual(true, host.Service.Preferences.UseCustomBoardBackground);
    }

    internal static void ChangeLanguage_InvalidLocale_ReturnsDeclaredStatus()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.RejectLocale = true;
        PlayerPreferencesClient client = host.CreateClient();

        PreferencesStatus status = client.ChangeLanguage(SessionToken, RequestedLocale)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(PreferencesStatus.InvalidLocale, status);
    }

    internal static void ChangeLanguage_RejectedWrite_DoesNotRetry()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.RejectLocale = true;
        PlayerPreferencesClient client = host.CreateClient();

        client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult();

        TestAssert.AreEqual(1, host.Service.WriteAttempts);
    }

    internal static void ChangeLanguage_UnknownFault_PropagatesFailure()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.ThrowUnknownFault = true;
        PlayerPreferencesClient client = host.CreateClient();

        TestAssert.Throws<FaultException>(() =>
            client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult());
    }

    internal static void ChangeLanguage_ServiceFault_PreservesServerCorrelation()
    {
        using PreferencesHost host = new PreferencesHost();
        host.Service.ThrowServiceFault = true;
        PlayerPreferencesClient client = host.CreateClient();

        FaultException<Proxy.ServiceFault> fault =
            TestAssert.Throws<FaultException<Proxy.ServiceFault>>(() => client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult());

        TestAssert.AreEqual(ServerCorrelation, fault.Detail.CorrelationId);
    }

    internal static void ChangeLanguage_InvalidClientConfiguration_PropagatesFailure()
    {
        Proxy.PlayerPreferencesServiceClient CreateInvalidClient()
        {
            throw new InvalidOperationException("Invalid test client configuration.");
        }
        PlayerPreferencesClient client = new PlayerPreferencesClient(
            NullLogger<PlayerPreferencesClient>.Instance, CreateInvalidClient);

        TestAssert.Throws<InvalidOperationException>(() =>
            client.ChangeLanguage(SessionToken, RequestedLocale).GetAwaiter().GetResult());
    }

    internal static void CreatePreferencesClient_CustomHost_UsesPreferencesEndpoint()
    {
        const string TestHost = "10.0.0.8";
        const string ExpectedAddress =
            "net.tcp://10.0.0.8:8098/ReversiGameSystem/PlayerPreferences";
        string? previousHost = Environment.GetEnvironmentVariable(
            ServerConnectionFactory.ServerHostVariable);
        try
        {
            Environment.SetEnvironmentVariable(ServerConnectionFactory.ServerHostVariable, TestHost);

            using Proxy.PlayerPreferencesServiceClient client =
                ServerConnectionFactory.CreatePreferencesClient();

            TestAssert.AreEqual(ExpectedAddress, client.Endpoint.Address.Uri.AbsoluteUri);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ServerConnectionFactory.ServerHostVariable, previousHost);
        }
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
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    internal sealed class PreferencesService : IPreferencesTestService
    {
        internal bool RejectLocale { get; set; }
        internal bool ThrowUnknownFault { get; set; }
        internal bool ThrowServiceFault { get; set; }
        internal int WriteAttempts { get; private set; }
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
            _host.Abort();
        }
    }
}
