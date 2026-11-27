using System;
using Microsoft.Extensions.Logging.Abstractions;
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
            Sessions,
            NullLogger<SessionApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullVerificationCodes_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionApplicationService(
            Accounts,
            null!,
            Sessions,
            NullLogger<SessionApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionApplicationService(
            Accounts,
            VerificationService,
            null!,
            NullLogger<SessionApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionApplicationService(
            Accounts,
            VerificationService,
            Sessions,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }
}
