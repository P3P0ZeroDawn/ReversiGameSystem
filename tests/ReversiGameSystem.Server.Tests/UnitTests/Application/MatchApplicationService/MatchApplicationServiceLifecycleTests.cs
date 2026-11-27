using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.MatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class MatchApplicationServiceLifecycleTests : MatchApplicationServiceTestBase
{
    [TestMethod]
    [DataRow(null, MatchStatus.MissingSessionToken)]
    [DataRow("invalid-token", MatchStatus.InvalidSession)]
    public void StartSystemMatch_RejectedSession_ReturnsFailureWithoutRepositoryCall(
        string? sessionToken,
        MatchStatus expectedStatus)
    {
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.StartSystemMatch(sessionToken!, 10L);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.StartSystemMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void StartSystemMatch_ValidSession_StartsMatchAndRaisesMatchStarted()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        Match expectedMatch = new Match
        {
            Id = 70L,
            State = "InProgress"
        };
        Matches.StartSystemMatchResult = new MatchResult<Match>(expectedMatch);
        Match? startedMatch = null;
        Service.MatchStarted += match => startedMatch = match;
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<Match> actualResult = Service.StartSystemMatch(token, 70L);
        MatchStatus actualStatus = actualResult.Status;
        Match? actualMatch = actualResult.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedMatch, actualMatch);
        Assert.AreSame(expectedMatch, startedMatch);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-4L)]
    public void GetParticipantMatch_NonPositiveMatchId_ReturnsInvalidMatchIdWithoutRepositoryCall(
        long matchId)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.InvalidMatchId;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.GetParticipantMatch(token, matchId);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.LoadParticipantMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetParticipantMatch_ValidMatchId_ReturnsRepositoryMatch()
    {
        long expectedAccountId = 10L;
        long expectedMatchId = 70L;
        string token = AddAuthenticatedSession(expectedAccountId, "PlayerTen");
        Match expectedMatch = new Match
        {
            Id = expectedMatchId
        };
        Matches.LoadParticipantMatchResult = new MatchResult<Match>(expectedMatch);
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<Match> actualResult = Service.GetParticipantMatch(token, expectedMatchId);
        MatchStatus actualStatus = actualResult.Status;
        Match? actualMatch = actualResult.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedMatch, actualMatch);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    public void ChangeTurnDuration_NonPositiveMatchId_ReturnsInvalidMatchIdWithoutRepositoryCall(
        long matchId)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.InvalidMatchId;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.ChangeTurnDuration(token, matchId, 30);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.ChangeTurnDurationCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(9)]
    [DataRow(121)]
    public void ChangeTurnDuration_OutOfRangeSeconds_ReturnsInvalidTurnDurationWithoutCall(
        int seconds)
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        MatchStatus expectedStatus = MatchStatus.InvalidTurnDuration;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.ChangeTurnDuration(token, 70L, seconds);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.ChangeTurnDurationCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void ChangeTurnDuration_ValidArguments_DelegatesToRepository()
    {
        string token = AddAuthenticatedSession(10L, "PlayerTen");
        int expectedSeconds = 60;
        Match expectedMatch = new Match
        {
            Id = 70L,
            TurnDurationSeconds = expectedSeconds
        };
        Matches.ChangeTurnDurationResult = new MatchResult<Match>(expectedMatch);
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<Match> actualResult =
            Service.ChangeTurnDuration(token, 70L, expectedSeconds);
        MatchStatus actualStatus = actualResult.Status;
        int actualSeconds = Matches.LastTurnDurationSeconds;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedSeconds, actualSeconds);
    }
}
