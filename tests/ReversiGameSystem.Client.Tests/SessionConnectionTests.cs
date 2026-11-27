using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

/// <summary>Checks the actual client adapter and generated proxy against a running host.</summary>
[TestClass]
[TestCategory("Integration")]
public sealed class SessionConnectionTests
{
    [TestMethod]
    public void GuestLifecycle_ThroughGeneratedProxy()
    {
        using InMemoryClientWcfHost host = new InMemoryClientWcfHost();
        SessionClient client = new SessionClient(
            NullLogger<SessionClient>.Instance,
            () => new Proxy.SessionServiceClient(
                InMemoryClientWcfHost.CreateBinding(),
                host.SessionEndpoint));
        string name = "Test_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        SessionResult<GuestAccountSession> firstResult =
            client.OpenGuestSession(name).GetAwaiter().GetResult();
        Assert.AreEqual(SessionStatus.Success, firstResult.Status);
        GuestAccountSession first = TestAssert.IsNotNull(firstResult.Value);
        try
        {
            Assert.AreEqual("Guest-" + name, first.DisplayName);
            Assert.AreEqual(false, string.IsNullOrWhiteSpace(first.SessionToken));
            AssertNameReserved(client, name);
            client.CloseSession(first.SessionToken).GetAwaiter().GetResult();
            client.CloseSession(first.SessionToken).GetAwaiter().GetResult();
            SessionResult<GuestAccountSession> secondResult =
                client.OpenGuestSession(name).GetAwaiter().GetResult();
            Assert.AreEqual(SessionStatus.Success, secondResult.Status);
            GuestAccountSession second = TestAssert.IsNotNull(secondResult.Value);
            try
            {
                Assert.AreEqual("Guest-" + name, second.DisplayName);
                Assert.AreEqual(false, first.SessionToken == second.SessionToken);
            }
            finally
            {
                client.CloseSession(second.SessionToken).GetAwaiter().GetResult();
            }
        }
        finally
        {
            client.CloseSession(first.SessionToken).GetAwaiter().GetResult();
        }
    }

    private static void AssertNameReserved(SessionClient client, string name)
    {
        SessionResult<GuestAccountSession> result =
            client.OpenGuestSession(name).GetAwaiter().GetResult();
        Assert.AreEqual(SessionStatus.GuestNameUnavailable, result.Status);
    }
}
