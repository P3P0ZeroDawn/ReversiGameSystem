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
    [FaultContract(typeof(Proxy.MatchFault))]
    [FaultContract(typeof(Proxy.ServiceFault))]
    Proxy.MatchSummary[] GetMatchHistory(string sessionToken);
    [OperationContract]
    [FaultContract(typeof(Proxy.MatchFault))]
    [FaultContract(typeof(Proxy.ServiceFault))]
    Proxy.MatchDetails GetMatchDetails(string sessionToken, long matchId);
    [OperationContract]
    void OpenMatchChannel(string sessionToken, long matchId);
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
    internal int HistoryCalls { get; private set; }
    internal int DetailsCalls { get; private set; }
    internal int OpenCalls { get; private set; }
    internal Proxy.MatchFailure? HistoryFailure { get; set; }
    internal bool DetailsTechnicalFailure { get; set; }
    internal Action? BeforeHistoryResponse { get; set; }
    internal Action? BeforeDetailsResponse { get; set; }
    internal Proxy.MatchSummary[] History { get; set; } = new[]
    {
        new Proxy.MatchSummary
        {
            MatchId = 19,
            AssignedSeatNumber = 1,
            Participants = CreateInformation(19).Participants,
            State = Proxy.MatchState.Finished,
            RecordedAtUtc = DateTime.UtcNow,
            BlackPieceCount = 35,
            WhitePieceCount = 29,
            Conclusion = new Proxy.MatchConclusion { WinnerSeatNumber = 1 }
        }
    };
    internal Proxy.MatchDetails Recording { get; set; } = CreateRecording();
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

    private static Proxy.MatchDetails CreateRecording()
    {
        const int BoardCellCount = 64;
        const int RecordedCellIndex = 7;
        Proxy.BoardCellState[] first = new Proxy.BoardCellState[BoardCellCount];
        Proxy.BoardCellState[] final = new Proxy.BoardCellState[BoardCellCount];
        final[RecordedCellIndex] = Proxy.BoardCellState.White;
        return new Proxy.MatchDetails
        {
            MatchId = 19,
            Participants = CreateInformation(19).Participants,
            State = Proxy.MatchState.Finished,
            TurnDurationSeconds = 40,
            RecordedAtUtc = DateTime.UtcNow,
            Conclusion = new Proxy.MatchConclusion { WinnerSeatNumber = 2 },
            BoardFrames = new[] { first, final },
            Moves = new[]
            {
                new Proxy.MoveRecord
                {
                    MoveNumber = 1, SeatNumber = 2, Row = 0, Column = RecordedCellIndex,
                    PlayedAtUtc = DateTime.UtcNow, FlippedPieceCount = 1,
                    Origin = Proxy.MoveOrigin.Timeout
                }
            }
        };
    }

    public void OpenMatchChannel(string sessionToken, long matchId)
    {
        OpenCalls++;
    }

    public Proxy.MatchSummary[] GetMatchHistory(string sessionToken)
    {
        HistoryCalls++;
        if (HistoryFailure.HasValue)
        {
            throw new FaultException<Proxy.MatchFault>(
                new Proxy.MatchFault { Reason = HistoryFailure.Value });
        }
        BeforeHistoryResponse?.Invoke();
        return History;
    }

    public Proxy.MatchDetails GetMatchDetails(string sessionToken, long matchId)
    {
        DetailsCalls++;
        if (DetailsTechnicalFailure)
        {
            throw new FaultException<Proxy.ServiceFault>(new Proxy.ServiceFault());
        }
        BeforeDetailsResponse?.Invoke();
        return Recording;
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
