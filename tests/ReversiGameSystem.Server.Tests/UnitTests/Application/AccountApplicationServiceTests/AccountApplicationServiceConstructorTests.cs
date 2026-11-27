using System;
using Microsoft.Extensions.Logging.Abstractions;
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
            Sessions,
            NullLogger<AccountApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullImages_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountApplicationService(
            Accounts,
            null!,
            VerificationService,
            Sessions,
            NullLogger<AccountApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullVerificationCodes_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountApplicationService(
            Accounts,
            Images,
            null!,
            Sessions,
            NullLogger<AccountApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountApplicationService(
            Accounts,
            Images,
            VerificationService,
            null!,
            NullLogger<AccountApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action action = () => _ = new AccountApplicationService(
            Accounts,
            Images,
            VerificationService,
            Sessions,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }
}
