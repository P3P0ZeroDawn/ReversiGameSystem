using System;
using System.ServiceModel;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

/// <summary>Checks generated consumers against the disposable real net.tcp host.</summary>
[TestClass]
[TestCategory("LiveIntegration")]
public sealed class FriendshipEndToEndTests
{
    [TestMethod]
    public void Presence_RealAccounts_ReceivesLoginAndSessionClosureCallbacks()
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
            Assert.AreEqual(true, first.IsConnected);
            Assert.AreEqual(true, second.IsConnected);
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
        AssertSingleFriend(observer.ReadFriends(), "ReviewBeta", true);

        // Act
        session.CloseSession(token).GetAwaiter().GetResult();

        // Assert
        WaitForPresence(observer, false);
        Assert.AreEqual(true, observer.IsConnected);
    }

    private static void InvalidToken_RealBoundary_RejectsSession()
    {
        // Arrange
        using FriendshipClient invalid = CreateFriendshipClient();

        // Act
        FriendshipResult result = invalid.GetFriends("synthetic-invalid-session")
            .GetAwaiter().GetResult();

        // Assert
        Assert.AreEqual(FriendshipStatus.InvalidSession, result.Status);
        Assert.AreEqual(false, invalid.IsConnected);
    }

    private static void WaitForPresence(FriendshipClient observer, bool online)
    {
        // Read the local snapshot only: success requires a delivered duplex notification.
        Assert.AreEqual(true, SpinWait.SpinUntil(() =>
        {
            FriendSummary[] friends = observer.ReadFriends();
            return friends.Length == 1 && friends[0].Username == "ReviewBeta"
                && friends[0].IsOnline == online;
        }, TimeSpan.FromSeconds(10)));
        AssertSingleFriend(observer.ReadFriends(), "ReviewBeta", online);
    }

    private static void AssertSingleFriend(FriendSummary[] friends, string username, bool online)
    {
        Assert.AreEqual(1, friends.Length);
        Assert.AreEqual(username, friends[0].Username);
        Assert.AreEqual(true, friends[0].AccountId > 0);
        Assert.AreEqual(online, friends[0].IsOnline);
    }

    private static SessionClient CreateSessionClient()
    {
        const int SessionPort = 18091;
        Uri endpoint = new UriBuilder(Uri.UriSchemeNetTcp, "localhost", SessionPort,
            "ReversiReview/Session").Uri;
        return new SessionClient(NullLogger<SessionClient>.Instance,
            () => new Proxy.SessionServiceClient(CreateBinding(), new EndpointAddress(endpoint)));
    }

    private static FriendshipClient CreateFriendshipClient()
    {
        const int FriendshipPort = 18094;
        Uri endpoint = new UriBuilder(Uri.UriSchemeNetTcp, "localhost", FriendshipPort,
            "ReversiReview/Friendship").Uri;
        return new FriendshipClient(NullLogger<FriendshipClient>.Instance,
            context => new Proxy.FriendshipServiceClient(context, CreateBinding(),
                new EndpointAddress(endpoint)));
    }

    private static AuthenticatedAccountSession Authenticate(
        SessionClient sessions,
        string login,
        string verificationCode)
    {
        SessionResult<AuthenticatedAccountSession> result = sessions.CompleteLogin(
            login,
            verificationCode).GetAwaiter().GetResult();
        Assert.AreEqual(SessionStatus.Success, result.Status);
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
