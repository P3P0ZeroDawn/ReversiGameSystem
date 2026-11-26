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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class GameplayClientTransportTests
{
    private const long MatchId = 73;
    private const long StateVersion = 9;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [TestMethod]
    public void RestoreMatchConnection_FinishedMatch_LoadsFinalState()
    {
        using GameplayHost host = new GameplayHost();
        host.Service.IsFinished = true;
        using GameplayClient client = host.CreateClient();

        MatchStatus status = client.RestoreMatchConnection(MatchId, CancellationToken.None)
            .GetAwaiter().GetResult();

        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(ConfirmedMatchState.Finished, client.ReadMatchState(MatchId).State);
    }

    [TestMethod]
    public void LoadMatchState_CancelledRequest_CanRestoreChannel()
    {
        using GameplayHost host = new GameplayHost();
        host.Service.DelayStateRead = true;
        using GameplayClient client = host.CreateClient();
        using CancellationTokenSource cancellation = new CancellationTokenSource(TestTimeout);
        Task<MatchStatus> loading = client.LoadMatchState(MatchId, cancellation.Token);
        bool cancelled = false;
        try
        {
            Assert.AreEqual(true, host.Service.StateReadStarted.Wait(TestTimeout));
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

        Assert.AreEqual(true, cancelled);
        Assert.AreEqual(MatchStatus.Success,
            client.RestoreMatchConnection(MatchId, CancellationToken.None)
                .GetAwaiter().GetResult());
    }

    [TestMethod]
    public void PlacePiece_ConfirmedVersion_PreservesPlacementAtRemoteBoundary()
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

        Assert.AreEqual(MatchStatus.Success, status);
        Proxy.PiecePlacement received = host.Service.Placement
            ?? throw new InvalidOperationException("The service did not receive the placement.");
        Assert.AreEqual(placement.ExpectedVersion, received.ExpectedMatchStateVersion);
        Assert.AreEqual(placement.Row, received.Row);
        Assert.AreEqual(placement.Column, received.Column);
    }

    [TestMethod]
    public void Callback_ProcessingFailure_ReachesErrorBoundary()
    {
        using GameplayHost host = new GameplayHost();
        host.Service.PublishInvalidCallback = true;
        using GameplayClient client = host.CreateClient();
        using ManualResetEventSlim reported = new ManualResetEventSlim();
        Exception? captured = null;
        void OnFailure(Exception failure)
        {
            captured = failure;
            reported.Set();
        }
        CallbackErrorBehavior.FailureReported += OnFailure;
        try
        {
            client.LoadMatchState(MatchId).GetAwaiter().GetResult();
            Assert.AreEqual(true, reported.Wait(TestTimeout));
            Assert.AreEqual(true, captured is ArgumentNullException);
        }
        finally
        {
            CallbackErrorBehavior.FailureReported -= OnFailure;
        }
    }

    [TestMethod]
    public void LoadObservableMatches_RemoteList_StoresConfirmedMatchInformation()
    {
        using GameplayHost host = new GameplayHost();
        using GameplayClient client = host.CreateClient();

        MatchStatus status = client.LoadObservableMatches().GetAwaiter().GetResult();

        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(MatchId, client.ReadObservableMatches()[0].MatchId);
    }

    [TestMethod]
    public void ObserveMatch_ReadOnlyEntry_DoesNotOpenParticipantPresenceChannel()
    {
        using GameplayHost host = new GameplayHost();
        using GameplayClient client = host.CreateClient();

        MatchStatus status = client.ObserveMatch(MatchId).GetAwaiter().GetResult();

        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(1, host.Service.ObservationCalls);
        Assert.AreEqual(0, host.Service.ParticipantChannelCalls);
        Assert.IsNull(client.ReadMatchInformation(MatchId).AssignedSeatNumber);
    }

    [TestMethod]
    public void LoadMoveHistory_ConfirmedMoves_MapsDomainMoves()
    {
        using GameplayHost host = new GameplayHost();
        using GameplayClient client = host.CreateClient();

        MatchStatus status = client.LoadMoveHistory(MatchId).GetAwaiter().GetResult();

        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(StateVersion, client.ReadMoveHistory(MatchId)[0].MoveNumber);
        Assert.IsTrue(client.ReadMoveHistory(MatchId)[0].IsAutomatic);
    }

    [TestMethod]
    public void RestoreMatchConnection_ObserverReconnect_ReopensObservationOnly()
    {
        using GameplayHost host = new GameplayHost();
        using GameplayClient client = host.CreateClient();
        using ManualResetEventSlim disconnected = new ManualResetEventSlim();
        client.ConnectionLost += disconnected.Set;
        Assert.AreEqual(MatchStatus.Success, client.ObserveMatch(MatchId).GetAwaiter().GetResult());
        host.Service.AbortObserverChannel();
        Assert.IsTrue(disconnected.Wait(TestTimeout));

        MatchStatus status = client.RestoreMatchConnection(MatchId, CancellationToken.None)
            .GetAwaiter().GetResult();

        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(2, host.Service.ObservationCalls);
        Assert.AreEqual(0, host.Service.ParticipantChannelCalls);
    }

    [TestMethod]
    public void RestoreMatchConnection_ObservedMatchFinished_LoadsFinalState()
    {
        using GameplayHost host = new GameplayHost();
        using GameplayClient client = host.CreateClient();
        using ManualResetEventSlim disconnected = new ManualResetEventSlim();
        client.ConnectionLost += disconnected.Set;
        Assert.AreEqual(MatchStatus.Success, client.ObserveMatch(MatchId).GetAwaiter().GetResult());
        host.Service.IsFinished = true;
        host.Service.AbortObserverChannel();
        Assert.IsTrue(disconnected.Wait(TestTimeout));

        MatchStatus status = client.RestoreMatchConnection(MatchId, CancellationToken.None)
            .GetAwaiter().GetResult();

        Assert.AreEqual(MatchStatus.Success, status);
        Assert.AreEqual(ConfirmedMatchState.Finished, client.ReadMatchState(MatchId).State);
        Assert.AreEqual(0, host.Service.ParticipantChannelCalls);
    }

    [ServiceContract(Name = "IGameplayMatchService", Namespace = "urn:reversi:services:v1",
        CallbackContract = typeof(Proxy.IGameplayMatchServiceCallback))]
    internal interface IGameplayTestService
    {
        [OperationContract]
        Proxy.MatchInformation[] GetObservableMatches(string sessionToken);

        [OperationContract]
        [FaultContract(typeof(Proxy.MatchFault), Namespace = "urn:reversi:contracts:v1")]
        Proxy.MatchStateSnapshot ObserveMatch(string sessionToken, long matchId);

        [OperationContract]
        Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId);

        [OperationContract]
        Proxy.MoveRecord[] GetMoveHistory(string sessionToken, long matchId);

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
        private ICommunicationObject? _observerChannel;

        internal int ObservationCalls { get; private set; }
        internal int ParticipantChannelCalls { get; private set; }
        internal bool IsFinished { get; set; }
        internal bool PublishInvalidCallback { get; set; }
        internal volatile bool DelayStateRead;
        internal Proxy.PiecePlacement? Placement { get; private set; }
        internal ManualResetEventSlim StateReadStarted { get; } = new ManualResetEventSlim();
        internal ManualResetEventSlim ContinueStateRead { get; } = new ManualResetEventSlim();

        public Proxy.MatchInformation[] GetObservableMatches(string sessionToken)
        {
            return new[] { GetMatchInformation(sessionToken, MatchId) };
        }

        public Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId)
        {
            return new Proxy.MatchInformation
            {
                MatchId = matchId,
                TurnDurationSeconds = 30,
                AssignedSeatNumber = null,
                Participants = Array.Empty<Proxy.MatchParticipantSummary>()
            };
        }

        public Proxy.MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
        {
            return new[]
            {
                new Proxy.MoveRecord
                {
                    MoveNumber = (int)StateVersion,
                    Origin = Proxy.MoveOrigin.System
                }
            };
        }

        public Proxy.MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
        {
            ObservationCalls++;
            _observerChannel = OperationContext.Current.Channel;
            if (IsFinished)
            {
                throw new FaultException<Proxy.MatchFault>(new Proxy.MatchFault
                {
                    Reason = Proxy.MatchFailure.SpectatingUnavailable
                });
            }
            return GetMatchState(sessionToken, matchId);
        }

        internal void AbortObserverChannel()
        {
            _observerChannel?.Abort();
        }

        public void OpenMatchChannel(string sessionToken, long matchId)
        {
            ParticipantChannelCalls++;
            if (PublishInvalidCallback)
            {
                OperationContext.Current.GetCallbackChannel<Proxy.IGameplayMatchServiceCallback>()
                    .OnMatchStateChanged(new Proxy.MatchStateSnapshot { MatchId = matchId });
            }
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
                MatchStateVersion = IsFinished ? StateVersion + 1 : StateVersion,
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
