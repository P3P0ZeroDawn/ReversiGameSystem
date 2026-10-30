using System;
using System.Text;

namespace ReversiGameSystem.Services;

internal static class FailureDiagnostics
{
    internal static Exception ForLogging(Exception exception)
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

        return new Exception(diagnostic.ToString());
    }
}
