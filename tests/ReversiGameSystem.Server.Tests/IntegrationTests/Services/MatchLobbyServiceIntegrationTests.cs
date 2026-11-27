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

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class MatchLobbyServiceIntegrationTests
{
    private const string PrivateProviderMessage = "private-storage-failure";

    private UtcClockStub _clock = null!;
    private SessionStore _sessions = null!;
    private MatchRepositoryStub _matches = null!;
    private MatchCallbackSubscriptions<IMatchLobbyServiceCallback> _subscriptions = null!;
    private RecordingLobbyCallback _callback = null!;
    private WcfChannelScope<IMatchLobbyService> _scope = null!;
    private IMatchLobbyService _channel = null!;
    private string _sessionToken = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _clock = new UtcClockStub();
        _sessions = new SessionStore(_clock);
        _matches = new MatchRepositoryStub();
        TemplatedEmailSenderStub emailSender = new TemplatedEmailSenderStub();
        MatchApplicationService applicationService = new MatchApplicationService(
            _sessions,
            _matches,
            _clock,
            emailSender);
        _subscriptions = new MatchCallbackSubscriptions<IMatchLobbyServiceCallback>(
            (_, _) => true,
            NullLogger.Instance);
        _callback = new RecordingLobbyCallback();
        MatchLobbyService service = new MatchLobbyService(applicationService, _subscriptions);
        _scope = WcfTestHostHelper
            .OpenDuplexChannel<IMatchLobbyService, IMatchLobbyServiceCallback>(
                service,
                "MatchLobby",
                _callback);
        _channel = _scope.Channel;
        _sessionToken = TestSessionHelper.CreateSession(
            _sessions,
            10L,
            "PlayerOne").SessionToken;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        _subscriptions?.Dispose();
    }

    [TestMethod]
    public void CreateMatch_ThroughWcf_ReturnsMappedLobbySnapshot()
    {
        long expectedMatchId = 44L;
        string expectedCode = "ABC234";
        int expectedDuration = 40;
        _matches.CreateMatchResult = new MatchResult<Match>(new Match
        {
            Id = expectedMatchId,
            Code = expectedCode,
            State = "Waiting",
            TurnDurationSeconds = expectedDuration
        });
        MatchConfiguration configuration = new MatchConfiguration
        {
            OpponentKind = OpponentKind.Human,
            TurnDurationSeconds = expectedDuration,
            CountsTowardStatistics = false
        };

        LobbySnapshot actualSnapshot = _channel.CreateMatch(_sessionToken, configuration);
        long actualMatchId = actualSnapshot.MatchId;
        string? actualCode = actualSnapshot.MatchCode;
        int actualDuration = actualSnapshot.Configuration.TurnDurationSeconds;

        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedCode, actualCode);
        Assert.AreEqual(expectedDuration, actualDuration);
    }

    [TestMethod]
    public void InviteToMatch_FriendshipNotFoundThroughWcf_ReturnsTypedFaultAndKeepsOpen()
    {
        _matches.PreparePrivateInvitationResult =
            new MatchResult<MatchInvitationPreparation>(MatchStatus.FriendshipNotFound);
        FriendshipFailure expectedReason = FriendshipFailure.FriendshipNotFound;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        MatchConfiguration configuration = new MatchConfiguration
        {
            OpponentKind = OpponentKind.Human,
            TurnDurationSeconds = 40
        };
        Action action = () => _channel.InviteToMatch(_sessionToken, 20L, configuration);

        FaultException<FriendshipFault> fault =
            Assert.Throws<FaultException<FriendshipFault>>(action);
        FriendshipFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void JoinMatch_InvalidCodeThroughWcf_ReturnsTypedFaultAndKeepsOpen()
    {
        MatchFailure expectedReason = MatchFailure.InvalidMatchCode;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.JoinMatch(_sessionToken, "bad");

        FaultException<MatchFault> fault =
            Assert.Throws<FaultException<MatchFault>>(action);
        MatchFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void StartSystemMatch_ThroughWcf_ReturnsInProgressSnapshot()
    {
        long expectedMatchId = 66L;
        MatchState expectedState = MatchState.InProgress;
        _matches.StartSystemMatchResult = new MatchResult<Match>(new Match
        {
            Id = expectedMatchId,
            State = "InProgress"
        });

        MatchStateSnapshot actualSnapshot =
            _channel.StartSystemMatch(_sessionToken, expectedMatchId);
        long actualMatchId = actualSnapshot.MatchId;
        MatchState actualState = actualSnapshot.State;

        Assert.AreEqual(expectedMatchId, actualMatchId);
        Assert.AreEqual(expectedState, actualState);
    }

    [TestMethod]
    public void CancelMatch_SubscribedCreatorThroughWcf_DeliversLobbyClosedCallback()
    {
        long expectedClosedMatchId = 77L;
        _matches.LoadParticipantMatchResult = new MatchResult<Match>(new Match
        {
            Id = expectedClosedMatchId,
            State = "Cancelled"
        });
        _channel.OpenLobbyChannel(_sessionToken);

        _channel.CancelMatch(_sessionToken, expectedClosedMatchId);

        bool received = SpinWait.SpinUntil(
            () => _callback.ClosedMatchId == expectedClosedMatchId,
            5000);
        long actualClosedMatchId = _callback.ClosedMatchId;
        Assert.IsTrue(received);
        Assert.AreEqual(expectedClosedMatchId, actualClosedMatchId);
    }

    [TestMethod]
    public void GetAvailableMatches_RepositoryFailureThroughWcf_ReturnsSanitizedServiceFault()
    {
        _matches.ThrowOnGetAvailableMatches = true;
        bool expectedHasCorrelation = true;
        bool expectedContainsSecret = false;
        Action action = () => _channel.GetAvailableMatches(_sessionToken);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        bool actualHasCorrelation = !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId);
        bool actualContainsSecret = fault.ToString().Contains(PrivateProviderMessage);

        Assert.AreEqual(expectedHasCorrelation, actualHasCorrelation);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingLobbyCallback : IMatchLobbyServiceCallback
    {
        private long _closedMatchId;

        internal long ClosedMatchId
        {
            get
            {
                return Interlocked.Read(ref _closedMatchId);
            }
        }

        public void OnMatchStarted(MatchStateSnapshot state)
        {
        }

        public void OnLobbyClosed(long matchId)
        {
            Interlocked.Exchange(ref _closedMatchId, matchId);
        }
    }
}
