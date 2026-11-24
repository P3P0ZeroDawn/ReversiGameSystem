using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class RemoteCallTests
{
    private const string UnusedEndpoint = "net.tcp://localhost:1/UnusedPreferences";

    [TestMethod]
    public void SendServiceCall_Timeout_PreservesCauseAndAddsCorrelation()
    {
        TimeoutException originalFailure = new TimeoutException("Timeout test.");
        void SendRequest()
        {
            SendFailingRequest(originalFailure);
        }

        TimeoutException reportedFailure = Assert.Throws<TimeoutException>(SendRequest);

        Assert.AreEqual(true, ReferenceEquals(originalFailure, reportedFailure.InnerException));
        Assert.AreEqual(true, reportedFailure.Message.Contains("CorrelationId:"));
    }

    [TestMethod]
    public void SendServiceCall_ConnectionFailure_PreservesCauseAndAddsCorrelation()
    {
        CommunicationException originalFailure = new CommunicationException("Connection test.");
        void SendRequest()
        {
            SendFailingRequest(originalFailure);
        }

        CommunicationException reportedFailure =
            Assert.Throws<CommunicationException>(SendRequest);

        Assert.AreEqual(true, ReferenceEquals(originalFailure, reportedFailure.InnerException));
        Assert.AreEqual(true, reportedFailure.Message.Contains("CorrelationId:"));
    }

    [TestMethod]
    public void SendServiceCall_ProgrammingFailure_PropagatesOriginalException()
    {
        InvalidOperationException originalFailure = new InvalidOperationException("State test.");
        void SendRequest()
        {
            SendFailingRequest(originalFailure);
        }

        InvalidOperationException reportedFailure =
            Assert.Throws<InvalidOperationException>(SendRequest);

        Assert.AreEqual(true, ReferenceEquals(originalFailure, reportedFailure));
    }

    private static void SendFailingRequest(Exception failure)
    {
        using Proxy.PlayerPreferencesServiceClient client =
            new Proxy.PlayerPreferencesServiceClient(new NetTcpBinding(SecurityMode.None),
                new EndpointAddress(UnusedEndpoint));
        Task<int> FailRequest()
        {
            return Task.FromException<int>(failure);
        }
        RemoteCall.SendServiceCall(client, FailRequest, NullLogger<Proxy.PlayerPreferencesServiceClient>.Instance)
            .GetAwaiter().GetResult();
    }
}
