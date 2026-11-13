using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Checks that technical context survives without copying provider secrets.</summary>
[TestClass]
public sealed class FailureDiagnosticsTests
{
    [TestMethod]
    public void Describe_NestedProviderFailure_ExcludesMessagesAndPrivateData()
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
