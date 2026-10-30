using System;
using System.Text;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Formats bounded exception chains without provider messages or private data.</summary>
public static class FailureDiagnostics
{
    /// <summary>Preserves failure types, status codes and stack locations.</summary>
    /// <param name="exception">The captured failure.</param>
    /// <returns>A diagnostic description excluding exception messages and data.</returns>
    public static string Describe(Exception exception)
    {
        const int MaximumDepth = 8;
        StringBuilder diagnostic = new StringBuilder();
        Exception? current = exception;
        for (int depth = 0; current != null && depth < MaximumDepth; depth++)
        {
            diagnostic.Append(current.GetType().FullName);
            diagnostic.Append(" HResult=").Append(current.HResult);
            diagnostic.AppendLine().AppendLine(current.StackTrace);
            current = current.InnerException;
        }

        return diagnostic.ToString();
    }
}
