using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionStoreTests;

[TestClass]
[TestCategory("Unit")]
public sealed class AuthenticatedSessionTests : SessionStoreTestBase
{
    [TestMethod]
    public void TryAddAuthenticated_NullSession_ThrowsArgumentNullException()
    {
        Action action = () => Store.TryAddAuthenticated(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void TryAddAuthenticated_DuplicateToken_ReturnsFalse()
    {
        AuthenticatedSession first = CreateAuthenticatedSession("token-1");
        AuthenticatedSession duplicate = CreateAuthenticatedSession("token-1");
        Store.TryAddAuthenticated(first);

        bool actualAdded = Store.TryAddAuthenticated(duplicate);

        Assert.IsFalse(actualAdded);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void FindAuthenticated_MissingToken_ReturnsMissingSessionToken(string? token)
    {
        SessionStatus expectedStatus = SessionStatus.MissingSessionToken;

        SessionResult<AuthenticatedSession> actualResult = Store.FindAuthenticated(token!);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void FindAuthenticated_UnknownToken_ReturnsInvalidSession()
    {
        SessionStatus expectedStatus = SessionStatus.InvalidSession;

        SessionResult<AuthenticatedSession> actualResult = Store.FindAuthenticated("unknown");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void FindAuthenticated_ActiveToken_ReturnsSession()
    {
        AuthenticatedSession session = CreateAuthenticatedSession("token-1", accountId: 42);
        Store.TryAddAuthenticated(session);
        long? expectedAccountId = 42;

        SessionResult<AuthenticatedSession> actualResult = Store.FindAuthenticated("token-1");

        Assert.AreEqual(expectedAccountId, actualResult.Value?.AccountId);
    }

    [TestMethod]
    public void FindAuthenticated_ExpiredToken_ReturnsInvalidSessionAndRaisesRevoked()
    {
        AuthenticatedSession session = CreateAuthenticatedSession("token-1");
        Store.TryAddAuthenticated(session);
        string? actualRevokedToken = null;
        Store.SessionRevoked += token => actualRevokedToken = token;
        Clock.UtcNow = session.ExpiresAtUtc;
        SessionStatus expectedStatus = SessionStatus.InvalidSession;
        string expectedRevokedToken = "token-1";

        SessionResult<AuthenticatedSession> actualResult = Store.FindAuthenticated("token-1");

        Assert.AreEqual(expectedStatus, actualResult.Status);
        Assert.AreEqual(expectedRevokedToken, actualRevokedToken);
    }

    [TestMethod]
    public void RevokeSession_ActiveToken_InvalidatesSession()
    {
        AuthenticatedSession session = CreateAuthenticatedSession("token-1");
        Store.TryAddAuthenticated(session);
        SessionStatus expectedStatus = SessionStatus.InvalidSession;

        Store.RevokeSession("token-1");
        SessionResult<AuthenticatedSession> actualResult = Store.FindAuthenticated("token-1");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void RevokeAccountSessions_MatchingAccount_RevokesAllMatchingTokensOnly()
    {
        Store.TryAddAuthenticated(CreateAuthenticatedSession("token-a1", accountId: 42));
        Store.TryAddAuthenticated(CreateAuthenticatedSession("token-a2", accountId: 42));
        Store.TryAddAuthenticated(CreateAuthenticatedSession("token-b1", accountId: 99));
        List<string> actualRevoked = new List<string>();
        Store.SessionRevoked += token => actualRevoked.Add(token);
        int expectedRevokedCount = 2;
        SessionStatus expectedOtherStatus = SessionStatus.Success;

        Store.RevokeAccountSessions(42);
        SessionResult<AuthenticatedSession> actualOther = Store.FindAuthenticated("token-b1");

        Assert.AreEqual(expectedRevokedCount, actualRevoked.Count);
        Assert.AreEqual(expectedOtherStatus, actualOther.Status);
    }

    [TestMethod]
    public void RemoveExpiredSessions_MixedSessions_RemovesOnlyExpired()
    {
        AuthenticatedSession expired = CreateAuthenticatedSession("expired-1");
        Store.TryAddAuthenticated(expired);
        Clock.UtcNow = Clock.UtcNow.AddMinutes(10);
        AuthenticatedSession active = CreateAuthenticatedSession("active-1");
        Store.TryAddAuthenticated(active);
        Clock.UtcNow = expired.ExpiresAtUtc;
        SessionStatus expectedExpiredStatus = SessionStatus.InvalidSession;
        SessionStatus expectedActiveStatus = SessionStatus.Success;

        Store.RemoveExpiredSessions();
        SessionResult<AuthenticatedSession> actualExpired = Store.FindAuthenticated("expired-1");
        SessionResult<AuthenticatedSession> actualActive = Store.FindAuthenticated("active-1");

        Assert.AreEqual(expectedExpiredStatus, actualExpired.Status);
        Assert.AreEqual(expectedActiveStatus, actualActive.Status);
    }
}
