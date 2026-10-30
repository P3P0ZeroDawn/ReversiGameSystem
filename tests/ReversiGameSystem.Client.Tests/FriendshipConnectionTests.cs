using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class FriendshipConnectionTests
{
    internal static void GeneratedProxy_ReceivesPresenceAndPropagatesTypedSessionFault()
    {
        string address = "net.pipe://localhost/ReversiFriendshipTests/" + Guid.NewGuid().ToString("N");
        FriendshipStubService service = new();
        ServiceHost host = new(service, new Uri(address));
        try
        {
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(Proxy.IFriendshipService), CreateBinding(), "");
            // The generated consumer exposes sync and Task variants of each operation.
            // A test host dispatches only the sync implementation; wire metadata is unchanged.
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                operation.TaskMethod = null;
            }
            host.Open();
            using FriendshipClient client = new(NullLogger<FriendshipClient>.Instance,
                context => new Proxy.FriendshipServiceClient(context, CreateBinding(), new EndpointAddress(address)));
            TestAssert.AreEqual(false, client.GetFriendsAsync("test-session").GetAwaiter().GetResult()[0].IsOnline);
            TestAssert.AreEqual(true, Guid.TryParse(service.LastCorrelationId, out _));
            TestAssert.IsNotNull(service.Callback).OnFriendChanged(2, Proxy.FriendChange.Connected);
            TestAssert.AreEqual(true, SpinWait.SpinUntil(
                () => client.GetSnapshot()[0].IsOnline, TimeSpan.FromSeconds(5)));
            service.Callback!.OnFriendChanged(2, Proxy.FriendChange.Disconnected);
            TestAssert.AreEqual(true, SpinWait.SpinUntil(
                () => !client.GetSnapshot()[0].IsOnline, TimeSpan.FromSeconds(5)));
            service.Callback.OnFriendChanged(2, Proxy.FriendChange.Removed);
            TestAssert.AreEqual(true, SpinWait.SpinUntil(
                () => client.GetSnapshot().Length == 0, TimeSpan.FromSeconds(5)));
            service.RejectSession = true;
            FaultException<Proxy.SessionFault> fault = TestAssert.Throws<FaultException<Proxy.SessionFault>>(
                () => client.GetFriendsAsync("test-session").GetAwaiter().GetResult());
            TestAssert.AreEqual(Proxy.SessionFailure.InvalidSession, fault.Detail.Reason);
            TestAssert.AreEqual(false, client.IsConnected);
            service.RejectSession = false;
            TestAssert.AreEqual(1, client.GetFriendsAsync("test-session").GetAwaiter().GetResult().Length);
            client.Close();
            TestAssert.AreEqual(false, client.IsConnected);
            TestAssert.AreEqual(0, client.GetSnapshot().Length);
            host.Close();
        }
        finally
        {
            host.Abort();
        }
    }

    internal static void Keepalive_CloseWhileRefreshPending_DoesNotReopenChannel()
    {
        string address = "net.pipe://localhost/ReversiFriendshipTests/" + Guid.NewGuid().ToString("N");
        FriendshipStubService service = new();
        ServiceHost host = new(service, new Uri(address));
        try
        {
            ServiceEndpoint endpoint = host.AddServiceEndpoint(
                typeof(Proxy.IFriendshipService), CreateBinding(), "");
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                operation.TaskMethod = null;
            }
            host.Open();
            using FriendshipClient client = new(NullLogger<FriendshipClient>.Instance,
                context => new Proxy.FriendshipServiceClient(
                    context, CreateBinding(), new EndpointAddress(address)));
            using ManualResetEventSlim refreshed = new();
            client.FriendsChanged += () => refreshed.Set();
            client.GetFriendsAsync("test-session").GetAwaiter().GetResult();
            refreshed.Reset();
            TestAssert.AreEqual(true, refreshed.Wait(TimeSpan.FromSeconds(12)));
            TestAssert.AreEqual(true, service.RequestCount >= 2);
            using ManualResetEventSlim started = new();
            using ManualResetEventSlim release = new();
            service.RequestStarted = started;
            service.ContinueRequest = release;
            Task refresh = client.GetFriendsAsync("test-session");
            TestAssert.AreEqual(true, started.Wait(TimeSpan.FromSeconds(5)));
            Task close = Task.Run(() => client.Close());
            TestAssert.AreEqual(false, close.Wait(TimeSpan.FromMilliseconds(100)));
            release.Set();
            Task.WhenAll(refresh, close).GetAwaiter().GetResult();
            int requestsAfterClose = service.RequestCount;
            TestAssert.AreEqual(false, SpinWait.SpinUntil(
                () => service.RequestCount > requestsAfterClose, TimeSpan.FromSeconds(11)));
            TestAssert.AreEqual(false, client.IsConnected);
            TestAssert.AreEqual(0, client.GetSnapshot().Length);
            host.Close();
        }
        finally
        {
            host.Abort();
        }
    }

    private static NetNamedPipeBinding CreateBinding() => new(NetNamedPipeSecurityMode.None)
    {
        OpenTimeout = TimeSpan.FromSeconds(5),
        SendTimeout = TimeSpan.FromSeconds(5),
        CloseTimeout = TimeSpan.FromSeconds(5)
    };
}
