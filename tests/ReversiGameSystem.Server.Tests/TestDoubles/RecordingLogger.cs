using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<RecordedLogEntry> _entries = new List<RecordedLogEntry>();

    public IReadOnlyList<RecordedLogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter(state, exception);
        _entries.Add(new RecordedLogEntry(logLevel, message, exception));
    }
}

internal sealed class RecordedLogEntry
{
    public RecordedLogEntry(LogLevel level, string message, Exception? exception)
    {
        Level = level;
        Message = message;
        Exception = exception;
    }

    public LogLevel Level { get; }

    public string Message { get; }

    public Exception? Exception { get; }
}
