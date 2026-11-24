using System;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
public sealed class SessionModelsTests
{
    [TestMethod]
    public void SuccessfulResult_PreservesSession()
    {
        AuthenticatedAccountSession session = CreateSession();

        SessionResult<AuthenticatedAccountSession> result =
            new SessionResult<AuthenticatedAccountSession>(session);

        Assert.AreEqual(SessionStatus.Success, result.Status);
        Assert.AreEqual(session, result.Value);
    }

    [TestMethod]
    public void FailureResult_DoesNotContainSession()
    {
        SessionResult<AuthenticatedAccountSession> result =
            new SessionResult<AuthenticatedAccountSession>(
                SessionStatus.InvalidCredentials);

        Assert.AreEqual(SessionStatus.InvalidCredentials, result.Status);
        Assert.AreEqual<AuthenticatedAccountSession?>(null, result.Value);
    }

    [TestMethod]
    public void SuccessStatusWithoutValue_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SessionResult<AuthenticatedAccountSession>(SessionStatus.Success));
    }

    [TestMethod]
    public void SessionMessages_SupportedCultures_AreAvailable()
    {
        foreach (string culture in new[] { "en-US", "es-MX" })
        {
            LocalizationManager.Instance.CultureName = culture;
            foreach (string key in new[]
            {
                "LoginScreen_InvalidCodeTitle", "LoginScreen_InvalidCodeMessage",
                "LoginScreen_ExpiredCodeTitle", "LoginScreen_ExpiredCodeMessage",
                "Session_CloseFailedTitle", "Session_CloseFailedMessage",
                "LoginScreen_VerificationCodeDialogMessage"
            })
            {
                Assert.AreEqual(
                    false,
                    string.IsNullOrWhiteSpace(LocalizationManager.Instance[key]));
            }

            Assert.AreEqual(
                false,
                LocalizationManager.Instance[
                    "LoginScreen_VerificationCodeDialogMessage"].Contains("{0}"));
            Assert.AreEqual("Guest-Player", GuestNameFormatter.Format("Guest-Player"));
        }
    }

    private static AuthenticatedAccountSession CreateSession()
    {
        return new AuthenticatedAccountSession(
            "disposable-session-token",
            42,
            "PlayerOne",
            "player@example.com",
            "en-US",
            DateTime.UtcNow.AddMinutes(10));
    }
}
