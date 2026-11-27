using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationCodeServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CalculatePlausibleExpirationUtcTests : VerificationCodeServiceTestBase
{
    [TestMethod]
    public void CalculatePlausibleExpirationUtc_ValidClock_AddsFiveMinutes()
    {
        DateTime expectedExpirationUtc = Clock.UtcNow.AddMinutes(5);

        DateTime actualExpirationUtc = Service.CalculatePlausibleExpirationUtc();

        Assert.AreEqual(expectedExpirationUtc, actualExpirationUtc);
    }
}
