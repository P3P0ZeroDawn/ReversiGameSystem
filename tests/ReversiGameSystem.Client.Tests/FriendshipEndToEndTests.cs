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
        AuthenticatedAccountSession? alpha = null;
        AuthenticatedAccountSession? beta = null;
        try
        {
            alpha = Authenticate(firstSession, "ReviewAlpha", "000001");
            beta = Authenticate(secondSession, "ReviewBeta", "000002");
            AssertSingleFriend(first.GetFriends(alpha.SessionToken)
                .GetAwaiter().GetResult().Friends, "ReviewBeta", false);

            // Act
            FriendSummary[] betaFriends = second.GetFriends(beta.SessionToken)
                .GetAwaiter().GetResult().Friends;

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
                    secondSession.CloseSession(beta.SessionToken).GetAwaiter().GetResult();
                }
            }
            finally
            {
                if (alpha != null)
                {
                    firstSession.CloseSession(alpha.SessionToken).GetAwaiter().GetResult();
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
        session.CloseSession(token).GetAwaiter().GetResult();

        // Assert
        WaitForPresence(observer, false);
        TestAssert.AreEqual(true, observer.IsConnected);
    }

    private static void InvalidToken_RealBoundary_RejectsSession()
    {
        // Arrange
        using FriendshipClient invalid = CreateFriendshipClient();

        // Act
        FriendshipResult result = invalid.GetFriends("synthetic-invalid-session")
            .GetAwaiter().GetResult();

        // Assert
        TestAssert.AreEqual(FriendshipStatus.InvalidSession, result.Status);
        TestAssert.AreEqual(false, invalid.IsConnected);
    }

    private static void WaitForPresence(FriendshipClient observer, bool online)
    {
        // Read the local snapshot only: success requires a delivered duplex notification.
        TestAssert.AreEqual(true, SpinWait.SpinUntil(() =>
        {
            FriendSummary[] friends = observer.GetSnapshot();
            return friends.Length == 1 && friends[0].Username == "ReviewBeta"
                && friends[0].IsOnline == online;
        }, TimeSpan.FromSeconds(10)));
        AssertSingleFriend(observer.GetSnapshot(), "ReviewBeta", online);
    }

    private static void AssertSingleFriend(FriendSummary[] friends, string username, bool online)
    {
        TestAssert.AreEqual(1, friends.Length);
        TestAssert.AreEqual(username, friends[0].Username);
        TestAssert.AreEqual(true, friends[0].AccountId > 0);
        TestAssert.AreEqual(online, friends[0].IsOnline);
    }

    private static SessionClient CreateSessionClient() => new SessionClient(
        NullLogger<SessionClient>.Instance, () => new Proxy.SessionServiceClient(CreateBinding(),
            new EndpointAddress("net.tcp://localhost:18091/ReversiReview/Session")));

    private static FriendshipClient CreateFriendshipClient() => new FriendshipClient(
        NullLogger<FriendshipClient>.Instance, context => new Proxy.FriendshipServiceClient(
            context, CreateBinding(), new EndpointAddress(
                "net.tcp://localhost:18094/ReversiReview/Friendship")));

    private static AuthenticatedAccountSession Authenticate(
        SessionClient sessions,
        string login,
        string verificationCode)
    {
        SessionResult<AuthenticatedAccountSession> result = sessions.CompleteLogin(
            login,
            verificationCode).GetAwaiter().GetResult();
        TestAssert.AreEqual(SessionStatus.Success, result.Status);
        return TestAssert.IsNotNull(result.Value);
    }

    private static NetTcpBinding CreateBinding()
    {
        NetTcpBinding binding = new NetTcpBinding(SecurityMode.None)
        {
            OpenTimeout = TimeSpan.FromSeconds(10),
            CloseTimeout = TimeSpan.FromSeconds(5),
            SendTimeout = TimeSpan.FromSeconds(10),
            ReceiveTimeout = TimeSpan.FromSeconds(15)
        };
        return binding;
    }
}
