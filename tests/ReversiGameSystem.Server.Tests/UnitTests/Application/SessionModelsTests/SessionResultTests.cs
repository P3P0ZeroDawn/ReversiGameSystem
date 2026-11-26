using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionModelsTests;

[TestClass]
[TestCategory("Unit")]
public sealed class SessionResultTests
{
    [TestMethod]
    public void FailureConstructor_SuccessStatus_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new SessionResult<string>(SessionStatus.Success);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void ValueConstructor_NullValue_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionResult<string>((string)null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void ValueConstructor_ValidValue_SetsSuccessStatus()
    {
        SessionStatus expectedStatus = SessionStatus.Success;

        SessionResult<string> actualResult = new SessionResult<string>("token");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void ValueConstructor_ValidValue_ExposesValue()
    {
        string expectedValue = "token";

        SessionResult<string> actualResult = new SessionResult<string>("token");

        Assert.AreEqual(expectedValue, actualResult.Value);
    }
}
