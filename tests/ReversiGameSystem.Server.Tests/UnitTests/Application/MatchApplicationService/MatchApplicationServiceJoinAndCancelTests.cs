using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.MatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchApplicationServiceJoinAndCancelTests
    : MatchApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(null, MatchStatus.MissingSessionToken)]
    [DataRow("invalid-token", MatchStatus.InvalidSession)]
    public void EvaluateLobbyAccess_RejectedSession_ReturnsFailureWithoutRepositoryCall(
        string? sessionToken,
        MatchStatus expectedStatus)
    {
        int expectedCallCount = 0;

        MatchStatus actualStatus = Service.EvaluateLobbyAccess(sessionToken!);
        int actualCallCount = Matches.EvaluateLobbyAccessCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void EvaluateLobbyAccess_ValidSession_ReturnsRepositoryStatus()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.InactiveAccount;
        Matches.LobbyAccessStatus = expectedStatus;

        MatchStatus actualStatus = Service.EvaluateLobbyAccess(token);

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void GetAvailableMatches_ValidSession_ReturnsRepositoryMatches()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Match[] expectedMatches = new Match[]
        {
            new Match
            {
                Id = 1L
            }
        };
        Matches.AvailableMatchesResult = new MatchResult<Match[]>(expectedMatches);
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<Match[]> actualResult = Service.GetAvailableMatches(token);
        MatchStatus actualStatus = actualResult.Status;
        Match[]? actualMatches = actualResult.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedMatches, actualMatches);
    }

    [TestMethod]
    public void JoinMatch_MissingSessionToken_ReturnsMissingSessionTokenWithoutRepositoryCall()
    {
        MatchStatus expectedStatus = MatchStatus.MissingSessionToken;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.JoinMatch(" ", "ABC234");
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.JoinMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("ABC")]
    [DataRow("ABC123")]
    [DataRow("ABC0EF")]
    public void JoinMatch_InvalidMatchCode_ReturnsInvalidMatchCodeWithoutRepositoryCall(
        string? code)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.InvalidMatchCode;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.JoinMatch(token, code!);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.JoinMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void JoinMatch_GuestSessionWithValidCode_ReturnsUnauthorizedWithoutJoining()
    {
        string token = AddGuestSession("guest-token");
        MatchStatus expectedStatus = MatchStatus.Unauthorized;
        int expectedJoinCalls = 0;

        MatchResult<Match> actualResult = Service.JoinMatch(token, "ABC234");
        MatchStatus actualStatus = actualResult.Status;
        int actualJoinCalls = Matches.JoinMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedJoinCalls, actualJoinCalls);
    }

    [TestMethod]
    public void JoinMatch_GuestSessionWithUnavailableMatch_ReturnsValidationFailure()
    {
        string token = AddGuestSession("guest-token");
        MatchStatus expectedStatus = MatchStatus.MatchNotFound;
        Matches.ValidateGuestJoinStatus = expectedStatus;
        int expectedJoinCalls = 0;

        MatchResult<Match> actualResult = Service.JoinMatch(token, "ABC234");
        MatchStatus actualStatus = actualResult.Status;
        int actualJoinCalls = Matches.JoinMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedJoinCalls, actualJoinCalls);
    }

    [TestMethod]
    public void JoinMatch_ValidSessionAndCode_NormalizesCodeAndRaisesMatchStarted()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Match expectedMatch = new Match
        {
            Id = 55L
        };
        Matches.JoinMatchResult = new MatchResult<Match>(expectedMatch);
        Match? startedMatch = null;
        Service.MatchStarted += match => startedMatch = match;
        MatchStatus expectedStatus = MatchStatus.Success;
        string expectedNormalizedCode = "ABC234";

        MatchResult<Match> actualResult = Service.JoinMatch(token, " abc234 ");
        MatchStatus actualStatus = actualResult.Status;
        string actualCode = Matches.LastCode;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedNormalizedCode, actualCode);
        Assert.AreSame(expectedMatch, startedMatch);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-2L)]
    public void CancelMatch_NonPositiveMatchId_ReturnsInvalidMatchIdWithoutRepositoryCall(
        long matchId)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.InvalidMatchId;
        int expectedCallCount = 0;

        MatchStatus actualStatus = Service.CancelMatch(token, matchId);
        int actualCallCount = Matches.CancelMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void CancelWaitingForSession_WithTrackedMatch_CancelsAndReturnsMatchId()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        long expectedMatchId = 42L;
        Matches.CreateMatchResult = new MatchResult<Match>(new Match
        {
            Id = expectedMatchId
        });
        Service.CreateMatch(token, new MatchSettings());
        int expectedCount = 1;

        long[] actualCancelledIds = Service.CancelWaitingForSession(token);
        int actualCount = actualCancelledIds.Length;
        long actualMatchId = actualCancelledIds[0];

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedMatchId, actualMatchId);
    }

    [TestMethod]
    public void CancelExpiredWaitingSessions_OnlyExpiredSession_CancelsExpiredAndKeepsActive()
    {
        string expiredToken = AddAuthenticatedSession(10L, "PlayerTen");
        long expectedCancelledId = 42L;
        Matches.CreateMatchResult = new MatchResult<Match>(new Match
        {
            Id = expectedCancelledId
        });
        Service.CreateMatch(expiredToken, new MatchSettings());
        Clock.UtcNow = Clock.UtcNow.AddHours(25);
        string activeToken = AddAuthenticatedSession(20L, "PlayerTwenty");
        Matches.CreateMatchResult = new MatchResult<Match>(new Match
        {
            Id = 43L
        });
        Service.CreateMatch(activeToken, new MatchSettings());
        int expectedCount = 1;

        long[] actualCancelledIds = Service.CancelExpiredWaitingSessions();
        int actualCount = actualCancelledIds.Length;
        long actualCancelledId = actualCancelledIds[0];

        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedCancelledId, actualCancelledId);
    }
}
