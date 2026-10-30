using System;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

internal static class FailureDiagnosticsTests
{
    internal static void ForLogging_ThrownFailure_PreservesOnlySafeDiagnostics()
    {
        // Arrange
        Exception failure = CaptureFailure();
        failure.Data.Add("Credential", "private-credential");

        // Act
        Exception diagnostic = FailureDiagnostics.ForLogging(failure);
        string output = diagnostic.ToString();

        // Assert
        TestAssert.AreEqual<Exception?>(null, diagnostic.InnerException);
        TestAssert.AreEqual(0, diagnostic.Data.Count);
        TestAssert.AreEqual(true, output.Contains(typeof(InvalidOperationException).FullName));
        TestAssert.AreEqual(true, output.Contains(typeof(TimeoutException).FullName));
        TestAssert.AreEqual(true, output.Contains(nameof(CaptureFailure)));
        TestAssert.AreEqual(false, output.Contains("private-"));
    }

    private static Exception CaptureFailure()
    {
        try
        {
            throw new InvalidOperationException("private-provider-value",
                new TimeoutException("private-provider-cause"));
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }
}
