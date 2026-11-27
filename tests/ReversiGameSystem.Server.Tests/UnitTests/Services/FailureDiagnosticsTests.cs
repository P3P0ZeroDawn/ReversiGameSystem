using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class FailureDiagnosticsTests
{
    [TestMethod]
    [DataRow("System.InvalidOperationException")]
    [DataRow("System.TimeoutException")]
    [DataRow("HResult=")]
    public void Describe_NestedProviderFailure_IncludesRequiredDiagnostics(string expectedToken)
    {
        Exception failure = CreateNestedFailure();
        bool expectedContains = true;

        string diagnostic = FailureDiagnostics.Describe(failure);
        bool actualContains = diagnostic.Contains(expectedToken);

        Assert.AreEqual(expectedContains, actualContains);
    }

    [TestMethod]
    public void Describe_NestedProviderFailure_ExcludesPrivateData()
    {
        Exception failure = CreateNestedFailure();
        bool expectedContains = false;

        string diagnostic = FailureDiagnostics.Describe(failure);
        bool actualContains = diagnostic.Contains("private-");

        Assert.AreEqual(expectedContains, actualContains);
    }

    private static Exception CreateNestedFailure()
    {
        Exception cause = new TimeoutException("private-provider-message");
        Exception failure = new InvalidOperationException("private-configuration-value", cause);
        failure.Data.Add("Credential", "private-credential");
        return failure;
    }
}
