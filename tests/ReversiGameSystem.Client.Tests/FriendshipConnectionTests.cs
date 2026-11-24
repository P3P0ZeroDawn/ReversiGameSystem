using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FriendshipConnectionTests
{
    [TestMethod]
    public void GeneratedProxy_ReceivesPresenceAndPropagatesTypedSessionFault()
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
            Assert.AreEqual(false, client.GetFriends("test-session")
                .GetAwaiter().GetResult().Friends[0].IsOnline);
            Assert.AreEqual(true, Guid.TryParse(service.LastCorrelationId, out _));
            TestAssert.IsNotNull(service.Callback).OnFriendChanged(2, Proxy.FriendChange.Connected);
            Assert.AreEqual(true, SpinWait.SpinUntil(
                () => client.ReadFriends()[0].IsOnline, TimeSpan.FromSeconds(5)));
            service.Callback!.OnFriendChanged(2, Proxy.FriendChange.Disconnected);
            Assert.AreEqual(true, SpinWait.SpinUntil(
                () => !client.ReadFriends()[0].IsOnline, TimeSpan.FromSeconds(5)));
            service.Callback.OnFriendChanged(2, Proxy.FriendChange.Removed);
            Assert.AreEqual(true, SpinWait.SpinUntil(
                () => client.ReadFriends().Length == 0, TimeSpan.FromSeconds(5)));
            service.RejectSession = true;
            FriendshipResult rejected = client.GetFriends("test-session").GetAwaiter().GetResult();
            Assert.AreEqual(FriendshipStatus.InvalidSession, rejected.Status);
            Assert.AreEqual(false, client.IsConnected);
            service.RejectSession = false;
            Assert.AreEqual(1, client.GetFriends("test-session")
                .GetAwaiter().GetResult().Friends.Length);
            client.Close();
            Assert.AreEqual(false, client.IsConnected);
            Assert.AreEqual(0, client.ReadFriends().Length);
            host.Close();
        }
        finally
        {
            host.Abort();
        }
    }

    [TestMethod]
    public void Keepalive_CloseWhileRefreshPending_DoesNotReopenChannel()
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
            client.GetFriends("test-session").GetAwaiter().GetResult();
            Assert.AreEqual(true, client.IsConnected);
            Assert.AreEqual(1, client.ReadFriends().Length);

            using ManualResetEventSlim started = new();
            using ManualResetEventSlim release = new();
            service.RequestStarted = started;
            service.ContinueRequest = release;
            Task refresh = client.GetFriends("test-session");
            Assert.AreEqual(true, started.Wait(TimeSpan.FromSeconds(5)));
            Task close = Task.Run(() => client.Close());
            Assert.AreEqual(false, close.Wait(TimeSpan.FromMilliseconds(100)));
            release.Set();
            Task.WhenAll(refresh, close).GetAwaiter().GetResult();
            Assert.AreEqual(false, client.IsConnected);
            Assert.AreEqual(0, client.ReadFriends().Length);
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
