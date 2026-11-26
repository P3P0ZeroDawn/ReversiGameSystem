using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class SessionApplicationServiceConstructorTests : SessionApplicationServiceTestBase
{
    [TestMethod]
    public void Constructor_NullAccounts_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionApplicationService(
            null!,
            VerificationService,
            Sessions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullVerificationCodes_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionApplicationService(
            Accounts,
            null!,
            Sessions);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionApplicationService(
            Accounts,
            VerificationService,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }
}
