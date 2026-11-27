using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.AccountServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class AccountServiceConstructorTests
{
    [TestMethod]
    public void Constructor_NullApplicationService_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountService(null!);

        Assert.Throws<ArgumentNullException>(action);
    }
}
