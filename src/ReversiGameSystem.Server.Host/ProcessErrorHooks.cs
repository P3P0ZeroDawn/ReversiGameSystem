using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Host;

internal sealed class ProcessErrorHooks : IDisposable
{
    private readonly ILogger<ProcessErrorHooks> _logger;

    internal ProcessErrorHooks(ILogger<ProcessErrorHooks> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnUnhandledException(
        object sender,
        UnhandledExceptionEventArgs eventArguments)
    {
        Exception? exception = eventArguments.ExceptionObject as Exception;
        try
        {
            if (exception != null)
            {
                _logger.LogCritical(
                    exception,
                    "The server process failed unexpectedly. IsTerminating: {IsTerminating}",
                    eventArguments.IsTerminating);
            }
            else
            {
                string exceptionType = eventArguments.ExceptionObject?.GetType().FullName
                    ?? "UnknownExceptionType";
                _logger.LogCritical(
                    "The server process failed with non-exception type {ExceptionType}. "
                    + "IsTerminating: {IsTerminating}",
                    exceptionType,
                    eventArguments.IsTerminating);
            }
        }
        catch (Exception loggingFailure)
        {
            FailureDiagnostics.WriteLoggerFallback(
                "The process diagnostic logger failed.",
                exception,
                loggingFailure);
        }
    }

    private void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs eventArguments)
    {
        try
        {
            _logger.LogError(
                eventArguments.Exception,
                "An unobserved background task failed.");
        }
        catch (Exception loggingFailure)
        {
            FailureDiagnostics.WriteLoggerFallback(
                "The process diagnostic logger failed.",
                eventArguments.Exception,
                loggingFailure);
        }
        finally
        {
            eventArguments.SetObserved();
        }
    }
}
