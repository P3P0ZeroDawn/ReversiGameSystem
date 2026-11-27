using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class ServiceErrorBehaviorTests
{
    private const string UnexpectedFailureCode = "UNEXPECTED_SERVICE_FAILURE";
    internal const string PrivateProviderMessage = "private-provider-message";
    internal const string CorrelationHeaderName = "CorrelationId";

    [TestMethod]
    public void UnexpectedFailure_ThroughWcf_ReturnsSanitizedFaultAndLogsOnce()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();
        string expectedFailureCode = UnexpectedFailureCode;
        int expectedErrorCount = 1;
        bool expectedContainsSecret = false;
        Action action = () => Invoke(logger, null, channel => channel.FailUnexpectedly());

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        string actualFailureCode = fault.Detail.FailureCode;
        bool actualIsGuid = Guid.TryParse(fault.Detail.CorrelationId, out _);
        int actualErrorCount = logger.ErrorCount;
        bool actualLogHasCorrelation = logger.Message.Contains(fault.Detail.CorrelationId);
        bool actualContainsSecret = fault.ToString().Contains(PrivateProviderMessage);

        Assert.AreEqual(expectedFailureCode, actualFailureCode);
        Assert.IsTrue(actualIsGuid);
        Assert.AreEqual(expectedErrorCount, actualErrorCount);
        Assert.IsTrue(actualLogHasCorrelation);
        Assert.IsInstanceOfType(logger.Exception, typeof(InvalidOperationException));
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    [TestMethod]
    public void UnexpectedFailure_WithIncomingCorrelation_PreservesIdentifier()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();
        string expectedCorrelationId = Guid.NewGuid().ToString("D");
        Action action = () => Invoke(
            logger,
            expectedCorrelationId,
            channel => channel.FailUnexpectedly());

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        string actualCorrelationId = fault.Detail.CorrelationId;
        bool actualLogHasCorrelation = logger.Message.Contains(expectedCorrelationId);

        Assert.AreEqual(expectedCorrelationId, actualCorrelationId);
        Assert.IsTrue(actualLogHasCorrelation);
    }

    [TestMethod]
    public void UnexpectedFailure_AfterCorrelationWasCreated_ReusesIdentifier()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();
        string expectedCorrelationId = Guid.NewGuid().ToString("D");
        Action action = () => Invoke(
            logger,
            null,
            channel => channel.FailAfterCreatingCorrelation(expectedCorrelationId));

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        string actualCorrelationId = fault.Detail.CorrelationId;
        bool actualLogHasCorrelation = logger.Message.Contains(expectedCorrelationId);

        Assert.AreEqual(expectedCorrelationId, actualCorrelationId);
        Assert.IsTrue(actualLogHasCorrelation);
    }

    [TestMethod]
    public void UndeclaredFault_ThroughWcf_IsSanitizedAndLogged()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();
        string expectedFailureCode = UnexpectedFailureCode;
        int expectedErrorCount = 1;
        bool expectedContainsSecret = false;
        Action action = () => Invoke(logger, null, channel => channel.ThrowUndeclaredFault());

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        string actualFailureCode = fault.Detail.FailureCode;
        int actualErrorCount = logger.ErrorCount;
        bool actualContainsSecret = fault.ToString().Contains(PrivateProviderMessage);

        Assert.AreEqual(expectedFailureCode, actualFailureCode);
        Assert.AreEqual(expectedErrorCount, actualErrorCount);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    [TestMethod]
    public void LoggingFailure_ThroughWcf_StillReturnsSanitizedFault()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger
        {
            ThrowOnLog = true
        };
        string expectedFailureCode = UnexpectedFailureCode;
        bool expectedContainsSecret = false;
        TextWriter originalError = Console.Error;
        using StringWriter fallbackOutput = new StringWriter();
        try
        {
            Console.SetError(fallbackOutput);
            Action action = () => Invoke(logger, null, channel => channel.FailUnexpectedly());

            FaultException<ServiceFault> fault =
                Assert.Throws<FaultException<ServiceFault>>(action);
            string actualFailureCode = fault.Detail.FailureCode;
            bool actualContainsSecret = fallbackOutput.ToString().Contains("private-");

            Assert.AreEqual(expectedFailureCode, actualFailureCode);
            Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [TestMethod]
    public void BusinessFault_ThroughWcf_PreservesTypeWithoutTechnicalLog()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();
        LeaderboardFailure expectedReason = LeaderboardFailure.InvalidMaximumEntries;
        int expectedErrorCount = 0;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        CommunicationState actualChannelState = CommunicationState.Created;
        Action action = () => Invoke(logger, null, channel =>
        {
            try
            {
                channel.RejectAsBusinessFault();
            }
            catch
            {
                actualChannelState = ((ICommunicationObject)channel).State;
                throw;
            }
        });

        FaultException<LeaderboardFault> fault =
            Assert.Throws<FaultException<LeaderboardFault>>(action);
        LeaderboardFailure actualReason = fault.Detail.Reason;
        int actualErrorCount = logger.ErrorCount;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedErrorCount, actualErrorCount);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    private static void Invoke(
        RecordingErrorLogger logger,
        string? correlationId,
        Action<IErrorProbeService> action)
    {
        Uri address = new Uri(
            "net.pipe://localhost/ReversiGameSystem.ServiceErrorBehaviorTests/"
            + Guid.NewGuid().ToString("N") + "/");
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        ServiceHost host = new ServiceHost(new ErrorProbeService(), address);
        host.Description.Behaviors.Add(new ServiceErrorBehavior(logger));
        host.AddServiceEndpoint(typeof(IErrorProbeService), binding, "Service");
        host.Open();

        ChannelFactory<IErrorProbeService> factory = new ChannelFactory<IErrorProbeService>(
            binding,
            new EndpointAddress(new Uri(address, "Service")));
        IErrorProbeService channel = factory.CreateChannel();
        try
        {
            using (OperationContextScope scope = new OperationContextScope(
                (IContextChannel)channel))
            {
                if (correlationId != null)
                {
                    MessageHeader header = MessageHeader.CreateHeader(
                        CorrelationHeaderName,
                        ContractNamespaces.Diagnostics,
                        correlationId);
                    OperationContext.Current.OutgoingMessageHeaders.Add(header);
                }

                action(channel);
            }

            ((ICommunicationObject)channel).Close();
            factory.Close();
            host.Close();
        }
        catch
        {
            ((ICommunicationObject)channel).Abort();
            factory.Abort();
            host.Abort();
            throw;
        }
    }

    private sealed class RecordingErrorLogger : ILogger<ServiceErrorBehavior>
    {
        internal int ErrorCount { get; private set; }

        internal Exception? Exception { get; private set; }

        internal string Message { get; private set; } = string.Empty;

        internal bool ThrowOnLog { get; set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Error)
            {
                return;
            }

            ErrorCount++;
            Exception = exception;
            Message = formatter(state, exception);
            if (ThrowOnLog)
            {
                throw new InvalidOperationException("private-logger-message");
            }
        }
    }
}

[ServiceContract(Namespace = ContractNamespaces.Services)]
internal interface IErrorProbeService
{
    [OperationContract]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    void FailUnexpectedly();

    [OperationContract]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    void FailAfterCreatingCorrelation(string correlationId);

    [OperationContract]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    void ThrowUndeclaredFault();

    [OperationContract]
    [FaultContract(typeof(LeaderboardFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    void RejectAsBusinessFault();
}

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
internal sealed class ErrorProbeService : IErrorProbeService
{
    public void FailUnexpectedly()
    {
        throw new InvalidOperationException(ServiceErrorBehaviorTests.PrivateProviderMessage);
    }

    public void FailAfterCreatingCorrelation(string correlationId)
    {
        MessageHeader header = MessageHeader.CreateHeader(
            ServiceErrorBehaviorTests.CorrelationHeaderName,
            ContractNamespaces.Diagnostics,
            correlationId);
        OperationContext.Current.OutgoingMessageHeaders.Add(header);
        throw new InvalidOperationException(ServiceErrorBehaviorTests.PrivateProviderMessage);
    }

    public void ThrowUndeclaredFault()
    {
        throw new FaultException(ServiceErrorBehaviorTests.PrivateProviderMessage);
    }

    public void RejectAsBusinessFault()
    {
        throw new FaultException<LeaderboardFault>(
            new LeaderboardFault
            {
                Reason = LeaderboardFailure.InvalidMaximumEntries
            },
            "The leaderboard request was rejected.");
    }
}
