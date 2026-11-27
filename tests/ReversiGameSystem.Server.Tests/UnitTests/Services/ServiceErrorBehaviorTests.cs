using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class ServiceErrorBehaviorTests
{
    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ServiceErrorBehavior(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void ProvideFault_DeclaredBusinessFault_LeavesFaultMessageNull()
    {
        RecordingLogger logger = new RecordingLogger();
        ServiceErrorBehavior behavior = new ServiceErrorBehavior(logger);
        FaultException<SessionFault> error = new FaultException<SessionFault>(
            new SessionFault
            {
                Reason = SessionFailure.InvalidSession
            },
            "Invalid session.");
        Message? fault = null;

        behavior.ProvideFault(error, MessageVersion.Soap12WSAddressing10, ref fault!);

        Assert.IsNull(fault);
    }

    [TestMethod]
    public void ProvideFault_UnexpectedException_CreatesFaultMessageWithoutPrivateDetails()
    {
        RecordingLogger logger = new RecordingLogger();
        ServiceErrorBehavior behavior = new ServiceErrorBehavior(logger);
        InvalidOperationException error =
            new InvalidOperationException("private-provider-secret");
        Message? fault = null;
        bool expectedIsFault = true;
        bool expectedContainsSecret = false;

        behavior.ProvideFault(error, MessageVersion.Soap12WSAddressing10, ref fault!);

        Assert.IsNotNull(fault);
        bool actualIsFault = fault.IsFault;
        bool actualContainsSecret = fault.ToString().Contains("private-provider-secret");
        Assert.AreEqual(expectedIsFault, actualIsFault);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    [TestMethod]
    public void HandleError_DeclaredBusinessFault_ReturnsTrueWithoutLogging()
    {
        RecordingLogger logger = new RecordingLogger();
        ServiceErrorBehavior behavior = new ServiceErrorBehavior(logger);
        FaultException<SessionFault> error = new FaultException<SessionFault>(
            new SessionFault
            {
                Reason = SessionFailure.MissingSessionToken
            },
            "Missing token.");
        int expectedLogCount = 0;

        bool actualHandled = behavior.HandleError(error);
        int actualLogCount = logger.ErrorCount;

        Assert.IsTrue(actualHandled);
        Assert.AreEqual(expectedLogCount, actualLogCount);
    }

    [TestMethod]
    public void HandleError_UnexpectedException_LogsOnceEvenIfInvokedTwice()
    {
        RecordingLogger logger = new RecordingLogger();
        ServiceErrorBehavior behavior = new ServiceErrorBehavior(logger);
        InvalidOperationException error = new InvalidOperationException("Boom");
        int expectedLogCount = 1;

        bool actualFirstHandled = behavior.HandleError(error);
        bool actualSecondHandled = behavior.HandleError(error);
        int actualLogCount = logger.ErrorCount;

        Assert.IsTrue(actualFirstHandled);
        Assert.IsTrue(actualSecondHandled);
        Assert.AreEqual(expectedLogCount, actualLogCount);
    }

    [TestMethod]
    public void HandleError_FatalException_LogsAndReturnsFalse()
    {
        RecordingLogger logger = new RecordingLogger();
        ServiceErrorBehavior behavior = new ServiceErrorBehavior(logger);
        OutOfMemoryException error = new OutOfMemoryException("Out of memory");
        int expectedLogCount = 1;

        bool actualHandled = behavior.HandleError(error);
        int actualLogCount = logger.ErrorCount;

        Assert.IsFalse(actualHandled);
        Assert.AreEqual(expectedLogCount, actualLogCount);
    }

    private sealed class RecordingLogger : ILogger<ServiceErrorBehavior>
    {
        internal int ErrorCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Error;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                ErrorCount++;
            }
        }
    }
}
