using System;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal static class ClientErrorHandlerTests
{
    internal static void HandleUnhandledFailure_ProgrammingError_RequiresShutdown()
    {
        int displayed = 0;
        ClientErrorHandler handler = new ClientErrorHandler(
            NullLogger<ClientErrorHandler>.Instance, (title, message) => displayed++);
        bool canContinue = handler.HandleUnhandledFailure(
            new InvalidOperationException("Unexpected state"), "TestOperation");
        TestAssert.AreEqual(false, canContinue);
        TestAssert.AreEqual(1, displayed);
    }

    internal static void HandleUnhandledFailure_DeclaredServerFault_PreservesApplication()
    {
        ClientErrorHandler handler = new ClientErrorHandler(
            NullLogger<ClientErrorHandler>.Instance, (title, message) => GC.KeepAlive(message));
        FaultException<Proxy.ServiceFault> fault = new FaultException<Proxy.ServiceFault>(
            new Proxy.ServiceFault { CorrelationId = "server-reference" });
        bool canContinue = handler.HandleUnhandledFailure(fault, "TestOperation");
        TestAssert.AreEqual(true, canContinue);
    }

    internal static void IsExpectedFailure_UnknownFault_DoesNotTreatAsDisconnection()
    {
        bool expected = RemoteCall.IsExpectedFailure(new FaultException("Unknown fault"));
        TestAssert.AreEqual(false, expected);
    }

    internal static void IsExpectedFailure_ProgrammingError_DoesNotTreatAsDisconnection()
    {
        bool expected = RemoteCall.IsExpectedFailure(new InvalidOperationException());
        TestAssert.AreEqual(false, expected);
    }

    internal static void CanMapFailure_UnknownMatchCode_DoesNotInventStatus()
    {
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = (Proxy.MatchFailure)int.MaxValue });
        TestAssert.AreEqual(false, MatchContractMapper.CanMapFailure(fault));
    }

    internal static void Callback_ConnectionFailure_RecordsCauseWithoutFatalNotification()
    {
        CommunicationException failure = new CommunicationException("The channel closed.");
        Exception? reported = null;
        bool fatalReported = false;
        void OnConnectionFailure(Exception exception)
        {
            reported = exception;
        }
        void OnFatalFailure(Exception exception)
        {
            fatalReported = true;
        }
        CallbackErrorBehavior.ConnectionFailureReported += OnConnectionFailure;
        CallbackErrorBehavior.FailureReported += OnFatalFailure;
        try
        {
            new CallbackErrorBehavior().HandleError(failure);
            TestAssert.AreEqual(true, ReferenceEquals(failure, reported));
            TestAssert.AreEqual(false, fatalReported);
        }
        finally
        {
            CallbackErrorBehavior.ConnectionFailureReported -= OnConnectionFailure;
            CallbackErrorBehavior.FailureReported -= OnFatalFailure;
        }
    }

    internal static void Callback_ProgrammingFailure_ReachesFatalBoundary()
    {
        InvalidOperationException failure = new InvalidOperationException("Callback state failed.");
        Exception? reported = null;
        void OnFailure(Exception exception)
        {
            reported = exception;
        }
        CallbackErrorBehavior.FailureReported += OnFailure;
        try
        {
            new CallbackErrorBehavior().HandleError(failure);
            TestAssert.AreEqual(true, ReferenceEquals(failure, reported));
        }
        finally
        {
            CallbackErrorBehavior.FailureReported -= OnFailure;
        }
    }

    internal static void FileLogger_AggregateFailure_PreservesCausesWithoutSecrets()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".log");
        try
        {
            using ClientFileLoggerProvider provider = new ClientFileLoggerProvider(path);
            ILogger logger = provider.CreateLogger(nameof(ClientErrorHandlerTests));
            AggregateException failure = new AggregateException(
                new InvalidOperationException("private-provider-value"),
                new IOException("private-cleanup-value"));
            logger.LogError(failure, "A test operation failed.");
            string output = File.ReadAllText(path);
            TestAssert.AreEqual(true, output.Contains(typeof(InvalidOperationException).FullName));
            TestAssert.AreEqual(true, output.Contains(typeof(IOException).FullName));
            TestAssert.AreEqual(false, output.Contains("private-"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
