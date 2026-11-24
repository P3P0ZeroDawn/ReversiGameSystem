using System;
using ReversiGameSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class FailureDiagnosticsTests
{
    [TestMethod]
    public void ForLogging_ThrownFailure_PreservesOnlySafeDiagnostics()
    {
        // Arrange
        Exception failure = CaptureFailure();
        failure.Data.Add("Credential", "private-credential");

        // Act
        Exception diagnostic = FailureDiagnostics.ForLogging(failure);
        string output = diagnostic.ToString();

        // Assert
        Assert.AreEqual<Exception?>(null, diagnostic.InnerException);
        Assert.AreEqual(0, diagnostic.Data.Count);
        Assert.AreEqual(true, output.Contains(typeof(InvalidOperationException).FullName));
        Assert.AreEqual(true, output.Contains(typeof(TimeoutException).FullName));
        Assert.AreEqual(true, output.Contains(nameof(CaptureFailure)));
        Assert.AreEqual(false, output.Contains("private-"));
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
