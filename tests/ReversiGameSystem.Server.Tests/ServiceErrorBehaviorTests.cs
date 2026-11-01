using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Contracts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ServiceErrorBehaviorTests
{
    internal void UnexpectedFailure_ThroughWcf_ReturnsSanitizedFaultAndLogsOnce()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();

        FaultException<ServiceFault> fault =
            TestAssert.Throws<FaultException<ServiceFault>>(() =>
                Invoke(logger, null, channel => channel.FailUnexpectedly()));

        TestAssert.AreEqual("UNEXPECTED_SERVICE_FAILURE", fault.Detail.FailureCode);
        TestAssert.IsTrue(Guid.TryParse(fault.Detail.CorrelationId, out Guid unused),
            "The generated correlation identifier must be a GUID.");
        TestAssert.AreEqual(1, logger.ErrorCount);
        TestAssert.IsTrue(logger.Message.Contains(fault.Detail.CorrelationId),
            "The fault and diagnostic entry must use the same correlation identifier.");
        TestAssert.IsTrue(logger.Exception is InvalidOperationException,
            "The logger must receive the original technical exception.");
        TestAssert.IsTrue(!fault.ToString().Contains("private-provider-message"),
            "The remote fault must not expose the provider message.");
    }

    internal void UnexpectedFailure_WithIncomingCorrelation_PreservesIdentifier()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();
        string correlationId = Guid.NewGuid().ToString("D");

        FaultException<ServiceFault> fault =
            TestAssert.Throws<FaultException<ServiceFault>>(() =>
                Invoke(logger, correlationId, channel => channel.FailUnexpectedly()));

        TestAssert.AreEqual(correlationId, fault.Detail.CorrelationId);
        TestAssert.IsTrue(logger.Message.Contains(correlationId),
            "The incoming correlation identifier must be preserved in diagnostics.");
    }

    internal void UnexpectedFailure_AfterCorrelationWasCreated_ReusesIdentifier()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();
        string correlationId = Guid.NewGuid().ToString("D");

        FaultException<ServiceFault> fault =
            TestAssert.Throws<FaultException<ServiceFault>>(() =>
                Invoke(logger, null, channel =>
                    channel.FailAfterCreatingCorrelation(correlationId)));

        TestAssert.AreEqual(correlationId, fault.Detail.CorrelationId);
        TestAssert.IsTrue(logger.Message.Contains(correlationId),
            "The previously created correlation identifier must be reused.");
    }

    internal void UndeclaredFault_ThroughWcf_IsSanitizedAndLogged()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();

        FaultException<ServiceFault> fault =
            TestAssert.Throws<FaultException<ServiceFault>>(() =>
                Invoke(logger, null, channel => channel.ThrowUndeclaredFault()));

        TestAssert.AreEqual("UNEXPECTED_SERVICE_FAILURE", fault.Detail.FailureCode);
        TestAssert.AreEqual(1, logger.ErrorCount);
        TestAssert.IsTrue(!fault.ToString().Contains("private-provider-message"),
            "An undeclared fault must not bypass sanitization.");
    }

    internal void LoggingFailure_ThroughWcf_StillReturnsSanitizedFault()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger
        {
            ThrowOnLog = true
        };
        TextWriter originalError = Console.Error;
        using StringWriter fallbackOutput = new StringWriter();
        try
        {
            Console.SetError(fallbackOutput);
            FaultException<ServiceFault> fault =
                TestAssert.Throws<FaultException<ServiceFault>>(() =>
                    Invoke(logger, null, channel => channel.FailUnexpectedly()));

            TestAssert.AreEqual("UNEXPECTED_SERVICE_FAILURE", fault.Detail.FailureCode);
            TestAssert.IsTrue(!fallbackOutput.ToString().Contains("private-"),
                "Fallback diagnostics must not expose provider messages.");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    internal void BusinessFault_ThroughWcf_PreservesTypeWithoutTechnicalLog()
    {
        RecordingErrorLogger logger = new RecordingErrorLogger();

        FaultException<LeaderboardFault> fault =
            TestAssert.Throws<FaultException<LeaderboardFault>>(() =>
                Invoke(logger, null, channel => channel.RejectAsBusinessFault()));

        TestAssert.AreEqual(
            LeaderboardFailure.InvalidMaximumEntries,
            fault.Detail.Reason);
        TestAssert.AreEqual(0, logger.ErrorCount);
    }

    private static void Invoke(
        RecordingErrorLogger logger,
        string? correlationId,
        Action<IErrorProbeService> action)
    {
        Uri address = new Uri("net.pipe://localhost/ReversiGameSystem.ServiceErrorBehaviorTests/"
            + Guid.NewGuid().ToString("N") + "/");
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        ServiceHost host = new ServiceHost(new ErrorProbeService(), address);
        host.Description.Behaviors.Add(new ServiceErrorBehavior(logger));
        host.AddServiceEndpoint(typeof(IErrorProbeService), binding, "Service");
        host.Open();

        ChannelFactory<IErrorProbeService> factory = new ChannelFactory<IErrorProbeService>(
            binding, new EndpointAddress(new Uri(address, "Service")));
        IErrorProbeService channel = factory.CreateChannel();
        try
        {
            using (OperationContextScope scope = new OperationContextScope(
                (IContextChannel)channel))
            {
                if (correlationId != null)
                {
                    MessageHeader header = MessageHeader.CreateHeader(
                        "CorrelationId",
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

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
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
        throw new InvalidOperationException("private-provider-message");
    }

    public void FailAfterCreatingCorrelation(string correlationId)
    {
        MessageHeader header = MessageHeader.CreateHeader(
            "CorrelationId",
            ContractNamespaces.Diagnostics,
            correlationId);
        OperationContext.Current.OutgoingMessageHeaders.Add(header);
        throw new InvalidOperationException("private-provider-message");
    }

    public void ThrowUndeclaredFault()
    {
        throw new FaultException("private-provider-message");
    }

    public void RejectAsBusinessFault()
    {
        throw new FaultException<LeaderboardFault>(new LeaderboardFault
        {
            Reason = LeaderboardFailure.InvalidMaximumEntries
        }, "The leaderboard request was rejected.");
    }
}
