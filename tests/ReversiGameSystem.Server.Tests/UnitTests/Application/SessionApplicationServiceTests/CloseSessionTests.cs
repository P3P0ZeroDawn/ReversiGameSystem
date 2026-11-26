using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CloseSessionTests : SessionApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void CloseSession_MissingToken_ReturnsMissingSessionToken(string? sessionToken)
    {
        SessionStatus expectedStatus = SessionStatus.MissingSessionToken;

        SessionStatus actualStatus = Service.CloseSession(sessionToken!);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void CloseSession_ActiveAuthenticatedSession_RevokesSessionAndReturnsSuccess()
    {
        AuthenticatedSession session = TestSessionHelper.CreateSession(
            Sessions,
            42,
            "PlayerOne");
        SessionStatus expectedStatus = SessionStatus.Success;
        SessionStatus expectedStoreStatus = SessionStatus.InvalidSession;

        SessionStatus actualStatus = Service.CloseSession(session.SessionToken);
        SessionResult<AuthenticatedSession> storedResult =
            Sessions.FindAuthenticated(session.SessionToken);

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedStoreStatus, storedResult.Status);
    }

    [TestMethod]
    public void CloseSession_UnknownToken_ReturnsSuccess()
    {
        SessionStatus expectedStatus = SessionStatus.Success;

        SessionStatus actualStatus = Service.CloseSession("unknown-token");

        Assert.AreEqual(expectedStatus, actualStatus);
    }
}
