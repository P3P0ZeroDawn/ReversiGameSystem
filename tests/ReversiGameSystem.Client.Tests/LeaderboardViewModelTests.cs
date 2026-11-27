using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Features.Leaderboards;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LeaderboardViewModelTests
{
    [TestMethod]
    [DataRow("en-US", "4 wins")]
    [DataRow("es-MX", "4 victorias")]
    public async Task LoadLeaderboard_PublicTransport_FormatsConfirmedRows(string culture, string expectedWins)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        using LeaderboardHost host = new LeaderboardHost();
        try
        {
            LocalizationManager.Instance.TrySetCulture(culture);
            LeaderboardClient client = host.CreateClient();
            LeaderboardViewModel model = new LeaderboardViewModel(client);
            model.Activate();
            LeaderboardStatus status = await model.LoadLeaderboard(10);
            Assert.AreEqual(LeaderboardStatus.Success, status);
            Assert.AreEqual("#1", model.Rows[0].Position);
            Assert.AreEqual("Player", model.Rows[0].Username);
            Assert.AreEqual(expectedWins, model.Rows[0].WinsText);
            Assert.AreEqual(7L, client.ReadLeaderboard()[0].AccountId);
            LeaderboardEntry[] copy = client.ReadLeaderboard();
            copy[0] = new LeaderboardEntry(9, 2, "Other", 0);
            Assert.AreEqual(7L, client.ReadLeaderboard()[0].AccountId);
        }
        finally
        {
            LocalizationManager.Instance.TrySetCulture(previousCulture);
        }
    }

    [TestMethod]
    public async Task LoadLeaderboard_DeactivatedDuringTransport_DiscardsRowsAndCanReactivate()
    {
        using LeaderboardHost host = new LeaderboardHost();
        LeaderboardViewModel model = new LeaderboardViewModel(host.CreateClient());
        model.Activate();
        host.Service.BeforeResponse = model.Deactivate;
        LeaderboardStatus discarded = await model.LoadLeaderboard(10);
        Assert.AreEqual(LeaderboardStatus.Inactive, discarded);
        Assert.AreEqual(0, model.Rows.Length);
        Assert.IsFalse(model.IsLoaded);
        Assert.IsFalse(model.IsBusy);
        host.Service.BeforeResponse = null;
        model.Activate();
        Assert.AreEqual(LeaderboardStatus.Success, await model.LoadLeaderboard(10));
        Assert.AreEqual(1, model.Rows.Length);
    }

    [TestMethod]
    public async Task LoadLeaderboard_TechnicalFault_PropagatesAndResetsBusy()
    {
        using LeaderboardHost host = new LeaderboardHost();
        host.Service.TechnicalFailure = true;
        LeaderboardViewModel model = new LeaderboardViewModel(host.CreateClient());
        model.Activate();
        await Assert.ThrowsExactlyAsync<FaultException<Proxy.ServiceFault>>(
            () => model.LoadLeaderboard(10));
        Assert.IsFalse(model.IsBusy);
        Assert.IsFalse(model.IsLoaded);
    }

    [TestMethod]
    public async Task LoadLeaderboard_EmptyTransportSnapshot_MarksLoaded()
    {
        using LeaderboardHost host = new LeaderboardHost();
        host.Service.Entries = Array.Empty<Proxy.LeaderboardEntry>();
        LeaderboardViewModel model = new LeaderboardViewModel(host.CreateClient());
        model.Activate();
        Assert.AreEqual(LeaderboardStatus.Success, await model.LoadLeaderboard(10));
        Assert.IsTrue(model.IsLoaded);
        Assert.AreEqual(0, model.Rows.Length);
    }

    [TestMethod]
    public async Task LoadLeaderboard_SecondCallWhileTransportPending_ReturnsBusy()
    {
        using LeaderboardHost host = new LeaderboardHost();
        using ManualResetEventSlim release = new ManualResetEventSlim();
        TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        host.Service.BeforeResponse = () =>
        {
            entered.SetResult(true);
            if (!release.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The leaderboard test response was not released.");
            }
        };
        LeaderboardViewModel model = new LeaderboardViewModel(host.CreateClient());
        model.Activate();
        Task<LeaderboardStatus> pending = model.LoadLeaderboard(10);
        try
        {
            await entered.Task;
            Assert.AreEqual(LeaderboardStatus.Busy, await model.LoadLeaderboard(10));
            Assert.AreEqual(1, host.Service.Reads);
        }
        finally
        {
            release.Set();
            await pending;
        }
    }

    [TestMethod]
    public async Task LoadLeaderboard_Inactive_DoesNotSendRequest()
    {
        using LeaderboardHost host = new LeaderboardHost();
        LeaderboardViewModel model = new LeaderboardViewModel(host.CreateClient());
        Assert.AreEqual(LeaderboardStatus.Inactive, await model.LoadLeaderboard(10));
        Assert.AreEqual(0, host.Service.Reads);
    }

    [TestMethod]
    public async Task LoadLeaderboard_RejectedLimit_PreservesConfirmedSnapshot()
    {
        using LeaderboardHost host = new LeaderboardHost();
        LeaderboardClient client = host.CreateClient();
        await client.LoadLeaderboard(10);
        LeaderboardStatus status = await client.LoadLeaderboard(0);
        Assert.AreEqual(LeaderboardStatus.InvalidMaximumEntries, status);
        Assert.AreEqual(7L, client.ReadLeaderboard()[0].AccountId);
    }

    [ServiceContract(Name = "ILeaderboardService", Namespace = "urn:reversi:services:v1")]
    public interface ILeaderboardTestService
    {
        [OperationContract(Action = "urn:reversi:services:v1/ILeaderboardService/GetLeaderboard",
            ReplyAction = "urn:reversi:services:v1/ILeaderboardService/GetLeaderboardResponse")]
        [FaultContract(typeof(Proxy.LeaderboardFault),
            Action = "urn:reversi:services:v1/ILeaderboardService/GetLeaderboardLeaderboardFaultFault",
            Name = "LeaderboardFault", Namespace = "urn:reversi:contracts:v1")]
        [FaultContract(typeof(Proxy.ServiceFault),
            Action = "urn:reversi:services:v1/ILeaderboardService/GetLeaderboardServiceFaultFault",
            Name = "ServiceFault", Namespace = "urn:reversi:diagnostics:v1")]
        Proxy.LeaderboardEntry[] GetLeaderboard(int maximumEntries);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public sealed class LeaderboardServiceStub : ILeaderboardTestService
    {
        internal Action? BeforeResponse { get; set; }
        internal bool TechnicalFailure { get; set; }
        internal int Reads { get; private set; }
        internal Proxy.LeaderboardEntry[] Entries { get; set; } = new[]
        {
            new Proxy.LeaderboardEntry
            {
                Account = new Proxy.AccountReference { AccountId = 7, Username = "Player" },
                Position = 1, Wins = 4
            }
        };

        public Proxy.LeaderboardEntry[] GetLeaderboard(int maximumEntries)
        {
            Reads++;
            if (TechnicalFailure)
            {
                throw new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault());
            }
            if (maximumEntries < 1)
            {
                throw new FaultException<Proxy.LeaderboardFault>(new Proxy.LeaderboardFault
                {
                    Reason = Proxy.LeaderboardFailure.InvalidMaximumEntries
                });
            }
            BeforeResponse?.Invoke();
            return Entries;
        }
    }

    private sealed class LeaderboardHost : IDisposable
    {
        private readonly ServiceHost _host;
        private readonly string _address;
        internal LeaderboardServiceStub Service { get; } = new LeaderboardServiceStub();

        internal LeaderboardHost()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            _address = "net.tcp://127.0.0.1:" + port + "/leaderboard-tests";
            _host = new ServiceHost(Service, new Uri(_address));
            _host.AddServiceEndpoint(typeof(ILeaderboardTestService),
                new NetTcpBinding(SecurityMode.None), string.Empty);
            _host.Open();
        }

        internal LeaderboardClient CreateClient()
        {
            return new LeaderboardClient(NullLogger<LeaderboardClient>.Instance,
                () => new Proxy.LeaderboardServiceClient(new NetTcpBinding(SecurityMode.None),
                    new EndpointAddress(_address)));
        }

        public void Dispose()
        {
            _host.Abort();
        }
    }
}
