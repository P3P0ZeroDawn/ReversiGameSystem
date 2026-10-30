using System;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Checks that technical context survives without copying provider secrets.</summary>
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
        TestAssert.IsTrue(output.Contains(typeof(InvalidOperationException).FullName),
            "The original failure type must remain in the safe diagnostic.");
        TestAssert.IsTrue(output.Contains(typeof(TimeoutException).FullName),
            "The cause type must remain in the safe diagnostic.");
        TestAssert.IsTrue(output.Contains(nameof(CaptureFailure)),
            "The original stack location must remain in the safe diagnostic.");
        TestAssert.IsTrue(!output.Contains("private-"),
            "The log exception must not retain provider messages or credentials.");
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

    internal static void Describe_NestedProviderFailure_ExcludesMessagesAndPrivateData()
    {
        // Arrange
        Exception cause = new TimeoutException("private-provider-message");
        Exception failure = new InvalidOperationException("private-configuration-value", cause);
        failure.Data.Add("Credential", "private-credential");

        // Act
        string diagnostic = FailureDiagnostics.Describe(failure);

        // Assert
        TestAssert.IsTrue(diagnostic.Contains(typeof(InvalidOperationException).FullName),
            "The outer failure type must remain available.");
        TestAssert.IsTrue(diagnostic.Contains(typeof(TimeoutException).FullName),
            "The underlying failure type must remain available.");
        TestAssert.IsTrue(diagnostic.Contains("HResult="), "The status must remain available.");
        TestAssert.IsTrue(!diagnostic.Contains("private-"),
            "Provider messages and private diagnostic data must not be copied.");
    }
}
