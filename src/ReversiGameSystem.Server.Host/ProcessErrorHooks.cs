using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        if (exception != null)
        {
            _logger.LogCritical(exception, "The server process failed unexpectedly.");
        }
        else
        {
            _logger.LogCritical("The server process failed with an unknown error.");
        }
    }

    private void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs eventArguments)
    {
        _logger.LogError(
            eventArguments.Exception,
            "An unobserved background task failed.");
        eventArguments.SetObserved();
    }
}
