using System;
using System.Runtime.Serialization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Localization;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal sealed class AuthenticationOutcomeMapperTests
{
    internal void Map_SuccessfulOutcome_PreservesSession()
    {
        Proxy.AuthenticatedSession remote = CreateSession();
        AuthenticationOutcome outcome = AuthenticationOutcomeMapper.Map(remote);
        TestAssert.AreEqual(AuthenticationStatus.Success, outcome.Status);
        AuthenticatedAccountSession session = TestAssert.IsNotNull(outcome.Session);
        TestAssert.AreEqual(remote.SessionToken, session.SessionToken);
        TestAssert.AreEqual<long>(42, session.AccountId);
        TestAssert.AreEqual("PlayerOne", session.Username);
        TestAssert.AreEqual("en-US", session.PreferredLocaleCode);
        TestAssert.AreEqual(remote.ExpiresAtUtc, session.ExpiresAtUtc);
    }

    internal void Map_ExpiredSession_RejectsResponse()
    {
        Proxy.AuthenticatedSession remote = CreateSession();
        remote.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
        AssertRejected(remote);
    }

    internal void Map_MissingToken_RejectsResponse()
    {
        Proxy.AuthenticatedSession remote = CreateSession();
        remote.SessionToken = string.Empty;
        AssertRejected(remote);
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
                "Session_CloseFailedTitle",
                "Session_CloseFailedMessage", "LoginScreen_VerificationCodeDialogMessage"
            })
            {
                TestAssert.AreEqual(false,
                    string.IsNullOrWhiteSpace(LocalizationManager.Instance[key]));
            }
            TestAssert.AreEqual(false, LocalizationManager.Instance[
                "LoginScreen_VerificationCodeDialogMessage"].Contains("{0}"));
            TestAssert.AreEqual("Guest-Player", GuestNameFormatter.Format("Guest-Player"));
        }
    }

    private static Proxy.AuthenticatedSession CreateSession() => new Proxy.AuthenticatedSession
    {
        SessionToken = "disposable-session-token",
        Account = new Proxy.AccountReference { AccountId = 42, Username = "PlayerOne" },
        EmailAddress = "player@example.com", PreferredLocaleCode = "en-US",
        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
    };

    private static void AssertRejected(Proxy.AuthenticatedSession remote)
    {
        try
        {
            AuthenticationOutcomeMapper.Map(remote);
        }
        catch (SerializationException)
        {
            return;
        }
        throw new InvalidOperationException("An invalid session was accepted.");
    }
}
