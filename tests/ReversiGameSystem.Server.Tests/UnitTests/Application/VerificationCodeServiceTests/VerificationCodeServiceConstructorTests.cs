using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationCodeServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class VerificationCodeServiceConstructorTests : VerificationCodeServiceTestBase
{
    [TestMethod]
    public void Constructor_NullProtector_ThrowsArgumentNullException()
    {
        Action action = () => _ = new VerificationCodeService(
            null!,
            Repository,
            EmailSender,
            Clock);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action action = () => _ = new VerificationCodeService(
            Protector,
            null!,
            EmailSender,
            Clock);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullEmailSender_ThrowsArgumentNullException()
    {
        Action action = () => _ = new VerificationCodeService(
            Protector,
            Repository,
            null!,
            Clock);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        Action action = () => _ = new VerificationCodeService(
            Protector,
            Repository,
            EmailSender,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }
}
