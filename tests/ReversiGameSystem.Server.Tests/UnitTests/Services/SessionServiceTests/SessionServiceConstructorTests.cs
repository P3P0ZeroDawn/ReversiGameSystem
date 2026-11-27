using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.UnitTests.Services.SessionServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class SessionServiceConstructorTests : SessionServiceTestBase
{
    [TestMethod]
    public void Constructor_NullApplicationService_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionService(
            null!,
            NullLogger<SessionService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action action = () => _ = new SessionService(ApplicationService, null!);

        Assert.Throws<ArgumentNullException>(action);
    }
}
