using System;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Client.Tests;

internal sealed class SessionModelsTests
{
    internal void SuccessfulResult_PreservesSession()
    {
        AuthenticatedAccountSession session = CreateSession();

        SessionResult<AuthenticatedAccountSession> result =
            new SessionResult<AuthenticatedAccountSession>(session);

        TestAssert.AreEqual(SessionStatus.Success, result.Status);
        TestAssert.AreEqual(session, result.Value);
    }

    internal void FailureResult_DoesNotContainSession()
    {
        SessionResult<AuthenticatedAccountSession> result =
            new SessionResult<AuthenticatedAccountSession>(
                SessionStatus.InvalidCredentials);

        TestAssert.AreEqual(SessionStatus.InvalidCredentials, result.Status);
        TestAssert.AreEqual<AuthenticatedAccountSession?>(null, result.Value);
    }

    internal void SuccessStatusWithoutValue_IsRejected()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            new SessionResult<AuthenticatedAccountSession>(SessionStatus.Success));
    }

    internal void SessionMessages_SupportedCultures_AreAvailable()
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
                TestAssert.AreEqual(
                    false,
                    string.IsNullOrWhiteSpace(LocalizationManager.Instance[key]));
            }

            TestAssert.AreEqual(
                false,
                LocalizationManager.Instance[
                    "LoginScreen_VerificationCodeDialogMessage"].Contains("{0}"));
            TestAssert.AreEqual("Guest-Player", GuestNameFormatter.Format("Guest-Player"));
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
