using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.SessionApplicationServiceTests;

[TestClass]
[TestCategory("Unit")]
public sealed class OpenGuestSessionTests : SessionApplicationServiceTestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow("ab")]
    public void OpenGuestSession_InvalidDisplayName_ReturnsInvalidGuestName(string displayName)
    {
        SessionStatus expectedStatus = SessionStatus.InvalidGuestName;

        SessionResult<GuestSession> actualResult = Service.OpenGuestSession(displayName);

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }

    [TestMethod]
    public void OpenGuestSession_ValidDisplayName_PrefixesGuestDisplayName()
    {
        string expectedDisplayName = "Guest-PlayerOne";

        SessionResult<GuestSession> actualResult = Service.OpenGuestSession("  PlayerOne  ");

        Assert.AreEqual(expectedDisplayName, actualResult.Value?.DisplayName);
    }

    [TestMethod]
    public void OpenGuestSession_ValidDisplayName_MarksGuestActiveInStore()
    {
        SessionResult<GuestSession> actualResult = Service.OpenGuestSession("PlayerOne");
        bool actualActive = Sessions.IsGuestActive(actualResult.Value!.SessionToken);

        Assert.IsTrue(actualActive);
    }

    [TestMethod]
    public void OpenGuestSession_DuplicateActiveName_ReturnsGuestNameUnavailable()
    {
        Service.OpenGuestSession("PlayerOne");
        SessionStatus expectedStatus = SessionStatus.GuestNameUnavailable;

        SessionResult<GuestSession> actualResult = Service.OpenGuestSession("playerone");

        Assert.AreEqual(expectedStatus, actualResult.Status);
    }
}
