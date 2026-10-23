using System;
using System.Net.Security;
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
        Proxy.GuestSession first = client.OpenGuestSessionAsync(name).GetAwaiter().GetResult();
        try
        {
            TestAssert.AreEqual("Guest-" + name, first.DisplayName);
            TestAssert.AreEqual(false, string.IsNullOrWhiteSpace(first.SessionToken));
            AssertNameReserved(client, name);
            client.CloseSessionAsync(first.SessionToken).GetAwaiter().GetResult();
            client.CloseSessionAsync(first.SessionToken).GetAwaiter().GetResult();
            Proxy.GuestSession second = client.OpenGuestSessionAsync(name).GetAwaiter().GetResult();
            try
            {
                TestAssert.AreEqual("Guest-" + name, second.DisplayName);
                TestAssert.AreEqual(false, first.SessionToken == second.SessionToken);
            }
            finally
            {
                client.CloseSessionAsync(second.SessionToken).GetAwaiter().GetResult();
            }
        }
        finally
        {
            client.CloseSessionAsync(first.SessionToken).GetAwaiter().GetResult();
        }
    }

    private static void AssertNameReserved(SessionClient client, string name)
    {
        try
        {
            client.OpenGuestSessionAsync(name).GetAwaiter().GetResult();
        }
        catch (FaultException<Proxy.SessionFault> fault)
        {
            TestAssert.AreEqual(Proxy.SessionFailure.GuestNameUnavailable, fault.Detail.Reason);
            return;
        }

        throw new InvalidOperationException("The host accepted a duplicate active guest name.");
    }

    private static Proxy.SessionServiceClient CreateProxy()
    {
        NetTcpBinding binding = new NetTcpBinding(SecurityMode.Transport);
        binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
        binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
        return new Proxy.SessionServiceClient(binding, new EndpointAddress(
            "net.tcp://localhost:8091/ReversiGameSystem/Session"));
    }
}
