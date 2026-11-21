using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class GameplayClientTransportTests
{
    private const long MatchId = 73;
    private const long StateVersion = 9;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    internal static void RestoreMatchConnection_FinishedMatch_LoadsFinalState()
    {
        using GameplayHost host = new GameplayHost();
        host.Service.IsFinished = true;
        using GameplayClient client = host.CreateClient();

        MatchStatus status = client.RestoreMatchConnection(MatchId, CancellationToken.None)
            .GetAwaiter().GetResult();

        TestAssert.AreEqual(MatchStatus.Success, status);
        TestAssert.AreEqual(ConfirmedMatchState.Finished, client.ReadMatchState(MatchId).State);
    }

    internal static void LoadMatchState_CancelledRequest_CanRestoreChannel()
    {
        using GameplayHost host = new GameplayHost();
        host.Service.DelayStateRead = true;
        using GameplayClient client = host.CreateClient();
        using CancellationTokenSource cancellation = new CancellationTokenSource(TestTimeout);
        Task<MatchStatus> loading = client.LoadMatchState(MatchId, cancellation.Token);
        bool cancelled = false;
        try
        {
            TestAssert.AreEqual(true, host.Service.StateReadStarted.Wait(TestTimeout));
            cancellation.Cancel();
            loading.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        finally
        {
            host.Service.DelayStateRead = false;
            host.Service.ContinueStateRead.Set();
        }

        TestAssert.AreEqual(true, cancelled);
        TestAssert.AreEqual(MatchStatus.Success,
            client.RestoreMatchConnection(MatchId, CancellationToken.None)
                .GetAwaiter().GetResult());
    }

    internal static void PlacePiece_ConfirmedVersion_PreservesPlacementAtRemoteBoundary()
    {
        using GameplayHost host = new GameplayHost();
        using GameplayClient client = host.CreateClient();
        MatchPlacement placement = new MatchPlacement
        {
            MatchId = MatchId,
            ExpectedVersion = StateVersion,
            Row = 2,
            Column = 3
        };

        MatchStatus status = client.PlacePiece(placement).GetAwaiter().GetResult();

        TestAssert.AreEqual(MatchStatus.Success, status);
        Proxy.PiecePlacement received = host.Service.Placement
            ?? throw new InvalidOperationException("The service did not receive the placement.");
        TestAssert.AreEqual(placement.ExpectedVersion, received.ExpectedMatchStateVersion);
        TestAssert.AreEqual(placement.Row, received.Row);
        TestAssert.AreEqual(placement.Column, received.Column);
    }

    [ServiceContract(Name = "IGameplayMatchService", Namespace = "urn:reversi:services:v1",
        CallbackContract = typeof(Proxy.IGameplayMatchServiceCallback))]
    internal interface IGameplayTestService
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = "urn:reversi:contracts:v1")]
        void OpenMatchChannel(string sessionToken, long matchId);

        [OperationContract]
        Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId);

        [OperationContract]
        Proxy.MatchStateSnapshot PlacePiece(string sessionToken, Proxy.PiecePlacement placement);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    internal sealed class GameplayService : IGameplayTestService, IDisposable
    {
        internal bool IsFinished { get; set; }
        internal volatile bool DelayStateRead;
        internal Proxy.PiecePlacement? Placement { get; private set; }
        internal ManualResetEventSlim StateReadStarted { get; } = new ManualResetEventSlim();
        internal ManualResetEventSlim ContinueStateRead { get; } = new ManualResetEventSlim();

        public void OpenMatchChannel(string sessionToken, long matchId)
        {
            if (IsFinished)
            {
                throw new FaultException<Proxy.MatchFault>(new Proxy.MatchFault
                {
                    Reason = Proxy.MatchFailure.MatchNotInProgress
                });
            }
        }

        public Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
        {
            if (DelayStateRead)
            {
                StateReadStarted.Set();
                if (!ContinueStateRead.Wait(TestTimeout))
                {
                    throw new TimeoutException("The test did not release the state read.");
                }
            }
            return new Proxy.MatchStateSnapshot
            {
                MatchId = matchId,
                MatchStateVersion = StateVersion,
                State = IsFinished ? Proxy.MatchState.Finished : Proxy.MatchState.InProgress,
                Board = Array.Empty<Proxy.BoardCellState>()
            };
        }

        public Proxy.MatchStateSnapshot PlacePiece(
            string sessionToken, Proxy.PiecePlacement placement)
        {
            Placement = placement;
            return GetMatchState(sessionToken, placement.MatchId);
        }

        public void Dispose()
        {
            StateReadStarted.Dispose();
            ContinueStateRead.Dispose();
        }
    }

    private sealed class GameplayHost : IDisposable
    {
        private readonly ServiceHost _host;
        private readonly string _address;
        internal GameplayService Service { get; } = new GameplayService();

        internal GameplayHost()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            _address = "net.tcp://localhost:" + port + "/GameplayClientTests";
            _host = new ServiceHost(Service);
            _host.AddServiceEndpoint(typeof(IGameplayTestService),
                new NetTcpBinding(SecurityMode.None), _address);
            _host.Open();
        }

        internal GameplayClient CreateClient()
        {
            return new GameplayClient("transport-test-session", _address,
                NullLogger<GameplayClient>.Instance);
        }

        public void Dispose()
        {
            Service.ContinueStateRead.Set();
            _host.Abort();
            Service.Dispose();
        }
    }
}
