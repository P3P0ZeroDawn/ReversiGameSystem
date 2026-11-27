using System;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Contracts.Callbacks;
using ReversiGameSystem.Server.Contracts.Chat;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;
using ReversiGameSystem.Server.Tests.TestDoubles;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class MatchChatServiceIntegrationTests
{
    private const string SimulatedFailureMessage = "Simulated chat repository failure.";

    private UtcClockStub _clock = null!;
    private SessionStore _sessions = null!;
    private ChatRepositoryStub _chatRepository = null!;
    private MatchCallbackSubscriptions<IMatchChatServiceCallback> _subscriptions = null!;
    private RecordingChatCallback _callback = null!;
    private WcfChannelScope<IMatchChatService> _scope = null!;
    private IMatchChatService _channel = null!;
    private string _sessionToken = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _clock = new UtcClockStub();
        _sessions = new SessionStore(_clock);
        _chatRepository = new ChatRepositoryStub();
        ChatApplicationService applicationService =
            new ChatApplicationService(_sessions, _chatRepository, _clock);
        _subscriptions = new MatchCallbackSubscriptions<IMatchChatServiceCallback>(
            (_, _) => true,
            NullLogger.Instance);
        _callback = new RecordingChatCallback();
        MatchChatService service = new MatchChatService(applicationService, _subscriptions);
        _scope = WcfTestHostHelper
            .OpenDuplexChannel<IMatchChatService, IMatchChatServiceCallback>(
                service,
                "MatchChat",
                _callback);
        _channel = _scope.Channel;
        _sessionToken = TestSessionHelper.CreateSession(_sessions, 10L, "PlayerOne").SessionToken;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        _subscriptions?.Dispose();
    }

    [TestMethod]
    public void SendChatMessage_SubscribedParticipantThroughWcf_DeliversChatMessageCallback()
    {
        long expectedMatchId = 50L;
        int expectedMessageNumber = 3;
        string expectedContent = "Good luck!";
        _chatRepository.Submission = new ChatMessageSubmission(new ChatMessage
        {
            MatchId = expectedMatchId,
            MessageNumber = expectedMessageNumber,
            SenderSeatNumber = 1,
            Content = expectedContent,
            SentAtUtc = _clock.UtcNow
        });
        _channel.OpenChatChannel(_sessionToken, expectedMatchId);

        MatchChatEntry actualSent =
            _channel.SendChatMessage(_sessionToken, expectedMatchId, expectedContent);

        bool received = SpinWait.SpinUntil(
            () => _callback.LastMessage != null
                && _callback.LastMessage.MessageNumber == expectedMessageNumber,
            5000);
        Assert.IsTrue(received);
        MatchChatEntry delivered = TestAssert.IsNotNull(_callback.LastMessage);
        int actualSentNumber = actualSent.MessageNumber;
        string actualDeliveredContent = delivered.Content;
        Assert.AreEqual(expectedMessageNumber, actualSentNumber);
        Assert.AreEqual(expectedContent, actualDeliveredContent);
    }

    [TestMethod]
    public void GetChatHistory_ThroughWcf_ReturnsMappedChatHistory()
    {
        long expectedMatchId = 50L;
        string expectedContent = "Hello";
        int expectedCount = 1;
        _chatRepository.History = new ChatHistory(new ChatMessage[]
        {
            new ChatMessage
            {
                MatchId = expectedMatchId,
                MessageNumber = 1,
                SenderSeatNumber = 1,
                Content = expectedContent,
                SentAtUtc = _clock.UtcNow
            }
        });

        MatchChatEntry[] actualMessages =
            _channel.GetChatHistory(_sessionToken, expectedMatchId);
        int actualCount = actualMessages.Length;
        string actualContent = actualMessages[0].Content;

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedContent, actualContent);
    }

    [TestMethod]
    public void SendChatMessage_EmptyContentThroughWcf_ReturnsTypedFaultAndKeepsOpen()
    {
        ChatFailure expectedReason = ChatFailure.EmptyMessage;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.SendChatMessage(_sessionToken, 50L, "   ");

        FaultException<ChatFault> fault =
            Assert.Throws<FaultException<ChatFault>>(action);
        ChatFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void GetChatHistory_RepositoryFailureThroughWcf_ReturnsSanitizedServiceFault()
    {
        _chatRepository.ThrowOnLoadHistory = true;
        bool expectedHasCorrelation = true;
        bool expectedContainsSecret = false;
        Action action = () => _channel.GetChatHistory(_sessionToken, 50L);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        bool actualHasCorrelation = !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId);
        bool actualContainsSecret = fault.ToString().Contains(SimulatedFailureMessage);

        Assert.AreEqual(expectedHasCorrelation, actualHasCorrelation);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    [CallbackBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    private sealed class RecordingChatCallback : IMatchChatServiceCallback
    {
        private MatchChatEntry? _lastMessage;

        internal MatchChatEntry? LastMessage
        {
            get
            {
                return Volatile.Read(ref _lastMessage);
            }
        }

        public void OnChatMessageReceived(MatchChatEntry message)
        {
            Volatile.Write(ref _lastMessage, message);
        }

        public void OnPlayerMuted(ChatMuteNotification mute)
        {
        }
    }
}
