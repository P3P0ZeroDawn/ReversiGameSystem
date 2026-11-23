using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Features.Gameplay;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class GameViewModelRecoveryTests
{
    private const long MatchId = 19;
    private const long StateVersion = 1;
    private const string ServerCorrelation = "recovery-server-reference";
    private static readonly TimeSpan FailureTimeout = TimeSpan.FromSeconds(5);

    internal static void ConnectionLost_UnexpectedRecoveryFault_ReportsBeforeViewDeactivation()
    {
        RecoveryService service = new RecoveryService();
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        string address = "net.tcp://localhost:" + port + "/RecoveryFailureTest";
        using ServiceHost host = new ServiceHost(service);
        host.AddServiceEndpoint(typeof(IRecoveryTestService),
            new NetTcpBinding(SecurityMode.None), address);
        host.Open();
        using GameplayClient gameplay = new GameplayClient("test-session", address,
            NullLogger<GameplayClient>.Instance);
        using MatchChatClient chat = MatchClientsTests.CreateChat("unused");
        using ManualResetEventSlim reported = new ManualResetEventSlim();
        GameViewModel model = new GameViewModel(gameplay, chat, MatchId, true,
            new ImmediateContext());
        Exception? failure = null;
        void CaptureFailure(Exception exception)
        {
            failure = exception;
            reported.Set();
        }
        model.UnexpectedRecoveryFailure += CaptureFailure;
        try
        {
            model.ActivateMatch().GetAwaiter().GetResult();
            service.RejectOpening = true;
            service.DisconnectNextRead = true;

            gameplay.LoadMatchState(MatchId, CancellationToken.None).GetAwaiter().GetResult();

            TestAssert.AreEqual(true, reported.Wait(FailureTimeout));
            TestAssert.AreEqual(true, failure is FaultException<Proxy.ServiceFault>);
            FaultException<Proxy.ServiceFault> serviceFault =
                (FaultException<Proxy.ServiceFault>)(failure
                    ?? throw new InvalidOperationException("The failure was not reported."));
            TestAssert.AreEqual(ServerCorrelation, serviceFault.Detail.CorrelationId);
            TestAssert.AreEqual(false, model.RecoveryFailed);
            TestAssert.AreEqual(false, model.CanSendMessage);
        }
        finally
        {
            model.DeactivateMatch().GetAwaiter().GetResult();
            host.Abort();
        }
    }

    [ServiceContract(Name = "IGameplayMatchService", Namespace = "urn:reversi:services:v1",
        CallbackContract = typeof(Proxy.IGameplayMatchServiceCallback))]
    internal interface IRecoveryTestService
    {
        [OperationContract]
        [FaultContract(typeof(Proxy.ServiceFault), Namespace = "urn:reversi:diagnostics:v1")]
        void OpenMatchChannel(string sessionToken, long matchId);

        [OperationContract]
        Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId);

        [OperationContract]
        Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    internal sealed class RecoveryService : IRecoveryTestService
    {
        internal volatile bool DisconnectNextRead;
        internal volatile bool RejectOpening;

        public void OpenMatchChannel(string sessionToken, long matchId)
        {
            if (RejectOpening)
            {
                throw new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault
                {
                    CorrelationId = ServerCorrelation
                });
            }
        }

        public Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId)
        {
            return new Proxy.MatchInformation
            {
                MatchId = matchId,
                Participants = Array.Empty<Proxy.MatchParticipantSummary>(),
                TurnDurationSeconds = 40
            };
        }

        public Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
        {
            if (DisconnectNextRead)
            {
                DisconnectNextRead = false;
                OperationContext.Current.Channel.Abort();
            }
            return new Proxy.MatchStateSnapshot
            {
                MatchId = matchId,
                MatchStateVersion = StateVersion,
                State = Proxy.MatchState.InProgress,
                Board = Array.Empty<Proxy.BoardCellState>()
            };
        }
    }

    private sealed class ImmediateContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            d(state);
        }
    }
}
