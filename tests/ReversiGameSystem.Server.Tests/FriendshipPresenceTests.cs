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

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class FriendshipPresenceTests
{
    [TestMethod]
    public void DuplexChannels_PublishPresenceAndOwnFriendSnapshot()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient one = context.Connect("one");
        FriendshipPresenceTestClient stranger = context.Connect("stranger");

        // Act
        FriendshipPresenceTestClient two = context.Connect("two");
        FriendSummary[] friends = one.Channel.GetFriends("one");

        // Assert
        Wait(() => one.Count(2, FriendChange.Connected) == 1);
        TestAssert.AreEqual(1, friends.Length);
        TestAssert.AreEqual(2L, friends[0].Account.AccountId);
        TestAssert.IsTrue(friends[0].IsOnline && friends[0].HasProfileImage, "Expected public online friend.");
        TestAssert.AreEqual(1L, two.Channel.GetFriends("two")[0].Account.AccountId);
        TestAssert.AreEqual(0, stranger.Channel.GetFriends("stranger").Length);
        TestAssert.AreEqual(0, stranger.Count(2, FriendChange.Connected));
    }

    [TestMethod]
    public void MultipleSessions_StayOnlineUntilLastRevocation()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect("one");
        context.AddSession("two-b", 2);
        context.AddSession("two-c", 2);
        context.Connect("two");
        context.Connect("two-b");
        context.Connect("two-c");
        Wait(() => observer.Count(2, FriendChange.Connected) == 1);

        // Act
        context.Revoke("two");
        context.Service.RemoveSession("two");
        context.Revoke("two-b");
        context.Service.RemoveSession("two-b");

        // Assert
        TestAssert.IsTrue(observer.Channel.GetFriends("one")[0].IsOnline, "Third session must keep account online.");
        TestAssert.AreEqual(0, observer.Count(2, FriendChange.Disconnected));
        context.Revoke("two-c");
        context.Service.RemoveSession("two-c");
        Wait(() => observer.Count(2, FriendChange.Disconnected) == 1);
        TestAssert.IsTrue(!observer.Channel.GetFriends("one")[0].IsOnline, "Last revocation must disconnect.");
    }

    [TestMethod]
    public void ClosedAndReplacementChannels_PreservePresence()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect("one");
        FriendshipPresenceTestClient old = context.Connect("two");
        Wait(() => observer.Count(2, FriendChange.Connected) == 1);

        // Act
        FriendshipPresenceTestClient replacement = context.Connect("two");
        Wait(() => ((ICommunicationObject)old.Channel).State is
            CommunicationState.Faulted or CommunicationState.Closed);
        old.Dispose();

        // Assert
        TestAssert.IsTrue(observer.Channel.GetFriends("one")[0].IsOnline, "Old close must not remove its replacement.");
        TestAssert.AreEqual(0, observer.Count(2, FriendChange.Disconnected));
        replacement.Close();
        Wait(() => observer.Count(2, FriendChange.Disconnected) == 1);
    }

    [TestMethod]
    public void InvalidInactiveAndExpiredSessions_ReturnTypedFaults()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient client = context.CreateClient();

        // Act
        FaultException<SessionFault> missing = TestAssert.Throws<FaultException<SessionFault>>(
            () => client.Channel.GetFriends(string.Empty));
        FaultException<SessionFault> invalid = TestAssert.Throws<FaultException<SessionFault>>(
            () => client.Channel.OpenFriendshipChannel("invalid"));
        context.Inactive = true;
        FaultException<SessionFault> inactive = TestAssert.Throws<FaultException<SessionFault>>(
            () => client.Channel.GetFriends("one"));
        context.Inactive = false;
        context.Expire("one");
        FaultException<SessionFault> expired = TestAssert.Throws<FaultException<SessionFault>>(
            () => client.Channel.GetFriends("one"));

        // Assert
        TestAssert.AreEqual(SessionFailure.MissingSessionToken, missing.Detail.Reason);
        TestAssert.AreEqual(SessionFailure.InvalidSession, invalid.Detail.Reason);
        TestAssert.AreEqual(SessionFailure.InactiveAccount, inactive.Detail.Reason);
        TestAssert.AreEqual(SessionFailure.InvalidSession, expired.Detail.Reason);
        TestAssert.AreEqual(0, context.FriendshipReads);
    }

    [TestMethod]
    public void UnsupportedOperations_ReturnDeclaredServiceFaultWithoutRepositoryAccess()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient client = context.CreateClient();

        // Act
        TestAssert.Throws<FaultException<ServiceFault>>(() => client.Channel.FindPlayers("one", "private"));
        TestAssert.Throws<FaultException<ServiceFault>>(() => client.Channel.SendFriendRequest("one", 2));
        TestAssert.Throws<FaultException<ServiceFault>>(() => client.Channel.GetPendingFriendRequests("one"));
        TestAssert.Throws<FaultException<ServiceFault>>(() => client.Channel.RespondToFriendRequest("one", "id", true));
        TestAssert.Throws<FaultException<ServiceFault>>(() => client.Channel.RemoveFriend("one", 2));

        // Assert
        TestAssert.AreEqual(0, context.FriendshipReads);
        context.FailRepository = true;
        FaultException<ServiceFault> fault = TestAssert.Throws<FaultException<ServiceFault>>(
            () => client.Channel.GetFriends("one"));
        TestAssert.IsTrue(!fault.ToString().Contains("Private provider diagnostic"), "Provider messages must stay private.");
    }

    [TestMethod]
    public void ExpiredReceiver_ReceivesNoCallbacks()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient revoked = context.Connect("one");
        context.Revoke("one");
        context.Service.RemoveSession("one");
        context.AddSession("one-live", 1);
        FriendshipPresenceTestClient live = context.Connect("one-live");

        // Act
        FriendshipPresenceTestClient two = context.Connect("two");
        two.Channel.GetFriends("two");

        // Assert
        Wait(() => live.Count(2, FriendChange.Connected) == 1);
        TestAssert.AreEqual(0, revoked.Count(2, FriendChange.Connected));
    }

    [TestMethod]
    public void ExpirySweep_PublishesDisconnection()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect("one");
        context.Connect("two");
        Wait(() => observer.Count(2, FriendChange.Connected) == 1);

        // Act
        context.Expire("two");

        // Assert
        Wait(() => observer.Count(2, FriendChange.Disconnected) == 1, 8000);
    }

    [TestMethod]
    public void EndedFriendship_ReceivesNoPresenceCallbacks()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect("one");
        Wait(() => Volatile.Read(ref context.RelationshipChecks) > 0);
        context.FriendshipActive = false;
        int initialChecks = Volatile.Read(ref context.RelationshipChecks);

        // Act
        context.Connect("two");

        // Assert
        Wait(() => Volatile.Read(ref context.RelationshipChecks) > initialChecks);
        TestAssert.AreEqual(0, observer.Count(2, FriendChange.Connected));
    }

    [TestMethod]
    public void RelationshipFailure_PreservesHealthyChannelAndAllowsPresenceRecovery()
    {
        // Arrange
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext();
        FriendshipPresenceTestClient observer = context.Connect("one");
        context.FailRelationshipChecks = true;

        // Act
        FriendshipPresenceTestClient friend = context.Connect("two");
        Wait(() => Volatile.Read(ref context.FailedRelationshipChecks) > 0);
        TestAssert.AreEqual(CommunicationState.Opened,
            ((ICommunicationObject)observer.Channel).State);
        context.FailRelationshipChecks = false;

        // Assert
        TestAssert.IsTrue(observer.Channel.GetFriends("one")[0].IsOnline,
            "The existing channel must recover its friend snapshot.");
        TestAssert.IsTrue(friend.Channel.GetFriends("two")[0].IsOnline,
            "A repository failure must not remove the observer's presence.");
        context.Revoke("two");
        context.Service.RemoveSession("two");
        Wait(() => observer.Count(2, FriendChange.Disconnected) == 1);
    }

    [TestMethod]
    public void RepositoryFailure_LogsOriginalExceptionAndReturnsDeclaredFault()
    {
        // Arrange
        FailureLogger logger = new FailureLogger();
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext(logger, logger);
        using FriendshipPresenceTestClient client = context.CreateClient();
        context.FailRepository = true;

        // Act
        TestAssert.Throws<FaultException<ServiceFault>>(() => client.Channel.GetFriends("one"));
        Wait(() => logger.Failure != null);

        // Assert
        TestAssert.IsTrue(ReferenceEquals(context.RepositoryFailure, logger.Failure),
            "The logger must receive the original exception, including its diagnostic structure.");
    }

    [TestMethod]
    public void LoggerFailure_ReportsBothFailuresWithoutExposingProviderMessages()
    {
        // Arrange
        FailureLogger logger = new FailureLogger { ThrowOnLog = true };
        using FriendshipPresenceTestContext context = new FriendshipPresenceTestContext(logger, logger);
        using FriendshipPresenceTestClient client = context.CreateClient();
        context.FailRepository = true;
        TextWriter previousError = Console.Error;
        using StringWriter fallback = new StringWriter();

        // Act
        try
        {
            Console.SetError(fallback);
            TestAssert.Throws<FaultException<ServiceFault>>(() => client.Channel.GetFriends("one"));
            Wait(() => fallback.ToString().Contains("CorrelationId:"));
        }
        finally
        {
            Console.SetError(previousError);
        }

        // Assert
        string output = fallback.ToString();
        TestAssert.IsTrue(output.Contains("CorrelationId:")
            && output.Contains(nameof(FriendshipService.GetFriends)),
            "Fallback diagnostics must retain the operation and correlation.");
        TestAssert.IsTrue(output.Contains(nameof(InvalidOperationException))
            && output.Contains(nameof(IOException)),
            "Fallback diagnostics must report both the operation and logger failures.");
        TestAssert.IsTrue(!output.Contains("private-"),
            "The independent diagnostic sink must exclude both provider messages.");
    }

    private sealed class FailureLogger : ILogger<FriendshipService>, ILogger<ServiceErrorBehavior>
    {
        internal bool ThrowOnLog { get; set; }
        internal Exception? Failure { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Failure = exception;
            if (ThrowOnLog)
            {
                throw new IOException("private-logging-provider-message");
            }
        }
    }

    private static void Wait(Func<bool> condition, int timeout = 5000) =>
        TestAssert.IsTrue(SpinWait.SpinUntil(condition, timeout), "Expected presence callback before timeout.");
}
