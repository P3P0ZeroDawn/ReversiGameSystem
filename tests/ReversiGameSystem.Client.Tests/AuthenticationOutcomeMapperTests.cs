using System;
using ReversiGameSystem.Services;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

internal sealed class AuthenticationOutcomeMapperTests
{
    internal void Map_SuccessfulOutcome_PreservesSession()
    {
        // Arrange
        DateTime expiration = new DateTime(
            2026,
            10,
            17,
            2,
            0,
            0,
            DateTimeKind.Utc);
        Proxy.AuthenticationOutcome remoteOutcome = new Proxy.AuthenticationOutcome
        {
            Status = Proxy.AuthenticationStatus.Success,
            Session = new Proxy.AuthenticatedSession
            {
                SessionToken = "session-token",
                Account = new Proxy.AccountReference
                {
                    AccountId = 42,
                    Username = "PlayerOne"
                },
                EmailAddress = "player@example.com",
                PreferredLocaleCode = "en-US",
                ExpiresAtUtc = expiration
            }
        };

        // Act
        AuthenticationOutcome outcome = AuthenticationOutcomeMapper.Map(remoteOutcome);

        // Assert
        TestAssert.AreEqual(AuthenticationStatus.Success, outcome.Status);
        AuthenticatedAccountSession session = TestAssert.IsNotNull(outcome.Session);
        TestAssert.AreEqual("session-token", session.SessionToken);
        TestAssert.AreEqual<long>(42, session.AccountId);
        TestAssert.AreEqual("PlayerOne", session.Username);
        TestAssert.AreEqual("en-US", session.PreferredLocaleCode);
        TestAssert.AreEqual(expiration, session.ExpiresAtUtc);
    }

    internal void Map_PersistenceUnavailable_ReturnsServiceUnavailable()
    {
        // Arrange
        Proxy.AuthenticationOutcome remoteOutcome = new Proxy.AuthenticationOutcome
        {
            Status = Proxy.AuthenticationStatus.PersistenceUnavailable
        };

        // Act
        AuthenticationOutcome outcome = AuthenticationOutcomeMapper.Map(remoteOutcome);

        // Assert
        TestAssert.AreEqual(AuthenticationStatus.ServiceUnavailable, outcome.Status);
        TestAssert.AreEqual<AuthenticatedAccountSession?>(null, outcome.Session);
    }
}
