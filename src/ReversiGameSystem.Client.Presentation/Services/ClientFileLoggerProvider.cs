using System;
using System.IO;
using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReversiGameSystem.Services;

internal sealed class ClientFileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _logFileLock = new object();
    private bool _disposed;
    private readonly EmergencyFailureLog _emergencyLog;

    internal ClientFileLoggerProvider(string filePath)
        : this(filePath, new EmergencyFailureLog())
    {
    }

    internal ClientFileLoggerProvider(string filePath, EmergencyFailureLog emergencyLog)
    {
        _emergencyLog = emergencyLog ?? throw new ArgumentNullException(nameof(emergencyLog));
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(this, categoryName);
    }

    private void Write(LogLevel level, string category, string message, Exception? exception)
    {
        string entry = DateTime.UtcNow.ToString("O") + " " + level + " " + category + " " + message;
        if (exception != null)
        {
            entry += Environment.NewLine + FailureDiagnostics.ForLogging(exception).Message;
        }
        lock (_logFileLock)
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                string directory = Path.GetDirectoryName(_filePath)
                    ?? throw new InvalidOperationException("The log directory is unavailable.");
                Directory.CreateDirectory(directory);
                File.AppendAllText(_filePath, entry + Environment.NewLine);
            }
            catch (Exception failure) when (failure is IOException
                or UnauthorizedAccessException or SecurityException)
            {
                if (!_emergencyLog.PersistEntry(entry, failure))
                {
                    throw;
                }
            }
        }
    }

    public void Dispose()
    {
        lock (_logFileLock)
        {
            _disposed = true;
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly ClientFileLoggerProvider _provider;
        private readonly string _category;

        internal FileLogger(ClientFileLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullLogger.Instance.BeginScope(state)
                ?? throw new InvalidOperationException("The logging scope is unavailable.");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information && logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _provider.Write(logLevel, _category, formatter(state, exception), exception);
            }
        }
    }
}
