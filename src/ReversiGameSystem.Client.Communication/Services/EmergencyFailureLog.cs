using System;
using System.IO;
using System.Security;
using System.Text;

namespace ReversiGameSystem.Services;

internal sealed class EmergencyFailureLog
{
    private const string TimestampFormat = "O";
    private const string LogDirectoryName = "emergency-logs";
    private readonly string[] _directories;
    private readonly object _fileLock = new object();
    private readonly string _fileName = "client-failure-" + Guid.NewGuid().ToString("N") + ".log";

    internal EmergencyFailureLog()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReversiGameSystem", LogDirectoryName),
            Path.Combine(Path.GetTempPath(), "ReversiGameSystem", LogDirectoryName))
    {
    }

    internal EmergencyFailureLog(string primaryDirectory, string secondaryDirectory)
    {
        _directories = new[] { primaryDirectory, secondaryDirectory };
    }

    internal string LastWrittenPath { get; private set; } = string.Empty;

    internal bool PersistFailure(Exception originalFailure, Exception reportingFailure,
        string operation, string correlationId)
    {
        string entry = "Operation: " + operation + ". CorrelationId: " + correlationId
            + ". ServerCorrelationId: " + RemoteCall.ReadServerCorrelation(originalFailure)
            + Environment.NewLine + FailureDiagnostics.ForLogging(originalFailure).Message;
        return PersistEntry(entry, reportingFailure);
    }

    internal bool PersistEntry(string originalEntry, Exception reportingFailure)
    {
        StringBuilder entry = new StringBuilder();
        entry.Append(DateTime.UtcNow.ToString(TimestampFormat));
        entry.Append(' ');
        entry.AppendLine(originalEntry);
        entry.Append("DiagnosticFailure: ");
        entry.Append(FailureDiagnostics.ForLogging(reportingFailure).Message);
        lock (_fileLock)
        {
            LastWrittenPath = string.Empty;
            foreach (string directory in _directories)
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    string path = Path.Combine(directory, _fileName);
                    File.AppendAllText(path, entry.ToString() + Environment.NewLine);
                    LastWrittenPath = path;
                    return true;
                }
                catch (Exception failure) when (failure is IOException
                    or UnauthorizedAccessException or SecurityException)
                {
                    entry.AppendLine();
                    entry.Append("EmergencyWriteFailure: ");
                    entry.Append(FailureDiagnostics.ForLogging(failure).Message);
                }
            }
            return false;
        }
    }
}
