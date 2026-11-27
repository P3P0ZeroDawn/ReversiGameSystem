using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.AccountApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class AccountApplicationServiceConstructorTests : AccountApplicationServiceTestBase
{
    [TestMethod]
    public void Constructor_NullAccounts_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountApplicationService(
            null!,
            Images,
            VerificationService,
            Sessions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullImages_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountApplicationService(
            Accounts,
            null!,
            VerificationService,
            Sessions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullVerificationCodes_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountApplicationService(
            Accounts,
            Images,
            null!,
            Sessions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountApplicationService(
            Accounts,
            Images,
            VerificationService,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }
}
