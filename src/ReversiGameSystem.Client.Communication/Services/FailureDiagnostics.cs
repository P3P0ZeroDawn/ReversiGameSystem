using System;
using System.Collections.Generic;
using System.Text;

namespace ReversiGameSystem.Services;

internal static class FailureDiagnostics
{
    internal static Exception ForLogging(Exception exception)
    {
        const int MaximumFailures = 8;
        StringBuilder diagnostic = new StringBuilder();
        List<Exception> failures = new List<Exception> { exception };
        for (int index = 0; index < failures.Count && index < MaximumFailures; index++)
        {
            Exception current = failures[index];
            diagnostic.Append(current.GetType().FullName);
            diagnostic.Append(" HResult=").Append(current.HResult);
            diagnostic.AppendLine().AppendLine(current.StackTrace);
            if (current is AggregateException aggregate)
            {
                foreach (Exception inner in aggregate.InnerExceptions)
                {
                    if (failures.Count < MaximumFailures)
                    {
                        failures.Add(inner);
                    }
                }
            }
            else if (current.InnerException != null && failures.Count < MaximumFailures)
            {
                failures.Add(current.InnerException);
            }
        }

        return new Exception(diagnostic.ToString());
    }
}
