using System;
using System.Collections.ObjectModel;
using System.IO;
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

            if (current is AggregateException aggregateException)
            {
                ReadOnlyCollection<Exception> innerExceptions =
                    aggregateException.Flatten().InnerExceptions;
                int limit = Math.Min(innerExceptions.Count, MaximumDepth);
                for (int index = 0; index < limit; index++)
                {
                    Exception inner = innerExceptions[index];
                    diagnostic.Append(inner.GetType().FullName);
                    diagnostic.Append(" HResult=").Append(inner.HResult);
                    diagnostic.AppendLine().AppendLine(inner.StackTrace);
                }

                break;
            }

            current = current.InnerException;
        }

        return diagnostic.ToString();
    }

    /// <summary>Writes bounded fallback diagnostics to stderr when the primary logger fails.</summary>
    /// <param name="context">The operation or process context description.</param>
    /// <param name="originalError">The original failure, if available.</param>
    /// <param name="loggingFailure">The exception thrown by the primary logger.</param>
    public static void WriteLoggerFallback(
        string context,
        Exception? originalError,
        Exception loggingFailure)
    {
        try
        {
            Console.Error.WriteLine($"[{DateTime.UtcNow:O}] {context}");
            if (originalError != null)
            {
                Console.Error.WriteLine("Original error:");
                Console.Error.WriteLine(Describe(originalError));
            }

            Console.Error.WriteLine("Logging failure:");
            Console.Error.WriteLine(Describe(loggingFailure));
        }
        catch (Exception writeFailure) when (writeFailure is IOException
            or ObjectDisposedException)
        {
            // No fallback left if stderr itself is unavailable.
        }
    }
}
