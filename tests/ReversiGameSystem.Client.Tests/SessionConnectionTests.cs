using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

/// <summary>Checks the actual client adapter and generated proxy against a running host.</summary>
internal static class SessionConnectionTests
{
    internal static void GuestLifecycle_ThroughGeneratedProxy()
    {
        SessionClient client = new SessionClient(NullLogger<SessionClient>.Instance, CreateProxy);
        string name = "Test_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        SessionResult<GuestAccountSession> firstResult =
            client.OpenGuestSession(name).GetAwaiter().GetResult();
        TestAssert.AreEqual(SessionStatus.Success, firstResult.Status);
        GuestAccountSession first = TestAssert.IsNotNull(firstResult.Value);
        try
        {
            TestAssert.AreEqual("Guest-" + name, first.DisplayName);
            TestAssert.AreEqual(false, string.IsNullOrWhiteSpace(first.SessionToken));
            AssertNameReserved(client, name);
            client.CloseSession(first.SessionToken).GetAwaiter().GetResult();
            client.CloseSession(first.SessionToken).GetAwaiter().GetResult();
            SessionResult<GuestAccountSession> secondResult =
                client.OpenGuestSession(name).GetAwaiter().GetResult();
            TestAssert.AreEqual(SessionStatus.Success, secondResult.Status);
            GuestAccountSession second = TestAssert.IsNotNull(secondResult.Value);
            try
            {
                TestAssert.AreEqual("Guest-" + name, second.DisplayName);
                TestAssert.AreEqual(false, first.SessionToken == second.SessionToken);
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
        TestAssert.AreEqual(SessionStatus.GuestNameUnavailable, result.Status);
    }

    private static Proxy.SessionServiceClient CreateProxy()
    {
        NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
        return new Proxy.SessionServiceClient(binding, new EndpointAddress(
            "net.tcp://localhost:8091/ReversiGameSystem/Session"));
    }
}
