using System;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Matches;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;
using WcfGameplayMatchService = ReversiGameSystem.Server.ServiceApi.GameplayMatchService;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class GameplayMatchServiceIntegrationTests
{
    private const string PrivateProviderMessage = "private-storage-failure";

    private UtcClockStub _clock = null!;
    private SessionStore _sessions = null!;
    private GameplayRepositoryStub _matches = null!;
    private GameplayMatchApplicationService _applicationService = null!;
    private MatchCallbackSubscriptions<IGameplayMatchServiceCallback> _subscriptions = null!;
    private WcfChannelScope<IGameplayMatchService> _scope = null!;
    private IGameplayMatchService _channel = null!;
    private string _playerOneToken = string.Empty;
    private string _playerTwoToken = string.Empty;
    private string _outsiderToken = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _clock = new UtcClockStub();
        _sessions = new SessionStore(_clock);
        _matches = new GameplayRepositoryStub();
        _applicationService = new GameplayMatchApplicationService(_sessions, _matches, _clock);
        _subscriptions = new MatchCallbackSubscriptions<IGameplayMatchServiceCallback>(
            (_, _) => true,
            NullLogger.Instance);
        WcfGameplayMatchService service = new WcfGameplayMatchService(
            _applicationService,
            new GameplayMatchPresence(),
            _subscriptions);
        _scope = WcfTestHostHelper
            .OpenDuplexChannel<IGameplayMatchService, IGameplayMatchServiceCallback>(
                service,
                "GameplayMatch",
                new RecordingGameplayCallback());
        _channel = _scope.Channel;
        _playerOneToken = TestSessionHelper.CreateSession(_sessions, 10L, "PlayerOne").SessionToken;
        _playerTwoToken = TestSessionHelper.CreateSession(_sessions, 20L, "PlayerTwo").SessionToken;
        _outsiderToken = TestSessionHelper.CreateSession(_sessions, 99L, "Outsider").SessionToken;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        _subscriptions?.Dispose();
    }

    [TestMethod]
    public void GetMatchState_ThroughWcf_ReturnsInitialBoardSnapshot()
    {
        long expectedMatchId = 100L;
        int expectedBoardLength = 64;
        int expectedTurnSeat = 1;
        _applicationService.TrackStartedMatch(CreateTwoPlayerMatch(expectedMatchId));

        MatchStateSnapshot actualSnapshot =
            _channel.GetMatchState(_playerOneToken, expectedMatchId);
        long actualMatchId = actualSnapshot.MatchId;
        int actualBoardLength = actualSnapshot.Board.Length;
        int? actualTurnSeat = actualSnapshot.CurrentTurnSeatNumber;

        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedBoardLength, actualBoardLength);
        Assert.AreEqual(expectedTurnSeat, actualTurnSeat);
    }

    [TestMethod]
    public void PlacePiece_ValidOpeningMoveThroughWcf_AdvancesVersionAndSwitchesTurn()
    {
        Match match = CreateTwoPlayerMatch(100L);
        _applicationService.TrackStartedMatch(match);
        long expectedVersion = match.MatchStateVersion + 1;
        int expectedNextTurnSeat = 2;
        PiecePlacement placement = new PiecePlacement
        {
            MatchId = 100L,
            ExpectedMatchStateVersion = match.MatchStateVersion,
            Row = 2,
            Column = 3
        };

        MatchStateSnapshot actualSnapshot = _channel.PlacePiece(_playerOneToken, placement);
        long actualVersion = actualSnapshot.MatchStateVersion;
        int? actualNextTurnSeat = actualSnapshot.CurrentTurnSeatNumber;

        Assert.AreEqual(expectedVersion, actualVersion);
        Assert.AreEqual(expectedNextTurnSeat, actualNextTurnSeat);
    }

    [TestMethod]
    public void PlacePiece_SubscribedParticipantThroughWcf_DeliversMatchStateChangedCallback()
    {
        Match match = CreateTwoPlayerMatch(100L);
        _applicationService.TrackStartedMatch(match);
        long expectedVersion = match.MatchStateVersion + 1;
        RecordingGameplayCallback playerTwoCallback = new RecordingGameplayCallback();
        DuplexChannelFactory<IGameplayMatchService> secondFactory =
            WcfTestHostHelper.CreateDuplexFactory<
                IGameplayMatchService,
                IGameplayMatchServiceCallback>(
                    _scope.EndpointAddress,
                    playerTwoCallback);
        IGameplayMatchService secondChannel = secondFactory.CreateChannel();
        try
        {
            secondChannel.OpenMatchChannel(_playerTwoToken, 100L);
            PiecePlacement placement = new PiecePlacement
            {
                MatchId = 100L,
                ExpectedMatchStateVersion = match.MatchStateVersion,
                Row = 2,
                Column = 3
            };

            _channel.PlacePiece(_playerOneToken, placement);

            bool received = SpinWait.SpinUntil(
                () => playerTwoCallback.LastState != null
                    && playerTwoCallback.LastState.MatchStateVersion == expectedVersion,
                5000);
            Assert.IsTrue(received);
            MatchStateSnapshot callbackState = TestAssert.IsNotNull(playerTwoCallback.LastState);
            long actualVersion = callbackState.MatchStateVersion;
            Assert.AreEqual(expectedVersion, actualVersion);
        }
        finally
        {
            WcfTestHostHelper.CloseOrAbort(secondChannel);
            WcfTestHostHelper.CloseOrAbort(secondFactory);
        }
    }

    [TestMethod]
    public void PlacePiece_UnauthorizedParticipantThroughWcf_ReturnsTypedFaultAndKeepsOpen()
    {
        Match match = CreateTwoPlayerMatch(100L);
        _applicationService.TrackStartedMatch(match);
        MatchFailure expectedReason = MatchFailure.Unauthorized;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        PiecePlacement placement = new PiecePlacement
        {
            MatchId = 100L,
            ExpectedMatchStateVersion = match.MatchStateVersion,
            Row = 2,
            Column = 3
        };
        Action action = () => _channel.PlacePiece(_outsiderToken, placement);

        FaultException<MatchFault> fault =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void ForfeitMatch_ThroughWcf_ReturnsAbandonmentConclusion()
    {
        _applicationService.TrackStartedMatch(CreateTwoPlayerMatch(100L));
        MatchEndReason expectedReason = MatchEndReason.Abandonment;
        int expectedWinnerSeat = 2;

        MatchConclusion actualConclusion = _channel.ForfeitMatch(_playerOneToken, 100L);
        MatchEndReason actualReason = actualConclusion.Reason;
        int? actualWinnerSeat = actualConclusion.WinnerSeatNumber;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedWinnerSeat, actualWinnerSeat);
    }

    [TestMethod]
    public void GetMatchState_RepositoryFailureThroughWcf_ReturnsSanitizedServiceFault()
    {
        _matches.ThrowOnLoadMatch = true;
        bool expectedHasCorrelation = true;
        bool expectedContainsSecret = false;
        Action action = () => _channel.GetMatchState(_playerOneToken, 100L);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        bool actualHasCorrelation = !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId);
        bool actualContainsSecret = fault.ToString().Contains(PrivateProviderMessage);

        Assert.AreEqual(expectedHasCorrelation, actualHasCorrelation);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    private static Match CreateTwoPlayerMatch(long matchId)
    {
        return new Match
        {
            Id = matchId,
            Code = "ABC234",
            State = "InProgress",
            TurnDurationSeconds = 40,
            Participants = new MatchParticipant[]
            {
                new MatchParticipant
                {
                    SeatNumber = 1,
                    AccountId = 10L,
                    DisplayName = "PlayerOne"
                },
                new MatchParticipant
                {
                    SeatNumber = 2,
                    AccountId = 20L,
                    DisplayName = "PlayerTwo"
                }
            }
        };
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingGameplayCallback : IGameplayMatchServiceCallback
    {
        private MatchStateSnapshot? _lastState;

        internal MatchStateSnapshot? LastState
        {
            get
            {
                return Volatile.Read(ref _lastState);
            }
        }

        public void OnMatchStateChanged(MatchStateSnapshot state)
        {
            Volatile.Write(ref _lastState, state);
        }

        public void OnParticipantConnectionChanged(long matchId, int seatNumber, bool isConnected)
        {
        }
    }
}
