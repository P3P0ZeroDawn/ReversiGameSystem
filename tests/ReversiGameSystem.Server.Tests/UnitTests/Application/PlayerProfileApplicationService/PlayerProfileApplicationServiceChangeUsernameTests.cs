using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Players;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PlayerProfileApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class PlayerProfileApplicationServiceChangeUsernameTests
    : PlayerProfileApplicationServiceTestBase
{
    [TestMethod]
    public void ChangeUsername_MissingSessionToken_ReturnsMissingSessionTokenWithoutRepositoryCall()
    {
        ProfileStatus expectedStatus = ProfileStatus.MissingSessionToken;
        int expectedCallCount = 0;

        ProfileStatus actualStatus = Service.ChangeUsername(" ", "ValidName");
        int actualCallCount = Profiles.ChangeUsernameCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ChangeUsername_InvalidSession_ReturnsInvalidSessionWithoutRepositoryCall()
    {
        ProfileStatus expectedStatus = ProfileStatus.InvalidSession;
        int expectedCallCount = 0;

        ProfileStatus actualStatus = Service.ChangeUsername("invalid-token", "ValidName");
        int actualCallCount = Profiles.ChangeUsernameCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow("ab")]
    [DataRow("bad name!")]
    public void ChangeUsername_InvalidUsername_ReturnsInvalidUsernameWithoutRepositoryCall(
        string? username)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        ProfileStatus expectedStatus = ProfileStatus.InvalidUsername;
        int expectedCallCount = 0;

        ProfileStatus actualStatus = Service.ChangeUsername(token, username!);
        int actualCallCount = Profiles.ChangeUsernameCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ChangeUsername_ValidUsernameWithWhitespace_TrimsAndDelegatesToRepository()
    {
        long expectedAccountId = 10L;
        string token = AddAuthenticatedSession(expectedAccountId, "PlayerTen");
        string expectedUsername = "NewPlayerName";
        ProfileStatus expectedStatus = ProfileStatus.Success;

        ProfileStatus actualStatus = Service.ChangeUsername(token, "  NewPlayerName  ");
        long actualAccountId = Profiles.LastAccountId;
        string actualUsername = Profiles.LastUsername;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedAccountId, actualAccountId);
        Assert.AreEqual(expectedUsername, actualUsername);
    }

    [TestMethod]
    public void ChangeUsername_RepositoryRejectsUnavailable_ReturnsUsernameUnavailable()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Profiles.ChangeUsernameResult = ProfileStatus.UsernameUnavailable;
        ProfileStatus expectedStatus = ProfileStatus.UsernameUnavailable;

        ProfileStatus actualStatus = Service.ChangeUsername(token, "TakenName");

        Assert.AreEqual(expectedStatus, actualStatus);
    }
}
