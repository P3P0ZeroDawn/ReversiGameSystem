using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Host;

internal sealed class PeriodicTaskRunner : IDisposable
{
    private const int TickIdle = 0;
    private const int TickRunning = 1;
    private readonly string _operationName;
    private readonly TimeSpan _interval;
    private readonly Action _action;
    private readonly ILogger _logger;
    private readonly Timer _timer;
    private int _running;
    private int _disposed;

    internal PeriodicTaskRunner(
        string operationName,
        TimeSpan interval,
        Action action,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException(
                "Operation name must not be empty.",
                nameof(operationName));
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                interval,
                "Interval must be positive.");
        }

        _operationName = operationName;
        _interval = interval;
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timer = new Timer(
            ExecuteTick,
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    internal void Start()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(PeriodicTaskRunner));
        }

        _timer.Change(_interval, _interval);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _timer.Dispose();
    }

    private void ExecuteTick(object? state)
    {
        if (Volatile.Read(ref _disposed) != 0
            || Interlocked.Exchange(ref _running, TickRunning) != TickIdle)
        {
            return;
        }

        try
        {
            _action();
        }
        catch (Exception exception)
        {
            string correlationId = Guid.NewGuid().ToString("D");
            try
            {
                _logger.LogError(
                    exception,
                    "The {Operation} periodic task failed. CorrelationId: {CorrelationId}",
                    _operationName,
                    correlationId);
            }
            catch (Exception loggingFailure)
            {
                string context =
                    "The periodic task diagnostic logger failed. Operation: "
                    + _operationName
                    + ". CorrelationId: "
                    + correlationId
                    + ".";
                FailureDiagnostics.WriteLoggerFallback(context, exception, loggingFailure);
            }
        }
        finally
        {
            Volatile.Write(ref _running, TickIdle);
        }
    }
}
