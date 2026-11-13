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
                _logger.LogCritical(exception, "The server process failed unexpectedly.");
            }
            else
            {
                _logger.LogCritical("The server process failed with an unknown error.");
            }
        }
        catch (Exception loggingFailure)
        {
            WriteFallback(exception, loggingFailure);
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
            WriteFallback(eventArguments.Exception, loggingFailure);
        }
        finally
        {
            eventArguments.SetObserved();
        }
    }

    private static void WriteFallback(Exception? failure, Exception loggingFailure)
    {
        string failureDescription = failure == null
            ? "Unknown error."
            : FailureDiagnostics.Describe(failure);
        Console.Error.WriteLine(
            "The process diagnostic logger failed.{0}{1}{0}{2}",
            Environment.NewLine,
            failureDescription,
            FailureDiagnostics.Describe(loggingFailure));
    }
}
