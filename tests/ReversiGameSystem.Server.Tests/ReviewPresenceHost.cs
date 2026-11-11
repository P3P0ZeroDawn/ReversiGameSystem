using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Friendships;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Persistence.Friendships;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Hosts real boundaries against a disposable LocalDB fixture only.</summary>
internal static class ReviewPresenceHost
{
    internal static void Run() => ReviewAccountDatabaseTests.RunWithReviewAccounts(Host);

    private static void Host(
        SessionApplicationService applicationService,
        ISessionRepository sessions)
    {
        SessionService session = new SessionService(
            applicationService,
            NullLogger<SessionService>.Instance);
        AccountPresenceRegistry presenceRegistry = new AccountPresenceRegistry();
        FriendshipSubscriptions friendshipSubscriptions = new FriendshipSubscriptions();
        FriendshipApplicationService friendshipApplicationService =
            new FriendshipApplicationService(sessions, new FriendRepository());
        using FriendshipService friendship = new FriendshipService(
            friendshipApplicationService,
            presenceRegistry,
            friendshipSubscriptions,
            NullLogger<FriendshipService>.Instance);
        session.SessionClosed += friendship.RemoveSession;
        ServiceHost sessionHost = new ServiceHost(session);
        ServiceHost friendshipHost = new ServiceHost(friendship);
        try
        {
            sessionHost.AddServiceEndpoint(typeof(ISessionService), CreateBinding(),
                "net.tcp://localhost:18091/ReversiReview/Session");
            friendshipHost.AddServiceEndpoint(typeof(IFriendshipService), CreateBinding(),
                "net.tcp://localhost:18094/ReversiReview/Friendship");
            sessionHost.Open();
            friendshipHost.Open();
            Console.WriteLine("Review presence host ready.");
            Console.ReadLine();
            friendshipHost.Close();
            sessionHost.Close();
        }
        finally
        {
            session.SessionClosed -= friendship.RemoveSession;
            friendshipHost.Abort();
            sessionHost.Abort();
        }
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
