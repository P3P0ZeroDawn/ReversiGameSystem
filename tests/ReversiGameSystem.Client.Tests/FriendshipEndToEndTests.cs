using System;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

/// <summary>Checks generated consumers against the disposable real net.tcp host.</summary>
internal static class FriendshipEndToEndTests
{
    internal static void Presence_RealAccounts_ReceivesLoginAndSessionClosureCallbacks()
    {
        // Arrange
        SessionClient firstSession = CreateSessionClient();
        SessionClient secondSession = CreateSessionClient();
        using FriendshipClient first = CreateFriendshipClient();
        using FriendshipClient second = CreateFriendshipClient();
        Proxy.AuthenticatedSession? alpha = null;
        Proxy.AuthenticatedSession? beta = null;
        try
        {
            alpha = firstSession.CompleteLoginAsync("ReviewAlpha", "000001")
                .GetAwaiter().GetResult();
            beta = secondSession.CompleteLoginAsync("ReviewBeta", "000002")
                .GetAwaiter().GetResult();
            AssertSingleFriend(first.GetFriendsAsync(alpha.SessionToken)
                .GetAwaiter().GetResult(), "ReviewBeta", false);

            // Act
            Proxy.FriendSummary[] betaFriends = second.GetFriendsAsync(beta.SessionToken)
                .GetAwaiter().GetResult();

            // Assert
            AssertSingleFriend(betaFriends, "ReviewAlpha", true);
            WaitForPresence(first, true);
            TestAssert.AreEqual(true, first.IsConnected);
            TestAssert.AreEqual(true, second.IsConnected);
            CloseSession_ActiveFriend_PublishesOffline(first, secondSession, beta.SessionToken);
            InvalidToken_RealBoundary_RejectsSession();
        }
        finally
        {
            first.Close();
            second.Close();
            try
            {
                if (beta != null)
                {
                    secondSession.CloseSessionAsync(beta.SessionToken).GetAwaiter().GetResult();
                }
            }
            finally
            {
                if (alpha != null)
                {
                    firstSession.CloseSessionAsync(alpha.SessionToken).GetAwaiter().GetResult();
                }
            }
        }
    }

    private static void CloseSession_ActiveFriend_PublishesOffline(FriendshipClient observer,
        SessionClient session, string token)
    {
        // Arrange
        AssertSingleFriend(observer.GetSnapshot(), "ReviewBeta", true);

        // Act
        session.CloseSessionAsync(token).GetAwaiter().GetResult();

        // Assert
        WaitForPresence(observer, false);
        TestAssert.AreEqual(true, observer.IsConnected);
    }

    private static void InvalidToken_RealBoundary_RejectsSession()
    {
        // Arrange
        using FriendshipClient invalid = CreateFriendshipClient();

        // Act
        FaultException<Proxy.SessionFault> fault = TestAssert.Throws<FaultException<Proxy.SessionFault>>(
            () => invalid.GetFriendsAsync("synthetic-invalid-session").GetAwaiter().GetResult());

        // Assert
        TestAssert.AreEqual(Proxy.SessionFailure.InvalidSession, fault.Detail.Reason);
        TestAssert.AreEqual(false, invalid.IsConnected);
    }

    private static void WaitForPresence(FriendshipClient observer, bool online)
    {
        // Read the local snapshot only: success requires a delivered duplex notification.
        TestAssert.AreEqual(true, SpinWait.SpinUntil(() =>
        {
            Proxy.FriendSummary[] friends = observer.GetSnapshot();
            return friends.Length == 1 && friends[0].Account.Username == "ReviewBeta"
                && friends[0].IsOnline == online;
        }, TimeSpan.FromSeconds(10)));
        AssertSingleFriend(observer.GetSnapshot(), "ReviewBeta", online);
    }

    private static void AssertSingleFriend(Proxy.FriendSummary[] friends, string username, bool online)
    {
        TestAssert.AreEqual(1, friends.Length);
        TestAssert.AreEqual(username, friends[0].Account.Username);
        TestAssert.AreEqual(true, friends[0].Account.AccountId > 0);
        TestAssert.AreEqual(online, friends[0].IsOnline);
    }

    private static SessionClient CreateSessionClient() => new SessionClient(
        NullLogger<SessionClient>.Instance, () => new Proxy.SessionServiceClient(CreateBinding(),
            new EndpointAddress("net.tcp://localhost:18091/ReversiReview/Session")));

    private static FriendshipClient CreateFriendshipClient() => new FriendshipClient(
        NullLogger<FriendshipClient>.Instance, context => new Proxy.FriendshipServiceClient(
            context, CreateBinding(), new EndpointAddress(
                "net.tcp://localhost:18094/ReversiReview/Friendship")));

    private static NetTcpBinding CreateBinding()
    {
        NetTcpBinding binding = new NetTcpBinding(SecurityMode.None)
        {
            OpenTimeout = TimeSpan.FromSeconds(10), CloseTimeout = TimeSpan.FromSeconds(5),
            SendTimeout = TimeSpan.FromSeconds(10), ReceiveTimeout = TimeSpan.FromSeconds(15)
        };
        return binding;
    }
}
