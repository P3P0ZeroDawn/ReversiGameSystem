using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.VerificationModelsTests;

[TestClass]
[TestCategory("Unit")]
public sealed class VerificationIdentityTests
{
    [TestMethod]
    public void ForLogin_ValidInput_BuildsNormalizedIdentityKey()
    {
        string expectedKey = "login:42:PLAYER@EXAMPLE.COM";

        VerificationIdentity actualIdentity = VerificationIdentity.ForLogin(
            42,
            "  player@example.com  ");

        Assert.AreEqual(expectedKey, actualIdentity.Key);
    }

    [TestMethod]
    public void ForRegistration_ValidInput_BuildsNormalizedIdentityKey()
    {
        string expectedKey = "registration:9:PLAYERONEPLAYER@EXAMPLE.COM";

        VerificationIdentity actualIdentity = VerificationIdentity.ForRegistration(
            "  PlayerOne  ",
            "  player@example.com  ");

        Assert.AreEqual(expectedKey, actualIdentity.Key);
    }

    [TestMethod]
    public void ForPasswordReset_ValidInput_BuildsNormalizedIdentityKey()
    {
        string expectedKey = "password-reset:42:PLAYER@EXAMPLE.COM";

        VerificationIdentity actualIdentity = VerificationIdentity.ForPasswordReset(
            42,
            "  player@example.com  ");

        Assert.AreEqual(expectedKey, actualIdentity.Key);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ForLogin_BlankLocale_DefaultsToEnglish(string? preferredLocaleCode)
    {
        string expectedLocaleCode = "en-US";

        VerificationIdentity actualIdentity = VerificationIdentity.ForLogin(
            42,
            "player@example.com",
            preferredLocaleCode);

        Assert.AreEqual(expectedLocaleCode, actualIdentity.PreferredLocaleCode);
    }

    [TestMethod]
    public void ForLogin_CustomLocale_TrimsLocale()
    {
        string expectedLocaleCode = "es-MX";

        VerificationIdentity actualIdentity = VerificationIdentity.ForLogin(
            42,
            "player@example.com",
            "  es-MX  ");

        Assert.AreEqual(expectedLocaleCode, actualIdentity.PreferredLocaleCode);
    }
}
