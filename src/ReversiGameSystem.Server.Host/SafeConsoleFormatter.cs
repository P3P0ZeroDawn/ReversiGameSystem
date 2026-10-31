using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Host;

/// <summary>
/// Writes technical diagnostics without exception messages or private exception data.
/// </summary>
public sealed class SafeConsoleFormatter : ConsoleFormatter
{
    /// <summary>Identifies the server's diagnostic console format.</summary>
    public const string FormatterName = "safe-diagnostics";

    /// <summary>Initializes the diagnostic console formatter.</summary>
    public SafeConsoleFormatter() : base(FormatterName)
    {
    }

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        textWriter.Write(logEntry.LogLevel);
        textWriter.Write(": ");
        textWriter.Write(logEntry.Category);
        textWriter.Write('[');
        textWriter.Write(logEntry.EventId.Id);
        textWriter.Write("] ");
        scopeProvider?.ForEachScope(WriteScope, textWriter);
        textWriter.WriteLine(logEntry.Formatter(logEntry.State, null));
        if (logEntry.Exception != null)
        {
            textWriter.Write(FailureDiagnostics.Describe(logEntry.Exception));
        }
    }

    private static void WriteScope(object? scope, TextWriter writer)
    {
        writer.Write("=> ");
        if (scope is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            foreach (KeyValuePair<string, object?> property in properties)
            {
                if (property.Key == "{OriginalFormat}")
                {
                    continue;
                }
                writer.Write(property.Key);
                writer.Write('=');
                writer.Write(property.Value);
                writer.Write(' ');
            }
        }
        else
        {
            writer.Write(scope);
            writer.Write(' ');
        }
    }
}
