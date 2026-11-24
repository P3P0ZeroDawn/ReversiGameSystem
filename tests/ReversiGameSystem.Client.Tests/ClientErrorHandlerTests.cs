using System;
using System.IO;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Services;
using ReversiGameSystem.Localization;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class ClientErrorHandlerTests
{
    [TestMethod]
    public void ReportFatalFailure_ProgrammingError_DisplaysFatalMessage()
    {
        string displayedTitle = string.Empty;
        ClientErrorHandler handler = new ClientErrorHandler(
            NullLogger<ClientErrorHandler>.Instance, (title, message) => displayedTitle = title);

        handler.ReportFatalFailure(
            new InvalidOperationException("Unexpected state"), "TestOperation");

        Assert.AreEqual(LocalizationManager.Instance["Client_FatalErrorTitle"], displayedTitle);
    }

    [TestMethod]
    public void ReportFatalFailure_UnhandledDeclaredFault_DisplaysFatalMessage()
    {
        string displayedTitle = string.Empty;
        ClientErrorHandler handler = new ClientErrorHandler(
            NullLogger<ClientErrorHandler>.Instance, (title, message) => displayedTitle = title);
        FaultException<Proxy.ServiceFault> fault = new FaultException<Proxy.ServiceFault>(
            new Proxy.ServiceFault { CorrelationId = "server-reference" });

        handler.ReportFatalFailure(fault, "TestOperation");

        Assert.AreEqual(LocalizationManager.Instance["Client_FatalErrorTitle"], displayedTitle);
    }

    [TestMethod]
    public void IsConnectionFailure_UnknownFault_DoesNotTreatAsDisconnection()
    {
        bool expected = RemoteCall.IsConnectionFailure(new FaultException("Unknown fault"));
        Assert.AreEqual(false, expected);
    }

    [TestMethod]
    public void IsConnectionFailure_ProgrammingError_DoesNotTreatAsDisconnection()
    {
        bool expected = RemoteCall.IsConnectionFailure(new InvalidOperationException());
        Assert.AreEqual(false, expected);
    }

    [TestMethod]
    public void CanMapFailure_UnknownMatchCode_DoesNotInventStatus()
    {
        FaultException<Proxy.MatchFault> fault = new FaultException<Proxy.MatchFault>(
            new Proxy.MatchFault { Reason = (Proxy.MatchFailure)int.MaxValue });
        Assert.AreEqual(false, MatchContractMapper.CanMapFailure(fault));
    }

    [TestMethod]
    public void Callback_ConnectionFailure_RecordsCauseWithoutFatalNotification()
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
            Assert.AreEqual(true, ReferenceEquals(failure, reported));
            Assert.AreEqual(false, fatalReported);
        }
        finally
        {
            CallbackErrorBehavior.ConnectionFailureReported -= OnConnectionFailure;
            CallbackErrorBehavior.FailureReported -= OnFatalFailure;
        }
    }

    [TestMethod]
    public void Callback_ProgrammingFailure_ReachesFatalBoundary()
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
            Assert.AreEqual(true, ReferenceEquals(failure, reported));
        }
        finally
        {
            CallbackErrorBehavior.FailureReported -= OnFailure;
        }
    }

    [TestMethod]
    public void FileLogger_AggregateFailure_PreservesCausesWithoutSecrets()
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
            Assert.AreEqual(true, output.Contains(typeof(InvalidOperationException).FullName));
            Assert.AreEqual(true, output.Contains(typeof(IOException).FullName));
            Assert.AreEqual(false, output.Contains("private-"));
        }
        finally
        {
            File.Delete(path);
        }
    }
    [TestMethod]
    public void RecordFailure_PrimaryLoggerFails_PersistsBothCausesAndCorrelation()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Reversi-fallback-"
            + Guid.NewGuid().ToString("N"));
        EmergencyFailureLog emergency = new EmergencyFailureLog(directory, directory);
        ClientErrorHandler handler = new ClientErrorHandler(new FailingLogger(),
            (title, message) => GC.KeepAlive(message), emergency);
        FaultException<Proxy.ServiceFault> failure = new FaultException<Proxy.ServiceFault>(
            new Proxy.ServiceFault { CorrelationId = "server-test-reference" });
        try
        {
            string correlation = handler.RecordFailure(failure, "RecoveryTest", LogLevel.Critical);

            string output = File.ReadAllText(emergency.LastWrittenPath);
            Assert.AreEqual(true, output.Contains(correlation));
            Assert.AreEqual(true, output.Contains("server-test-reference"));
            Assert.AreEqual(true, output.Contains("RecoveryTest"));
            Assert.AreEqual(true, output.Contains(typeof(IOException).FullName));
            Assert.AreEqual(true, output.Contains(typeof(FaultException<Proxy.ServiceFault>)
                .FullName));
            Assert.AreEqual(false, output.Contains("private-logger-value"));
        }
        finally
        {
            if (emergency.LastWrittenPath.Length > 0)
            {
                File.Delete(emergency.LastWrittenPath);
            }
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory);
            }
        }
    }

    [TestMethod]
    public void FileLogger_PrimaryFileUnavailable_PersistsOriginalEventInBackup()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Reversi-file-fallback-"
            + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        EmergencyFailureLog emergency = new EmergencyFailureLog(directory, directory);
        try
        {
            using ClientFileLoggerProvider provider =
                new ClientFileLoggerProvider(directory, emergency);
            ILogger logger = provider.CreateLogger("FallbackOperation");

            logger.LogError(new InvalidOperationException("private-original-value"),
                "CorrelationId: fallback-reference");

            string output = File.ReadAllText(emergency.LastWrittenPath);
            Assert.AreEqual(true, output.Contains("fallback-reference"));
            Assert.AreEqual(true, output.Contains("FallbackOperation"));
            Assert.AreEqual(true, output.Contains(typeof(InvalidOperationException).FullName));
            Assert.AreEqual(true, output.Contains(
                typeof(UnauthorizedAccessException).FullName));
            Assert.AreEqual(false, output.Contains("private-original-value"));
        }
        finally
        {
            if (emergency.LastWrittenPath.Length > 0)
            {
                File.Delete(emergency.LastWrittenPath);
            }
            Directory.Delete(directory);
        }
    }

    [TestMethod]
    public void RecordFailure_AllMediaUnavailable_ReportsUnavailableDiagnostics()
    {
        string blocker = Path.Combine(Path.GetTempPath(), "Reversi-blocked-"
            + Guid.NewGuid().ToString("N"));
        File.WriteAllText(blocker, string.Empty);
        string directory = Path.Combine(blocker, "logs");
        EmergencyFailureLog emergency = new EmergencyFailureLog(directory, directory);
        ClientErrorHandler handler = new ClientErrorHandler(new FailingLogger(),
            (title, message) => GC.KeepAlive(message), emergency);
        TextWriter previous = Console.Error;
        using StringWriter output = new StringWriter();
        try
        {
            Console.SetError(output);

            handler.RecordFailure(new InvalidOperationException("private-original-value"),
                "UnavailableDiagnosticsTest", LogLevel.Critical);

            Assert.AreEqual(true, handler.DiagnosticsUnavailable);
            Assert.AreEqual(string.Empty, emergency.LastWrittenPath);
        }
        finally
        {
            Console.SetError(previous);
            File.Delete(blocker);
        }
    }

    private sealed class FailingLogger : ILogger<ClientErrorHandler>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullLogger.Instance.BeginScope(state)
                ?? throw new InvalidOperationException("A logging scope is required.");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            throw new IOException("private-logger-value");
        }
    }
}
