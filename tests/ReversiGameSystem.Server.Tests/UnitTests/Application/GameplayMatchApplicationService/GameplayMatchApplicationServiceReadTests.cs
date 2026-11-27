using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Matches;
using ApplicationService =
    ReversiGameSystem.Server.Application.Matches.GameplayMatchApplicationService;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.GameplayMatchApplicationService;

[TestClass]
[TestCategory("Unit")]
public sealed class GameplayMatchApplicationServiceReadTests
    : GameplayMatchApplicationServiceTestBase
{
    [TestMethod]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(
            null!,
            Matches,
            Clock,
            NullLogger<ApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullMatches_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(
            Sessions,
            null!,
            Clock,
            NullLogger<ApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(
            Sessions,
            Matches,
            null!,
            NullLogger<ApplicationService>.Instance);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action action = () => _ = new ApplicationService(
            Sessions,
            Matches,
            Clock,
            null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [TestMethod]
    [DataRow(null, MatchStatus.MissingSessionToken)]
    [DataRow("invalid-token", MatchStatus.InvalidSession)]
    public void GetMatchState_RejectedSession_ReturnsFailureWithoutRepositoryCall(
        string? sessionToken,
        MatchStatus expectedStatus)
    {
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.GetMatchState(sessionToken!, 100L);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.LoadMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-5L)]
    public void GetMatchState_NonPositiveMatchId_ReturnsInvalidMatchIdWithoutRepositoryCall(
        long matchId)
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        MatchStatus expectedStatus = MatchStatus.InvalidMatchId;
        int expectedCallCount = 0;

        MatchResult<Match> actualResult = Service.GetMatchState(token, matchId);
        MatchStatus actualStatus = actualResult.Status;
        int actualCallCount = Matches.LoadMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedCallCount, actualCallCount);
    }

    [TestMethod]
    public void GetMatchState_FinishedMatchForNonParticipant_ReturnsUnauthorized()
    {
        string token = AddAuthenticatedSession(99L, "Outsider");
        Match finished = CreateTwoPlayerMatch(100L);
        finished.State = "Finished";
        Matches.LoadMatchResult = new MatchResult<Match>(finished);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;

        MatchResult<Match> actualResult = Service.GetMatchState(token, 100L);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void GetMatchState_TrackedInProgressMatch_ReturnsSnapshotWithoutRepositoryLoad()
    {
        string token = AddAuthenticatedSession(10L, "PlayerOne");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Success;
        int expectedLoadCalls = 0;

        MatchResult<Match> actualResult = Service.GetMatchState(token, 100L);
        MatchStatus actualStatus = actualResult.Status;
        int actualLoadCalls = Matches.LoadMatchCalls;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNotNull(actualResult.Value);
        Assert.AreEqual(expectedLoadCalls, actualLoadCalls);
    }

    [TestMethod]
    public void ObserveMatch_InactiveAccount_ReturnsLobbyAccessFailure()
    {
        string token = AddAuthenticatedSession(30L, "Spectator");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.InactiveAccount;
        Matches.LobbyAccessStatus = expectedStatus;

        MatchResult<Match> actualResult = Service.ObserveMatch(token, 100L);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ObserveMatch_FinishedMatch_ReturnsSpectatingUnavailable()
    {
        string token = AddAuthenticatedSession(30L, "Spectator");
        Match finished = CreateTwoPlayerMatch(100L);
        finished.State = "Finished";
        Matches.LoadMatchResult = new MatchResult<Match>(finished);
        MatchStatus expectedStatus = MatchStatus.SpectatingUnavailable;

        MatchResult<Match> actualResult = Service.ObserveMatch(token, 100L);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void ObserveMatch_InProgressMatch_GrantsMoveHistoryAccessAfterFinish()
    {
        string playerOneToken = AddAuthenticatedSession(10L, "PlayerOne");
        string spectatorToken = AddAuthenticatedSession(30L, "Spectator");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        Service.ObserveMatch(spectatorToken, 100L);
        Service.ForfeitMatch(playerOneToken, 100L);
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<MatchMove[]> actualResult = Service.GetMoveHistory(spectatorToken, 100L);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void GetObservableMatches_CombinesPersistedAndTrackedInProgressMatchesOrderedById()
    {
        string token = AddAuthenticatedSession(30L, "Spectator");
        Match persistedFinished = CreateTwoPlayerMatch(50L);
        persistedFinished.State = "Finished";
        Match persistedActive = CreateTwoPlayerMatch(200L);
        persistedActive.State = "InProgress";
        Matches.ObservableMatchesResult = new MatchResult<Match[]>(
            new Match[] { persistedFinished, persistedActive });
        Match trackedActive = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(trackedActive);
        int expectedCount = 2;
        long expectedFirstId = 100L;
        long expectedSecondId = 200L;

        MatchResult<Match[]> actualResult = Service.GetObservableMatches(token);
        Match[]? actualMatches = actualResult.Value;

        Assert.IsNotNull(actualMatches);
        int actualCount = actualMatches.Length;
        long actualFirstId = actualMatches[0].Id;
        long actualSecondId = actualMatches[1].Id;
        Assert.AreEqual(expectedCount, actualCount);
        Assert.AreEqual(expectedFirstId, actualFirstId);
        Assert.AreEqual(expectedSecondId, actualSecondId);
    }

    [TestMethod]
    public void GetMatchHistory_ValidSession_ReturnsRepositoryHistory()
    {
        long expectedAccountId = 10L;
        string token = AddAuthenticatedSession(expectedAccountId, "PlayerOne");
        Match[] expectedHistory = new Match[] { CreateTwoPlayerMatch(100L) };
        Matches.MatchHistoryResult = new MatchResult<Match[]>(expectedHistory);
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<Match[]> actualResult = Service.GetMatchHistory(token);
        MatchStatus actualStatus = actualResult.Status;
        Match[]? actualHistory = actualResult.Value;
        long actualAccountId = Matches.LastAccountId;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreSame(expectedHistory, actualHistory);
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }

    [TestMethod]
    public void GetSessionAccountId_ValidSession_ReturnsAccountId()
    {
        long expectedAccountId = 10L;
        string token = AddAuthenticatedSession(expectedAccountId, "PlayerOne");
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<long> actualResult = Service.GetSessionAccountId(token);
        MatchStatus actualStatus = actualResult.Status;
        long actualAccountId = actualResult.Value;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }

    [TestMethod]
    public void GetAuthorizedParticipant_NonParticipant_ReturnsUnauthorized()
    {
        string token = AddAuthenticatedSession(99L, "Outsider");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Unauthorized;

        MatchResult<MatchParticipant> actualResult =
            Service.GetAuthorizedParticipant(token, 100L);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
    }

    [TestMethod]
    public void GetAuthorizedParticipant_ValidParticipant_ReturnsParticipant()
    {
        long expectedAccountId = 20L;
        string token = AddAuthenticatedSession(expectedAccountId, "PlayerTwo");
        Match match = CreateTwoPlayerMatch(100L);
        Service.TrackStartedMatch(match);
        MatchStatus expectedStatus = MatchStatus.Success;

        MatchResult<MatchParticipant> actualResult =
            Service.GetAuthorizedParticipant(token, 100L);
        MatchStatus actualStatus = actualResult.Status;

        Assert.AreEqual(expectedStatus, actualStatus);
        Assert.IsNotNull(actualResult.Value);
        long actualAccountId = actualResult.Value.AccountId;
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }
}
