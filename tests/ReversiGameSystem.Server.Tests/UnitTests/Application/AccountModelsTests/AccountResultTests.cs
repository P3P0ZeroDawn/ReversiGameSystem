using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountModelsTests;

[TestClass]
[TestCategory("Unit")]
public sealed class AccountResultTests
{
    [TestMethod]
    public void FailureConstructor_SuccessStatus_ThrowsArgumentOutOfRangeException()
    {
        Action action = () => _ = new AccountResult<long>(AccountStatus.Success);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [TestMethod]
    public void ValueConstructor_NullValue_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountResult<string>((string)null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void ValueConstructor_ValidValue_SetsSuccessStatus()
    {
        AccountStatus expectedStatus = AccountStatus.Success;

        AccountResult<long> actualResult = new AccountResult<long>(42);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void ValueConstructor_ValidValue_ExposesValue()
    {
        long expectedValue = 42;

        AccountResult<long> actualResult = new AccountResult<long>(42);

        Assert.AreEqual(expectedValue, actualResult.Value);
    }
}
