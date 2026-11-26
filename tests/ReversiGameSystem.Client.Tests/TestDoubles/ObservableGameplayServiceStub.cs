using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests.TestDoubles;

[ServiceContract(Name = "IGameplayMatchService", Namespace = "urn:reversi:services:v1",
    CallbackContract = typeof(Proxy.IGameplayMatchServiceCallback))]
internal interface IObservableGameplayTestService
{
    [OperationContract]
    Proxy.MatchInformation[] GetObservableMatches(string sessionToken);
    [OperationContract]
    Proxy.MatchStateSnapshot ObserveMatch(string sessionToken, long matchId);
    [OperationContract]
    Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId);
    [OperationContract]
    Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId);
    [OperationContract]
    Proxy.MoveRecord[] GetMoveHistory(string sessionToken, long matchId);
}

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
internal sealed class ObservableGameplayServiceStub : IObservableGameplayTestService, IDisposable
{
    private readonly ServiceHost _host;
    internal ObservableGameplayServiceStub()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        string address = "net.tcp://localhost:" + port + "/ObservableGameplayTest";
        _host = new ServiceHost(this);
        _host.AddServiceEndpoint(typeof(IObservableGameplayTestService),
            new NetTcpBinding(SecurityMode.None), address);
        _host.Open();
        Gameplay = new GameplayClient("observer-test-session", address,
            NullLogger<GameplayClient>.Instance);
    }

    internal GameplayClient Gameplay { get; }
    internal int ListCalls { get; private set; }
    internal int ObserveCalls { get; private set; }
    internal Action? BeforeListResponse { get; set; }
    internal Proxy.MatchInformation[] Matches { get; set; } =
        new[] { CreateInformation(19), CreateInformation(20) };

    internal static ClientSession CreateSession()
    {
        ClientSession session = new ClientSession();
        session.StartAuthenticated(new AuthenticatedAccountSession("observer-test-session",
            1, "Observer", "observer@example.test", "en-US", DateTime.UtcNow.AddHours(1)));
        return session;
    }

    private static Proxy.MatchInformation CreateInformation(long matchId)
    {
        return new Proxy.MatchInformation
        {
            MatchId = matchId,
            AssignedSeatNumber = 1,
            TurnDurationSeconds = 40,
            Participants = new[]
            {
                new Proxy.MatchParticipantSummary { SeatNumber = 1, DisplayName = "Creator" },
                new Proxy.MatchParticipantSummary { SeatNumber = 2, DisplayName = "Opponent" }
            }
        };
    }

    public Proxy.MatchInformation[] GetObservableMatches(string sessionToken)
    {
        ListCalls++;
        BeforeListResponse?.Invoke();
        return Matches;
    }

    public Proxy.MatchStateSnapshot ObserveMatch(string sessionToken, long matchId)
    {
        ObserveCalls++;
        return GetMatchState(sessionToken, matchId);
    }

    public Proxy.MatchInformation GetMatchInformation(string sessionToken, long matchId)
    {
        return CreateInformation(matchId);
    }

    public Proxy.MatchStateSnapshot GetMatchState(string sessionToken, long matchId)
    {
        return new Proxy.MatchStateSnapshot
        {
            MatchId = matchId,
            MatchStateVersion = 1,
            State = Proxy.MatchState.InProgress,
            Board = new Proxy.BoardCellState[64],
            BlackPieceCount = 2,
            WhitePieceCount = 2,
            CurrentTurnSeatNumber = 1
        };
    }

    public Proxy.MoveRecord[] GetMoveHistory(string sessionToken, long matchId)
    {
        return Array.Empty<Proxy.MoveRecord>();
    }

    public void Dispose()
    {
        Gameplay.Dispose();
        _host.Abort();
    }
}
