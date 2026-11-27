using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Friendships;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class FriendshipPresenceIntegrationTests
{
    private const string TokenOne = "one";
    private const string TokenTwo = "two";
    private const string TokenTwoB = "two-b";
    private const string TokenTwoC = "two-c";
    private const string TokenStranger = "stranger";
    private const string CorrelationPrefix = "CorrelationId:";

    [TestMethod]
    public void DuplexChannels_FriendConnects_PublishesPresenceAndOwnFriendSnapshot()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient one = context.Connect(TokenOne);
        FriendshipPresenceTestClient stranger = context.Connect(TokenStranger);
        int expectedFriendCount = 1;
        long expectedFriendId = 2L;
        long expectedReverseFriendId = 1L;
        int expectedStrangerFriendCount = 0;
        int expectedStrangerEventCount = 0;

        FriendshipPresenceTestClient two = context.Connect(TokenTwo);
        FriendSummary[] friends = one.Channel.GetFriends(TokenOne);

        Wait(() => one.Count(2L, FriendChange.Connected) == 1);
        int actualFriendCount = friends.Length;
        long actualFriendId = friends[0].Account.AccountId;
        bool actualIsOnlineAndHasImage = friends[0].IsOnline && friends[0].HasProfileImage;
        long actualReverseFriendId = two.Channel.GetFriends(TokenTwo)[0].Account.AccountId;
        int actualStrangerFriendCount = stranger.Channel.GetFriends(TokenStranger).Length;
        int actualStrangerEventCount = stranger.Count(2L, FriendChange.Connected);
        Assert.AreEqual(expectedFriendCount, actualFriendCount);
        Assert.AreEqual(expectedFriendId, actualFriendId);
        Assert.IsTrue(actualIsOnlineAndHasImage);
        Assert.AreEqual(expectedReverseFriendId, actualReverseFriendId);
        Assert.AreEqual(expectedStrangerFriendCount, actualStrangerFriendCount);
        Assert.AreEqual(expectedStrangerEventCount, actualStrangerEventCount);
    }

    [TestMethod]
    public void MultipleSessions_PartialRevocation_StayOnlineUntilLastRevocation()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect(TokenOne);
        context.AddSession(TokenTwoB, 2L);
        context.AddSession(TokenTwoC, 2L);
        context.Connect(TokenTwo);
        context.Connect(TokenTwoB);
        context.Connect(TokenTwoC);
        Wait(() => observer.Count(2L, FriendChange.Connected) == 1);
        int expectedDisconnectBeforeLast = 0;
        bool expectedOnlineAfterLast = false;

        context.Expire(TokenTwo);
        context.Subscriptions.Remove(TokenTwo);
        context.Expire(TokenTwoB);
        context.Subscriptions.Remove(TokenTwoB);
        bool actualOnlineWithThirdSession = observer.Channel.GetFriends(TokenOne)[0].IsOnline;
        int actualDisconnectBeforeLast = observer.Count(2L, FriendChange.Disconnected);

        Assert.IsTrue(actualOnlineWithThirdSession);
        Assert.AreEqual(expectedDisconnectBeforeLast, actualDisconnectBeforeLast);
        context.Expire(TokenTwoC);
        context.Subscriptions.Remove(TokenTwoC);
        Wait(() => observer.Count(2L, FriendChange.Disconnected) == 1);
        bool actualOnlineAfterLast = observer.Channel.GetFriends(TokenOne)[0].IsOnline;
        Assert.AreEqual(expectedOnlineAfterLast, actualOnlineAfterLast);
    }

    [TestMethod]
    public void ClosedAndReplacementChannels_OldChannelClosed_PreservesPresence()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect(TokenOne);
        FriendshipPresenceTestClient oldClient = context.Connect(TokenTwo);
        Wait(() => observer.Count(2L, FriendChange.Connected) == 1);
        int expectedDisconnectAfterReplace = 0;

        FriendshipPresenceTestClient replacement = context.Connect(TokenTwo);
        Wait(() => oldClient.ChannelState is
            CommunicationState.Faulted or CommunicationState.Closed);
        oldClient.Dispose();
        bool actualOnlineAfterReplace = observer.Channel.GetFriends(TokenOne)[0].IsOnline;
        int actualDisconnectAfterReplace = observer.Count(2L, FriendChange.Disconnected);

        Assert.IsTrue(actualOnlineAfterReplace);
        Assert.AreEqual(expectedDisconnectAfterReplace, actualDisconnectAfterReplace);
        replacement.Close();
        Wait(() => observer.Count(2L, FriendChange.Disconnected) == 1);
    }

    [TestMethod]
    public void GetFriends_MissingSessionToken_ThrowsMissingSessionTokenFault()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient client = context.CreateClient();
        SessionFailure expectedReason = SessionFailure.MissingSessionToken;
        int expectedReads = 0;
        Action action = () => client.Channel.GetFriends(string.Empty);

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = fault.Detail.Reason;
        int actualReads = context.FriendshipReads;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedReads, actualReads);
    }

    [TestMethod]
    public void OpenFriendshipChannel_InvalidSession_ThrowsInvalidSessionFault()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient client = context.CreateClient();
        SessionFailure expectedReason = SessionFailure.InvalidSession;
        int expectedReads = 0;
        Action action = () => client.Channel.OpenFriendshipChannel("invalid");

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = fault.Detail.Reason;
        int actualReads = context.FriendshipReads;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedReads, actualReads);
    }

    [TestMethod]
    public void GetFriends_InactiveAccount_ThrowsInactiveAccountFault()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient client = context.CreateClient();
        context.Inactive = true;
        SessionFailure expectedReason = SessionFailure.InactiveAccount;
        int expectedReads = 0;
        Action action = () => client.Channel.GetFriends(TokenOne);

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = fault.Detail.Reason;
        int actualReads = context.FriendshipReads;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedReads, actualReads);
    }

    [TestMethod]
    public void GetFriends_ExpiredSession_ThrowsInvalidSessionFault()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient client = context.CreateClient();
        context.Expire(TokenOne);
        SessionFailure expectedReason = SessionFailure.InvalidSession;
        int expectedReads = 0;
        Action action = () => client.Channel.GetFriends(TokenOne);

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = fault.Detail.Reason;
        int actualReads = context.FriendshipReads;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedReads, actualReads);
    }

    [TestMethod]
    public void GetFriends_RepositoryFailureThroughWcf_ReturnsSanitizedServiceFault()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient client = context.CreateClient();
        context.FailRepository = true;
        bool expectedContainsSecret = false;
        Action action = () => client.Channel.GetFriends(TokenOne);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        bool actualContainsSecret = fault.ToString().Contains("Private provider diagnostic");

        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    [TestMethod]
    public void PresenceCallback_ExpiredReceiver_ReceivesNoCallbacks()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient revoked = context.Connect(TokenOne);
        context.Expire(TokenOne);
        context.Subscriptions.Remove(TokenOne);
        context.AddSession("one-live", 1L);
        FriendshipPresenceTestClient live = context.Connect("one-live");
        int expectedRevokedCount = 0;

        FriendshipPresenceTestClient two = context.Connect(TokenTwo);
        two.Channel.GetFriends(TokenTwo);

        Wait(() => live.Count(2L, FriendChange.Connected) == 1);
        int actualRevokedCount = revoked.Count(2L, FriendChange.Connected);
        Assert.AreEqual(expectedRevokedCount, actualRevokedCount);
    }

    [TestMethod]
    public void PresenceCallback_ExpirySweep_PublishesDisconnection()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect(TokenOne);
        context.Connect(TokenTwo);
        Wait(() => observer.Count(2L, FriendChange.Connected) == 1);

        context.Expire(TokenTwo);

        Wait(() => observer.Count(2L, FriendChange.Disconnected) == 1, 8000);
    }

    [TestMethod]
    public void PresenceCallback_EndedFriendship_ReceivesNoPresenceCallbacks()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect(TokenOne);
        Wait(() => context.RelationshipChecks > 0);
        context.FriendshipActive = false;
        int initialChecks = context.RelationshipChecks;
        int expectedConnectedCount = 0;

        context.Connect(TokenTwo);

        Wait(() => context.RelationshipChecks > initialChecks);
        int actualConnectedCount = observer.Count(2L, FriendChange.Connected);
        Assert.AreEqual(expectedConnectedCount, actualConnectedCount);
    }

    [TestMethod]
    public void PresenceCallback_RelationshipFailure_PreservesHealthyChannelAndAllowsRecovery()
    {
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect(TokenOne);
        context.FailRelationshipChecks = true;
        CommunicationState expectedObserverState = CommunicationState.Opened;

        FriendshipPresenceTestClient friend = context.Connect(TokenTwo);
        Wait(() => context.FailedRelationshipChecks > 0);
        CommunicationState actualObserverState = observer.ChannelState;
        context.FailRelationshipChecks = false;

        Assert.AreEqual(expectedObserverState, actualObserverState);
        Assert.IsTrue(observer.Channel.GetFriends(TokenOne)[0].IsOnline);
        Assert.IsTrue(friend.Channel.GetFriends(TokenTwo)[0].IsOnline);
        context.Expire(TokenTwo);
        context.Subscriptions.Remove(TokenTwo);
        Wait(() => observer.Count(2L, FriendChange.Disconnected) == 1);
    }

    [TestMethod]
    public void GetFriends_RepositoryFailure_LogsOriginalExceptionAndReturnsDeclaredFault()
    {
        FailureLogger logger = new FailureLogger();
        using FriendshipPresenceTestContext context =
            new FriendshipPresenceTestContext(logger, logger);
        using FriendshipPresenceTestClient client = context.CreateClient();
        context.FailRepository = true;
        Action action = () => client.Channel.GetFriends(TokenOne);

        Assert.Throws<FaultException<ServiceFault>>(action);
        Wait(() => logger.Failure != null);
        bool actualSameReference = ReferenceEquals(context.RepositoryFailure, logger.Failure);
        Assert.IsTrue(actualSameReference);
    }

    [TestMethod]
    public void GetFriends_LoggerFailure_ReportsBothFailuresWithoutExposingProviderMessages()
    {
        FailureLogger logger = new FailureLogger
        {
            ThrowOnLog = true
        };
        using FriendshipPresenceTestContext context =
            new FriendshipPresenceTestContext(logger, logger);
        using FriendshipPresenceTestClient client = context.CreateClient();
        context.FailRepository = true;
        TextWriter previousError = Console.Error;
        using StringWriter fallback = new StringWriter();
        bool expectedContainsSecret = false;

        try
        {
            Console.SetError(fallback);
            Action action = () => client.Channel.GetFriends(TokenOne);
            Assert.Throws<FaultException<ServiceFault>>(action);
            Wait(() => fallback.ToString().Contains(CorrelationPrefix)
                && fallback.ToString().Contains(nameof(IOException)));
        }
        finally
        {
            Console.SetError(previousError);
        }

        string output = fallback.ToString();
        bool actualHasOperation = output.Contains(CorrelationPrefix)
            && output.Contains(nameof(FriendshipService.GetFriends));
        bool actualHasBothFailures = output.Contains(nameof(InvalidOperationException))
            && output.Contains(nameof(IOException));
        bool actualContainsSecret = output.Contains("private-");
        Assert.IsTrue(actualHasOperation);
        Assert.IsTrue(actualHasBothFailures);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    private sealed class FailureLogger :
        ILogger<FriendshipSubscriptions>,
        ILogger<ServiceErrorBehavior>
    {
        internal bool ThrowOnLog { get; set; }

        internal Exception? Failure { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Failure = exception;
            if (ThrowOnLog)
            {
                throw new IOException("private-logging-provider-message");
            }
        }
    }

    private static void Wait(Func<bool> condition, int timeout = 5000)
    {
        Assert.IsTrue(
            SpinWait.SpinUntil(condition, timeout),
            "Expected presence callback before timeout.");
    }
}
